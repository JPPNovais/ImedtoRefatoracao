using System.Globalization;
using System.Text.Json;
using Dapper;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure.Receitas;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.Extensions.Logging;
using Npgsql;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Imedto.Backend.Infrastructure.Prontuarios;

/// <summary>
/// Contrato do gerador de PDF do prontuário completo.
/// </summary>
public interface IProntuarioPdfService
{
    /// <summary>
    /// Gera o PDF do histórico completo do prontuário do paciente.
    /// Lança <see cref="BusinessException"/> se prontuário não encontrado ou de outro tenant.
    /// Registra audit LGPD de Exportacao (best-effort — falha não bloqueia o download).
    /// </summary>
    Task<byte[]> GerarAsync(long pacienteId, long estabelecimentoId, Guid solicitanteUsuarioId);
}

/// <summary>
/// Geração do PDF do prontuário usando QuestPDF.
/// Reutiliza o pipeline de receitas: licença Community, fontes Nunito embarcadas,
/// cabeçalho institucional (logo/estabelecimento + bloco do paciente), marca d'água sutil.
///
/// Layout: cabeçalho institucional → bloco do paciente → lista de evoluções em ordem
/// cronológica (mais antigas primeiro), cada uma com data/autor/modelo/conteúdo legível
/// + indicação de anexos. Rodapé com numeração de páginas.
///
/// LGPD: sem PII em log. Multi-tenant: query filtra por estabelecimento_id.
/// Falha-fechada: prontuário de outro tenant retorna null → BusinessException genérica.
/// </summary>
public class QuestPdfProntuarioService : IProntuarioPdfService
{
    // ── Cores (sincronizadas com QuestPdfReceitaService) ──────────────────
    private const string CorInk = "#1A2440";
    private const string CorInkTitle = "#1D3557";
    private const string CorSecondary = "#475569";
    private const string CorMute = "#64748B";
    private const string CorMuteLight = "#94A3B8";
    private const string CorBorder = "#E2E8F0";
    private const string CorBorderStrong = "#CBD5E1";
    private const string CorCardBg = "#F8FAFC";
    private const string CorEvolucaoBg = "#F1F5F9";

    private const string Fonte = "Nunito";

    private readonly string _connStr;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IProntuarioRepository _prontuarioRepo;
    private readonly IProntuarioAcessoLogService _acessoLog;
    private readonly ILogger<QuestPdfProntuarioService> _logger;

    /// <summary>Nome lógico do HttpClient para baixar a logo (reutiliza o do serviço de receita).</summary>
    public const string HttpClientName = "PdfReceitaLogo";

    public QuestPdfProntuarioService(
        AppReadConnectionString conn,
        IHttpClientFactory httpFactory,
        IProntuarioRepository prontuarioRepo,
        IProntuarioAcessoLogService acessoLog,
        ILogger<QuestPdfProntuarioService> logger)
    {
        _connStr = conn.Value;
        _httpFactory = httpFactory;
        _prontuarioRepo = prontuarioRepo;
        _acessoLog = acessoLog;
        _logger = logger;
        // Reutiliza a inicialização do pipeline de receita (licença Community + fontes Nunito).
        // InicializarQuestPdf é idempotente via lock — seguro chamar múltiplas vezes.
        QuestPdfReceitaService.InicializarQuestPdf();
    }

    public async Task<byte[]> GerarAsync(long pacienteId, long estabelecimentoId, Guid solicitanteUsuarioId)
    {
        var dados = await CarregarDadosAsync(pacienteId, estabelecimentoId);
        if (dados is null)
            throw new BusinessException("Prontuário não encontrado.");

        // Audit LGPD de Exportacao — best-effort (falha não bloqueia o download).
        await RegistrarAuditAsync(dados.Cabecalho.ProntuarioId, estabelecimentoId, solicitanteUsuarioId);

        var logoBytes = await BaixarLogoAsync(dados.Cabecalho.EstabelecimentoFotoUrl);

        return GerarPdf(dados with { LogoBytes = logoBytes });
    }

    private async Task RegistrarAuditAsync(long prontuarioId, long estabelecimentoId, Guid usuarioId)
    {
        try
        {
            await _acessoLog.RegistrarAsync(
                prontuarioId,
                usuarioId,
                estabelecimentoId,
                TipoAcessoProntuario.Exportacao);
        }
        catch (Exception ex)
        {
            // Sem PII no log — apenas o fato técnico da falha.
            _logger.LogError(ex, "Falha ao registrar audit de exportação de prontuário.");
        }
    }

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

    // internal virtual para override em testes de unidade (sem banco real).
    internal virtual async Task<DadosProntuarioPdf> CarregarDadosAsync(long pacienteId, long estabelecimentoId)
    {
        // Cabeçalho: uma linha por prontuário (dados do estabelecimento + paciente).
        // Profissional não é exibido no topo porque o prontuário pode ter evoluções de vários autores;
        // cada evolução no corpo já exibe seu próprio autor.
        const string sqlCabecalho = """
            SELECT  p.id                         AS ProntuarioId,
                    pa.id                        AS PacienteId,
                    pa.nome_completo             AS PacienteNome,
                    pa.data_nascimento           AS PacienteDataNascimento,
                    pa.genero                    AS PacienteGenero,
                    e.nome_fantasia              AS EstabelecimentoNomeFantasia,
                    e.cnpj                       AS EstabelecimentoCnpj,
                    e.telefone                   AS EstabelecimentoTelefone,
                    e.endereco                   AS EstabelecimentoEndereco,
                    e.foto_url                   AS EstabelecimentoFotoUrl,
                    p.criado_em                  AS ProntuarioCriadoEm
            FROM    public.prontuarios p
            INNER JOIN public.estabelecimentos e  ON e.id = p.estabelecimento_id
            INNER JOIN public.pacientes pa        ON pa.id = p.paciente_id
            WHERE   p.paciente_id = @PacienteId
              AND   p.estabelecimento_id = @EstabelecimentoId
              AND   p.deletado_em IS NULL
              AND   pa.deletado_em IS NULL
            """;

        // Evoluções em ordem cronológica (mais antigas → mais recentes) para leitura clínica
        const string sqlEvolucoes = """
            SELECT  e.id                              AS Id,
                    e.criada_em                       AS CriadaEm,
                    u.nome_completo                   AS AutorNome,
                    mdp.nome                          AS ModeloNome,
                    e.conteudo::text                  AS ConteudoJson,
                    e.modelo_snapshot::text           AS ModeloSnapshotJson,
                    (SELECT COUNT(*)::int
                     FROM public.prontuario_anexos a
                     WHERE a.evolucao_id = e.id
                       AND a.deletado_em IS NULL)     AS ContagemAnexos
            FROM    public.prontuario_evolucoes e
            LEFT JOIN public.usuarios u               ON u.id = e.autor_usuario_id
            LEFT JOIN public.modelo_de_prontuario mdp ON mdp.id = e.modelo_de_prontuario_id_origem
            WHERE   e.prontuario_id = @ProntuarioId
              AND   e.deletado_em IS NULL
            ORDER BY e.criada_em ASC
            """;

        await using var conn = new NpgsqlConnection(_connStr);

        var cabecalho = await conn.QuerySingleOrDefaultAsync<ProntuarioCabecalhoRow>(sqlCabecalho, new
        {
            PacienteId = pacienteId,
            EstabelecimentoId = estabelecimentoId
        });

        if (cabecalho is null) return null;

        var evolucoes = await conn.QueryAsync<EvolucaoPdfRow>(sqlEvolucoes, new
        {
            ProntuarioId = cabecalho.ProntuarioId
        });

        return new DadosProntuarioPdf(cabecalho, evolucoes.ToList());
    }

    // ────────────────────────────────────────────────────────────────
    // Geração do PDF — layout institucional
    // ────────────────────────────────────────────────────────────────

    internal static byte[] GerarPdf(DadosProntuarioPdf dados)
    {
        var cab = dados.Cabecalho;

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0);
                page.DefaultTextStyle(x => x.FontFamily(Fonte).FontSize(9).FontColor(CorInk));
                page.PageColor(Colors.White);

                // Marca d'água sutil em todas as páginas
                page.Background().AlignCenter().AlignMiddle().Rotate(-25)
                    .Text("IMEDTO").FontSize(130).Bold().FontColor(CorInk);

                page.Content()
                    .PaddingHorizontal(18, Unit.Millimetre)
                    .PaddingTop(14, Unit.Millimetre)
                    .PaddingBottom(22, Unit.Millimetre)
                    .Column(col =>
                    {
                        // Cabeçalho institucional
                        col.Item().Element(c => DesenharCabecalho(c, cab, dados.LogoBytes));

                        // Bloco do paciente
                        col.Item().PaddingTop(4, Unit.Millimetre)
                            .Element(c => DesenharBlocoPaciente(c, cab));

                        // Título da seção de evoluções
                        col.Item().PaddingTop(5, Unit.Millimetre).Row(r =>
                        {
                            r.RelativeItem().Text("HISTÓRICO DE EVOLUÇÕES")
                                .FontSize(10).Bold().FontColor(CorInkTitle).LetterSpacing(0.5f);
                            r.AutoItem().AlignRight()
                                .Text($"{dados.Evolucoes.Count} evolução(ões)")
                                .FontSize(8).FontColor(CorMute);
                        });
                        col.Item().PaddingTop(1, Unit.Millimetre)
                            .LineHorizontal(0.6f).LineColor(CorInkTitle);
                        col.Item().PaddingTop(0.3f, Unit.Millimetre)
                            .LineHorizontal(0.6f).LineColor(CorInkTitle);

                        if (dados.Evolucoes.Count == 0)
                        {
                            col.Item().PaddingTop(8, Unit.Millimetre)
                                .AlignCenter()
                                .Text("Nenhuma evolução registrada.")
                                .FontSize(10).FontColor(CorMuteLight);
                        }
                        else
                        {
                            col.Item().PaddingTop(3, Unit.Millimetre)
                                .Element(c => DesenharListaEvolucoes(c, dados.Evolucoes));
                        }
                    });

                page.Footer().Element(c => DesenharRodape(c, cab));
            });
        });

        return doc.GeneratePdf();
    }

    private static void DesenharCabecalho(IContainer container, ProntuarioCabecalhoRow cab, byte[] logoBytes)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                // Esquerda: logo + nome do estabelecimento
                row.RelativeItem().Row(brand =>
                {
                    brand.ConstantItem(14, Unit.Millimetre).AlignMiddle()
                        .Width(12, Unit.Millimetre).Height(12, Unit.Millimetre)
                        .Element(c => DesenharLogo(c, cab, logoBytes));

                    brand.RelativeItem().PaddingLeft(2, Unit.Millimetre).AlignMiddle()
                        .Text(cab.EstabelecimentoNomeFantasia ?? "Estabelecimento")
                        .FontSize(15).Bold().FontColor(CorInkTitle);
                });

                // Direita: contato do estabelecimento
                row.RelativeItem().AlignRight().Column(contato =>
                {
                    if (!string.IsNullOrWhiteSpace(cab.EstabelecimentoEndereco))
                        contato.Item().Text(cab.EstabelecimentoEndereco!).FontSize(8).FontColor(CorSecondary);
                    var tel = FormatarTelefone(cab.EstabelecimentoTelefone);
                    if (tel is not null)
                        contato.Item().Text(tel).FontSize(8).FontColor(CorSecondary);
                    var cnpj = FormatarCnpj(cab.EstabelecimentoCnpj);
                    if (cnpj is not null)
                        contato.Item().Text($"CNPJ {cnpj}").FontSize(8).FontColor(CorSecondary);
                });
            });

            col.Item().PaddingTop(2, Unit.Millimetre).LineHorizontal(0.3f).LineColor(CorBorder);

            col.Item().PaddingTop(2, Unit.Millimetre).Row(titulo =>
            {
                titulo.RelativeItem().Text("PRONTUÁRIO MÉDICO")
                    .FontSize(12).Bold().FontColor(CorInkTitle).LetterSpacing(0.6f);
                titulo.RelativeItem().AlignRight()
                    .Text($"Emitido em {DateTime.Now:dd/MM/yyyy}")
                    .FontSize(8.5f).FontColor(CorMute);
            });
        });
    }

    private static void DesenharLogo(IContainer container, ProntuarioCabecalhoRow cab, byte[] logoBytes)
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

        var iniciais = ObterIniciais(cab.EstabelecimentoNomeFantasia);
        container.AlignCenter().AlignMiddle()
            .Background(CorInkTitle)
            .Width(12, Unit.Millimetre).Height(12, Unit.Millimetre)
            .Layers(layers =>
            {
                layers.PrimaryLayer().AlignCenter().AlignMiddle()
                    .Text(iniciais).FontSize(7).Bold().FontColor(Colors.White);
            });
    }

    private static void DesenharBlocoPaciente(IContainer container, ProntuarioCabecalhoRow cab)
    {
        container.Background(CorCardBg).Border(0.3f).BorderColor(CorBorder)
            .Padding(3, Unit.Millimetre).Column(col =>
            {
                col.Spacing(2);

                col.Item().Row(row =>
                {
                    row.RelativeItem(2).Element(c => DesenharCelula(c, "Paciente", cab.PacienteNome ?? "—", destaque: true));
                    var idade = CalcularIdade(cab.PacienteDataNascimento);
                    row.RelativeItem().Element(c => DesenharCelula(c, "Idade", idade.HasValue ? $"{idade} anos" : "—"));
                    row.RelativeItem().Element(c => DesenharCelula(c, "Sexo", FormatarGenero(cab.PacienteGenero)));
                    row.RelativeItem().Element(c => DesenharCelula(c, "Prontuário desde", FormatarData(cab.ProntuarioCriadoEm)));
                });
            });
    }

    private static void DesenharCelula(IContainer container, string label, string valor, bool destaque = false)
    {
        container.Column(c =>
        {
            c.Item().Text(label.ToUpperInvariant())
                .FontSize(6.5f).SemiBold().FontColor(CorMuteLight).LetterSpacing(0.3f);
            c.Item().Text(valor).FontSize(9).FontColor(CorInk)
                .ApplyWhen(destaque, t => t.Bold());
        });
    }

    private static void DesenharListaEvolucoes(IContainer container, IReadOnlyList<EvolucaoPdfRow> evolucoes)
    {
        container.Column(col =>
        {
            col.Spacing(4);
            for (var idx = 0; idx < evolucoes.Count; idx++)
            {
                var evo = evolucoes[idx];
                col.Item().Element(c => DesenharEvolucao(c, idx + 1, evo));
            }
        });
    }

    private static void DesenharEvolucao(IContainer container, int numero, EvolucaoPdfRow evo)
    {
        container.Column(col =>
        {
            // Cabeçalho da evolução: número · data · autor · modelo
            col.Item().Background(CorEvolucaoBg).Border(0.3f).BorderColor(CorBorderStrong)
                .Padding(2, Unit.Millimetre).Row(row =>
                {
                    row.AutoItem().AlignMiddle()
                        .Background(CorInkTitle).Width(6, Unit.Millimetre).Height(6, Unit.Millimetre)
                        .AlignCenter()
                        .Text(numero.ToString(CultureInfo.InvariantCulture))
                        .FontSize(7).Bold().FontColor(Colors.White);

                    row.RelativeItem().PaddingLeft(2, Unit.Millimetre).AlignMiddle().Column(info =>
                    {
                        info.Item().Text(evo.CriadaEm.ToLocalTime().ToString("dd/MM/yyyy 'às' HH:mm", CultureInfo.GetCultureInfo("pt-BR")))
                            .FontSize(9).Bold().FontColor(CorInkTitle);
                        var meta = new List<string>();
                        if (!string.IsNullOrWhiteSpace(evo.AutorNome)) meta.Add(evo.AutorNome!);
                        if (!string.IsNullOrWhiteSpace(evo.ModeloNome)) meta.Add(evo.ModeloNome!);
                        if (meta.Count > 0)
                            info.Item().Text(string.Join("  ·  ", meta)).FontSize(7.5f).FontColor(CorSecondary);
                    });

                    if (evo.ContagemAnexos > 0)
                    {
                        row.AutoItem().AlignRight().AlignMiddle()
                            .Text($"{evo.ContagemAnexos} anexo(s)")
                            .FontSize(7).FontColor(CorMute);
                    }
                });

            // Conteúdo legível da evolução
            var conteudoLegivel = ExtrairTextoLegivel(evo.ConteudoJson, evo.ModeloSnapshotJson);
            if (!string.IsNullOrWhiteSpace(conteudoLegivel))
            {
                col.Item().BorderLeft(2f).BorderColor(CorBorderStrong)
                    .PaddingLeft(3, Unit.Millimetre).PaddingTop(1, Unit.Millimetre)
                    .PaddingBottom(1, Unit.Millimetre)
                    .Text(conteudoLegivel).FontSize(8.5f).FontColor(CorInk);
            }
        });
    }

    private static void DesenharRodape(IContainer container, ProntuarioCabecalhoRow cab)
    {
        container.PaddingHorizontal(18, Unit.Millimetre).PaddingBottom(6, Unit.Millimetre)
            .Row(row =>
            {
                row.RelativeItem().AlignLeft()
                    .Text($"Documento confidencial — uso restrito a {cab.EstabelecimentoNomeFantasia ?? "estabelecimento"}")
                    .FontSize(7).FontColor(CorMuteLight);

                row.RelativeItem().AlignRight().Column(meta =>
                {
                    meta.Item().AlignRight().Text(t =>
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

    // ────────────────────────────────────────────────────────────────
    // Extração de texto legível do conteúdo JSON da evolução
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Extrai texto legível do conteúdo JSON da evolução para exibição no PDF.
    /// Usa o ModeloSnapshot para obter os rótulos das seções; o conteúdo para os valores.
    /// Segue a mesma lógica do frontend (formatarSecaoLegivel em useEvolucaoResumo.ts).
    /// </summary>
    internal static string ExtrairTextoLegivel(string conteudoJson, string modeloSnapshotJson)
    {
        if (string.IsNullOrWhiteSpace(conteudoJson)) return null;

        try
        {
            var conteudo = JsonDocument.Parse(conteudoJson).RootElement;
            var sb = new System.Text.StringBuilder();

            // Tenta usar o snapshot do modelo para obter rótulos das seções
            Dictionary<string, string> rotulos = null;
            if (!string.IsNullOrWhiteSpace(modeloSnapshotJson))
            {
                rotulos = ExtrairRotulosDoModelo(modeloSnapshotJson);
            }

            if (conteudo.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in conteudo.EnumerateObject())
                {
                    var rotulo = rotulos?.GetValueOrDefault(prop.Name) ?? prop.Name;
                    var valor = ExtrairValorTexto(prop.Value);
                    if (string.IsNullOrWhiteSpace(valor)) continue;

                    sb.Append(rotulo.ToUpperInvariant());
                    sb.Append(": ");
                    sb.AppendLine(valor.Trim());
                }
            }

            var resultado = sb.ToString().Trim();
            return resultado.Length > 0 ? resultado : null;
        }
        catch
        {
            // JSON malformado — retorna nulo; bloco de conteúdo simplesmente não é exibido.
            return null;
        }
    }

    private static Dictionary<string, string> ExtrairRotulosDoModelo(string modeloSnapshotJson)
    {
        try
        {
            var rotulos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var snapshot = JsonDocument.Parse(modeloSnapshotJson).RootElement;

            // O snapshot pode conter "secoes" ou "campos" com id/rotulo
            if (snapshot.TryGetProperty("secoes", out var secoes) && secoes.ValueKind == JsonValueKind.Array)
            {
                foreach (var secao in secoes.EnumerateArray())
                {
                    if (secao.TryGetProperty("id", out var id) && secao.TryGetProperty("rotulo", out var rotulo))
                        rotulos[id.GetString() ?? ""] = rotulo.GetString() ?? "";
                    else if (secao.TryGetProperty("key", out var key) && secao.TryGetProperty("label", out var label))
                        rotulos[key.GetString() ?? ""] = label.GetString() ?? "";
                }
            }
            return rotulos;
        }
        catch
        {
            return null;
        }
    }

    private static string ExtrairValorTexto(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "Sim",
            JsonValueKind.False => "Não",
            JsonValueKind.Array => string.Join(", ", element.EnumerateArray()
                .Select(ExtrairValorTexto)
                .Where(v => !string.IsNullOrWhiteSpace(v))),
            JsonValueKind.Object => string.Join("; ", element.EnumerateObject()
                .Select(p => $"{p.Name}: {ExtrairValorTexto(p.Value)}")
                .Where(v => !string.IsNullOrWhiteSpace(v))),
            _ => null
        };
    }

    // ────────────────────────────────────────────────────────────────
    // Helpers de formatação
    // ────────────────────────────────────────────────────────────────

    private static int? CalcularIdade(DateTime? nascimento)
    {
        if (nascimento is null) return null;
        var hoje = DateTime.UtcNow.Date;
        var anos = hoje.Year - nascimento.Value.Year;
        if (hoje < nascimento.Value.Date.AddYears(anos)) anos--;
        return anos < 0 ? null : anos;
    }

    private static string FormatarData(DateTime d)
        => d.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("pt-BR"));

    private static string FormatarGenero(string g)
    {
        if (string.IsNullOrWhiteSpace(g)) return "—";
        return g.Trim().ToUpperInvariant() switch
        {
            "F" or "FEMININO" => "Feminino",
            "M" or "MASCULINO" => "Masculino",
            _ => g,
        };
    }

    private static string FormatarCnpj(string cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj)) return null;
        var d = new string(cnpj.ToUpperInvariant().Where(char.IsAsciiLetterOrDigit).ToArray());
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

    private static string ObterIniciais(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome)) return "?";
        var partes = nome.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length == 1) return partes[0][..Math.Min(2, partes[0].Length)].ToUpperInvariant();
        return ($"{partes[0][0]}{partes[^1][0]}").ToUpperInvariant();
    }
}
