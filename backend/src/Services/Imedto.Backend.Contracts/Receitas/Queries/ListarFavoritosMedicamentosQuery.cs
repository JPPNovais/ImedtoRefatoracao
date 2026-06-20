using Imedto.Backend.Contracts.Receitas.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Receitas.Queries;

/// <summary>
/// Lista os medicamentos favoritos do profissional autenticado no estabelecimento ativo,
/// ordenados por frequência de uso (mais usados primeiro).
/// Multi-tenant: filtra por (profissional_usuario_id, estabelecimento_id).
/// </summary>
public class ListarFavoritosMedicamentosQuery : IQuery<IEnumerable<MedicamentoFavoritoDto>>
{
    public Guid ProfissionalUsuarioId { get; set; }
    public long EstabelecimentoId { get; set; }
    /// <summary>Máximo de favoritos retornados. Default 50 — suficiente para autocomplete móvel.</summary>
    public int Limite { get; set; } = 50;
}
