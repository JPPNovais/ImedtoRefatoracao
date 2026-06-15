using Imedto.Backend.Contracts.Admin.Migracao;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Migracao;

/// <summary>
/// Dispara a carga da Onda 1 — apenas muda o status para "migrando".
/// A carga em si é feita pelo CarregarOnda1JobHandler (assíncrono — CA22).
/// </summary>
public sealed class DisparaMigracaoCommandHandler
{
    private readonly IMigracaoJobRepository _jobRepo;

    public DisparaMigracaoCommandHandler(IMigracaoJobRepository jobRepo)
    {
        _jobRepo = jobRepo;
    }

    public async Task Handle(DisparaMigracaoCommand cmd, CancellationToken ct = default)
    {
        if (cmd.JobId <= 0) throw new BusinessException("Job inválido.");
        if (cmd.AdminId == Guid.Empty) throw new BusinessException("Admin é obrigatório.");

        var job = await _jobRepo.ObterPorIdAdminOuNulo(cmd.JobId, ct)
            ?? throw new BusinessException("Job não encontrado.");

        // Transição de estado: preview_pronto → migrando (CA22 — retorna imediatamente)
        job.MarcarMigrando(cmd.AdminId);
        await _jobRepo.Salvar(job, ct);
        // Job será pego pelo CarregarOnda1JobHandler na próxima rodada do scheduler.
    }
}
