using Imedto.Backend.Contracts.Unidades.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Unidades;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Unidades.Commands;

public class DeletarUnidadeCommandHandler : ICommandHandler<DeletarUnidadeCommand>
{
    private readonly IUnidadeRepository _unidades;
    private readonly IEstabelecimentoRepository _estabelecimentos;

    public DeletarUnidadeCommandHandler(IUnidadeRepository unidades, IEstabelecimentoRepository estabelecimentos)
    {
        _unidades = unidades;
        _estabelecimentos = estabelecimentos;
    }

    public async Task Handle(DeletarUnidadeCommand command)
    {
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var unidade = await _unidades.ObterPorIdOuNulo(command.UnidadeId, command.EstabelecimentoId)
            ?? throw new BusinessException("Unidade não encontrada.");
        var estab = await _estabelecimentos.ObterPorIdOuNulo(command.EstabelecimentoId)
            ?? throw new BusinessException("Estabelecimento não encontrado.");

        if (estab.DonoUsuarioId != command.UsuarioSolicitanteId)
            throw new BusinessException("Apenas o dono pode excluir unidades.");

        if (unidade.IsPrincipal)
            throw new BusinessException("Não é possível excluir a unidade principal. Marque outra como principal antes.");

        await _unidades.Excluir(unidade);
    }
}
