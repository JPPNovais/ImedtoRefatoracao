using System.Text.Json;
using Imedto.Backend.Contracts.Admin.Migracao;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Migracao;

/// <summary>
/// Salva (upsert) um template a partir dos mapas revisados de um job.
/// CA18: admin pode salvar o mapeamento revisado como template reutilizável.
/// Scoped.
/// </summary>
public sealed class SalvarTemplateDeOrigemCommandHandler
{
    private readonly IMigracaoJobRepository _jobRepo;
    private readonly IMigracaoMapaRepository _mapaRepo;
    private readonly IMigracaoTemplateRepository _templateRepo;

    public SalvarTemplateDeOrigemCommandHandler(
        IMigracaoJobRepository jobRepo,
        IMigracaoMapaRepository mapaRepo,
        IMigracaoTemplateRepository templateRepo)
    {
        _jobRepo      = jobRepo;
        _mapaRepo     = mapaRepo;
        _templateRepo = templateRepo;
    }

    public async Task Handle(SalvarTemplateDeOrigemCommand cmd, CancellationToken ct = default)
    {
        if (cmd.JobId <= 0) throw new BusinessException("Job inválido.");
        if (string.IsNullOrWhiteSpace(cmd.NomeTemplate)) throw new BusinessException("Nome do template é obrigatório.");
        if (cmd.RevisadoPorUsuarioId == Guid.Empty) throw new BusinessException("Usuário revisor é obrigatório.");

        // Admin pode ver qualquer job — busca sem filtro de tenant.
        var jobAtual = await _jobRepo.ObterPorIdAdminOuNulo(cmd.JobId, ct)
            ?? throw new BusinessException("Job não encontrado.");

        var mapas = await _mapaRepo.ListarPorJob(jobAtual.Id, jobAtual.EstabelecimentoId, ct);
        if (mapas.Count == 0) throw new BusinessException("Nenhum mapa encontrado para este job.");

        // Para cada mapa, upsert do template.
        foreach (var mapa in mapas)
        {
            var existente = await _templateRepo.ObterPorNomeEEntidadeOuNulo(
                cmd.NomeTemplate, mapa.Entidade, ct);

            if (existente is not null)
            {
                existente.AtualizarMapa(mapa.MapaJson);
                await _templateRepo.Salvar(existente, ct);
            }
            else
            {
                var novo = MigracaoTemplate.Criar(
                    cmd.NomeTemplate,
                    mapa.Entidade,
                    mapa.MapaJson,
                    cmd.RevisadoPorUsuarioId);
                await _templateRepo.Salvar(novo, ct);
            }
        }
    }
}
