using Dapper;
using Imedto.Backend.Application.Orcamentos.Commands;
using Imedto.Backend.Contracts.Orcamentos.Queries;
using Imedto.Backend.Contracts.Orcamentos.Queries.Results;
using Imedto.Backend.Domain.Orcamentos;
using Imedto.Backend.Domain.Orcamentos.Calculos;
using Imedto.Backend.Domain.Orcamentos.Catalogos;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Npgsql;

namespace Imedto.Backend.Application.Orcamentos.Queries;

/// <summary>
/// Espelha em servidor o cálculo do form em construção. Não persiste — só lê
/// nomes de formas de pagamento (catálogo) para enriquecer o DTO de retorno
/// e, quando solicitado, recalcula honorário de equipe e local cirúrgico
/// usando as tabelas do catálogo.
/// </summary>
public class PreviewOrcamentoQueryHandler : IRequestHandler<PreviewOrcamentoQuery, PreviewOrcamentoDto>
{
    private readonly string _connStr;

    public PreviewOrcamentoQueryHandler(AppReadConnectionString conn) => _connStr = conn.Value;

    public async Task<PreviewOrcamentoDto> Handle(PreviewOrcamentoQuery q)
    {
        var totalItens = q.Itens.Sum(i =>
        {
            var bruto = i.Quantidade * i.ValorUnitario;
            var desconto = bruto * (i.DescontoPercent / 100m);
            return Math.Round(bruto - desconto, 2);
        });
        var totalCirurgias = q.Cirurgias.Sum(c => c.ValorTotal);
        var totalEquipe = q.Equipe.Sum(e => e.Valor);
        var totalImplantes = q.Implantes.Sum(i => Math.Round(i.Quantidade * i.CustoUnitario, 2));
        var totalAnestesia = q.Anestesia?.Valor ?? 0m;

        // ── Equipes com catálogo (cálculo server-side por tempo)
        var equipesCalculadas = new List<EquipeCalculadaDto>();
        if (q.EquipeComCatalogo.Count > 0)
        {
            var ids = q.EquipeComCatalogo.Select(e => e.ValorProfissionalId).Distinct().ToArray();
            var valores = await CarregarValoresProfissionais(ids, q.EstabelecimentoId);
            foreach (var item in q.EquipeComCatalogo)
            {
                if (!valores.TryGetValue(item.ValorProfissionalId, out var vp)) continue;
                var unit = OrcamentoCalculadora.CalcularValorProfissional(
                    item.TempoMinutos, vp.TempoBaseMinutos, vp.ValorTempoBase,
                    vp.TempoAdicionalMinutos, vp.ValorAdicional, vp.ValorPlus);
                var qtd = Math.Max(1, item.Quantidade);
                equipesCalculadas.Add(new EquipeCalculadaDto
                {
                    ValorProfissionalId = item.ValorProfissionalId,
                    TempoMinutos = item.TempoMinutos,
                    Quantidade = qtd,
                    ValorUnitario = unit,
                    ValorTotal = Math.Round(unit * qtd, 2),
                });
            }
            // Quando recebido EquipeComCatalogo, somamos por aqui em vez de pela lista crua.
            totalEquipe = equipesCalculadas.Sum(e => e.ValorTotal);
        }

        // ── Local cirúrgico
        var totalLocal = 0m;
        if (q.LocalCirurgia is { } loc)
        {
            var tipo = OrcamentoMapping.ParseTipoLocal(loc.Tipo);
            var config = await CarregarConfigLocal(q.EstabelecimentoId, tipo);
            // Preview tolera config ausente — devolve 0 e o front exibe "configure em Orçamento → Configurações".
            totalLocal = OrcamentoCalculadora.CalcularValorLocal(tipo, loc.TempoMinutos, config);
        }

        var totalGeral = totalItens + totalCirurgias + totalEquipe
                       + totalImplantes + totalLocal + totalAnestesia;
        var somaFormas = q.FormasPagamento.Sum(f => f.Valor);
        var diferenca = Math.Round(totalGeral - somaFormas, 2);
        var integridadeOk = q.FormasPagamento.Count == 0 || Math.Abs(diferenca) < 0.01m;

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
            TotalEquipe = Math.Round(totalEquipe, 2),
            TotalImplantes = totalImplantes,
            TotalLocal = totalLocal,
            TotalAnestesia = totalAnestesia,
            TotalGeral = Math.Round(totalGeral, 2),
            SomaFormas = Math.Round(somaFormas, 2),
            Diferenca = diferenca,
            IntegridadeOk = integridadeOk,
            Formas = formasDetalhadas,
            Equipes = equipesCalculadas,
        };
    }

    private async Task<Dictionary<long, string>> CarregarNomesFormas(List<long> ids, long estabelecimentoId)
    {
        if (ids.Count == 0) return new Dictionary<long, string>();
        await using var conn = new NpgsqlConnection(_connStr);
        var rows = await conn.QueryAsync<(long Id, string Nome)>(
            "SELECT id, nome FROM formas_pagamento WHERE id = ANY(@Ids) AND estabelecimento_id = @Estab",
            new { Ids = ids.ToArray(), Estab = estabelecimentoId });
        return rows.ToDictionary(r => r.Id, r => r.Nome);
    }

    private record ValorProfRow(long Id, int TempoBaseMinutos, decimal ValorTempoBase,
        int TempoAdicionalMinutos, decimal ValorAdicional, decimal ValorPlus);

    private async Task<Dictionary<long, ValorProfRow>> CarregarValoresProfissionais(long[] ids, long estabelecimentoId)
    {
        if (ids.Length == 0) return new();
        await using var conn = new NpgsqlConnection(_connStr);
        var rows = await conn.QueryAsync<ValorProfRow>(
            """
            SELECT id, tempo_base_minutos AS TempoBaseMinutos, valor_tempo_base AS ValorTempoBase,
                   tempo_adicional_minutos AS TempoAdicionalMinutos, valor_adicional AS ValorAdicional,
                   valor_plus AS ValorPlus
            FROM orcamento_valor_profissional
            WHERE id = ANY(@Ids) AND estabelecimento_id = @Estab AND ativo = true
            """,
            new { Ids = ids, Estab = estabelecimentoId });
        return rows.ToDictionary(r => r.Id);
    }

    private async Task<ConfiguracaoLocalCirurgia?> CarregarConfigLocal(long estabelecimentoId, TipoLocalCirurgia tipo)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        var row = await conn.QuerySingleOrDefaultAsync<(int TempoBaseMinutos, decimal ValorBase,
            int TempoAdicionalMinutos, decimal ValorAdicional)?>(
            """
            SELECT tempo_base_minutos AS TempoBaseMinutos, valor_base AS ValorBase,
                   tempo_adicional_minutos AS TempoAdicionalMinutos, valor_adicional AS ValorAdicional
            FROM orcamento_configuracao_local_cirurgia
            WHERE estabelecimento_id = @Estab AND tipo_local = @Tipo
            """,
            new { Estab = estabelecimentoId, Tipo = tipo.ToString() });
        if (row is null) return null;
        // Construir uma instância via fábrica para usar nos cálculos. O preview não precisa
        // persistir, então qualquer falha de validação aqui significaria dados ruins no banco.
        try
        {
            return ConfiguracaoLocalCirurgia.Criar(estabelecimentoId, tipo,
                row.Value.TempoBaseMinutos, row.Value.ValorBase,
                Math.Max(1, row.Value.TempoAdicionalMinutos), row.Value.ValorAdicional);
        }
        catch (BusinessException)
        {
            return null;
        }
    }
}
