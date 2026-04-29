using Imedto.Backend.Contracts.Receitas.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Receitas.Queries;

public class ListarMedicamentosFavoritosQuery : IQuery<IEnumerable<MedicamentoFavoritoDto>>
{
    public Guid ProfissionalUsuarioId { get; set; }
    public long EstabelecimentoId { get; set; }
    public int Top { get; set; } = 20;
}
