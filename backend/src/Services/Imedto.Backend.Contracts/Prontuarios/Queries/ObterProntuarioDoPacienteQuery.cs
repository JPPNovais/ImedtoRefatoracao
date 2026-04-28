using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Prontuarios.Queries;

/// <summary>
/// Retorna o prontuário do paciente + timeline de evoluções.
/// <c>null</c> se o prontuário ainda não foi iniciado.
/// </summary>
public class ObterProntuarioDoPacienteQuery : IQuery<ProntuarioCompletoDto>
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    public int TamanhoTimeline { get; set; } = 50;
}
