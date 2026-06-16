using Imedto.Backend.Contracts.Admin.Migracao;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Migracao;

/// <summary>
/// Addendum 6 — CA104/CA116: agora chama <see cref="MaterializarRegistrosCommandHandler"/>
/// ANTES de contar os registros e marcar o job como preview_pronto.
///
/// Fluxo (mapa_em_revisao → preview_pronto):
/// 1. Valida job (admin, status mapa_em_revisao).
/// 2. Materializa os registros pendentes (escrita — CA102) — passo de ESCRITA.
/// 3. Conta os registros materializados por entidade.
/// 4. Marca job como preview_pronto.
/// 5. Retorna o preview com TotalRegistros real (nunca zero quando há blocos aceitos).
/// </summary>
public sealed class PreviewOnda1QueryHandler
{
    private readonly IMigracaoJobRepository _jobRepo;
    private readonly IMigracaoRegistroRepository _registroRepo;
    private readonly IMigracaoJobEventoRepository _eventoRepo;
    private readonly MaterializarRegistrosCommandHandler _materializarHandler;

    public PreviewOnda1QueryHandler(
        IMigracaoJobRepository jobRepo,
        IMigracaoRegistroRepository registroRepo,
        IMigracaoJobEventoRepository eventoRepo,
        MaterializarRegistrosCommandHandler materializarHandler)
    {
        _jobRepo             = jobRepo;
        _registroRepo        = registroRepo;
        _eventoRepo          = eventoRepo;
        _materializarHandler = materializarHandler;
    }

    public async Task<PreviewMigracaoResult> Handle(long jobId, Guid adminId, CancellationToken ct = default)
    {
        if (jobId <= 0) throw new BusinessException("Job inválido.");
        if (adminId == Guid.Empty) throw new BusinessException("Admin é obrigatório.");

        var job = await _jobRepo.ObterPorIdAdminOuNulo(jobId, ct)
            ?? throw new BusinessException("Job não encontrado.");

        if (job.Status != MigracaoJob.StatusMapaEmRevisao)
            throw new BusinessException("Job precisa estar em revisão de mapa para gerar preview.");

        // Addendum 6 — CA102/CA104: materializa ANTES de contar.
        // Escrita idempotente: apaga pendentes e regera dos mapas aprovados atuais.
        // importado_*/rejeitado/pulado nunca são tocados (R-M7/CA110).
        await _materializarHandler.ExecutarAsync(jobId, ct);

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
