using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Prontuarios.Commands;

public class AtualizarModeloDeProntuarioCommandHandler : ICommandHandler<AtualizarModeloDeProntuarioCommand>
{
    private readonly IModeloDeProntuarioRepository _repository;

    public AtualizarModeloDeProntuarioCommandHandler(IModeloDeProntuarioRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(AtualizarModeloDeProntuarioCommand command)
    {
        var modelo = await _repository.ObterPorIdOuNulo(command.ModeloId)
            ?? throw new BusinessException("Modelo não encontrado.");

        // Isolamento: não deixar editar padrão-sistema pelo backend regular nem
        // modelo de outro estabelecimento (mensagem padronizada — nao vaza existencia).
        if (modelo.EhPadraoSistema)
            throw new BusinessException("Modelo padrão-sistema só pode ser editado pela ferramenta admin.");
        if (modelo.EstabelecimentoId != command.EstabelecimentoId)
            throw new BusinessException("Modelo não encontrado.");

        modelo.AtualizarDados(command.Nome, command.Descricao, command.EstruturaJson);
        await _repository.Salvar(modelo);
    }
}
