using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Prontuarios.Commands;

public class ExcluirModeloDeProntuarioCommandHandler : ICommandHandler<ExcluirModeloDeProntuarioCommand>
{
    private readonly IModeloDeProntuarioRepository _repository;

    public ExcluirModeloDeProntuarioCommandHandler(IModeloDeProntuarioRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(ExcluirModeloDeProntuarioCommand command)
    {
        var modelo = await _repository.ObterPorIdOuNulo(command.ModeloId)
            ?? throw new BusinessException("Modelo não encontrado.");

        if (modelo.EhPadraoSistema)
            throw new BusinessException("Modelos padrão do sistema não podem ser excluídos.");
        if (modelo.EstabelecimentoId != command.EstabelecimentoId)
            throw new BusinessException("Modelo não encontrado.");

        await _repository.Excluir(modelo);
    }
}
