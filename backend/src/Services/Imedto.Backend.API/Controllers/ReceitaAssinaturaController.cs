using Imedto.Backend.Application.AssinaturaDigital.Commands;
using Imedto.Backend.Contracts.AssinaturaDigital.Commands;
using Imedto.Backend.Contracts.AssinaturaDigital.Queries;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Imedto.Backend.API.Controllers;

/// <summary>
/// Endpoints de assinatura digital de receitas.
/// Acesso restrito a Profissional/Dono (médico) — recepcionista não assina.
///
/// - <c>POST /api/receitas/{id}/assinar</c> — dispara assinatura (retorna 202).
/// - <c>GET  /api/receitas/{id}/status-assinatura</c> — polling de status.
/// - <c>POST /api/webhooks/assinatura/{receita_id}</c> — callback do BirdID (sem autenticação de usuário).
/// </summary>
[Authorize]
[RequiresEstabelecimento]
[RequiresPapel(TenantPapel.Profissional, TenantPapel.Dono)]
[ApiController]
[Produces("application/json")]
public class ReceitaAssinaturaController : ControllerBase
{
    private readonly ICommandBus _commandBus;
    private readonly IRequestBus _requestBus;
    private readonly ICurrentTenantAccessor _tenant;
    private readonly bool _habilitado;

    public ReceitaAssinaturaController(
        ICommandBus commandBus,
        IRequestBus requestBus,
        ICurrentTenantAccessor tenant,
        IConfiguration configuration)
    {
        _commandBus = commandBus;
        _requestBus = requestBus;
        _tenant = tenant;
        _habilitado = configuration.GetValue<bool>("AssinaturaDigital:Habilitado");
    }

    private IActionResult? FeatureDesabilitada() =>
        _habilitado ? null : StatusCode(503, new { mensagem = "Assinatura digital não está disponível neste momento." });

    /// <summary>
    /// Dispara assinatura digital de uma receita emitida.
    /// Retorna 202 Accepted — a confirmação chega via webhook do provedor.
    /// Apenas o médico prescritor pode chamar (o handler valida e retorna 422 se não for).
    /// </summary>
    [HttpPost("api/receitas/{id:long}/assinar")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Assinar(long id)
    {
        if (FeatureDesabilitada() is { } r) return r;
        await _commandBus.Send(new DispararAssinaturaCommand
        {
            ReceitaId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            CallerUsuarioId = _tenant.UsuarioId,
        });
        return Accepted(new { status = "AssinaturaPendente" });
    }

    /// <summary>
    /// Retorna o status de assinatura da receita. Usado pelo polling do frontend (4s).
    /// Retorna presigned URL S3 com TTL 5 min quando status = AssinadaIcp.
    /// </summary>
    [HttpGet("api/receitas/{id:long}/status-assinatura")]
    [ProducesResponseType(typeof(StatusAssinaturaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterStatus(long id)
    {
        if (FeatureDesabilitada() is { } r) return r;
        var dto = await _requestBus.Query<ObterStatusAssinaturaQuery, StatusAssinaturaDto>(
            new ObterStatusAssinaturaQuery
            {
                ReceitaId = id,
                EstabelecimentoId = _tenant.EstabelecimentoId,
                CallerUsuarioId = _tenant.UsuarioId,
            });
        return Ok(dto);
    }
}

/// <summary>
/// Controller público para webhooks de callback do provedor de assinatura.
/// SEM <c>[Authorize]</c> — autenticação é via validação HMAC do payload no handler.
/// </summary>
[ApiController]
[Produces("application/json")]
public class AssinaturaWebhookController : ControllerBase
{
    private readonly ICommandBus _commandBus;

    public AssinaturaWebhookController(ICommandBus commandBus)
    {
        _commandBus = commandBus;
    }

    /// <summary>
    /// Recebe callback do BirdID/VIDaaS após assinatura (sucesso ou falha).
    /// O handler valida o HMAC antes de qualquer mutação.
    /// Retorna 200 para evitar retry infinito do provedor mesmo em caso de descarte.
    /// </summary>
    [HttpPost("api/webhooks/assinatura/{receita_id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Callback(
        long receita_id,
        [FromHeader(Name = "X-BirdID-Signature")] string? headerAssinatura)
    {
        // Lê o payload bruto para validação HMAC.
        using var reader = new System.IO.StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync();

        try
        {
            await _commandBus.Send(new ProcessarCallbackAssinaturaCommand
            {
                ReceitaId = receita_id,
                PayloadJson = payload,
                HeaderAssinatura = headerAssinatura ?? string.Empty,
            });
        }
        catch (UnauthorizedAccessException)
        {
            // CA-07: HMAC inválido → 401 sem detalhe.
            return Unauthorized();
        }

        return Ok();
    }
}
