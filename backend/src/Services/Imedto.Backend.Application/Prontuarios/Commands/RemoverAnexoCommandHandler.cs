using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.Application.Prontuarios.Commands;

/// <summary>
/// Remove (soft-delete) um anexo do prontuário. Regra autor-ou-dono (briefing 001):
/// somente o autor do anexo (<c>CriadoPorUsuarioId</c>) ou o Dono pode remover.
/// Negação genérica: "não encontrado" — nunca revela que o anexo pertence a colega.
/// Blob S3 retido conforme política LGPD; apenas <c>deletado_em</c> preenchido.
/// </summary>
public class RemoverAnexoCommandHandler : ICommandHandler<RemoverAnexoCommand>
{
    private readonly IProntuarioAnexoRepository _anexoRepo;

    public RemoverAnexoCommandHandler(IProntuarioAnexoRepository anexoRepo)
    {
        _anexoRepo = anexoRepo;
    }

    public virtual async Task Handle(RemoverAnexoCommand command)
    {
        var anexo = await _anexoRepo.ObterPorIdOuNulo(command.AnexoId, command.EstabelecimentoId);

        // Gating autor-ou-dono — falha-fechada: "não encontrado" independente do motivo
        // (não existe, é de outro tenant ou de colega).
        var ehDono = command.SolicitantePapel == TenantPapel.Dono;
        if (anexo is null || (!ehDono && anexo.CriadoPorUsuarioId != command.SolicitanteUsuarioId))
            throw new BusinessException("Anexo não encontrado.");

        if (anexo.DeletadoEm is not null)
            throw new BusinessException("Anexo já foi removido.");

        anexo.MarcarComoDeletado(command.SolicitanteUsuarioId);
        await _anexoRepo.Salvar(anexo);
    }
}
