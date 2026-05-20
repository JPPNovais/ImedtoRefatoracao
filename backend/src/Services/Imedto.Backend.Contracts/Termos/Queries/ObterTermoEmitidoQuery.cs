using Imedto.Backend.Contracts.Termos.Dtos;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Termos.Queries;

public class ObterTermoEmitidoQuery : IQuery<TermoEmitidoDetalheDto>
{
    public long PacienteId { get; set; }
    public long TermoEmitidoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
}
