using Imedto.Backend.Contracts.PacienteConvenios.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.PacienteConvenios.Queries;

public class ListarPacienteConveniosQuery : IQuery<IReadOnlyList<PacienteConvenioDto>>
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid UsuarioSolicitanteId { get; set; }
}

/// <summary>
/// Retorna carteirinhas ativas do paciente para pré-seleção no check-in (R8).
/// Não grava audit (é um helper do check-in, sem acesso a prontuário).
/// </summary>
public class ObterCarteirinhaAtivaCheckInQuery : IQuery<IReadOnlyList<CarteirinhaCheckInDto>>
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
}
