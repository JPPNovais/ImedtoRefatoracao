using Imedto.Backend.Contracts.Vinculos.Queries;
using Imedto.Backend.Contracts.Vinculos.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Vinculos.Queries;

/// <summary>
/// Lista os profissionais (Dono + vínculos ativos/convidados) do estabelecimento.
/// Acesso liberado a qualquer membro ativo do tenant — o gate de "Apenas Dono"
/// foi removido porque a Agenda/criação de agendamento precisa do seletor de
/// profissional (qualquer membro do tenant precisa ver com quem agenda).
///
/// Defesa multi-tenant: o controller usa <c>[RequiresEstabelecimento]</c> que
/// só deixa passar usuários com vínculo ativo OU dono nesse estabelecimento.
/// Operações de ESCRITA (convidar, inativar, trocar modelo) continuam Dono-only
/// nos seus respectivos handlers.
/// </summary>
public class ListarProfissionaisEstabelecimentoQueryHandlers
    : IRequestHandler<ListarProfissionaisEstabelecimentoQuery, IEnumerable<ProfissionalVinculadoDto>>
{
    private readonly VinculoQueryRepository _queryRepository;

    public ListarProfissionaisEstabelecimentoQueryHandlers(VinculoQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public Task<IEnumerable<ProfissionalVinculadoDto>> Handle(ListarProfissionaisEstabelecimentoQuery query) =>
        _queryRepository.ListarProfissionaisDoEstabelecimento(query.EstabelecimentoId);
}
