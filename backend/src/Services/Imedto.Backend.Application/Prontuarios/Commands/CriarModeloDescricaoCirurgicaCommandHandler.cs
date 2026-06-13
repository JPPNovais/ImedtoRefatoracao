using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Prontuarios.Commands;

public class CriarModeloDescricaoCirurgicaCommandHandler : ICommandHandler<CriarModeloDescricaoCirurgicaCommand>
{
    private readonly IModeloDescricaoCirurgicaRepository _repository;

    public CriarModeloDescricaoCirurgicaCommandHandler(IModeloDescricaoCirurgicaRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(CriarModeloDescricaoCirurgicaCommand command)
    {
        if (await _repository.ExisteOutroComMesmoTitulo(command.EstabelecimentoId, command.Titulo ?? string.Empty, 0))
            throw new BusinessException("Já existe um modelo com este título.");

        var modelo = ModeloDescricaoCirurgica.CriarDoEstabelecimento(
            command.EstabelecimentoId,
            command.Titulo,
            command.Corpo);

        await _repository.Salvar(modelo);
    }
}
