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
        var item = await _repository.ObterPorId(command.ItemId);

        if (item.EhPadraoSistema || item.EstabelecimentoId is null)
            throw new BusinessException("Opções padrão do sistema não podem ser excluídas.");
        if (item.EstabelecimentoId != command.EstabelecimentoId)
            throw new BusinessException("Esta opção não pertence ao seu estabelecimento.");

        await _repository.Excluir(item);
    }
}
