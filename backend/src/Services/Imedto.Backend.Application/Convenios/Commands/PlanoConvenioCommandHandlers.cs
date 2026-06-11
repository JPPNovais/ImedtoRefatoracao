using Imedto.Backend.Contracts.Convenios.Commands;
using Imedto.Backend.Domain.Convenios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Convenios.Commands;

public class AdicionarPlanoConvenioCommandHandler : ICommandHandler<AdicionarPlanoConvenioCommand>
{
    private readonly IConvenioRepository _repo;
    public AdicionarPlanoConvenioCommandHandler(IConvenioRepository repo) => _repo = repo;

    public async Task Handle(AdicionarPlanoConvenioCommand cmd)
    {
        var convenio = await _repo.ObterPorIdOuNulo(cmd.ConvenioId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Convênio não encontrado.");

        convenio.AdicionarPlano(cmd.Nome);
        await _repo.Salvar(convenio);
    }
}

public class AtualizarPlanoConvenioCommandHandler : ICommandHandler<AtualizarPlanoConvenioCommand>
{
    private readonly IConvenioRepository _repo;
    public AtualizarPlanoConvenioCommandHandler(IConvenioRepository repo) => _repo = repo;

    public async Task Handle(AtualizarPlanoConvenioCommand cmd)
    {
        var convenio = await _repo.ObterPorIdOuNulo(cmd.ConvenioId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Convênio não encontrado.");

        convenio.AtualizarPlano(cmd.PlanoId, cmd.Nome);
        await _repo.Salvar(convenio);
    }
}

public class InativarPlanoConvenioCommandHandler : ICommandHandler<InativarPlanoConvenioCommand>
{
    private readonly IConvenioRepository _repo;
    public InativarPlanoConvenioCommandHandler(IConvenioRepository repo) => _repo = repo;

    public async Task Handle(InativarPlanoConvenioCommand cmd)
    {
        var convenio = await _repo.ObterPorIdOuNulo(cmd.ConvenioId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Convênio não encontrado.");

        convenio.InativarPlano(cmd.PlanoId);
        await _repo.Salvar(convenio);
    }
}
