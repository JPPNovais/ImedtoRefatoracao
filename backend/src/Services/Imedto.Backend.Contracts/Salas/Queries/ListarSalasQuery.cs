using Imedto.Backend.Contracts.Salas.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Salas.Queries;

public class ListarSalasQuery : IQuery<IEnumerable<SalaDto>>
{
    public long EstabelecimentoId { get; set; }
    public Guid UsuarioSolicitanteId { get; set; }
    /// <summary>Quando true, retorna apenas salas com <c>ativo=true</c>.</summary>
    public bool ApenasAtivas { get; set; }
}
