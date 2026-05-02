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
        var item = await _repository.ObterPorIdOuNulo(command.ItemId)
            ?? throw new BusinessException("Opção não encontrada.");

        if (item.EhPadraoSistema || item.EstabelecimentoId is null)
            throw new BusinessException("Opções padrão do sistema não podem ser excluídas.");
        if (item.EstabelecimentoId != command.EstabelecimentoId)
            throw new BusinessException("Opção não encontrada.");

        await _repository.Excluir(item);
    }
}
