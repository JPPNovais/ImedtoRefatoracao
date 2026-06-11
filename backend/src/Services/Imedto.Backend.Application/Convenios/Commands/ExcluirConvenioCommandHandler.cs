using Imedto.Backend.Contracts.Convenios.Commands;
using Imedto.Backend.Domain.Convenios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Convenios.Commands;

public class ExcluirConvenioCommandHandler : ICommandHandler<ExcluirConvenioCommand>
{
    private readonly IConvenioRepository _repo;

    public ExcluirConvenioCommandHandler(IConvenioRepository repo) => _repo = repo;

    public async Task Handle(ExcluirConvenioCommand cmd)
    {
        // Multi-tenant: 404 genérico se não encontrado ou alheio (CA132).
        var convenio = await _repo.ObterPorIdOuNulo(cmd.ConvenioId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Convênio não encontrado.");

        // R3: exclusão física só se nunca referenciado (CA134).
        var emUso = await _repo.TemCarteirinhasOuCobrancas(cmd.ConvenioId);
        if (emUso)
            throw new BusinessException("Convênio em uso — inative em vez de excluir.");

        await _repo.Excluir(convenio);
    }
}
