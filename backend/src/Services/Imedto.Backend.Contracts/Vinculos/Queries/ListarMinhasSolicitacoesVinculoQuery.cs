using Imedto.Backend.Contracts.Vinculos.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Vinculos.Queries;

/// <summary>Solicitações enviadas pelo profissional logado.</summary>
public class ListarMinhasSolicitacoesVinculoQuery : IQuery<IEnumerable<SolicitacaoVinculoDto>>
{
    public Guid ProfissionalUsuarioId { get; set; }
}
