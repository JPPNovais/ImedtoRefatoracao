using Imedto.Backend.Contracts.Unidades.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Unidades;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Unidades.Commands;

public class CriarUnidadeCommandHandler : ICommandHandler<CriarUnidadeCommand>
{
    private readonly IUnidadeRepository _unidades;
    private readonly IEstabelecimentoRepository _estabelecimentos;

    public CriarUnidadeCommandHandler(IUnidadeRepository unidades, IEstabelecimentoRepository estabelecimentos)
    {
        _unidades = unidades;
        _estabelecimentos = estabelecimentos;
    }

    public async Task Handle(CriarUnidadeCommand command)
    {
        var estab = await _estabelecimentos.ObterPorIdOuNulo(command.EstabelecimentoId)
            ?? throw new BusinessException("Estabelecimento não encontrado.");

        if (estab.DonoUsuarioId != command.UsuarioSolicitanteId)
            throw new BusinessException("Apenas o dono pode cadastrar unidades.");

        if (await _unidades.ExisteOutraComMesmoNome(command.EstabelecimentoId, command.Nome ?? string.Empty, 0))
            throw new BusinessException("Já existe uma unidade com esse nome neste estabelecimento.");

        var unidade = UnidadeEstabelecimento.Criar(
            command.EstabelecimentoId,
            command.Nome,
            isPrincipal: command.IsPrincipal,
            new EnderecoUnidadeInput(
                command.Cep, command.Logradouro, command.Numero, command.Complemento,
                command.Bairro, command.Cidade, command.Estado),
            command.Telefone);

        // Se há outra principal e esta também é, desmarca a anterior.
        if (command.IsPrincipal)
        {
            var atual = await _unidades.ObterPrincipalDoEstabelecimento(command.EstabelecimentoId);
            if (atual != null)
            {
                atual.RemoverFlagPrincipal();
                await _unidades.Salvar(atual);
            }
        }

        await _unidades.Salvar(unidade);
    }
}
