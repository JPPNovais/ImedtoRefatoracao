using Imedto.Backend.Contracts.Unidades.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Unidades;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Unidades.Commands;

public class AtualizarUnidadeCommandHandler : ICommandHandler<AtualizarUnidadeCommand>
{
    private readonly IUnidadeRepository _unidades;
    private readonly IEstabelecimentoRepository _estabelecimentos;

    public AtualizarUnidadeCommandHandler(IUnidadeRepository unidades, IEstabelecimentoRepository estabelecimentos)
    {
        _unidades = unidades;
        _estabelecimentos = estabelecimentos;
    }

    public async Task Handle(AtualizarUnidadeCommand command)
    {
        var unidade = await _unidades.ObterPorId(command.UnidadeId);
        var estab = await _estabelecimentos.ObterPorId(unidade.EstabelecimentoId);

        if (estab.DonoUsuarioId != command.UsuarioSolicitanteId)
            throw new BusinessException("Apenas o dono pode editar unidades.");

        if (await _unidades.ExisteOutraComMesmoNome(unidade.EstabelecimentoId, command.Nome ?? string.Empty, unidade.Id))
            throw new BusinessException("Já existe uma unidade com esse nome neste estabelecimento.");

        unidade.AtualizarDados(
            command.Nome,
            new EnderecoUnidadeInput(
                command.Cep, command.Logradouro, command.Numero, command.Complemento,
                command.Bairro, command.Cidade, command.Estado),
            command.Telefone);

        // Sincroniza flag principal: se está marcando como principal, desmarca a atual.
        if (command.IsPrincipal && !unidade.IsPrincipal)
        {
            var atual = await _unidades.ObterPrincipalDoEstabelecimento(unidade.EstabelecimentoId);
            if (atual != null && atual.Id != unidade.Id)
            {
                atual.RemoverFlagPrincipal();
                await _unidades.Salvar(atual);
            }
            unidade.MarcarComoPrincipal();
        }
        else if (!command.IsPrincipal && unidade.IsPrincipal)
        {
            // Política: não permite desmarcar se for a única principal — mantém pelo menos uma marcada.
            // Para "trocar" o principal, o usuário marca outra como principal (caso acima).
            throw new BusinessException("Para remover a flag principal, marque outra unidade como principal primeiro.");
        }

        await _unidades.Salvar(unidade);
    }
}
