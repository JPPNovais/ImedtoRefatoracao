using Imedto.Backend.Contracts.Admin.Migracao;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Migracao;

/// <summary>
/// Aprova a análise por IA de um job de migração represado em aguardando_aprovacao.
/// Addendum 003 — R-A2/CA41/CA42/CA43/CA46/CA49.
///
/// Transição: aguardando_aprovacao → aguardando_mapa.
/// O recorrente inferir-mapa-migracao selecionará o job na próxima rodada.
///
/// RBAC: policy ImedtoAdmin (reuso — toda a área admin já exige).
/// Multi-tenant: job resolvido via ObterPorIdAdminOuNulo (escopo admin cross-tenant,
///   apropriado pois o admin não é de um tenant específico — R-A4/CA43).
/// LGPD: sem PII em logs; audit de transição via AtualizadoEm (CA49).
/// BusinessException ("não encontrado") para job inexistente — mensagem genérica (R-A7/CA46).
/// </summary>
public sealed class AprovarAnaliseCommandHandler
{
    private readonly IMigracaoJobRepository _jobRepo;

    public AprovarAnaliseCommandHandler(IMigracaoJobRepository jobRepo)
    {
        _jobRepo = jobRepo;
    }

    public async Task Handle(AprovarAnaliseCommand command, CancellationToken ct = default)
    {
        if (command.JobId <= 0) throw new BusinessException("Job inválido.");
        if (command.AdminId == Guid.Empty) throw new BusinessException("Admin é obrigatório.");

        // CA46 — mensagem genérica: não revela se o job existe em outro tenant.
        var job = await _jobRepo.ObterPorIdAdminOuNulo(command.JobId, ct)
            ?? throw new BusinessException("Não encontrado.");

        // Domínio valida: só aguardando_aprovacao pode ser aprovado (CA42).
        // Lança BusinessException → 422 automático pelo GlobalExceptionFilter.
        job.AprovarAnalise(command.AdminId);

        await _jobRepo.Salvar(job, ct);
    }
}
