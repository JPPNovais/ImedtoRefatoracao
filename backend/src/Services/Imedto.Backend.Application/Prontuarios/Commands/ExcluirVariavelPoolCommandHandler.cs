using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Prontuarios.Commands;

public class ExcluirVariavelPoolCommandHandler : ICommandHandler<ExcluirVariavelPoolCommand>
{
    private readonly IProntuarioVariavelPoolRepository _repository;

    public ExcluirVariavelPoolCommandHandler(IProntuarioVariavelPoolRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(ExcluirVariavelPoolCommand command)
    {
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var item = await _repository.ObterPorIdOuNulo(command.ItemId, command.EstabelecimentoId)
            ?? throw new BusinessException("Opção não encontrada.");

        if (item.EhPadraoSistema)
            throw new BusinessException("Opções padrão do sistema não podem ser excluídas.");

        await _repository.Excluir(item);
    }
}
