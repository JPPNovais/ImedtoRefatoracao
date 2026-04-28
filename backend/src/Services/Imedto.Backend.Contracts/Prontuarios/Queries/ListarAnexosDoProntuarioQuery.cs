using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Prontuarios.Queries;

public class ListarAnexosDoProntuarioQuery : IQuery<IEnumerable<AnexoDto>>
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public long? EvolucaoId { get; set; } // opcional: só os da evolução
}

public class ObterUrlAnexoQuery : IQuery<AnexoUrlDto>
{
    public long AnexoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    public int TtlSegundos { get; set; } = 300;
}
