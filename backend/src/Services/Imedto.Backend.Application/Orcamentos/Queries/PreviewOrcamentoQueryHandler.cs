using Dapper;
using Imedto.Backend.Contracts.Orcamentos.Queries;
using Imedto.Backend.Contracts.Orcamentos.Queries.Results;
using Imedto.Backend.Domain.Orcamentos.Calculos;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.SharedKernel.Cqrs;
using Npgsql;

namespace Imedto.Backend.Application.Orcamentos.Queries;

/// <summary>
/// Espelha em servidor o cálculo do form em construção. Não persiste — só lê
/// nomes de formas de pagamento (catálogo) para enriquecer o DTO de retorno.
/// </summary>
public class PreviewOrcamentoQueryHandler : IRequestHandler<PreviewOrcamentoQuery, PreviewOrcamentoDto>
{
    private readonly string _connStr;

    public PreviewOrcamentoQueryHandler(AppReadConnectionString conn) => _connStr = conn.Value;

    public async Task<PreviewOrcamentoDto> Handle(PreviewOrcamentoQuery q)
    {
        // Subtotais individuais — espelham Orcamento.Total no domain.
        var totalItens = q.Itens.Sum(i =>
        {
            var bruto = i.Quantidade * i.ValorUnitario;
            var desconto = bruto * (i.DescontoPercent / 100m);
            return Math.Round(bruto - desconto, 2);
        });
        var totalCirurgias = q.Cirurgias.Sum(c => c.ValorTotal);
        var totalEquipe = q.Equipe.Sum(e => e.Valor);
        var totalImplantes = q.Implantes.Sum(i => Math.Round(i.Quantidade * i.CustoUnitario, 2));
        var totalInternacao = q.Internacao is null
            ? 0m
            : Math.Round(q.Internacao.Dias * q.Internacao.ValorDiaria, 2);
        var totalAnestesia = q.Anestesia?.Valor ?? 0m;

        var totalGeral = totalItens + totalCirurgias + totalEquipe
                       + totalImplantes + totalInternacao + totalAnestesia;
        var somaFormas = q.FormasPagamento.Sum(f => f.Valor);
        var diferenca = Math.Round(totalGeral - somaFormas, 2);
        var integridadeOk = q.FormasPagamento.Count == 0 || Math.Abs(diferenca) < 0.01m;

        // Detalhamento das formas (com acréscimo/entrada/parcela calculados).
        var nomesFormas = await CarregarNomesFormas(
            q.FormasPagamento.Select(f => f.FormaPagamentoId).Distinct().ToList(),
            q.EstabelecimentoId);

        var formasDetalhadas = q.FormasPagamento.Select(f =>
        {
            var calc = OrcamentoCalculadora.CalcularFormaPagamento(
                f.Valor, f.AcrescimoPercentual, f.EntradaPercentual, f.Parcelas);
            return new FormaPagamentoCalculadaDto
            {
                FormaPagamentoId = f.FormaPagamentoId,
                FormaPagamentoNome = nomesFormas.GetValueOrDefault(f.FormaPagamentoId),
                Valor = f.Valor,
                Parcelas = f.Parcelas,
                AcrescimoPercentual = f.AcrescimoPercentual,
                EntradaPercentual = f.EntradaPercentual,
                TotalBruto = calc.TotalBruto,
                Entrada = calc.Entrada,
                ValorParcela = calc.ValorParcela,
            };
        }).ToList();

        return new PreviewOrcamentoDto
        {
            TotalItens = totalItens,
            TotalCirurgias = totalCirurgias,
            TotalEquipe = totalEquipe,
            TotalImplantes = totalImplantes,
            TotalInternacao = totalInternacao,
            TotalAnestesia = totalAnestesia,
            TotalGeral = Math.Round(totalGeral, 2),
            SomaFormas = Math.Round(somaFormas, 2),
            Diferenca = diferenca,
            IntegridadeOk = integridadeOk,
            Formas = formasDetalhadas,
        };
    }

    private async Task<Dictionary<long, string>> CarregarNomesFormas(List<long> ids, long estabelecimentoId)
    {
        if (ids.Count == 0) return new Dictionary<long, string>();
        await using var conn = new NpgsqlConnection(_connStr);
        // Filtra pelo estabelecimento_id também — defesa em profundidade.
        var rows = await conn.QueryAsync<(long Id, string Nome)>(
            "SELECT id, nome FROM formas_pagamento WHERE id = ANY(@Ids) AND estabelecimento_id = @Estab",
            new { Ids = ids.ToArray(), Estab = estabelecimentoId });
        return rows.ToDictionary(r => r.Id, r => r.Nome);
    }
}
