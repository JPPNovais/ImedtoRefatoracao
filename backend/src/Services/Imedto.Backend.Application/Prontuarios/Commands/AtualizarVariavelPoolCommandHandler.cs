using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Prontuarios.Commands;

public class AtualizarVariavelPoolCommandHandler : ICommandHandler<AtualizarVariavelPoolCommand>
{
    private readonly IProntuarioVariavelPoolRepository _repository;

    public AtualizarVariavelPoolCommandHandler(IProntuarioVariavelPoolRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(AtualizarVariavelPoolCommand command)
    {
        var item = await _repository.ObterPorIdOuNulo(command.ItemId)
            ?? throw new BusinessException("Opção não encontrada.");

        if (item.EhPadraoSistema || item.EstabelecimentoId is null)
            throw new BusinessException("Opções padrão do sistema não podem ser editadas.");
        // Mensagem padronizada (defense-in-depth: nao vaza existencia cross-tenant).
        if (item.EstabelecimentoId != command.EstabelecimentoId)
            throw new BusinessException("Opção não encontrada.");

        if (await _repository.ExisteOutraComMesmoNome(command.EstabelecimentoId, item.Tipo, command.Nome ?? string.Empty, item.Id))
            throw new BusinessException("Já existe uma opção com esse nome para esta lista.");

        item.Renomear(command.Nome);
        await _repository.Salvar(item);
    }
}
