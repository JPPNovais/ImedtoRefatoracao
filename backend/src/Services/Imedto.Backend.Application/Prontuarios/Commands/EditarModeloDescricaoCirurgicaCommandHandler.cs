using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Prontuarios.Commands;

public class EditarModeloDescricaoCirurgicaCommandHandler : ICommandHandler<EditarModeloDescricaoCirurgicaCommand>
{
    private readonly IModeloDescricaoCirurgicaRepository _repository;

    public EditarModeloDescricaoCirurgicaCommandHandler(IModeloDescricaoCirurgicaRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(EditarModeloDescricaoCirurgicaCommand command)
    {
        // Multi-tenant falha-fechada: só retorna registro do próprio tenant.
        // Padrão-sistema (EstabelecimentoId IS NULL) nunca é retornado aqui.
        var modelo = await _repository.ObterPorIdOuNulo(command.ModeloId, command.EstabelecimentoId)
            ?? throw new BusinessException("Modelo não encontrado.");

        if (await _repository.ExisteOutroComMesmoTitulo(command.EstabelecimentoId, command.Titulo ?? string.Empty, modelo.Id))
            throw new BusinessException("Já existe um modelo com este título.");

        modelo.Editar(command.Titulo, command.Corpo);
        await _repository.Salvar(modelo);
    }
}
