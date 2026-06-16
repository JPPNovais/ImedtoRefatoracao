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
    private readonly IMigracaoJobEventoRepository _eventoRepo;

    public DisparaMigracaoCommandHandler(
        IMigracaoJobRepository jobRepo,
        IMigracaoJobEventoRepository eventoRepo)
    {
        _jobRepo    = jobRepo;
        _eventoRepo = eventoRepo;
    }

    public async Task Handle(DisparaMigracaoCommand cmd, CancellationToken ct = default)
    {
        if (cmd.JobId <= 0) throw new BusinessException("Job inválido.");
        if (cmd.AdminId == Guid.Empty) throw new BusinessException("Admin é obrigatório.");

        var job = await _jobRepo.ObterPorIdAdminOuNulo(cmd.JobId, ct)
            ?? throw new BusinessException("Job não encontrado.");

        // Transição de estado: preview_pronto → migrando (CA22 — retorna imediatamente)
        var statusAnterior = job.Status;
        job.MarcarMigrando(cmd.AdminId);
        await _jobRepo.Salvar(job, ct);
        // Job será pego pelo CarregarOnda1JobHandler na próxima rodada do scheduler.

        var evt = MigracaoJobEvento.Criar(job.Id, job.EstabelecimentoId, statusAnterior, job.Status, cmd.AdminId);
        await _eventoRepo.Gravar(evt, ct);
    }
}
