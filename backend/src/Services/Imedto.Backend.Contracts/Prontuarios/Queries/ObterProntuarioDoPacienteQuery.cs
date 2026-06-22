using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.Contracts.Prontuarios.Queries;

/// <summary>
/// Retorna o prontuário do paciente + timeline de evoluções.
/// <c>null</c> se o prontuário ainda não foi iniciado.
/// O campo <c>Alertas</c> do <see cref="ProntuarioCompletoDto"/> retorna populado
/// apenas para quem tem direito de leitura (R2 LGPD): Dono ou Profissional que
/// atendeu/está atendendo o paciente.
/// </summary>
public class ObterProntuarioDoPacienteQuery : IQuery<ProntuarioCompletoDto>
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    public TenantPapel SolicitantePapel { get; set; }
    public int TamanhoTimeline { get; set; } = 50;
}
