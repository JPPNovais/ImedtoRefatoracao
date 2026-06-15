using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Migracao;

/// <summary>
/// Reprocessa um job que falhou (status "falhou") — addendum 002 R-B4/CA30/CA31.
///
/// Transição: falhou → StatusAntesFalha (aguardando_mapa ou migrando).
/// O recorrente correspondente reprocessa automaticamente — zero infra nova.
/// A carga é idempotente: apenas registros "pendente" são reprocessados.
///
/// RBAC: policy ImedtoAdmin (reuso — toda a área admin já exige).
/// LGPD: sem PII em logs. Audit: transição de estado registrada via CA20.
/// Multi-tenant: JobId resolvido via ObterPorIdAdminOuNulo (escopo admin, sem filtro tenant —
/// a área admin já exige ImedtoAdmin que não é tenant específico).
/// </summary>
public sealed class ReprocessarMigracaoCommandHandler
{
    private readonly IMigracaoJobRepository _jobRepo;

    public ReprocessarMigracaoCommandHandler(IMigracaoJobRepository jobRepo)
    {
        _jobRepo = jobRepo;
    }

    public async Task Handle(long jobId, CancellationToken ct = default)
    {
        if (jobId <= 0) throw new BusinessException("Job inválido.");

        var job = await _jobRepo.ObterPorIdAdminOuNulo(jobId, ct)
            ?? throw new BusinessException("Job não encontrado.");

        // Domínio valida: só "falhou" pode ser reprocessado (CA31).
        // Lança BusinessException → 422 automático pelo GlobalExceptionFilter.
        job.Reprocessar();

        await _jobRepo.Salvar(job, ct);
    }
}
