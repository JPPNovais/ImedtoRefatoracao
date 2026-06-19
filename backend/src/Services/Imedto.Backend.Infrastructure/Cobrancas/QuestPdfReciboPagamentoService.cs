using System.Globalization;
using Dapper;
using Imedto.Backend.Domain.Cobrancas;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Infrastructure.Receitas;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.Extensions.Logging;
using Npgsql;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Imedto.Backend.Infrastructure.Cobrancas;

/// <summary>
/// Contrato do gerador de PDF de recibo de pagamento (F8).
/// </summary>
public interface IReciboPagamentoPdfService
{
    /// <summary>
    /// Gera o PDF do recibo. Lança <see cref="BusinessException"/> se:
    /// - pagamento não encontrado ou de outro tenant (404 genérico via 422);
    /// - pagamento estornado (CA120).
    /// Registra audit LGPD de Leitura no <c>paciente_acesso_log</c> (best-effort, CA127).
    /// Grava flag <c>recibo_emitido_em</c> na 1ª emissão (CA128).
    /// </summary>
    Task<byte[]> GerarAsync(long pagamentoId, long estabelecimentoId, Guid solicitanteUsuarioId);
}

/// <summary>
/// Geração do PDF do recibo de pagamento usando QuestPDF.
/// Reusa o mesmo pipeline da receita: Nunito embarcada, cabeçalho institucional,
/// logo/iniciais do estabelecimento, rodapé de meta.
///
/// LGPD: sem PII clínica (CID/diagnóstico) no recibo — apenas dado financeiro/identificação.
/// Nome de arquivo no Content-Disposition definido pelo controller: recibo-{pagamentoId}.pdf.
/// Multi-tenant: a query filtra por estabelecimento_id via JOIN com cobrancas.
/// Repositório falha-fechada: sem tenant claim → retorna null → 422 genérico.
/// </summary>
public class QuestPdfReciboPagamentoService : IReciboPagamentoPdfService
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
    private const string CorSemFiscal = "#B45309"; // Amber-700 — rótulo aviso

    private const string Fonte = "Nunito";

    // ── Fontes: compartilha a inicialização com o serviço de receita ──────
    // QuestPdfReceitaService.InicializarQuestPdf() é idempotente via lock,
    // portanto basta chamar no ctor — as fontes já estarão registradas se
    // o serviço de receita tiver rodado antes (mesma instância de assembly).
    private readonly string _connStr;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ICobrancaRepository _cobrancaRepo;
    private readonly IPacienteAcessoLogService _acessoLog;
    private readonly ILogger<QuestPdfReciboPagamentoService> _logger;

    /// <summary>Nome lógico do HttpClient para baixar a logo (reutiliza o mesmo do serviço de receita).</summary>
    public const string HttpClientName = "PdfReceitaLogo";

    public QuestPdfReciboPagamentoService(
        AppReadConnectionString conn,
        IHttpClientFactory httpFactory,
        ICobrancaRepository cobrancaRepo,
        IPacienteAcessoLogService acessoLog,
        ILogger<QuestPdfReciboPagamentoService> logger)
    {
        _connStr = conn.Value;
        _httpFactory = httpFactory;
        _cobrancaRepo = cobrancaRepo;
        _acessoLog = acessoLog;
        _logger = logger;
        // Reutiliza a inicialização do pipeline de receita (licença + fontes Nunito)
        Receitas.QuestPdfReceitaService.InicializarQuestPdf();
    }

    public async Task<byte[]> GerarAsync(long pagamentoId, long estabelecimentoId, Guid solicitanteUsuarioId)
    {
        // CA121: carregar dados via Dapper (leitura) — multi-tenant via JOIN
        var dados = await CarregarDadosAsync(pagamentoId, estabelecimentoId);
        if (dados is null)
            throw new BusinessException("Não encontrado.");

        // CA120: pagamento estornado → 422 via domain
        // Carrega o aggregate para acessar os estornos
        var cobranca = await _cobrancaRepo.ObterPorIdOuNulo(dados.CobrancaId, estabelecimentoId);
        if (cobranca is null)
            throw new BusinessException("Não encontrado.");

        var pagamento = cobranca.Pagamentos.FirstOrDefault(p => p.Id == pagamentoId);
        if (pagamento is null)
            throw new BusinessException("Não encontrado.");

        // Lança BusinessException se estornado; grava flag na 1ª emissão (CA128)
        pagamento.RegistrarEmissaoRecibo(cobranca.Estornos);

        // CA128: persiste flag recibo_emitido_em (apenas se foi a 1ª emissão — o método é idempotente)
        await _cobrancaRepo.Salvar(cobranca);

        // CA127: audit de acesso LGPD best-effort
        await RegistrarAuditAsync(dados.PacienteId, estabelecimentoId, solicitanteUsuarioId);

        var logoBytes = await BaixarLogoAsync(dados.EstabelecimentoFotoUrl);

        return GerarPdf(new DadosReciboPdf(dados) { LogoBytes = logoBytes });
    }

    /// <summary>
    /// Registra audit LGPD de Leitura no paciente_acesso_log.
    /// Best-effort: engole exceção — o recibo nunca é bloqueado por falha de audit (CA127).
    /// </summary>
    private async Task RegistrarAuditAsync(long pacienteId, long estabelecimentoId, Guid usuarioId)
    {
        try
        {
            await _acessoLog.RegistrarAsync(pacienteId, usuarioId, estabelecimentoId, TipoAcessoPaciente.Leitura);
        }
        catch (Exception ex)
        {
            // CA127: falha do audit não bloqueia a emissão. Sem PII no log (CA126).
            _logger.LogError(ex, "Falha ao registrar audit de emissão de recibo.");
        }
    }

    /// <summary>Baixa logo do estabelecimento com timeout curto; retorna null em qualquer falha.</summary>
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
        catch { return null; }
    }

    // ────────────────────────────────────────────────────────────────
    // Carregamento de dados — Dapper (leitura, sem EF)
    // ────────────────────────────────────────────────────────────────

    // internal virtual para override em testes de unidade sem banco.
    internal virtual async Task<ReciboPagamentoRow> CarregarDadosAsync(long pagamentoId, long estabelecimentoId)
    {
        const string sql = """
            SELECT
                p.id                                        AS PagamentoId,
                p.cobranca_id                               AS CobrancaId,
                c.paciente_id                               AS PacienteId,
                pa.nome_completo                            AS PacienteNome,
                p.valor                                     AS ValorPago,
                COALESCE(fp.nome, '')                       AS FormaPagamentoNome,
                p.parcelas                                  AS Parcelas,
                p.data_pagamento                            AS DataPagamento,
                COALESCE(u.nome_completo, u.email, '')      AS RegistradoPorNome,
                c.origem                                    AS CobrancaOrigem,
                c.descricao                                 AS CobrancaDescricao,
                e.nome_fantasia                             AS EstabelecimentoNomeFantasia,
                e.cnpj                                      AS EstabelecimentoCnpj,
                e.telefone                                  AS EstabelecimentoTelefone,
                e.endereco                                  AS EstabelecimentoEndereco,
                e.foto_url                                  AS EstabelecimentoFotoUrl
            FROM pagamentos p
            INNER JOIN cobrancas c        ON c.id = p.cobranca_id
            INNER JOIN estabelecimentos e ON e.id = c.estabelecimento_id
            LEFT JOIN pacientes pa         ON pa.id = c.paciente_id
            LEFT JOIN usuarios u           ON u.id = p.registrado_por_usuario_id
            LEFT JOIN formas_pagamento fp  ON fp.id = p.forma_pagamento_id
            WHERE p.id = @PagamentoId
              AND c.estabelecimento_id = @EstabelecimentoId
            """;

        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.QuerySingleOrDefaultAsync<ReciboPagamentoRow>(sql, new
        {
            PagamentoId = pagamentoId,
            EstabelecimentoId = estabelecimentoId
        });
    }

    // ────────────────────────────────────────────────────────────────
    // Geração do PDF — layout do recibo
    // ────────────────────────────────────────────────────────────────

    internal static byte[] GerarPdf(DadosReciboPdf dados)
    {
        var d = dados.Dados;

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0);
                page.DefaultTextStyle(x => x.FontFamily(Fonte).FontSize(9).FontColor(CorInk));
                page.PageColor(Colors.White);

                // Marca d'água discreta "IMEDTO" (mesma sutileza da receita emitida)
                page.Background().AlignCenter().AlignMiddle().Rotate(-25)
                    .Text("IMEDTO").FontSize(130).Bold()
                    .FontColor(CorInk).LineHeight(0.9f);

                page.Content()
                    .PaddingHorizontal(18, Unit.Millimetre)
                    .PaddingTop(14, Unit.Millimetre)
                    .PaddingBottom(22, Unit.Millimetre)
                    .Column(col =>
                    {
                        // Cabeçalho institucional
                        col.Item().Element(c => DesenharCabecalho(c, d, dados.LogoBytes));

                        // Título e rótulo sem valor fiscal
                        col.Item().PaddingTop(5, Unit.Millimetre).Element(c => DesenharTituloRecibo(c));

                        // Bloco do paciente
                        col.Item().PaddingTop(4, Unit.Millimetre).Element(c => DesenharBlocoPaciente(c, d));

                        // Bloco do pagamento
                        col.Item().PaddingTop(4, Unit.Millimetre).Element(c => DesenharBlocoPagamento(c, d));

                        // Referência da cobrança
                        col.Item().PaddingTop(4, Unit.Millimetre).Element(c => DesenharReferenciaCobranca(c, d));

                        // Registrado por
                        col.Item().PaddingTop(4, Unit.Millimetre)
                            .Text($"Registrado por: {d.RegistradoPorNome}")
                            .FontSize(8.5f).FontColor(CorSecondary);
                    });

                page.Footer().Element(c => DesenharRodape(c));
            });
        });

        return doc.GeneratePdf();
    }

    private static void DesenharCabecalho(IContainer container, ReciboPagamentoRow d, byte[] logoBytes)
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
                        .Element(c => DesenharLogo(c, d, logoBytes));

                    brand.RelativeItem().PaddingLeft(2, Unit.Millimetre).AlignMiddle()
                        .Text(d.EstabelecimentoNomeFantasia ?? "Estabelecimento")
                        .FontSize(15).Bold().FontColor(CorInkTitle);
                });

                // Direita: contato
                row.RelativeItem().AlignRight().Column(contato =>
                {
                    if (!string.IsNullOrWhiteSpace(d.EstabelecimentoEndereco))
                        contato.Item().Text(d.EstabelecimentoEndereco!).FontSize(8).FontColor(CorSecondary);
                    var tel = FormatarTelefone(d.EstabelecimentoTelefone);
                    if (tel is not null)
                        contato.Item().Text(tel).FontSize(8).FontColor(CorSecondary);
                    var cnpj = FormatarCnpj(d.EstabelecimentoCnpj);
                    if (cnpj is not null)
                        contato.Item().Text($"CNPJ {cnpj}").FontSize(8).FontColor(CorSecondary);
                });
            });

            col.Item().PaddingTop(2, Unit.Millimetre).LineHorizontal(0.3f).LineColor(CorBorder);
        });
    }

    private static void DesenharLogo(IContainer container, ReciboPagamentoRow d, byte[] logoBytes)
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
            catch { /* cai no placeholder */ }
        }

        var iniciais = ObterIniciais(d.EstabelecimentoNomeFantasia);
        container.AlignCenter().AlignMiddle()
            .Background(CorInkTitle)
            .Width(12, Unit.Millimetre).Height(12, Unit.Millimetre)
            .Layers(layers =>
            {
                layers.PrimaryLayer().AlignCenter().AlignMiddle().Text(iniciais)
                    .FontSize(7).Bold().FontColor(Colors.White);
            });
    }

    private static void DesenharTituloRecibo(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Text("RECIBO")
                    .FontSize(16).Bold().FontColor(CorInkTitle).LetterSpacing(0.6f);

                row.RelativeItem().AlignRight()
                    .Background("#FEF3C7").Border(0.4f).BorderColor("#D97706")
                    .PaddingHorizontal(4, Unit.Millimetre).PaddingVertical(1.5f, Unit.Millimetre)
                    .AlignCenter()
                    .Text("DOCUMENTO SEM VALOR FISCAL")
                    .FontSize(8).Bold().FontColor(CorSemFiscal);
            });

            col.Item().PaddingTop(1, Unit.Millimetre).LineHorizontal(0.6f).LineColor(CorInkTitle);
            col.Item().PaddingTop(0.3f, Unit.Millimetre).LineHorizontal(0.6f).LineColor(CorInkTitle);
        });
    }

    private static void DesenharBlocoPaciente(IContainer container, ReciboPagamentoRow d)
    {
        container.Background(CorCardBg).Border(0.3f).BorderColor(CorBorder)
            .Padding(3, Unit.Millimetre).Column(col =>
            {
                col.Item().Text("PACIENTE")
                    .FontSize(6.5f).SemiBold().FontColor(CorMuteLight).LetterSpacing(0.3f);
                col.Item().PaddingTop(1, Unit.Millimetre)
                    .Text(d.PacienteNome ?? "—")
                    .FontSize(11).Bold().FontColor(CorInk);
            });
    }

    private static void DesenharBlocoPagamento(IContainer container, ReciboPagamentoRow d)
    {
        container.Background(CorCardBg).Border(0.3f).BorderColor(CorBorder)
            .Padding(3, Unit.Millimetre).Column(col =>
            {
                col.Spacing(2);

                col.Item().Row(row =>
                {
                    row.RelativeItem().Element(c => DesenharCelula(c, "Valor pago",
                        d.ValorPago.ToString("C", CultureInfo.GetCultureInfo("pt-BR")),
                        destaque: true));
                    row.RelativeItem().Element(c => DesenharCelula(c, "Forma de pagamento",
                        d.FormaPagamentoNome));
                    row.RelativeItem().Element(c => DesenharCelula(c, "Parcelas",
                        d.Parcelas > 1 ? $"{d.Parcelas}x" : "À vista"));
                    row.RelativeItem().Element(c => DesenharCelula(c, "Data do pagamento",
                        d.DataPagamento.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("pt-BR"))));
                });
            });
    }

    private static void DesenharReferenciaCobranca(IContainer container, ReciboPagamentoRow d)
    {
        container.Column(col =>
        {
            col.Item().Text("REFERÊNCIA")
                .FontSize(7).SemiBold().FontColor(CorMuteLight).LetterSpacing(0.3f);

            col.Item().PaddingTop(0.8f, Unit.Millimetre).BorderTop(0.3f).BorderColor(CorBorderStrong)
                .PaddingTop(1, Unit.Millimetre).Column(ref2 =>
                {
                    ref2.Item().Text($"Origem: {d.CobrancaOrigem}").FontSize(9).FontColor(CorInk);
                    if (!string.IsNullOrWhiteSpace(d.CobrancaDescricao))
                    {
                        ref2.Item().PaddingTop(0.5f, Unit.Millimetre)
                            .Text($"Descrição: {d.CobrancaDescricao!}")
                            .FontSize(9).FontColor(CorInk);
                    }
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

    private static void DesenharRodape(IContainer container)
    {
        container.PaddingHorizontal(18, Unit.Millimetre).PaddingBottom(6, Unit.Millimetre)
            .Row(row =>
            {
                row.RelativeItem()
                    .Text("Este documento é um recibo operacional interno e não possui valor fiscal.")
                    .FontSize(7).Italic().FontColor(CorMuteLight);

                row.RelativeItem().AlignRight().Column(meta =>
                {
                    meta.Item().Text($"Emitido em {DateTime.Now:dd/MM/yyyy 'às' HH:mm}")
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

    // ── Helpers de formatação ──────────────────────────────────────────────────

    private static string FormatarTelefone(string tel)
    {
        if (string.IsNullOrWhiteSpace(tel)) return null;
        var d = new string(tel.Where(char.IsDigit).ToArray());
        if (d.Length == 11) return $"({d[..2]}) {d[2..7]}-{d[7..]}";
        if (d.Length == 10) return $"({d[..2]}) {d[2..6]}-{d[6..]}";
        return tel;
    }

    private static string FormatarCnpj(string cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj)) return null;
        // Preserva [A-Z0-9] para suportar CNPJ alfanumérico (IN RFB 2.229/2024).
        var d = new string(cnpj.ToUpperInvariant().Where(char.IsAsciiLetterOrDigit).ToArray());
        if (d.Length != 14) return cnpj;
        return $"{d[..2]}.{d[2..5]}.{d[5..8]}/{d[8..12]}-{d[12..]}";
    }

    private static string ObterIniciais(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome)) return "?";
        var partes = nome.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length == 1) return partes[0][..Math.Min(2, partes[0].Length)].ToUpperInvariant();
        return $"{partes[0][0]}{partes[^1][0]}".ToUpperInvariant();
    }
}
