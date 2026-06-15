using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Application.Migracao.Commands;
using Imedto.Backend.Contracts.Migracao.Commands;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.API.Controllers;

/// <summary>
/// Endpoints da Central de Migração (briefing 2026-06-15_001 — Marco 1).
///
/// Upload: qualquer usuário autenticado do tenant (dono ou profissional).
/// Operações admin (revisão de mapa, disparo, relatório): marcos 2–5 — não implementado aqui.
///
/// RBAC: ICurrentTenantAccessor via [RequiresEstabelecimento] — CA3.
/// Multi-tenant: estabelecimento_id vem do header X-Estabelecimento-Id — CA2.
/// Limite de 50MB no request (CA19) — trava adicional além da validação no handler.
/// </summary>
[ApiController]
[Authorize]
[Route("api/migracao")]
[Produces("application/json")]
[RequestSizeLimit(55 * 1024 * 1024)] // 55MB — margem p/ envelope HTTP acima do limite de 50MB do negócio
public class MigracaoController : ControllerBase
{
    private readonly IniciarMigracaoCommandHandler _iniciar;
    private readonly ICurrentTenantAccessor _tenant;

    public MigracaoController(
        IniciarMigracaoCommandHandler iniciar,
        ICurrentTenantAccessor tenant)
    {
        _iniciar = iniciar;
        _tenant = tenant;
    }

    /// <summary>
    /// Inicia uma migração: valida arquivo, faz upload do ZIP no S3 e cria o job.
    ///
    /// O cliente deve ter aceitado o termo de responsabilidade (checkbox obrigatório no front).
    /// Limite: 50MB (CA19). Formato aceito: application/zip.
    /// </summary>
    /// <response code="201">Job criado com sucesso. Corpo: { jobId, status }.</response>
    /// <response code="400">Arquivo ausente ou formato inválido.</response>
    /// <response code="422">Arquivo acima de 50MB ou regra de negócio violada.</response>
    [HttpPost("upload")]
    [RequiresEstabelecimento]
    [ProducesResponseType(typeof(IniciarMigracaoResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Upload(
        IFormFile arquivo,
        [FromForm] string? origem = null,
        CancellationToken ct = default)
    {
        if (arquivo is null || arquivo.Length == 0)
            throw new BusinessException("Arquivo é obrigatório.");

        // Valida MIME type — defense-in-depth (front também valida).
        if (!arquivo.ContentType.Equals("application/zip", StringComparison.OrdinalIgnoreCase)
            && !arquivo.ContentType.Equals("application/x-zip-compressed", StringComparison.OrdinalIgnoreCase)
            && !arquivo.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException("Apenas arquivos ZIP são aceitos.");
        }

        var result = await _iniciar.Handle(new IniciarMigracaoCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            UsuarioId = _tenant.UsuarioId,
            ArquivoStream = arquivo.OpenReadStream(),
            ArquivoTamanhoBytes = arquivo.Length,
            ArquivoNomeOriginal = arquivo.FileName,
            Origem = origem
        }, ct);

        return CreatedAtAction(
            nameof(ObterJob),
            new { jobId = result.JobId },
            result);
    }

    /// <summary>Obtém status de um job de migração do tenant.</summary>
    /// <response code="200">Status do job.</response>
    /// <response code="404">Job não encontrado (ou não pertence ao tenant).</response>
    [HttpGet("{jobId:long}")]
    [RequiresEstabelecimento]
    [ProducesResponseType(typeof(IniciarMigracaoResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterJob(
        long jobId,
        [FromServices] Domain.Migracao.IMigracaoJobRepository repo,
        CancellationToken ct)
    {
        var job = await repo.ObterPorIdDoEstabelecimentoOuNulo(jobId, _tenant.EstabelecimentoId, ct);
        if (job is null) return NotFound(new { mensagem = "Não encontrado." });

        return Ok(new IniciarMigracaoResult
        {
            JobId = job.Id,
            Status = job.Status
        });
    }
}
