using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Vinculos.Commands;

public class ReativarVinculoCommandHandler : ICommandHandler<ReativarVinculoCommand>
{
    private readonly IVinculoRepository _vinculoRepo;
    private readonly IModeloPermissaoRepository _permissoes;

    public ReativarVinculoCommandHandler(
        IVinculoRepository vinculoRepo,
        IModeloPermissaoRepository permissoes)
    {
        _vinculoRepo = vinculoRepo;
        _permissoes = permissoes;
    }

    public async Task Handle(ReativarVinculoCommand command)
    {
        var vinculo = await _vinculoRepo.ObterPorIdOuNulo(command.VinculoId)
            ?? throw new BusinessException("Vínculo não encontrado.");

        // Dono OU usuário com gerir_profissionais podem reativar.
        // UsuarioTemPermissaoExtra trata Dono como pass-through.
        var temPermissao = await _permissoes.UsuarioTemPermissaoExtra(
            command.UsuarioSolicitanteId,
            vinculo.EstabelecimentoId,
            PermissoesExtras.GerirProfissionais);

        if (!temPermissao)
            throw new BusinessException("Você não tem permissão para reativar este vínculo.");

        vinculo.Reativar();
        await _vinculoRepo.Salvar(vinculo);
    }
}
