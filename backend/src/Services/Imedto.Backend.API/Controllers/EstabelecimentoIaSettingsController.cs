using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Domain.Ia;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.API.Controllers;

/// <summary>
/// Item 2.12: configurações de IA por estabelecimento. Apenas o dono pode ler/editar.
/// Linha em <c>establishment_ai_settings</c> é opcional — falta de registro = defaults globais.
/// </summary>
[ApiController]
[Authorize]
[RequiresEstabelecimento]
[Route("api/estabelecimento/ia-settings")]
[Produces("application/json")]
public class EstabelecimentoIaSettingsController : ControllerBase
{
    private readonly IEstabelecimentoIaSettingsRepository _repo;
    private readonly ICurrentTenantAccessor _tenant;

    public EstabelecimentoIaSettingsController(
        IEstabelecimentoIaSettingsRepository repo,
        ICurrentTenantAccessor tenant)
    {
        _repo = repo;
        _tenant = tenant;
    }

    /// <summary>Retorna as settings do tenant. Cai em defaults sem persistir se nunca foi configurado.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(EstabelecimentoIaSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Obter(CancellationToken ct)
    {
        GarantirEhDono();

        var settings = await _repo.ObterPorEstabelecimentoOuNulo(_tenant.EstabelecimentoId, ct)
                       ?? EstabelecimentoIaSettings.CriarPadrao(_tenant.EstabelecimentoId);

        return Ok(EstabelecimentoIaSettingsDto.De(settings));
    }

    /// <summary>Upsert das settings do tenant. Cria com defaults antes de aplicar as mudanças se ainda não existe.</summary>
    [HttpPut]
    [ProducesResponseType(typeof(EstabelecimentoIaSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Atualizar(
        [FromBody] AtualizarEstabelecimentoIaSettingsRequest body,
        CancellationToken ct)
    {
        if (body is null)
            throw new BusinessException("Corpo da requisição é obrigatório.");

        GarantirEhDono();

        var settings = await _repo.ObterPorEstabelecimentoOuNulo(_tenant.EstabelecimentoId, ct)
                       ?? EstabelecimentoIaSettings.CriarPadrao(_tenant.EstabelecimentoId);

        settings.AtualizarModelo(body.AiProvider, body.AiModel);
        settings.AtualizarLimites(body.RateLimitPerMinute, body.RateLimitPerDay);
        settings.AtualizarMinimizacao(ParseNivel(body.DataMinimizationLevel));
        if (body.AiEnabled) settings.Habilitar();
        else settings.Desabilitar();

        await _repo.Salvar(settings, ct);

        return Ok(EstabelecimentoIaSettingsDto.De(settings));
    }

    /// <summary>
    /// PUT/GET de settings é privilégio do dono do estabelecimento. Vínculos comuns não acessam
    /// — defense-in-depth: o filter <see cref="RequiresEstabelecimentoAttribute"/> só garante
    /// "tem acesso", aqui restringimos ainda mais.
    /// </summary>
    private void GarantirEhDono()
    {
        if (!_tenant.EhDono)
            throw new BusinessException("Apenas o dono do estabelecimento pode alterar as configurações de IA.");
    }

    private static NivelMinimizacaoDados ParseNivel(string valor)
    {
        return valor?.Trim().ToLowerInvariant() switch
        {
            "minimized" => NivelMinimizacaoDados.Minimized,
            "standard" => NivelMinimizacaoDados.Standard,
            null or "" => NivelMinimizacaoDados.Standard,
            _ => throw new BusinessException("Nível de minimização inválido (use 'standard' ou 'minimized').")
        };
    }
}

public class AtualizarEstabelecimentoIaSettingsRequest
{
    public bool AiEnabled { get; set; } = true;
    public string AiProvider { get; set; } = "anthropic";
    public string AiModel { get; set; } = "claude-sonnet-4-6";
    public int RateLimitPerMinute { get; set; } = 10;
    public int RateLimitPerDay { get; set; } = 200;
    public string DataMinimizationLevel { get; set; } = "standard";
}

public class EstabelecimentoIaSettingsDto
{
    public long EstabelecimentoId { get; set; }
    public bool AiEnabled { get; set; }
    public string AiProvider { get; set; } = string.Empty;
    public string AiModel { get; set; } = string.Empty;
    public int RateLimitPerMinute { get; set; }
    public int RateLimitPerDay { get; set; }
    public string DataMinimizationLevel { get; set; } = string.Empty;
    public DateTime? AtualizadaEm { get; set; }

    public static EstabelecimentoIaSettingsDto De(EstabelecimentoIaSettings s) => new()
    {
        EstabelecimentoId = s.Id,
        AiEnabled = s.AiEnabled,
        AiProvider = s.AiProvider,
        AiModel = s.AiModel,
        RateLimitPerMinute = s.RateLimitPerMinute,
        RateLimitPerDay = s.RateLimitPerDay,
        DataMinimizationLevel = s.DataMinimizationLevel == NivelMinimizacaoDados.Minimized ? "minimized" : "standard",
        AtualizadaEm = s.AtualizadaEm
    };
}
