using Imedto.Backend.Contracts.Relatorios.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Relatorios.Queries;

/// <summary>
/// Relatório de pessoas. <see cref="Tipo"/> aceita <c>pacientes</c> ou
/// <c>profissionais</c>. Substitui rpc_report_patients_summary e
/// rpc_report_professionals_performance.
/// </summary>
public class RelatorioPessoasQuery : IQuery<RelatorioPessoasDto>
{
    public long EstabelecimentoId { get; set; }
    public DateOnly DataInicio { get; set; }
    public DateOnly DataFim { get; set; }
    public string Tipo { get; set; } = "pacientes";
}
