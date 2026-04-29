using Dapper;
using Npgsql;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Imedto.Backend.Infrastructure.Receitas;

/// <summary>
/// Contrato do gerador de PDF de receitas.
/// </summary>
public interface IReceitaPdfService
{
    /// <summary>Gera o PDF da receita. Lança <see cref="SharedKernel.Domain.BusinessException"/> se não encontrada.</summary>
    Task<byte[]> GerarAsync(long receitaId, long estabelecimentoId);
}

/// <summary>
/// Geração real de PDF da receita usando QuestPDF.
///
/// Licença QuestPDF Community (gratuita para organizações com receita anual &lt; USD 1M/ano
/// ou para uso acadêmico/OSS). Checar https://www.questpdf.com/license.html antes de
/// usar em produção se a receita do cliente ultrapassar o limite.
///
/// Estrutura do PDF:
/// - Cabeçalho: texto simples extraído de <c>CabecalhoHtml</c> (HTML stripped).
/// - Bloco de identificação: tipo de receita, notificação, paciente, profissional, datas.
/// - Lista numerada de itens (medicamento, posologia, concentração, forma, via, duração, observação).
/// - Rodapé: texto de <c>RodapeHtml</c> (stripped) + linha de assinatura com nome e CRM/CRO.
/// - Marca d'água "RASCUNHO" ou "CANCELADA" conforme status.
///
/// LGPD: nenhum campo de PII do paciente ou nome de medicamento é logado.
/// </summary>
public class QuestPdfReceitaService : IReceitaPdfService
{
    private readonly string _connStr;

    public QuestPdfReceitaService(AppReadConnectionString conn)
    {
        _connStr = conn.Value;

        // Configura licença Community uma única vez.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GerarAsync(long receitaId, long estabelecimentoId)
    {
        var dados = await CarregarDadosAsync(receitaId, estabelecimentoId);
        if (dados is null)
            throw new SharedKernel.Domain.BusinessException("Receita não encontrada.");

        return GerarPdf(dados);
    }

    // ────────────────────────────────────────────────────────────────
    // Carregamento de dados (Dapper — sem EF para não acionar interceptores)
    // ────────────────────────────────────────────────────────────────

    private async Task<DadosPdf?> CarregarDadosAsync(long receitaId, long estabelecimentoId)
    {
        const string sqlReceita = """
            SELECT  r.id                    AS Id,
                    r.tipo                  AS Tipo,
                    r.tipo_notificacao      AS TipoNotificacao,
                    r.status                AS Status,
                    r.emitida_em            AS EmitidaEm,
                    r.validade_ate          AS ValidadeAte,
                    r.observacoes           AS Observacoes,
                    pa.nome_completo        AS PacienteNome,
                    u.nome_completo         AS ProfissionalNome,
                    CASE WHEN pr.conselho IS NOT NULL
                         THEN pr.conselho || ' ' || pr.uf || ' ' || pr.numero_registro
                         ELSE NULL END      AS ProfissionalCrmCro,
                    c.cabecalho_html        AS CabecalhoHtml,
                    c.rodape_html           AS RodapeHtml,
                    c.emissor_padrao        AS EmissorPadrao
            FROM    public.receitas r
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
    // Geração do PDF
    // ────────────────────────────────────────────────────────────────

    private static byte[] GerarPdf(DadosPdf dados)
    {
        var r = dados.Receita;
        var itens = dados.Itens;

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                // ── Cabeçalho ──────────────────────────────────────────
                page.Header().Column(col =>
                {
                    var cabecalho = StripHtml(r.CabecalhoHtml);
                    if (!string.IsNullOrWhiteSpace(cabecalho))
                    {
                        col.Item().Text(cabecalho).FontSize(9).FontColor(Colors.Grey.Darken2);
                        col.Item().PaddingBottom(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                    }

                    col.Item().PaddingTop(4).Text(TipoLabel(r.Tipo, r.TipoNotificacao))
                        .FontSize(14).Bold().AlignCenter();
                });

                // ── Conteúdo principal ──────────────────────────────────
                page.Content().PaddingTop(12).Column(col =>
                {
                    // Bloco de identificação
                    col.Item().Table(tab =>
                    {
                        tab.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                        });

                        CelulaInfo(tab, "Paciente", r.PacienteNome ?? "—");
                        CelulaInfo(tab, "Profissional", NomeProfissional(r));

                        var emitidaEm = r.EmitidaEm?.ToString("dd/MM/yyyy") ?? "Rascunho";
                        CelulaInfo(tab, "Data de emissão", emitidaEm);

                        var validade = r.ValidadeAte?.ToString("dd/MM/yyyy") ?? "—";
                        CelulaInfo(tab, "Válida até", validade);
                    });

                    col.Item().PaddingTop(16).PaddingBottom(4)
                        .Text("Medicamentos prescritos:").Bold();

                    // Lista numerada de itens
                    for (var idx = 0; idx < itens.Count; idx++)
                    {
                        var item = itens[idx];
                        col.Item().PaddingBottom(8).Column(itemCol =>
                        {
                            // Cabeçalho do item (número + medicamento)
                            var cabecalhoItem = $"{idx + 1}. {item.Medicamento}";
                            if (!string.IsNullOrWhiteSpace(item.Concentracao))
                                cabecalhoItem += $" {item.Concentracao}";
                            if (!string.IsNullOrWhiteSpace(item.FormaFarmaceutica))
                                cabecalhoItem += $" — {item.FormaFarmaceutica}";

                            itemCol.Item().Text(cabecalhoItem).Bold();
                            itemCol.Item().PaddingLeft(12).Text($"Posologia: {item.Posologia}");

                            if (!string.IsNullOrWhiteSpace(item.Via))
                                itemCol.Item().PaddingLeft(12).Text($"Via: {item.Via}");
                            if (!string.IsNullOrWhiteSpace(item.Quantidade))
                                itemCol.Item().PaddingLeft(12).Text($"Quantidade: {item.Quantidade}");
                            if (!string.IsNullOrWhiteSpace(item.Duracao))
                                itemCol.Item().PaddingLeft(12).Text($"Duração: {item.Duracao}");
                            if (!string.IsNullOrWhiteSpace(item.Observacao))
                                itemCol.Item().PaddingLeft(12).Text($"Observação: {item.Observacao}")
                                    .FontColor(Colors.Grey.Darken2);
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(r.Observacoes))
                    {
                        col.Item().PaddingTop(8).Text("Observações gerais:").Bold();
                        col.Item().Text(r.Observacoes).FontColor(Colors.Grey.Darken2);
                    }
                });

                // ── Rodapé ──────────────────────────────────────────────
                page.Footer().Column(col =>
                {
                    col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                    col.Item().PaddingTop(4).Row(row =>
                    {
                        var rodape = StripHtml(r.RodapeHtml);
                        if (!string.IsNullOrWhiteSpace(rodape))
                            row.RelativeItem().Text(rodape).FontSize(8).FontColor(Colors.Grey.Darken2);

                        row.RelativeItem().AlignRight().Column(assColl =>
                        {
                            assColl.Item().PaddingBottom(24).Text(string.Empty); // espaço p/ assinatura
                            assColl.Item().LineHorizontal(0.5f);
                            assColl.Item().Text(NomeProfissional(r)).AlignCenter().FontSize(9);
                        });
                    });
                });

                // ── Marca d'água ────────────────────────────────────────
                if (r.Status == "Rascunho")
                    page.Foreground().AlignCenter().AlignMiddle().Text("RASCUNHO")
                        .FontSize(72).Bold().FontColor(Colors.Grey.Lighten2).Italic();
                else if (r.Status == "Cancelada")
                    page.Foreground().AlignCenter().AlignMiddle().Text("CANCELADA")
                        .FontSize(72).Bold().FontColor(Colors.Red.Lighten3).Italic();
            });
        });

        return doc.GeneratePdf();
    }

    // ────────────────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────────────────

    private static void CelulaInfo(TableDescriptor tab, string label, string valor)
    {
        tab.Cell().PaddingBottom(4).Column(c =>
        {
            c.Item().Text(label).Bold().FontSize(8).FontColor(Colors.Grey.Darken1);
            c.Item().Text(valor).FontSize(10);
        });
    }

    private static string NomeProfissional(ReceitaRow r)
    {
        var nome = r.EmissorPadrao ?? r.ProfissionalNome ?? "Profissional";
        if (!string.IsNullOrWhiteSpace(r.ProfissionalCrmCro))
            nome += $" ({r.ProfissionalCrmCro})";
        return nome;
    }

    private static string TipoLabel(string tipo, string? tipoNotificacao)
    {
        return tipo switch
        {
            "Comum"       => "RECEITUÁRIO COMUM",
            "Controlada"  => $"RECEITUÁRIO CONTROLADO — NOTIFICAÇÃO {tipoNotificacao ?? ""}".Trim(),
            "Antibiotico" => "RECEITUÁRIO ANTIBIÓTICO",
            "Especial"    => "RECEITUÁRIO ESPECIAL",
            _             => $"RECEITUÁRIO — {tipo.ToUpperInvariant()}"
        };
    }

    /// <summary>
    /// Remove tags HTML para uso em áreas de texto simples do PDF.
    /// Não é parser HTML completo — suficiente para os campos de cabeçalho/rodapé do legado.
    /// </summary>
    private static string? StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return null;
        return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ")
            .Replace("&nbsp;", " ").Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">")
            .Trim();
    }

    // ────────────────────────────────────────────────────────────────
    // DTOs internos (leitura Dapper)
    // ────────────────────────────────────────────────────────────────

    private sealed record ReceitaRow(
        long Id,
        string Tipo,
        string? TipoNotificacao,
        string Status,
        DateTime? EmitidaEm,
        DateTime? ValidadeAte,
        string? Observacoes,
        string? PacienteNome,
        string? ProfissionalNome,
        string? ProfissionalCrmCro,
        string? CabecalhoHtml,
        string? RodapeHtml,
        string? EmissorPadrao);

    private sealed record ItemRow(
        int Ordem,
        string Medicamento,
        string Posologia,
        string? Concentracao,
        string? FormaFarmaceutica,
        string? Via,
        string? Quantidade,
        string? Duracao,
        string? Observacao);

    private sealed record DadosPdf(ReceitaRow Receita, List<ItemRow> Itens);
}
