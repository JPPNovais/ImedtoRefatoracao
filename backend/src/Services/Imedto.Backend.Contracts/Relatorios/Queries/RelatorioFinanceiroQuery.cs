using Imedto.Backend.Contracts.Relatorios.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Relatorios.Queries;

/// <summary>
/// Relatório financeiro consolidado. <see cref="AgruparPor"/> aceita
/// <c>dia</c>, <c>semana</c>, <c>mes</c>, <c>categoria</c> ou <c>forma_pagamento</c>.
/// Substitui rpc_report_cash_flow, rpc_report_financial_summary e
/// rpc_report_financial_by_category.
/// </summary>
public class RelatorioFinanceiroQuery : IQuery<RelatorioFinanceiroDto>
{
    public long EstabelecimentoId { get; set; }
    public DateOnly DataInicio { get; set; }
    public DateOnly DataFim { get; set; }
    public string AgruparPor { get; set; } = "dia";
    /// <summary>F7/R20 — quando true, inclui a visão por paciente (custo/lucro). Default false para retrocompatibilidade.</summary>
    public bool IncluirPorPaciente { get; set; } = false;
}
