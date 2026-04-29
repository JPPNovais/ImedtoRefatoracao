using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Assinaturas.Queries;

/// <summary>Lista os planos ativos do catálogo (uso pelo seletor de plano no frontend).</summary>
public class ListarPlanosQuery : IQuery<IEnumerable<PlanoDto>>
{
}
