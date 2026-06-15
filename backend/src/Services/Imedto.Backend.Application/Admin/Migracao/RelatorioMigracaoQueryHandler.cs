using Imedto.Backend.Contracts.Admin.Migracao;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Migracao;

public sealed class RelatorioMigracaoQueryHandler
{
    private readonly IMigracaoJobRepository _jobRepo;
    private readonly IMigracaoRegistroRepository _registroRepo;

    public RelatorioMigracaoQueryHandler(
        IMigracaoJobRepository jobRepo,
        IMigracaoRegistroRepository registroRepo)
    {
        _jobRepo = jobRepo;
        _registroRepo = registroRepo;
    }

    public async Task<RelatorioMigracaoResult> Handle(long jobId, CancellationToken ct = default)
    {
        if (jobId <= 0) throw new BusinessException("Job inválido.");

        var job = await _jobRepo.ObterPorIdAdminOuNulo(jobId, ct)
            ?? throw new BusinessException("Job não encontrado.");

        if (job.Status != MigracaoJob.StatusConcluido
            && job.Status != MigracaoJob.StatusConcluidoComErros
            && job.Status != MigracaoJob.StatusDesfeito)
            throw new BusinessException("Relatório disponível apenas para jobs concluídos ou desfeitos.");

        var relatorio = await _registroRepo.ObterRelatorio(jobId, ct);

        var porEntidade = relatorio.PorEntidade.ToDictionary(
            kv => kv.Key,
            kv => new RelatorioEntidadeResult
            {
                Criados = kv.Value.Criados,
                Atualizados = kv.Value.Atualizados,
                Rejeitados = kv.Value.Rejeitados,
                Pulados = kv.Value.Pulados,
                MotivosRejeicao = kv.Value.MotivosRejeicao,
            });

        return new RelatorioMigracaoResult
        {
            TotalCriados = relatorio.TotalCriados,
            TotalAtualizados = relatorio.TotalAtualizados,
            TotalRejeitados = relatorio.TotalRejeitados,
            TotalPulados = relatorio.TotalPulados,
            PorEntidade = porEntidade,
        };
    }
}
