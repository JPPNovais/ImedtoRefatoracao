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
        // Defense-in-depth multi-tenant: filtro padrao-sistema OR estabelecimento ativo.
        var modelo = await _repository.ObterVisivelOuNulo(command.ModeloId, command.EstabelecimentoId)
            ?? throw new BusinessException("Modelo não encontrado.");

        // Padrao-sistema é visível mas só pode ser editado pela ferramenta admin.
        if (modelo.EhPadraoSistema)
            throw new BusinessException("Modelo padrão-sistema só pode ser editado pela ferramenta admin.");

        modelo.AtualizarDados(command.Nome, command.Descricao, command.EstruturaJson);
        await _repository.Salvar(modelo);
    }
}
