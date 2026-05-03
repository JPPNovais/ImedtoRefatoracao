using Imedto.Backend.Contracts.Pacientes.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Pacientes.Queries;

public class ObterPacienteQuery : IQuery<PacienteDto>
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    /// <summary>Audit LGPD: registrar quem leu os dados do paciente.</summary>
    public Guid SolicitanteUsuarioId { get; set; }
}
