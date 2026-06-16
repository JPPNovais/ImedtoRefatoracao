using Imedto.Backend.Contracts.Admin.Migracao;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Migracao;

public sealed class PreviewOnda1QueryHandler
{
    private readonly IMigracaoJobRepository _jobRepo;
    private readonly IMigracaoRegistroRepository _registroRepo;
    private readonly IMigracaoJobEventoRepository _eventoRepo;

    public PreviewOnda1QueryHandler(
        IMigracaoJobRepository jobRepo,
        IMigracaoRegistroRepository registroRepo,
        IMigracaoJobEventoRepository eventoRepo)
    {
        _jobRepo      = jobRepo;
        _registroRepo = registroRepo;
        _eventoRepo   = eventoRepo;
    }

    public async Task<PreviewMigracaoResult> Handle(long jobId, Guid adminId, CancellationToken ct = default)
    {
        if (jobId <= 0) throw new BusinessException("Job inválido.");
        if (adminId == Guid.Empty) throw new BusinessException("Admin é obrigatório.");

        var job = await _jobRepo.ObterPorIdAdminOuNulo(jobId, ct)
            ?? throw new BusinessException("Job não encontrado.");

        if (job.Status != MigracaoJob.StatusMapaEmRevisao)
            throw new BusinessException("Job precisa estar em revisão de mapa para gerar preview.");

        var registros = await _registroRepo.ListarPorJob(jobId, ct);

        var porEntidade = registros
            .Where(r => r.Status == "pendente")
            .GroupBy(r => r.Entidade)
            .ToDictionary(
                g => g.Key,
                g => new EntidadePreview { Pendentes = g.Count() });

        var statusAnterior = job.Status;
        job.MarcarPreviewPronto(adminId);
        await _jobRepo.Salvar(job, ct);

        var evt = MigracaoJobEvento.Criar(job.Id, job.EstabelecimentoId, statusAnterior, job.Status, adminId);
        await _eventoRepo.Gravar(evt, ct);

        return new PreviewMigracaoResult
        {
            TotalRegistros = registros.Count(r => r.Status == "pendente"),
            PorEntidade = porEntidade,
        };
    }
}
