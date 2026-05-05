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
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo
        // (itens padrao-do-sistema com estabelecimento_id IS NULL nao sao retornados).
        var item = await _repository.ObterPorIdOuNulo(command.ItemId, command.EstabelecimentoId)
            ?? throw new BusinessException("Opção não encontrada.");

        if (item.EhPadraoSistema)
            throw new BusinessException("Opções padrão do sistema não podem ser editadas.");

        if (await _repository.ExisteOutraComMesmoNome(command.EstabelecimentoId, item.Tipo, command.Nome ?? string.Empty, item.Id))
            throw new BusinessException("Já existe uma opção com esse nome para esta lista.");

        item.Renomear(command.Nome);
        await _repository.Salvar(item);
    }
}
