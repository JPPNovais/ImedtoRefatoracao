using Imedto.Backend.Contracts.Termos.Dtos;
using Imedto.Backend.Contracts.Termos.Queries;
using Imedto.Backend.Domain.Termos;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Termos.Queries;

public sealed class ListarModelosTermoQueryHandlers
    : IRequestHandler<ListarModelosTermoQuery, PaginaModelosTermoDto>
{
    private readonly ITermoModeloQueryRepository _repo;

    public ListarModelosTermoQueryHandlers(ITermoModeloQueryRepository repo) => _repo = repo;

    public Task<PaginaModelosTermoDto> Handle(ListarModelosTermoQuery q) =>
        _repo.Listar(q.EstabelecimentoId, q.Busca, q.Categoria, q.SomenteAtivos, q.IncluirPadroes, q.Pagina, q.Tamanho);
}

public sealed class ListarModelosPadraoTermoQueryHandlers
    : IRequestHandler<ListarModelosPadraoTermoQuery, IReadOnlyList<TermoModeloDto>>
{
    private readonly ITermoModeloQueryRepository _repo;

    public ListarModelosPadraoTermoQueryHandlers(ITermoModeloQueryRepository repo) => _repo = repo;

    public Task<IReadOnlyList<TermoModeloDto>> Handle(ListarModelosPadraoTermoQuery q) => _repo.ListarPadroes();
}

public sealed class ObterModeloTermoQueryHandlers : IRequestHandler<ObterModeloTermoQuery, TermoModeloDto>
{
    private readonly ITermoModeloQueryRepository _repo;

    public ObterModeloTermoQueryHandlers(ITermoModeloQueryRepository repo) => _repo = repo;

    public async Task<TermoModeloDto> Handle(ObterModeloTermoQuery q)
    {
        var dto = await _repo.ObterPorIdDoEstabelecimentoOuPadrao(q.ModeloId, q.EstabelecimentoId)
            ?? throw new BusinessException("Modelo não encontrado.");
        return dto;
    }
}

public sealed class ListarVariaveisDisponiveisQueryHandlers
    : IRequestHandler<ListarVariaveisDisponiveisQuery, IReadOnlyList<VariavelDisponivelDto>>
{
    private readonly ITermoResolverDeVariaveis _resolver;

    public ListarVariaveisDisponiveisQueryHandlers(ITermoResolverDeVariaveis resolver) => _resolver = resolver;

    public Task<IReadOnlyList<VariavelDisponivelDto>> Handle(ListarVariaveisDisponiveisQuery q)
    {
        IReadOnlyList<VariavelDisponivelDto> itens = _resolver.VariaveisDisponiveis
            .Select(v => new VariavelDisponivelDto { Chave = v.Chave, Rotulo = v.Rotulo, Categoria = v.Categoria })
            .ToList();
        return Task.FromResult(itens);
    }
}
