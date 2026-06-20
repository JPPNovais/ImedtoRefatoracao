using Imedto.Backend.Contracts.Pacientes.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Pacientes.Queries;

public class ObterPacienteQuery : IQuery<PacienteDto>
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    /// <summary>Audit LGPD: registrar quem leu os dados do paciente.</summary>
    public Guid SolicitanteUsuarioId { get; set; }
    /// <summary>
    /// Quando <c>true</c>, CPF e telefone retornam mascarados no DTO (ex.: "•••.•••.•••-09").
    /// Default <c>false</c> — comportamento existente inalterado (web usa o completo).
    /// </summary>
    public bool MascararContato { get; set; }
}
