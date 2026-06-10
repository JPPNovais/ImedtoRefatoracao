using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Dapper;
using Imedto.Backend.Domain.Termos;
using Microsoft.Extensions.Logging;
using Npgsql;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Imedto.Backend.Infrastructure.Termos;

/// <summary>
/// Contrato do gerador de PDF probatório de termos emitidos.
/// </summary>
public interface ITermoPdfGeradoService
{
    /// <summary>
    /// Gera o PDF probatório do termo. Lança <see cref="SharedKernel.Domain.BusinessException"/> se
    /// não encontrado no tenant. Registra audit LGPD (best-effort) com ação "termo-pdf-gerado".
    /// </summary>
    Task<byte[]> GerarAsync(long termoId, long estabelecimentoId, Guid solicitanteUsuarioId);
}

/// <summary>
/// Geração do PDF probatório do termo de consentimento via QuestPDF.
///
/// Conteúdo: cabeçalho institucional + bloco do paciente + metadados curtos +
/// snapshot HTML como texto rico + bloco de evidência do aceite + marca d'água
/// por status + rodapé.
///
/// Marca d'água por status:
///   Assinado  → "IMEDTO" sutil.
///   Revogado  → "REVOGADO" diagonal vermelho.
///   Pendente  → "AGUARDANDO ASSINATURA".
///   Recusado/Expirado → "IMEDTO" sutil (sem assinatura ativa).
///
/// LGPD:
///   - Token de aceite nunca exposto completo — somente os últimos 6 caracteres.
///   - Sem PII no nome do arquivo (<c>termo-{id}.pdf</c>).
///   - Audit best-effort em <c>termo_audit_log</c> via <see cref="ITermoAuditLogger"/>.
///
/// Multi-tenant: a query filtra <c>estabelecimento_id</c>; ausência → BusinessException genérica.
///
/// Identidade visual: mesmas constantes de cor (PDF_THEME), fonte Nunito e estrutura
/// de cabeçalho/bloco-paciente do <c>QuestPdfReceitaService</c>.
/// </summary>
public class QuestPdfTermoService : ITermoPdfGeradoService
{
    // ── Cores (sincronizadas com PDF_THEME do frontend / QuestPdfReceitaService) ─
    private const string CorInk = "#1A2440";
    private const string CorInkTitle = "#1D3557";
    private const string CorSecondary = "#475569";
    private const string CorMute = "#64748B";
    private const string CorMuteLight = "#94A3B8";
    private const string CorBorder = "#E2E8F0";
    private const string CorBorderStrong = "#CBD5E1";
    private const string CorCardBg = "#F8FAFC";
    private const string CorDanger = "#DC2626";
    private const string CorEvidenciaBg = "#F0F4F8";
    private const string CorEvidenciaBorda = "#CBD5E1";
    private const string CorRevogadoBg = "#FEE2E2";
    private const string CorRevogadoBorda = "#FCA5A5";
    private const string CorRevogadoTexto = "#991B1B";

    private const string Fonte = "Nunito";

    // Compartilha o lock/flag de registro com QuestPdfReceitaService — ambos
    // registram as mesmas fontes do mesmo assembly; o flag de QuestPdfReceitaService
    // já protege contra duplo-registro. Aqui chamamos o mesmo método estático.
    private static readonly object _fontesLock = new();
    private static bool _fontesRegistradas;

    private readonly string _connStr;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ITermoAuditLogger _audit;
    private readonly ILogger<QuestPdfTermoService> _logger;

    /// <summary>Nome lógico do HttpClient usado para baixar a logo (registrado no Program.cs).</summary>
    public const string HttpClientName = "PdfTermoLogo";

    public QuestPdfTermoService(
        AppReadConnectionString conn,
        IHttpClientFactory httpFactory,
        ITermoAuditLogger audit,
        ILogger<QuestPdfTermoService> logger)
    {
        _connStr = conn.Value;
        _httpFactory = httpFactory;
        _audit = audit;
        _logger = logger;
        InicializarQuestPdf();
    }

    /// <summary>Configura licença e registra fontes — idempotente. Exposto para testes.</summary>
    internal static void InicializarQuestPdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        RegistrarFontesNunitoUmaVez();
    }

    public async Task<byte[]> GerarAsync(long termoId, long estabelecimentoId, Guid solicitanteUsuarioId)
    {
        var dados = await CarregarDadosAsync(termoId, estabelecimentoId);
        if (dados is null)
            throw new SharedKernel.Domain.BusinessException("Termo não encontrado.");

        // CA7/CA8: Audit LGPD best-effort — falha não bloqueia o PDF.
        await RegistrarAuditAsync(estabelecimentoId, solicitanteUsuarioId, dados.Termo.Id);

        var logoBytes = await BaixarLogoAsync(dados.Termo.EstabelecimentoFotoUrl);

        return GerarPdf(dados with { LogoBytes = logoBytes });
    }

    /// <summary>
    /// Registra audit de acesso ao PDF gerado. Best-effort: engole exceção — o download
    /// nunca é bloqueado (CA8). Sem PII no log (CA11).
    /// </summary>
    private async Task RegistrarAuditAsync(long estabelecimentoId, Guid usuarioId, long termoId)
    {
        try
        {
            await _audit.RegistrarAsync(
                estabelecimentoId, usuarioId,
                "termo-pdf-gerado", "TermoEmitido", termoId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao registrar audit de geração de PDF de termo.");
        }
    }

    /// <summary>
    /// Baixa os bytes da logo com timeout curto (3s). Qualquer falha retorna null — o
    /// cabeçalho usa o placeholder de iniciais. Nunca bloqueia a geração.
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

    // internal virtual para override em testes de unidade (sem banco).
    internal virtual async Task<DadosTermoPdf> CarregarDadosAsync(long termoId, long estabelecimentoId)
    {
        const string sql = """
            SELECT  t.id                        AS Id,
                    t.estabelecimento_id        AS EstabelecimentoId,
                    t.paciente_id               AS PacienteId,
                    t.status                    AS Status,
                    t.assinatura_tipo           AS AssinaturaTipo,
                    t.assinado_em               AS AssinadoEm,
                    t.ip_assinatura             AS IpAssinatura,
                    t.user_agent_assinatura     AS UserAgentAssinatura,
                    t.hash_integridade          AS HashIntegridade,
                    t.token_aceite              AS TokenAceite,
                    t.revogado_em               AS RevogadoEm,
                    t.revogado_motivo           AS RevogadoMotivo,
                    t.criado_em                 AS CriadoEm,
                    t.versao_modelo             AS VersaoModelo,
                    t.conteudo_snapshot_html    AS ConteudoSnapshotHtml,
                    m.titulo                    AS ModeloTitulo,
                    m.categoria                 AS ModeloCategoria,
                    u_emissor.nome_completo     AS EmissorNome,
                    pa.nome_completo            AS PacienteNome,
                    pa.cpf                      AS PacienteCpf,
                    pa.data_nascimento          AS PacienteDataNascimento,
                    pa.genero                   AS PacienteGenero,
                    pa.telefone                 AS PacienteTelefone,
                    e.nome_fantasia             AS EstabelecimentoNomeFantasia,
                    e.cnpj                      AS EstabelecimentoCnpj,
                    e.telefone                  AS EstabelecimentoTelefone,
                    e.endereco                  AS EstabelecimentoEndereco,
                    e.foto_url                  AS EstabelecimentoFotoUrl
            FROM    public.termo_emitido t
            LEFT JOIN public.termo_modelo m     ON m.id = t.termo_modelo_id
            LEFT JOIN public.pacientes pa       ON pa.id = t.paciente_id
            LEFT JOIN public.estabelecimentos e ON e.id = t.estabelecimento_id
            LEFT JOIN public.usuarios u_emissor ON u_emissor.id = t.emitido_por_usuario_id
            WHERE   t.id = @TermoId
              AND   t.estabelecimento_id = @EstabelecimentoId
            """;

        await using var conn = new NpgsqlConnection(_connStr);
        var row = await conn.QuerySingleOrDefaultAsync<TermoRow>(sql, new
        {
            TermoId = termoId,
            EstabelecimentoId = estabelecimentoId,
        });

        if (row is null) return null;
        return new DadosTermoPdf(row);
    }

    // ────────────────────────────────────────────────────────────────
    // Geração do PDF — layout institucional
    // ────────────────────────────────────────────────────────────────

    internal static byte[] GerarPdf(DadosTermoPdf dados)
    {
        var t = dados.Termo;
        const string accent = CorInkTitle;

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0);
                page.DefaultTextStyle(x => x.FontFamily(Fonte).FontSize(9).FontColor(CorInk));
                page.PageColor(Colors.White);

                // Marca d'água em todas as páginas
                page.Background().Element(c => DesenharWatermarkPorStatus(c, t.Status));

                page.Content()
                    .PaddingHorizontal(18, Unit.Millimetre)
                    .PaddingTop(14, Unit.Millimetre)
                    .PaddingBottom(22, Unit.Millimetre)
                    .Column(col =>
                    {
                        // Cabeçalho institucional
                        col.Item().Element(c => DesenharCabecalho(c, t, accent, dados.LogoBytes));

                        // Bloco do paciente
                        col.Item().PaddingTop(4, Unit.Millimetre).Element(c => DesenharBlocoPaciente(c, t));

                        // Metadados curtos
                        col.Item().PaddingTop(3, Unit.Millimetre).Element(c => DesenharMetadados(c, t));

                        // Corpo: snapshot HTML → blocos textuais
                        col.Item().PaddingTop(5, Unit.Millimetre).Element(c => DesenharCorpo(c, t));

                        // Bloco de revogação (status = Revogado)
                        if (string.Equals(t.Status, "Revogado", StringComparison.OrdinalIgnoreCase))
                        {
                            col.Item().PaddingTop(4, Unit.Millimetre)
                                .Element(c => DesenharBlocoRevogacao(c, t));
                        }

                        // Bloco de evidência do aceite
                        col.Item().PaddingTop(5, Unit.Millimetre)
                            .Element(c => DesenharBlocoEvidencia(c, t));
                    });

                // Rodapé fixo
                page.Footer().Element(c => DesenharRodape(c, t));
            });
        });

        return doc.GeneratePdf();
    }

    // ────────────────────────────────────────────────────────────────
    // Componentes do layout
    // ────────────────────────────────────────────────────────────────

    private static void DesenharCabecalho(IContainer container, TermoRow t, string accent, byte[] logoBytes)
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
                        .Element(c => DesenharLogo(c, t, accent, logoBytes));

                    brand.RelativeItem().PaddingLeft(2, Unit.Millimetre).AlignMiddle()
                        .Text(t.EstabelecimentoNomeFantasia ?? "Estabelecimento")
                        .FontSize(15).Bold().FontColor(accent);
                });

                // Direita: contato
                row.RelativeItem().AlignRight().Column(contato =>
                {
                    var linhas = new List<string>();
                    if (!string.IsNullOrWhiteSpace(t.EstabelecimentoEndereco)) linhas.Add(t.EstabelecimentoEndereco!);
                    var tel = FormatarTelefone(t.EstabelecimentoTelefone);
                    if (tel is not null) linhas.Add(tel);
                    var cnpj = FormatarCnpj(t.EstabelecimentoCnpj);
                    if (cnpj is not null) linhas.Add($"CNPJ {cnpj}");
                    foreach (var l in linhas)
                        contato.Item().Text(l).FontSize(8).FontColor(CorSecondary);
                });
            });

            col.Item().PaddingTop(2, Unit.Millimetre).LineHorizontal(0.3f).LineColor(CorBorder);

            // Título + subtítulo
            col.Item().PaddingTop(2, Unit.Millimetre).Row(titulo =>
            {
                var cat = string.IsNullOrWhiteSpace(t.ModeloCategoria)
                    ? ""
                    : $" — {t.ModeloCategoria.ToUpperInvariant()}";
                titulo.RelativeItem()
                    .Text($"TERMO DE CONSENTIMENTO{cat}")
                    .FontSize(12).Bold().FontColor(accent).LetterSpacing(0.6f);

                var sub = $"Modelo: {t.ModeloTitulo} (v{t.VersaoModelo})";
                titulo.RelativeItem().AlignRight().Text(sub)
                    .FontSize(8.5f).FontColor(CorMute);
            });

            // Linha dupla
            col.Item().PaddingTop(1, Unit.Millimetre).LineHorizontal(0.6f).LineColor(accent);
            col.Item().PaddingTop(0.3f, Unit.Millimetre).LineHorizontal(0.6f).LineColor(accent);
        });
    }

    private static void DesenharLogo(IContainer container, TermoRow t, string accent, byte[] logoBytes)
    {
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
                // Imagem corrompida — cai no placeholder.
            }
        }

        var iniciais = ObterIniciais(t.EstabelecimentoNomeFantasia);
        container.AlignCenter().AlignMiddle()
            .Background(accent)
            .Width(12, Unit.Millimetre).Height(12, Unit.Millimetre)
            .Layers(layers =>
            {
                layers.PrimaryLayer().AlignCenter().AlignMiddle().Text(iniciais)
                    .FontSize(7).Bold().FontColor(Colors.White);
            });
    }

    private static void DesenharBlocoPaciente(IContainer container, TermoRow t)
    {
        container.Background(CorCardBg).Border(0.3f).BorderColor(CorBorder)
            .Padding(3, Unit.Millimetre).Column(col =>
        {
            col.Spacing(2);

            col.Item().Row(row =>
            {
                row.RelativeItem(2).Element(c => DesenharCelula(c, "Paciente", t.PacienteNome ?? "—", destaque: true));
                var idade = CalcularIdade(t.PacienteDataNascimento);
                row.RelativeItem().Element(c => DesenharCelula(c, "Idade", idade.HasValue ? $"{idade} anos" : "—"));
                row.RelativeItem().Element(c => DesenharCelula(c, "Sexo", FormatarGenero(t.PacienteGenero)));
                row.RelativeItem().Element(c => DesenharCelula(c, "Telefone", FormatarTelefone(t.PacienteTelefone) ?? "—"));
            });

            col.Item().PaddingVertical(0.5f, Unit.Millimetre).LineHorizontal(0.3f).LineColor(CorBorder);

            col.Item().Row(row =>
            {
                row.RelativeItem().Element(c => DesenharCelula(c, "CPF", FormatarCpf(t.PacienteCpf) ?? "—"));
                row.RelativeItem().Element(c => DesenharCelula(c, "Nascimento", FormatarData(t.PacienteDataNascimento)));
                row.RelativeItem(2).Element(c => DesenharCelula(c, "Emitido por", t.EmissorNome ?? "—"));
            });
        });
    }

    private static void DesenharCelula(IContainer container, string label, string valor, bool destaque = false)
    {
        container.Column(c =>
        {
            c.Item().Text(label.ToUpperInvariant())
                .FontSize(6.5f).SemiBold().FontColor(CorMuteLight).LetterSpacing(0.3f);

            var t = c.Item().Text(valor).FontSize(9).FontColor(CorInk);
            if (destaque) t.Bold();
        });
    }

    private static void DesenharMetadados(IContainer container, TermoRow t)
    {
        var partes = new List<string>();
        partes.Add($"Emitido em {FormatarDataHora(t.CriadoEm)}");
        partes.Add($"ID #{t.Id}");

        container.Text(string.Join("   ·   ", partes))
            .FontSize(8).FontColor(CorMute);
    }

    private static void DesenharCorpo(IContainer container, TermoRow t)
    {
        var blocos = HtmlParaBlocos(t.ConteudoSnapshotHtml ?? "");

        if (blocos.Count == 0)
        {
            container.Text("Conteúdo do termo não disponível.")
                .FontSize(9).Italic().FontColor(CorMute);
            return;
        }

        container.Column(col =>
        {
            col.Spacing(1);
            foreach (var bloco in blocos)
            {
                if (bloco.Texto.Length == 0)
                {
                    col.Item().Height(3, Unit.Millimetre);
                    continue;
                }

                col.Item().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontFamily(Fonte).FontColor(CorInk));
                    switch (bloco.Tipo)
                    {
                        case TipoBloco.H1:
                            text.Span(bloco.Texto).FontSize(13).Bold().FontColor(CorInkTitle);
                            break;
                        case TipoBloco.H2:
                            text.Span(bloco.Texto).FontSize(11).Bold().FontColor(CorInkTitle);
                            break;
                        case TipoBloco.H3:
                            text.Span(bloco.Texto).FontSize(10).SemiBold().FontColor(CorInkTitle);
                            break;
                        case TipoBloco.Li:
                            text.Span("•  ").SemiBold().FontColor(CorMute);
                            text.Span(bloco.Texto).FontSize(10);
                            break;
                        default:
                            text.Span(bloco.Texto).FontSize(10);
                            break;
                    }
                });
            }
        });
    }

    private static void DesenharBlocoRevogacao(IContainer container, TermoRow t)
    {
        container.Background(CorRevogadoBg)
            .Border(0.4f).BorderColor(CorRevogadoBorda)
            .Padding(3, Unit.Millimetre).Column(col =>
        {
            col.Item().Text($"REVOGADO EM {FormatarDataHora(t.RevogadoEm)}")
                .FontSize(11).Bold().FontColor(CorRevogadoTexto);

            if (!string.IsNullOrWhiteSpace(t.RevogadoMotivo))
            {
                col.Item().PaddingTop(1, Unit.Millimetre)
                    .Text($"Motivo: {t.RevogadoMotivo}")
                    .FontSize(9).FontColor(CorRevogadoTexto);
            }
        });
    }

    private static void DesenharBlocoEvidencia(IContainer container, TermoRow t)
    {
        container.Background(CorEvidenciaBg)
            .Border(0.4f).BorderColor(CorEvidenciaBorda)
            .Padding(4, Unit.Millimetre).Column(col =>
        {
            col.Item().Text("EVIDÊNCIA DO ACEITE")
                .FontSize(8).Bold().FontColor(CorInkTitle).LetterSpacing(0.4f);

            col.Item().PaddingTop(1, Unit.Millimetre).LineHorizontal(0.3f).LineColor(CorBorderStrong);

            col.Item().PaddingTop(2, Unit.Millimetre).Column(linhas =>
            {
                linhas.Spacing(2);

                var statusNorm = t.Status?.ToUpperInvariant() ?? "";

                if (statusNorm == "ASSINADO" || statusNorm == "REVOGADO")
                {
                    // Assinado: exibe dados completos do aceite
                    linhas.Item().Text(text =>
                    {
                        text.Span("Aceito digitalmente em ").FontSize(9).FontColor(CorSecondary);
                        text.Span(FormatarDataHora(t.AssinadoEm)).FontSize(9).Bold().FontColor(CorInk);
                    });

                    linhas.Item().Text(text =>
                    {
                        text.Span("Por: ").FontSize(9).FontColor(CorSecondary);
                        text.Span(t.PacienteNome ?? "—").FontSize(9).Bold().FontColor(CorInk);
                    });

                    if (!string.IsNullOrWhiteSpace(t.IpAssinatura))
                    {
                        linhas.Item().Text(text =>
                        {
                            text.Span("IP de origem: ").FontSize(9).FontColor(CorSecondary);
                            text.Span(t.IpAssinatura!).FontSize(9).FontColor(CorInk);
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(t.UserAgentAssinatura))
                    {
                        var ua = t.UserAgentAssinatura!.Length > 80
                            ? t.UserAgentAssinatura[..80] + "…"
                            : t.UserAgentAssinatura;
                        linhas.Item().Text(text =>
                        {
                            text.Span("Dispositivo: ").FontSize(9).FontColor(CorSecondary);
                            text.Span(ua).FontSize(9).FontColor(CorInk);
                        });
                    }

                    // CA3: apenas os últimos 6 caracteres do token — nunca o completo.
                    if (!string.IsNullOrWhiteSpace(t.TokenAceite) && t.TokenAceite.Length >= 6)
                    {
                        var tokenParcial = $"…{t.TokenAceite[^6..]}";
                        linhas.Item().Text(text =>
                        {
                            text.Span("Identificador do aceite: ").FontSize(9).FontColor(CorSecondary);
                            text.Span(tokenParcial).FontSize(9).FontColor(CorInk);
                        });
                    }
                }
                else if (statusNorm == "RECUSADO")
                {
                    linhas.Item().Text(text =>
                    {
                        text.Span("Recusado em ").FontSize(9).FontColor(CorSecondary);
                        text.Span(FormatarDataHora(t.AssinadoEm)).FontSize(9).Bold().FontColor(CorInk);
                    });

                    if (!string.IsNullOrWhiteSpace(t.IpAssinatura))
                    {
                        linhas.Item().Text(text =>
                        {
                            text.Span("IP de origem: ").FontSize(9).FontColor(CorSecondary);
                            text.Span(t.IpAssinatura!).FontSize(9).FontColor(CorInk);
                        });
                    }
                }
                else if (statusNorm == "EXPIRADO")
                {
                    linhas.Item().Text("Link de aceite expirou sem assinatura.")
                        .FontSize(9).Italic().FontColor(CorMute);
                }
                else
                {
                    // Pendente e qualquer outro estado sem aceite
                    linhas.Item().Text("Documento ainda não assinado — aguardando aceite.")
                        .FontSize(9).Italic().FontColor(CorMute);
                }

                // Hash de integridade (sempre que houver)
                if (!string.IsNullOrWhiteSpace(t.HashIntegridade))
                {
                    col.Item().PaddingTop(2, Unit.Millimetre).Text(text =>
                    {
                        text.Span("Hash de integridade (SHA-256): ").FontSize(8).FontColor(CorSecondary);
                        text.Span(t.HashIntegridade!).FontSize(8).FontColor(CorMute);
                    });
                }
            });
        });
    }

    private static void DesenharWatermarkPorStatus(IContainer container, string status)
    {
        var (texto, cor, fontSize) = (status ?? "").ToUpperInvariant() switch
        {
            "REVOGADO"  => ("REVOGADO", CorDanger, 100f),
            "PENDENTE"  => ("AGUARDANDO ASSINATURA", CorMute, 70f),
            _           => ("IMEDTO", CorInk, 130f),
        };

        container.AlignCenter().AlignMiddle().Rotate(-25)
            .Text(texto).FontSize(fontSize).Bold()
            .FontColor(cor).LineHeight(0.9f);
    }

    private static void DesenharRodape(IContainer container, TermoRow t)
    {
        var statusNorm = (t.Status ?? "").ToUpperInvariant();
        var aviso = statusNorm switch
        {
            "PENDENTE"  => "Aguardando aceite digital pelo paciente.",
            "ASSINADO"  => "Aceito digitalmente — evidência registrada acima.",
            "REVOGADO"  => "Este consentimento foi revogado conforme registrado acima.",
            "RECUSADO"  => "O paciente recusou este consentimento.",
            "EXPIRADO"  => "O link de aceite expirou sem resposta.",
            _           => string.Empty,
        };

        container.PaddingHorizontal(18, Unit.Millimetre).PaddingBottom(6, Unit.Millimetre)
            .Row(row =>
        {
            row.RelativeItem().Column(sign =>
            {
                if (!string.IsNullOrEmpty(aviso))
                    sign.Item().Text(aviso).FontSize(8).Italic().FontColor(CorMute);

                if (!string.IsNullOrWhiteSpace(t.HashIntegridade))
                {
                    var hashCurto = t.HashIntegridade.Length > 16
                        ? t.HashIntegridade[..16] + "…"
                        : t.HashIntegridade;
                    sign.Item().Text($"Hash: {hashCurto}")
                        .FontSize(7).FontColor(CorMuteLight);
                }
            });

            row.RelativeItem().AlignRight().Column(meta =>
            {
                meta.Item().Text($"Emitido em {DateTime.Now:dd/MM/yyyy 'às' HH:mm}")
                    .FontSize(7).FontColor(CorMuteLight);
                meta.Item().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(7).FontColor(CorMuteLight));
                    text.Span("Página ");
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                });
            });
        });
    }

    // ────────────────────────────────────────────────────────────────
    // Parser HTML → blocos textuais (portado de useTermoPdf.ts)
    // ────────────────────────────────────────────────────────────────

    internal enum TipoBloco { P, H1, H2, H3, Li }

    internal sealed record BlocoTexto(TipoBloco Tipo, string Texto);

    /// <summary>
    /// Converte HTML do snapshot em blocos textuais para QuestPDF.
    /// Trata: h1-h6, p, div, blockquote, ul/ol+li, br. O resto vira parágrafo.
    /// Não é parser CSS completo — objetiva fidelidade ao texto, não ao estilo (CA2).
    /// </summary>
    internal static List<BlocoTexto> HtmlParaBlocos(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return new();

        // Remove tags de script/style — não devem aparecer no snapshot, mas defensivo.
        html = Regex.Replace(html, @"<(script|style)[^>]*>.*?</(script|style)>",
            "", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        var blocos = new List<BlocoTexto>();
        ParseNo(html, blocos);
        return blocos;
    }

    private static void ParseNo(string html, List<BlocoTexto> saida)
    {
        // Pattern: captura tags e texto entre elas.
        // Estratégia: processar tag por tag com índice de posição.
        var pos = 0;
        var len = html.Length;

        while (pos < len)
        {
            var tagStart = html.IndexOf('<', pos);
            if (tagStart < 0)
            {
                // Só texto restante
                var txt = DecodeHtmlEntities(html[pos..]).Trim();
                if (txt.Length > 0) saida.Add(new BlocoTexto(TipoBloco.P, txt));
                break;
            }

            // Texto antes da tag
            if (tagStart > pos)
            {
                var antes = DecodeHtmlEntities(html[pos..tagStart]).Trim();
                if (antes.Length > 0) saida.Add(new BlocoTexto(TipoBloco.P, antes));
            }

            var tagEnd = html.IndexOf('>', tagStart);
            if (tagEnd < 0) break; // HTML malformado

            var tagContent = html[(tagStart + 1)..tagEnd].TrimStart('/').Trim();
            var tagName = tagContent.Split(' ', 2)[0].ToLowerInvariant().TrimStart('/');

            // Self-closing tags
            if (tagName == "br")
            {
                saida.Add(new BlocoTexto(TipoBloco.P, string.Empty));
                pos = tagEnd + 1;
                continue;
            }

            // Tags de bloco com conteúdo interno
            var tipoBloco = tagName switch
            {
                "h1"           => TipoBloco.H1,
                "h2"           => TipoBloco.H2,
                "h3" or "h4"   => TipoBloco.H3,
                "h5" or "h6"   => TipoBloco.H3,
                "li"           => TipoBloco.Li,
                "p" or "div" or "blockquote" or "section" or "article" => TipoBloco.P,
                _ => (TipoBloco?)null,
            };

            if (tipoBloco.HasValue && !tagContent.StartsWith('/'))
            {
                // Encontra o fechamento correspondente
                var closeTag = $"</{tagName}>";
                var closeIdx = html.IndexOf(closeTag, tagEnd + 1, StringComparison.OrdinalIgnoreCase);
                if (closeIdx < 0)
                {
                    // Sem fechamento — trata como self-closing
                    pos = tagEnd + 1;
                    continue;
                }

                var innerHtml = html[(tagEnd + 1)..closeIdx];

                if (tagName is "ul" or "ol")
                {
                    // Processa itens filhos
                    ParseNo(innerHtml, saida);
                }
                else
                {
                    // Extrai texto do conteúdo interno (removendo tags internas)
                    var innerText = ExtractText(innerHtml);
                    if (innerText.Length > 0)
                        saida.Add(new BlocoTexto(tipoBloco.Value, innerText));
                }

                pos = closeIdx + closeTag.Length;
            }
            else
            {
                // Tag desconhecida ou fechamento — avança
                pos = tagEnd + 1;
            }
        }
    }

    /// <summary>
    /// Extrai texto puro de um fragmento HTML, preservando quebras de linha de &lt;br&gt;.
    /// </summary>
    private static string ExtractText(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        // br → espaço
        var sem_br = Regex.Replace(html, @"<br\s*/?>", " ", RegexOptions.IgnoreCase);
        // Remove demais tags
        var sem_tags = Regex.Replace(sem_br, "<[^>]+>", " ");
        return DecodeHtmlEntities(sem_tags.Replace("\n", " ").Replace("\r", " "))
            .Replace("  ", " ").Trim();
    }

    private static string DecodeHtmlEntities(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Replace("&nbsp;", " ")
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&quot;", "\"")
                .Replace("&#39;", "'")
                .Replace("&apos;", "'");
    }

    // ────────────────────────────────────────────────────────────────
    // Helpers de formatação
    // ────────────────────────────────────────────────────────────────

    private static string FormatarData(DateTime? d)
        => d?.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("pt-BR")) ?? "—";

    private static string FormatarDataHora(DateTime? d)
    {
        if (d is null) return "—";
        return d.Value.ToLocalTime().ToString("dd/MM/yyyy 'às' HH:mm", CultureInfo.GetCultureInfo("pt-BR"));
    }

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
        return g.Trim().ToUpperInvariant() switch
        {
            "F" or "FEMININO"  => "Feminino",
            "M" or "MASCULINO" => "Masculino",
            _                  => g,
        };
    }

    private static int? CalcularIdade(DateTime? nascimento)
    {
        if (nascimento is null) return null;
        var hoje = DateTime.UtcNow.Date;
        var anos = hoje.Year - nascimento.Value.Year;
        if (hoje < nascimento.Value.Date.AddYears(anos)) anos--;
        return anos < 0 ? null : anos;
    }

    private static string ObterIniciais(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome)) return "?";
        var partes = nome.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length == 1) return partes[0][..Math.Min(2, partes[0].Length)].ToUpperInvariant();
        return $"{partes[0][0]}{partes[^1][0]}".ToUpperInvariant();
    }

    // ────────────────────────────────────────────────────────────────
    // Registro das fontes Nunito (recursos embarcados no assembly)
    // As fontes ficam em Receitas/Fonts/ — registradas pelo QuestPdfReceitaService.
    // Este serviço reutiliza o mesmo flag estático; se o ReceitaService registrou
    // primeiro, aqui é no-op.
    // ────────────────────────────────────────────────────────────────

    private static void RegistrarFontesNunitoUmaVez()
    {
        if (_fontesRegistradas) return;
        lock (_fontesLock)
        {
            if (_fontesRegistradas) return;
            try
            {
                // Fontes embarcadas em Receitas/Fonts/ — mesmo assembly.
                var asm = typeof(QuestPdfTermoService).Assembly;
                RegistrarFonte(asm, "Imedto.Backend.Infrastructure.Receitas.Fonts.Nunito-Regular.ttf");
                RegistrarFonte(asm, "Imedto.Backend.Infrastructure.Receitas.Fonts.Nunito-SemiBold.ttf");
                RegistrarFonte(asm, "Imedto.Backend.Infrastructure.Receitas.Fonts.Nunito-Bold.ttf");
            }
            catch
            {
                // Falha silenciosa — QuestPDF usa fonte default.
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

// ─────────────────────────────────────────────────────────────────────────────
// DTOs internos do QuestPdfTermoService
// Extraídos do record principal para que CarregarDadosAsync seja virtual.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Row Dapper com todos os dados necessários para o PDF probatório.</summary>
internal sealed record TermoRow(
    long Id,
    long EstabelecimentoId,
    long PacienteId,
    string Status,
    string AssinaturaTipo,
    DateTime? AssinadoEm,
    string IpAssinatura,
    string UserAgentAssinatura,
    string HashIntegridade,
    string TokenAceite,
    DateTime? RevogadoEm,
    string RevogadoMotivo,
    DateTime CriadoEm,
    int VersaoModelo,
    string ConteudoSnapshotHtml,
    string ModeloTitulo,
    string ModeloCategoria,
    string EmissorNome,
    string PacienteNome,
    string PacienteCpf,
    DateTime? PacienteDataNascimento,
    string PacienteGenero,
    string PacienteTelefone,
    string EstabelecimentoNomeFantasia,
    string EstabelecimentoCnpj,
    string EstabelecimentoTelefone,
    string EstabelecimentoEndereco,
    string EstabelecimentoFotoUrl);

internal sealed record DadosTermoPdf(TermoRow Termo)
{
    /// <summary>Bytes da logo do estabelecimento (null = usa placeholder de iniciais).</summary>
    public byte[] LogoBytes { get; init; }
}
