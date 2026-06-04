using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Vinculos.Commands;

public class InativarVinculoCommandHandler : ICommandHandler<InativarVinculoCommand>
{
    private readonly IVinculoRepository _vinculoRepo;
    private readonly IEstabelecimentoRepository _estabelecimentoRepo;
    private readonly IModeloPermissaoRepository _permissoes;

    public InativarVinculoCommandHandler(
        IVinculoRepository vinculoRepo,
        IEstabelecimentoRepository estabelecimentoRepo,
        IModeloPermissaoRepository permissoes)
    {
        _vinculoRepo = vinculoRepo;
        _estabelecimentoRepo = estabelecimentoRepo;
        _permissoes = permissoes;
    }

    public async Task Handle(InativarVinculoCommand command)
    {
        var vinculo = await _vinculoRepo.ObterPorIdOuNulo(command.VinculoId)
            ?? throw new BusinessException("Vínculo não encontrado.");
        var estab = await _estabelecimentoRepo.ObterPorId(vinculo.EstabelecimentoId);

        // Defesa-em-profundidade: bloqueia inativar/remover vínculo cujo profissional é o Dono.
        // O front já desabilita o checkbox, mas se vier por API direta (admin abusando, script,
        // ou dado anômalo) precisa falhar fechado — Dono sem acesso quebra a clínica inteira.
        if (vinculo.ProfissionalUsuarioId == estab.DonoUsuarioId)
            throw new BusinessException("O dono do estabelecimento não pode ser desativado.");

        // Dono OU usuário com gerir_profissionais (pass-through pelo UsuarioTemPermissaoExtra)
        // OU o próprio profissional encerrando o próprio vínculo.
        var ehProprioProfissional = vinculo.ProfissionalUsuarioId == command.UsuarioSolicitanteId;
        var temPermissao = await _permissoes.UsuarioTemPermissaoExtra(
            command.UsuarioSolicitanteId,
            vinculo.EstabelecimentoId,
            PermissoesExtras.GerirProfissionais);

        if (!ehProprioProfissional && !temPermissao)
            throw new BusinessException("Você não tem permissão para inativar este vínculo.");

        vinculo.Inativar();
        await _vinculoRepo.Salvar(vinculo);
    }
}
