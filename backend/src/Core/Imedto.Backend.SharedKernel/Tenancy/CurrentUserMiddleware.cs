using Microsoft.AspNetCore.Http;

namespace Imedto.Backend.SharedKernel.Tenancy;

/// <summary>
/// Middleware que popula <see cref="ICurrentTenantAccessor.UsuarioId"/> a partir
/// da claim <c>sub</c> do JWT em qualquer request autenticada — independente de
/// haver <c>[RequiresEstabelecimento]</c> na action.
///
/// <para>
/// Defense-in-depth completa: handlers podem usar <c>_tenant.UsuarioId</c> em
/// vez de confiar em campos do command/query (que poderiam ser falsificados se
/// um caller futuro nao passar pelo controller).
/// </para>
///
/// <para>
/// Roda APOS <c>UseAuthentication</c> (precisa do User populado) e ANTES dos
/// controllers. Se nao ha User autenticado ou claim sub ausente/invalida,
/// nao seta nada (UsuarioId continua Guid.Empty — comportamento atual).
/// </para>
/// </summary>
public class CurrentUserMiddleware
{
    private readonly RequestDelegate _next;

    public CurrentUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentTenantAccessor accessor)
    {
        var sub = context.User?.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(sub) && Guid.TryParse(sub, out var usuarioId))
        {
            // Cast seguro: registrado como CurrentTenantAccessor (impl unica).
            // Esta dependencia eh aceitavel: o middleware esta no mesmo namespace
            // e e o unico caller fora do RequiresEstabelecimentoAttribute.
            if (accessor is CurrentTenantAccessor concreto)
                concreto.DefinirUsuario(usuarioId);
        }

        await _next(context);
    }
}
