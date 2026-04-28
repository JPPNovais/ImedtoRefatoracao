using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Prontuarios.Commands;

public class CriarModeloDeProntuarioCommandHandler : ICommandHandler<CriarModeloDeProntuarioCommand>
{
    private readonly IModeloDeProntuarioRepository _repository;

    public CriarModeloDeProntuarioCommandHandler(IModeloDeProntuarioRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(CriarModeloDeProntuarioCommand command)
    {
        var modelo = ModeloDeProntuario.CriarDoEstabelecimento(
            command.EstabelecimentoId,
            command.Nome,
            command.Descricao,
            command.EstruturaJson);

        await _repository.Salvar(modelo);
    }
}
