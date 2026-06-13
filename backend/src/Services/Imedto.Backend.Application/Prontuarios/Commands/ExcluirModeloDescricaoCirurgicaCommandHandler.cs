using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Prontuarios.Commands;

public class ExcluirModeloDescricaoCirurgicaCommandHandler : ICommandHandler<ExcluirModeloDescricaoCirurgicaCommand>
{
    private readonly IModeloDescricaoCirurgicaRepository _repository;

    public ExcluirModeloDescricaoCirurgicaCommandHandler(IModeloDescricaoCirurgicaRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(ExcluirModeloDescricaoCirurgicaCommand command)
    {
        // Multi-tenant falha-fechada: padrão-sistema (NULL) nunca retornado aqui.
        var modelo = await _repository.ObterPorIdOuNulo(command.ModeloId, command.EstabelecimentoId)
            ?? throw new BusinessException("Modelo não encontrado.");

        // Guarda de profundidade: caso o repo seja alterado futuramente,
        // padrão-sistema continua imutável pelo tenant.
        if (modelo.EhPadraoSistema)
            throw new BusinessException("Modelos padrão do sistema não podem ser alterados.");

        await _repository.Excluir(modelo);
    }
}
