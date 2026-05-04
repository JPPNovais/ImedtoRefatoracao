using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.SharedKernel.Filters;

namespace Imedto.Backend.API.Filters;

/// <summary>
/// Bloqueia todas as requisições autenticadas cujo usuário ainda não completou o onboarding
/// (onboarding_completo = false no banco). Retorna 403 para forçar o frontend a redirecionar
/// para /onboarding antes de qualquer operação.
///
/// Endpoints isentos devem ser marcados com [AllowBeforeOnboarding] ou residir no AuthController.
/// O resultado é cacheado 2 min em memória para evitar consulta ao banco a cada request.
/// </summary>
public class OnboardingCompletadoFilter : IAsyncActionFilter
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);

    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IMemoryCache _cache;

    public OnboardingCompletadoFilter(IUsuarioRepository usuarioRepository, IMemoryCache cache)
    {
        _usuarioRepository = usuarioRepository;
        _cache = cache;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Ignora requisições não autenticadas — o [Authorize] de cada controller já trata isso.
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            await next();
            return;
        }

        // Endpoints marcados como isentos passam livremente.
        var temBypass = context.ActionDescriptor.EndpointMetadata
            .OfType<AllowBeforeOnboardingAttribute>()
            .Any();
        if (temBypass)
        {
            await next();
            return;
        }

        var sub = context.HttpContext.User.FindFirst("sub")?.Value;
        if (sub is null || !Guid.TryParse(sub, out var usuarioId))
        {
            await next();
            return;
        }

        var cacheKey = $"onboarding:{usuarioId}";
        if (!_cache.TryGetValue(cacheKey, out bool onboardingCompleto))
        {
            var usuario = await _usuarioRepository.ObterPorIdOuNulo(usuarioId);
            onboardingCompleto = usuario?.OnboardingCompleto ?? false;
            _cache.Set(cacheKey, onboardingCompleto, CacheTtl);
        }

        if (!onboardingCompleto)
        {
            context.Result = new ObjectResult(new
            {
                tipo = "OnboardingPendente",
                mensagem = "Complete o cadastro inicial antes de continuar."
            })
            { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }

        await next();
    }
}
