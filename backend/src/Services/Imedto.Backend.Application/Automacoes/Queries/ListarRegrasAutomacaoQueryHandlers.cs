using Imedto.Backend.Contracts.Automacoes.Queries;
using Imedto.Backend.Domain.Automacoes;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Automacoes.Queries;

/// <summary>
/// Listagens leves — usam diretamente os repositórios de escrita (read-only via AsNoTracking)
/// porque a quantidade é pequena (regras por estabelecimento ≤ dezenas) e os DTOs são quase 1:1
/// com o aggregate. Para volumes maiores, mover para Dapper.
/// </summary>
public class ListarRegrasAutomacaoQueryHandlers : IRequestHandler<ListarRegrasAutomacaoQuery, IEnumerable<RegraAutomacaoDto>>
{
    private readonly IRegraAutomacaoRepository _repo;

    public ListarRegrasAutomacaoQueryHandlers(IRegraAutomacaoRepository repo) => _repo = repo;

    public async Task<IEnumerable<RegraAutomacaoDto>> Handle(ListarRegrasAutomacaoQuery query)
    {
        var regras = await _repo.ListarPorEstabelecimento(query.EstabelecimentoId);
        return regras.Select(r => new RegraAutomacaoDto
        {
            Id = r.Id,
            EstabelecimentoId = r.EstabelecimentoId,
            Nome = r.Nome,
            EventoGatilho = r.EventoGatilho,
            CondicoesJson = r.CondicoesJson,
            AcoesJson = r.AcoesJson,
            Ativa = r.Ativa,
            CriadoEm = r.CriadoEm,
            AtualizadoEm = r.AtualizadoEm
        });
    }
}

public class ListarEventosAutomacaoQueryHandlers : IRequestHandler<ListarEventosAutomacaoQuery, IEnumerable<EventoAutomacaoDto>>
{
    private readonly IEventoAutomacaoRepository _repo;

    public ListarEventosAutomacaoQueryHandlers(IEventoAutomacaoRepository repo) => _repo = repo;

    public async Task<IEnumerable<EventoAutomacaoDto>> Handle(ListarEventosAutomacaoQuery query)
    {
        var eventos = await _repo.ListarParaDebug(query.EstabelecimentoId, query.Status, query.Pagina, query.Tamanho);
        return eventos.Select(e => new EventoAutomacaoDto
        {
            Id = e.Id,
            RegraId = e.RegraId,
            Status = e.Status.ToString(),
            TentativaN = e.TentativaN,
            ExecutarEm = e.ExecutarEm,
            ExecutadoEm = e.ExecutadoEm,
            UltimaFalha = e.UltimaFalha,
            CriadoEm = e.CriadoEm
        });
    }
}
