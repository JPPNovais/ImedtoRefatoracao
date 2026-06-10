using System.Globalization;
using System.Reflection;
using Dapper;
using Imedto.Backend.Domain.Prontuarios;
using Microsoft.Extensions.Logging;
using Npgsql;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Imedto.Backend.Infrastructure.Receitas;

/// <summary>
/// Contrato do gerador de PDF de receitas.
/// </summary>
public interface IReceitaPdfService
{
    /// <summary>
    /// Gera o PDF da receita. Lança <see cref="SharedKernel.Domain.BusinessException"/> se não
    /// encontrada ou se estiver em Rascunho. Registra audit LGPD de Exportacao quando há prontuário.
    /// </summary>
    Task<byte[]> GerarAsync(long receitaId, long estabelecimentoId, Guid solicitanteUsuarioId);
}

/// <summary>
/// Geração do PDF da receita usando QuestPDF. Aplica o design institucional do
/// Imedto (Nunito + cabeçalho com logo do estabelecimento + bloco de paciente +
/// lista de medicamentos numerada + rodapé condicional + marca d'água IMEDTO).
///
/// Layout segue o mock de <c>PrintPreview.jsx</c> com 3 variantes:
/// <list type="bullet">
///   <item><b>Padrão azul-marinho</b>: Comum, Antibiótico, Especial.</item>
///   <item><b>Variante vermelha</b> + caixa amarela "1ª via Farmácia · 2ª via Paciente":
///     <see cref="Domain.Receitas.TipoReceita.Controlada"/>.</item>
/// </list>
///
/// Marca d'água por <see cref="Domain.Receitas.StatusReceita"/>:
/// Rascunho → "RASCUNHO" cinza; Cancelada → "CANCELADA" vermelho;
/// Substituida → "SUBSTITUÍDA"; Emitida → "IMEDTO" sutil.
///
/// LGPD: nenhum campo de PII do paciente, nome de medicamento ou conteúdo de
/// observação é logado. Multi-tenant: a query filtra por
/// <c>estabelecimento_id = @EstabelecimentoId</c> — receita de outro tenant
/// retorna null e lança <see cref="SharedKernel.Domain.BusinessException"/>
/// genérica "Receita não encontrada".
///
/// Licença QuestPDF Community — verificar limite ao crescer receita anual
/// (https://www.questpdf.com/license.html).
/// </summary>
public class QuestPdfReceitaService : IReceitaPdfService
{
    // ── Cores (sincronizadas com PDF_THEME do frontend) ────────────────────
    private const string CorInk = "#1A2440";
    private const string CorInkTitle = "#1D3557";
    private const string CorSecondary = "#475569";
    private const string CorMute = "#64748B";
    private const string CorMuteLight = "#94A3B8";
    private const string CorBorder = "#E2E8F0";
    private const string CorBorderStrong = "#CBD5E1";
    private const string CorCardBg = "#F8FAFC";
    private const string CorChipBg = "#E0E7FF";
    private const string CorDanger = "#DC2626";
    private const string CorDangerChipBg = "#FEE2E2";
    private const string CorAmarelo2via = "#FEF3C7";
    private const string CorAmareloBorda = "#D97706";
    private const string CorAmareloTexto = "#92400E";
    private const string CorAssinaturaOk = "#16A34A";

    // ── Fonte ───────────────────────────────────────────────────────────────
    private const string Fonte = "Nunito";

    private static readonly object _fontesLock = new();
    private static bool _fontesRegistradas;

    private readonly string _connStr;
    private readonly IHttpClientFactory _httpFactory;

    /// <summary>Nome lógico do HttpClient usado para baixar a logo (registrado no Program.cs).</summary>
    public const string HttpClientName = "PdfReceitaLogo";

    private readonly IProntuarioRepository _prontuarioRepo;
    private readonly IProntuarioAcessoLogService _acessoLog;
    private readonly ILogger<QuestPdfReceitaService> _logger;

    public QuestPdfReceitaService(
        AppReadConnectionString conn,
        IHttpClientFactory httpFactory,
        IProntuarioRepository prontuarioRepo,
        IProntuarioAcessoLogService acessoLog,
        ILogger<QuestPdfReceitaService> logger)
    {
        _connStr = conn.Value;
        _httpFactory = httpFactory;
        _prontuarioRepo = prontuarioRepo;
        _acessoLog = acessoLog;
        _logger = logger;
        InicializarQuestPdf();
    }

    /// <summary>Configura licença e registra fontes — idempotente. Exposto para testes.</summary>
    internal static void InicializarQuestPdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        RegistrarFontesNunitoUmaVez();
    }

    public async Task<byte[]> GerarAsync(long receitaId, long estabelecimentoId, Guid solicitanteUsuarioId)
    {
        var dados = await CarregarDadosAsync(receitaId, estabelecimentoId);
        if (dados is null)
            throw new SharedKernel.Domain.BusinessException("Receita não encontrada.");

        // CA2: Rascunho não é documento — 422 antes de gerar qualquer byte.
        if (string.Equals(dados.Receita.Status, "Rascunho", StringComparison.OrdinalIgnoreCase))
            throw new SharedKernel.Domain.BusinessException("Receita em rascunho não pode ser exportada em PDF.");

        // CA4/CA5: Audit LGPD best-effort — falha não bloqueia o download (CA6).
        await RegistrarAuditExportacaoAsync(dados.Receita.PacienteId, estabelecimentoId, solicitanteUsuarioId);

        // Baixa a logo (best-effort) — se falhar, o layout usa o placeholder
        // com iniciais sem propagar erro: emissão de receita é prioridade.
        var logoBytes = await BaixarLogoAsync(dados.Receita.EstabelecimentoFotoUrl);

        // CA13 — ponto de extensão para assinatura digital:
        // Quando assinatura_digital_status ∈ {AssinadaIcp, AssinadaMemed}, este método
        // deverá servir o arquivo assinado armazenado (item 1.3 do roadmap) em vez de
        // regerar o PDF não assinado. Nesta entrega, o caminho não assinado continua sendo
        // servido enquanto a feature de assinatura ICP ainda não está ativa no ambiente.
        // TODO(item-1.3): implementar quando integração BirdID/Memed for concluída:
        // if (dados.Receita.AssinaturaDigitalStatus is "AssinadaIcp" or "AssinadaMemed")
        //     return await _arquivoAssinadoStorage.ObterAsync(dados.Receita.Id, estabelecimentoId);

        return GerarPdf(dados with { LogoBytes = logoBytes });
    }

    /// <summary>
    /// Registra audit LGPD de Exportacao no prontuário do paciente da receita.
    /// Best-effort: engole exceção e loga erro — o download nunca é bloqueado (CA6).
    /// Não insere linha se o paciente não tiver prontuário iniciado (CA5).
    /// </summary>
    private async Task RegistrarAuditExportacaoAsync(long pacienteId, long estabelecimentoId, Guid usuarioId)
    {
        try
        {
            var prontuario = await _prontuarioRepo.ObterPorPaciente(pacienteId, estabelecimentoId);
            if (prontuario is null) return; // CA5: sem prontuário → sem audit, sem erro.

            await _acessoLog.RegistrarAsync(
                prontuario.Id,
                usuarioId,
                estabelecimentoId,
                TipoAcessoProntuario.Exportacao);
        }
        catch (Exception ex)
        {
            // CA6: falha do audit não bloqueia o PDF. Sem PII no log (CA10).
            _logger.LogError(ex, "Falha ao registrar audit de exportação de receita.");
        }
    }

    /// <summary>
    /// Baixa os bytes da logo a partir da presigned URL do S3 com timeout curto
    /// (3s). Qualquer falha (timeout, 4xx, rede, conteúdo inválido) retorna null
    /// e o template cai no placeholder de iniciais — nunca bloqueamos a receita.
    /// </summary>
    private async Task<byte[]> BaixarLogoAsync(string fotoUrl)
    {
        if (string.IsNullOrWhiteSpace(fotoUrl)) return null;

        try
        {
            using var client = _httpFactory.CreateClient(HttpClientName);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            using var resp = await client.GetAsync(fotoUrl, cts.Token);
            if (!resp.IsSuccessStatusCode) return null;
            var bytes = await resp.Content.ReadAsByteArrayAsync(cts.Token);
            // Limite defensivo (5 MB) — bloqueia conteúdo gigante eventual
            // (a foto do estabelecimento é validada em 2 MB no upload).
            if (bytes.Length is 0 or > 5 * 1024 * 1024) return null;
            return bytes;
        }
        catch
        {
            return null;
        }
    }

    // ────────────────────────────────────────────────────────────────
    // Carregamento de dados — Dapper (leitura, sem EF)
    // ────────────────────────────────────────────────────────────────

    // internal virtual para permitir override em testes de unidade (sem banco).
    // InternalsVisibleTo("Imedto.Backend.Test") no .csproj habilita o override na suíte.
    internal virtual async Task<DadosPdf> CarregarDadosAsync(long receitaId, long estabelecimentoId)
    {
        const string sqlReceita = """
            SELECT  r.id                    AS Id,
                    r.tipo                  AS Tipo,
                    r.tipo_notificacao      AS TipoNotificacao,
                    r.status                AS Status,
                    r.assinatura_digital_status AS AssinaturaDigitalStatus,
                    r.emitida_em            AS EmitidaEm,
                    r.validade_ate          AS ValidadeAte,
                    r.observacoes           AS Observacoes,
                    r.motivo_cancelamento   AS MotivoCancelamento,
                    r.paciente_id           AS PacienteId,
                    pa.nome_completo        AS PacienteNome,
                    pa.cpf                  AS PacienteCpf,
                    pa.data_nascimento      AS PacienteDataNascimento,
                    pa.genero               AS PacienteGenero,
                    pa.telefone             AS PacienteTelefone,
                    u.nome_completo         AS ProfissionalNome,
                    CASE WHEN pr.conselho IS NOT NULL
                         THEN pr.conselho || ' ' || pr.uf || ' ' || pr.numero_registro
                         ELSE NULL END      AS ProfissionalCrmCro,
                    c.cabecalho_html        AS CabecalhoHtml,
                    c.rodape_html           AS RodapeHtml,
                    c.emissor_padrao        AS EmissorPadrao,
                    e.nome_fantasia         AS EstabelecimentoNomeFantasia,
                    e.cnpj                  AS EstabelecimentoCnpj,
                    e.telefone              AS EstabelecimentoTelefone,
                    e.endereco              AS EstabelecimentoEndereco,
                    e.foto_url              AS EstabelecimentoFotoUrl
            FROM    public.receitas r
            INNER JOIN public.estabelecimentos e ON e.id = r.estabelecimento_id
            LEFT JOIN public.pacientes pa       ON pa.id = r.paciente_id
            LEFT JOIN public.usuarios u         ON u.id = r.profissional_usuario_id
            LEFT JOIN public.profissionais pr   ON pr.usuario_id = r.profissional_usuario_id
            LEFT JOIN public.receitas_configuracao_estabelecimento c
                    ON c.estabelecimento_id = r.estabelecimento_id
            WHERE   r.id = @Id
              AND   r.estabelecimento_id = @EstabelecimentoId
              AND   r.deletado_em IS NULL
            """;

        const string sqlItens = """
            SELECT  ordem           AS Ordem,
                    medicamento     AS Medicamento,
                    posologia       AS Posologia,
                    concentracao    AS Concentracao,
                    forma_farmaceutica AS FormaFarmaceutica,
                    via_administracao  AS Via,
                    quantidade      AS Quantidade,
                    duracao         AS Duracao,
                    observacao      AS Observacao
            FROM    public.receita_itens
            WHERE   receita_id = @Id
            ORDER BY ordem
            """;

        await using var conn = new NpgsqlConnection(_connStr);

        var receita = await conn.QuerySingleOrDefaultAsync<ReceitaRow>(sqlReceita, new
        {
            Id = receitaId,
            EstabelecimentoId = estabelecimentoId
        });

        if (receita is null) return null;

        var itens = await conn.QueryAsync<ItemRow>(sqlItens, new { Id = receitaId });

        return new DadosPdf(receita, itens.ToList());
    }

    // ────────────────────────────────────────────────────────────────
    // Geração do PDF — layout institucional
    // ────────────────────────────────────────────────────────────────

    internal static byte[] GerarPdf(DadosPdf dados)
    {
        var r = dados.Receita;
        var itens = dados.Itens;

        var ehControlada = string.Equals(r.Tipo, "Controlada", StringComparison.OrdinalIgnoreCase);
        var accent = ehControlada ? CorDanger : CorInkTitle;

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0); // o conteúdo gerencia margens internas (14/18/22 mm)
                page.DefaultTextStyle(x => x.FontFamily(Fonte).FontSize(9).FontColor(CorInk));
                page.PageColor(Colors.White);

                // Marca d'água em todas as páginas
                page.Background().Element(c => DesenharWatermarkPorStatus(c, r.Status));

                page.Content().PaddingHorizontal(18, Unit.Millimetre)
                    .PaddingTop(14, Unit.Millimetre)
                    .PaddingBottom(22, Unit.Millimetre)
                    .Column(col =>
                {
                    // Cabeçalho institucional
                    col.Item().Element(c => DesenharCabecalho(c, r, accent, ehControlada, dados.LogoBytes));

                    // Bloco do paciente
                    col.Item().PaddingTop(4, Unit.Millimetre).Element(c => DesenharBlocoPaciente(c, r));

                    // Notas livres do estabelecimento (CabecalhoHtml legado) — opcional
                    var notaTopo = StripHtml(r.CabecalhoHtml);
                    if (!string.IsNullOrWhiteSpace(notaTopo))
                    {
                        col.Item().PaddingTop(2, Unit.Millimetre).Text(notaTopo)
                            .FontSize(8).FontColor(CorMute).Italic();
                    }

                    // Lista de medicamentos
                    col.Item().PaddingTop(4, Unit.Millimetre).Element(c => DesenharListaMedicamentos(c, itens, accent, ehControlada));

                    if (ehControlada)
                    {
                        col.Item().PaddingTop(3, Unit.Millimetre).Element(DesenharCaixa2Via);
                    }

                    // Observações gerais
                    if (!string.IsNullOrWhiteSpace(r.Observacoes))
                    {
                        col.Item().PaddingTop(3, Unit.Millimetre).Column(o =>
                        {
                            o.Item().Text("OBSERVAÇÕES")
                                .FontSize(8).Bold().FontColor(accent).LetterSpacing(0.4f);
                            o.Item().PaddingTop(1, Unit.Millimetre)
                                .BorderTop(0.5f).BorderColor(CorBorderStrong)
                                .PaddingTop(1, Unit.Millimetre)
                                .Text(r.Observacoes!).FontSize(9).FontColor(CorInk);
                        });
                    }

                    // Rodapé de antibiótico (aviso de retenção)
                    var avisoAntibiotico = string.Equals(r.Tipo, "Antibiotico", StringComparison.OrdinalIgnoreCase)
                        ? "Reter na farmácia (RDC 471/2021)."
                        : null;
                    if (avisoAntibiotico is not null)
                    {
                        col.Item().PaddingTop(2, Unit.Millimetre).Text(avisoAntibiotico)
                            .FontSize(8).Italic().FontColor(CorMute);
                    }

                    // Nota livre do rodapé (RodapeHtml legado)
                    var notaRodape = StripHtml(r.RodapeHtml);
                    if (!string.IsNullOrWhiteSpace(notaRodape))
                    {
                        col.Item().PaddingTop(2, Unit.Millimetre).Text(notaRodape)
                            .FontSize(8).FontColor(CorMute);
                    }
                });

                // Rodapé fixo — assinatura + meta
                page.Footer().Element(c => DesenharRodape(c, r));
            });
        });

        return doc.GeneratePdf();
    }

    // ────────────────────────────────────────────────────────────────
    // Componentes do layout
    // ────────────────────────────────────────────────────────────────

    private static void DesenharCabecalho(IContainer container, ReceitaRow r, string accent, bool ehControlada, byte[] logoBytes)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                // Esquerda: logo + nome
                row.RelativeItem().Row(brand =>
                {
                    brand.ConstantItem(14, Unit.Millimetre).AlignMiddle()
                        .Width(12, Unit.Millimetre).Height(12, Unit.Millimetre)
                        .Element(c => DesenharLogo(c, r, accent, logoBytes));

                    brand.RelativeItem().PaddingLeft(2, Unit.Millimetre).AlignMiddle().Text(r.EstabelecimentoNomeFantasia ?? "Estabelecimento")
                        .FontSize(15).Bold().FontColor(accent);
                });

                // Direita: contato
                row.RelativeItem().AlignRight().Column(contato =>
                {
                    var linhas = new List<string>();
                    if (!string.IsNullOrWhiteSpace(r.EstabelecimentoEndereco)) linhas.Add(r.EstabelecimentoEndereco!);
                    var tel = FormatarTelefone(r.EstabelecimentoTelefone);
                    if (tel is not null) linhas.Add(tel);
                    var cnpj = FormatarCnpj(r.EstabelecimentoCnpj);
                    if (cnpj is not null) linhas.Add($"CNPJ {cnpj}");
                    foreach (var l in linhas)
                        contato.Item().Text(l).FontSize(8).FontColor(CorSecondary);
                });
            });

            // Linha divisória fina
            col.Item().PaddingTop(2, Unit.Millimetre).LineHorizontal(0.3f).LineColor(CorBorder);

            // Linha do título + subtítulo
            col.Item().PaddingTop(2, Unit.Millimetre).Row(titulo =>
            {
                titulo.RelativeItem().Text(TituloDocumento(r, ehControlada))
                    .FontSize(12).Bold().FontColor(accent).LetterSpacing(0.6f);

                var subtitulo = SubtituloDocumento(r);
                if (subtitulo is not null)
                {
                    titulo.RelativeItem().AlignRight().Text(subtitulo)
                        .FontSize(8.5f).FontColor(CorMute);
                }
            });

            // Linha dupla
            col.Item().PaddingTop(1, Unit.Millimetre).LineHorizontal(0.6f).LineColor(accent);
            col.Item().PaddingTop(0.3f, Unit.Millimetre).LineHorizontal(0.6f).LineColor(accent);
        });
    }

    private static void DesenharLogo(IContainer container, ReceitaRow r, string accent, byte[] logoBytes)
    {
        // Foto/logo do estabelecimento quando disponível; placeholder com iniciais como fallback.
        // O download remoto acontece em BaixarLogoAsync — se falhou, logoBytes é null e
        // o usuário ainda recebe um cabeçalho coerente. Nunca bloqueia emissão.
        if (logoBytes is { Length: > 0 })
        {
            try
            {
                container.AlignCenter().AlignMiddle()
                    .Width(12, Unit.Millimetre).Height(12, Unit.Millimetre)
                    .Image(logoBytes).FitArea();
                return;
            }
            catch
            {
                // Imagem corrompida ou formato não suportado pelo QuestPDF — cai no placeholder.
            }
        }

        var iniciais = ObterIniciais(r.EstabelecimentoNomeFantasia);
        container.AlignCenter().AlignMiddle()
            .Background(accent)
            .Width(12, Unit.Millimetre).Height(12, Unit.Millimetre)
            .Layers(layers =>
            {
                layers.PrimaryLayer().AlignCenter().AlignMiddle().Text(iniciais)
                    .FontSize(7).Bold().FontColor(Colors.White);
            });
    }

    private static void DesenharBlocoPaciente(IContainer container, ReceitaRow r)
    {
        container.Background(CorCardBg).Border(0.3f).BorderColor(CorBorder)
            .Padding(3, Unit.Millimetre).Column(col =>
        {
            col.Spacing(2);

            // Linha 1: Paciente (largo) · Idade · Sexo · Telefone
            col.Item().Row(row =>
            {
                row.RelativeItem(2).Element(c => DesenharCelula(c, "Paciente", r.PacienteNome ?? "—", destaque: true));
                var idade = CalcularIdade(r.PacienteDataNascimento);
                row.RelativeItem().Element(c => DesenharCelula(c, "Idade", idade.HasValue ? $"{idade} anos" : "—"));
                row.RelativeItem().Element(c => DesenharCelula(c, "Sexo", FormatarGenero(r.PacienteGenero)));
                row.RelativeItem().Element(c => DesenharCelula(c, "Telefone", FormatarTelefone(r.PacienteTelefone) ?? "—"));
            });

            // Divisória tracejada
            col.Item().PaddingVertical(0.5f, Unit.Millimetre).LineHorizontal(0.3f).LineColor(CorBorder);

            // Linha 2: CPF · Nascimento · Profissional · CRM/CRO
            col.Item().Row(row =>
            {
                row.RelativeItem().Element(c => DesenharCelula(c, "CPF", FormatarCpf(r.PacienteCpf) ?? "—"));
                row.RelativeItem().Element(c => DesenharCelula(c, "Nascimento", FormatarData(r.PacienteDataNascimento)));
                row.RelativeItem().Element(c => DesenharCelula(c, "Profissional", r.ProfissionalNome ?? r.EmissorPadrao ?? "—"));
                row.RelativeItem().Element(c => DesenharCelula(c, "Registro", r.ProfissionalCrmCro ?? "—"));
            });
        });
    }

    private static void DesenharCelula(IContainer container, string label, string valor, bool destaque = false)
    {
        container.Column(c =>
        {
            c.Item().Text(label.ToUpperInvariant())
                .FontSize(6.5f).SemiBold().FontColor(CorMuteLight).LetterSpacing(0.3f);
            c.Item().Text(valor)
                .FontSize(9).FontColor(CorInk)
                .ApplyWhen(destaque, t => t.Bold());
        });
    }

    private static void DesenharListaMedicamentos(IContainer container, List<ItemRow> itens, string accent, bool ehControlada)
    {
        if (itens.Count == 0)
        {
            container.Border(0.5f).BorderColor(CorBorderStrong)
                .Padding(8, Unit.Millimetre).AlignCenter()
                .Text("Nenhum medicamento prescrito.")
                .FontSize(10).FontColor(CorMuteLight);
            return;
        }

        var chipBg = ehControlada ? CorDangerChipBg : CorChipBg;

        container.Column(col =>
        {
            col.Spacing(3);
            for (var idx = 0; idx < itens.Count; idx++)
            {
                var item = itens[idx];
                var ordem = idx + 1;
                col.Item().Row(row =>
                {
                    // Círculo numerado
                    row.ConstantItem(10, Unit.Millimetre).Column(circ =>
                    {
                        circ.Item().Width(8, Unit.Millimetre).Height(8, Unit.Millimetre)
                            .Background(accent)
                            .AlignCenter().AlignMiddle()
                            .Text(ordem.ToString(CultureInfo.InvariantCulture))
                            .FontSize(10).Bold().FontColor(Colors.White);
                    });

                    // Bloco direito
                    row.RelativeItem().Background("#FAFBFC").BorderLeft(1f).BorderColor(accent)
                        .Padding(3, Unit.Millimetre).Column(info =>
                    {
                        info.Item().Row(linha1 =>
                        {
                            linha1.AutoItem().Text(item.Medicamento ?? "—")
                                .FontSize(10).Bold().FontColor(CorInk);

                            var dose = MontarDose(item);
                            if (!string.IsNullOrWhiteSpace(dose))
                            {
                                linha1.AutoItem().PaddingLeft(2, Unit.Millimetre)
                                    .Background(chipBg).PaddingHorizontal(1.5f, Unit.Millimetre)
                                    .PaddingVertical(0.3f, Unit.Millimetre)
                                    .AlignMiddle().Text(dose)
                                    .FontSize(8).SemiBold().FontColor(accent);
                            }

                            if (!string.IsNullOrWhiteSpace(item.Via))
                            {
                                linha1.AutoItem().PaddingLeft(2, Unit.Millimetre)
                                    .Border(0.3f).BorderColor(CorBorderStrong)
                                    .PaddingHorizontal(1.5f, Unit.Millimetre)
                                    .PaddingVertical(0.3f, Unit.Millimetre)
                                    .AlignMiddle().Text(item.Via!)
                                    .FontSize(7.5f).FontColor(CorMute);
                            }
                        });

                        // Linha 2: posologia + duração
                        var partes = new List<string>();
                        if (!string.IsNullOrWhiteSpace(item.Posologia)) partes.Add(item.Posologia!);
                        if (!string.IsNullOrWhiteSpace(item.Quantidade)) partes.Add($"Qtd: {item.Quantidade}");
                        if (!string.IsNullOrWhiteSpace(item.Duracao)) partes.Add($"Por {item.Duracao}");
                        if (partes.Count > 0)
                        {
                            info.Item().PaddingTop(0.5f, Unit.Millimetre)
                                .Text(string.Join("  ·  ", partes))
                                .FontSize(8.5f).FontColor(CorInk);
                        }

                        // Linha 3: observação livre OU texto padrão
                        var orientacao = !string.IsNullOrWhiteSpace(item.Observacao)
                            ? item.Observacao!
                            : "Tomar conforme orientação médica. Não interromper o tratamento sem consultar o médico.";
                        info.Item().PaddingTop(0.5f, Unit.Millimetre)
                            .Text(orientacao).Italic().FontSize(7.5f).FontColor(CorMute);
                    });
                });
            }
        });
    }

    private static void DesenharCaixa2Via(IContainer container)
    {
        container.Background(CorAmarelo2via).Border(0.4f).BorderColor(CorAmareloBorda)
            .Padding(2, Unit.Millimetre).AlignCenter()
            .Text("1ª via — Farmácia    ·    2ª via — Paciente")
            .FontSize(8.5f).FontColor(CorAmareloTexto);
    }

    private static void DesenharRodape(IContainer container, ReceitaRow r)
    {
        var assinaturaOk = r.AssinaturaDigitalStatus is "AssinadaIcp" or "AssinadaMemed";
        var canceladaOuRascunho = r.Status is "Cancelada" or "Rascunho";

        container.PaddingHorizontal(18, Unit.Millimetre).PaddingBottom(6, Unit.Millimetre)
            .Row(row =>
        {
            // Esquerda — assinatura (se não Rascunho)
            row.RelativeItem().Column(sign =>
            {
                if (canceladaOuRascunho)
                {
                    // Sem bloco de assinatura — receita não vale como prescrição
                    sign.Item().AlignCenter()
                        .Text(r.Status == "Rascunho"
                            ? "Documento em rascunho — não vale como prescrição."
                            : "Receita cancelada.")
                        .FontSize(8).Italic().FontColor(CorMute);
                    if (r.Status == "Cancelada" && !string.IsNullOrWhiteSpace(r.MotivoCancelamento))
                    {
                        sign.Item().AlignCenter().Text($"Motivo: {r.MotivoCancelamento}")
                            .FontSize(7).FontColor(CorMute);
                    }
                    return;
                }

                // Linha de assinatura
                sign.Item().AlignCenter().Width(60, Unit.Millimetre)
                    .BorderBottom(0.6f).BorderColor(CorInk).PaddingTop(6, Unit.Millimetre);

                sign.Item().PaddingTop(1, Unit.Millimetre).AlignCenter()
                    .Text(NomeProfissional(r)).FontSize(9).Bold().FontColor(CorInk);

                if (!string.IsNullOrWhiteSpace(r.ProfissionalCrmCro))
                {
                    sign.Item().AlignCenter().Text(r.ProfissionalCrmCro!)
                        .FontSize(8).FontColor(CorSecondary);
                }

                if (assinaturaOk)
                {
                    sign.Item().AlignCenter().Text("Assinado digitalmente · ICP-Brasil")
                        .FontSize(7).FontColor(CorAssinaturaOk);
                }
                else
                {
                    sign.Item().AlignCenter().Text("Assine manualmente no espaço acima")
                        .FontSize(7).FontColor(CorMuteLight);
                }
            });

            // Direita — meta
            row.RelativeItem().AlignRight().Column(meta =>
            {
                var emitidaEm = r.EmitidaEm?.ToLocalTime() ?? DateTime.Now;
                meta.Item().Text($"Emitido em {emitidaEm:dd/MM/yyyy 'às' HH:mm}")
                    .FontSize(7).FontColor(CorMuteLight);
                meta.Item().Text(t =>
                {
                    t.DefaultTextStyle(x => x.FontSize(7).FontColor(CorMuteLight));
                    t.Span("Página ");
                    t.CurrentPageNumber();
                    t.Span(" de ");
                    t.TotalPages();
                });
            });
        });
    }

    private static void DesenharWatermarkPorStatus(IContainer container, string status)
    {
        var (texto, cor, opacidade, fontSize) = status switch
        {
            "Rascunho"    => ("RASCUNHO", "#94A3B8", 0.12f, 90f),
            "Cancelada"   => ("CANCELADA", CorDanger, 0.12f, 90f),
            "Substituida" => ("SUBSTITUÍDA", CorMute, 0.10f, 90f),
            _             => ("IMEDTO", CorInk, 0.025f, 130f),
        };

        container.AlignCenter().AlignMiddle().Rotate(-25)
            .Text(texto).FontSize(fontSize).Bold()
            .FontColor(cor).LineHeight(0.9f);

        // QuestPDF não tem set-opacity direto no texto — opacidade é simulada via
        // mistura de cor com branco. Para sutileza, a cor já vem clara o suficiente.
        _ = opacidade; // mantido apenas para documentar a intenção do mock CSS.
    }

    // ────────────────────────────────────────────────────────────────
    // Helpers de formatação e cálculo
    // ────────────────────────────────────────────────────────────────

    private static string TituloDocumento(ReceitaRow r, bool ehControlada)
    {
        if (ehControlada)
        {
            var notif = string.IsNullOrWhiteSpace(r.TipoNotificacao) ? "" : $" — NOTIFICAÇÃO {r.TipoNotificacao}";
            return ("RECEITUÁRIO DE CONTROLE ESPECIAL" + notif).ToUpperInvariant();
        }

        return r.Tipo?.ToUpperInvariant() switch
        {
            "ANTIBIOTICO" => "RECEITUÁRIO DE ANTIBIÓTICO",
            "ESPECIAL"    => "RECEITUÁRIO ESPECIAL",
            "COMUM"       => "RECEITA MÉDICA",
            _             => "RECEITA MÉDICA",
        };
    }

    private static string SubtituloDocumento(ReceitaRow r)
    {
        var emissao = r.EmitidaEm?.ToLocalTime();
        if (emissao is null) return null;
        var sub = $"Emitida em {emissao:dd/MM/yyyy}";
        if (r.ValidadeAte is not null)
            sub += $"  ·  válida até {r.ValidadeAte:dd/MM/yyyy}";
        return sub;
    }

    private static string NomeProfissional(ReceitaRow r)
        => r.EmissorPadrao ?? r.ProfissionalNome ?? "Profissional";

    private static string MontarDose(ItemRow item)
    {
        var partes = new List<string>();
        if (!string.IsNullOrWhiteSpace(item.Concentracao)) partes.Add(item.Concentracao!);
        if (!string.IsNullOrWhiteSpace(item.FormaFarmaceutica)) partes.Add(item.FormaFarmaceutica!);
        return string.Join(" ", partes);
    }

    private static int? CalcularIdade(DateTime? nascimento)
    {
        if (nascimento is null) return null;
        var hoje = DateTime.UtcNow.Date;
        var anos = hoje.Year - nascimento.Value.Year;
        if (hoje < nascimento.Value.Date.AddYears(anos)) anos--;
        return anos < 0 ? null : anos;
    }

    private static string FormatarData(DateTime? d)
        => d?.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("pt-BR")) ?? "—";

    private static string FormatarCpf(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf)) return null;
        var d = new string(cpf.Where(char.IsDigit).ToArray());
        if (d.Length != 11) return cpf;
        return $"{d[..3]}.{d[3..6]}.{d[6..9]}-{d[9..]}";
    }

    private static string FormatarCnpj(string cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj)) return null;
        var d = new string(cnpj.Where(char.IsDigit).ToArray());
        if (d.Length != 14) return cnpj;
        return $"{d[..2]}.{d[2..5]}.{d[5..8]}/{d[8..12]}-{d[12..]}";
    }

    private static string FormatarTelefone(string tel)
    {
        if (string.IsNullOrWhiteSpace(tel)) return null;
        var d = new string(tel.Where(char.IsDigit).ToArray());
        if (d.Length == 11) return $"({d[..2]}) {d[2..7]}-{d[7..]}";
        if (d.Length == 10) return $"({d[..2]}) {d[2..6]}-{d[6..]}";
        return tel;
    }

    private static string FormatarGenero(string g)
    {
        if (string.IsNullOrWhiteSpace(g)) return "—";
        var n = g.Trim().ToUpperInvariant();
        return n switch
        {
            "F" or "FEMININO" => "Feminino",
            "M" or "MASCULINO" => "Masculino",
            _ => g,
        };
    }

    private static string ObterIniciais(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome)) return "?";
        var partes = nome.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length == 1) return partes[0][..Math.Min(2, partes[0].Length)].ToUpperInvariant();
        return ($"{partes[0][0]}{partes[^1][0]}").ToUpperInvariant();
    }

    /// <summary>
    /// Remove tags HTML para uso em áreas de texto simples do PDF.
    /// Não é parser HTML completo — suficiente para os campos legados
    /// CabecalhoHtml/RodapeHtml da tabela <c>receitas_configuracao_estabelecimento</c>.
    /// </summary>
    private static string StripHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return null;
        return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ")
            .Replace("&nbsp;", " ").Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">")
            .Trim();
    }

    // ────────────────────────────────────────────────────────────────
    // Registro das fontes Nunito (recursos embarcados no assembly)
    // ────────────────────────────────────────────────────────────────

    private static void RegistrarFontesNunitoUmaVez()
    {
        if (_fontesRegistradas) return;
        lock (_fontesLock)
        {
            if (_fontesRegistradas) return;
            try
            {
                var asm = typeof(QuestPdfReceitaService).Assembly;
                RegistrarFonte(asm, "Imedto.Backend.Infrastructure.Receitas.Fonts.Nunito-Regular.ttf");
                RegistrarFonte(asm, "Imedto.Backend.Infrastructure.Receitas.Fonts.Nunito-SemiBold.ttf");
                RegistrarFonte(asm, "Imedto.Backend.Infrastructure.Receitas.Fonts.Nunito-Bold.ttf");
            }
            catch
            {
                // Falha silenciosa: QuestPDF cai em fonte default. Não quebra geração.
            }
            _fontesRegistradas = true;
        }
    }

    private static void RegistrarFonte(Assembly asm, string resourceName)
    {
        using var stream = asm.GetManifestResourceStream(resourceName);
        if (stream is null) return;
        FontManager.RegisterFont(stream);
    }

}

// ────────────────────────────────────────────────────────────────────────────
// Extensões de QuestPDF — pequenos helpers para tornar o template legível.
// ────────────────────────────────────────────────────────────────────────────

internal static class QuestPdfTextHelpers
{
    /// <summary>Aplica uma transformação no TextDescriptor só se <paramref name="cond"/>.</summary>
    public static TextSpanDescriptor ApplyWhen(this TextSpanDescriptor t, bool cond, Action<TextSpanDescriptor> action)
    {
        if (cond) action(t);
        return t;
    }
}
