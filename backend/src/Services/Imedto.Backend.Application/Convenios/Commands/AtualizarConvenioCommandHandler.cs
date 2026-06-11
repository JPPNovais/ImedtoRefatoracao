using Imedto.Backend.Contracts.Convenios.Commands;
using Imedto.Backend.Domain.Convenios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Convenios.Commands;

public class AtualizarConvenioCommandHandler : ICommandHandler<AtualizarConvenioCommand>
{
    private readonly IConvenioRepository _repo;

    public AtualizarConvenioCommandHandler(IConvenioRepository repo) => _repo = repo;

    public async Task Handle(AtualizarConvenioCommand cmd)
    {
        // Multi-tenant: filtro por estabelecimentoId no repo. 404 genérico se alheio (R1/CA132).
        var convenio = await _repo.ObterPorIdOuNulo(cmd.ConvenioId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Convênio não encontrado.");

        convenio.Atualizar(cmd.Nome, cmd.RegistroAns);

        if (!cmd.Ativo && convenio.Ativo) convenio.Inativar();
        else if (cmd.Ativo && !convenio.Ativo) convenio.Reativar();

        await _repo.Salvar(convenio);
    }
}
