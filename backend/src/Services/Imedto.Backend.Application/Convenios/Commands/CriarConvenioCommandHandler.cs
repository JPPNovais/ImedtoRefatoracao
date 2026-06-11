using Imedto.Backend.Contracts.Convenios.Commands;
using Imedto.Backend.Domain.Convenios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Convenios.Commands;

public class CriarConvenioCommandHandler : ICommandHandler<CriarConvenioCommand>
{
    private readonly IConvenioRepository _repo;

    public CriarConvenioCommandHandler(IConvenioRepository repo) => _repo = repo;

    public async Task Handle(CriarConvenioCommand cmd)
    {
        var convenio = Convenio.Criar(cmd.EstabelecimentoId, cmd.Nome, cmd.RegistroAns);
        await _repo.Salvar(convenio);
    }
}
