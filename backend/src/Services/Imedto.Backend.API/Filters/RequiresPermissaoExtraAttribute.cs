using Microsoft.AspNetCore.Mvc.Filters;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.API.Filters;

/// <summary>
/// Verifica se o usuário autenticado possui a permissão fina informada no estabelecimento
/// da request (catálogo em <see cref="PermissoesExtras"/>). O dono do estabelecimento
/// sempre passa — a lógica fica em <see cref="IModeloPermissaoRepository.UsuarioTemPermissaoExtra"/>.
///
/// Funciona em dois modos:
/// 1. Com tenant: quando a action está atrás de <see cref="RequiresEstabelecimentoAttribute"/>,
///    usa <see cref="ICurrentTenantAccessor"/> (populado pelo filter de tenant).
/// 2. Com route key: quando não há tenant, lê o id do estabelecimento do route value indicado
///    pelo segundo parâmetro do construtor. Útil para controllers como
///    <c>EstabelecimentoController</c> que usam o id como route param.
///
/// Resposta de bloqueio: HTTP 403 via <see cref="ForbiddenException"/> (tratado pelo
/// <c>GlobalExceptionFilter</c>). Mensagem genérica — sem detalhar qual permissão faltou.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RequiresPermissaoExtraAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _permissao;
    private readonly string? _estabelecimentoIdRouteKey;

    /// <param name="permissao">Constante de <see cref="PermissoesExtras"/>.</param>
    /// <param name="estabelecimentoIdRouteKey">
    /// Nome do route value com o id do estabelecimento. Use quando a action não está
    /// atrás de <see cref="RequiresEstabelecimentoAttribute"/> (ex: "id" em <c>{id:long}</c>).
    /// </param>
    public RequiresPermissaoExtraAttribute(string permissao, string? estabelecimentoIdRouteKey = null)
    {
        if (string.IsNullOrWhiteSpace(permissao))
            throw new ArgumentException("Permissão é obrigatória.", nameof(permissao));
        _permissao = permissao.Trim();
        _estabelecimentoIdRouteKey = estabelecimentoIdRouteKey;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
    {
        var sp = ctx.HttpContext.RequestServices;
        var modelo = sp.GetRequiredService<IModeloPermissaoRepository>();

        var (usuarioId, estabelecimentoId) = ResolverIds(ctx, sp);

        var temPermissao = await modelo.UsuarioTemPermissaoExtra(
            usuarioId,
            estabelecimentoId,
            _permissao,
            ctx.HttpContext.RequestAborted);

        if (!temPermissao)
            throw new ForbiddenException("Você não tem permissão para esta operação.");

        await next();
    }

    private (Guid UsuarioId, long EstabelecimentoId) ResolverIds(
        ActionExecutingContext ctx, IServiceProvider sp)
    {
        var tenant = sp.GetRequiredService<ICurrentTenantAccessor>();

        if (tenant.TemTenantDefinido)
            return (tenant.UsuarioId, tenant.EstabelecimentoId);

        // Fallback: lê userId do JWT e estabelecimentoId do route value.
        var subClaim = ctx.HttpContext.User.FindFirst("sub")?.Value
            ?? throw new ForbiddenException("Usuário não autenticado.");
        var usuarioId = Guid.Parse(subClaim);

        var routeKey = _estabelecimentoIdRouteKey ?? "id";
        if (!ctx.RouteData.Values.TryGetValue(routeKey, out var routeVal)
            || !long.TryParse(routeVal?.ToString(), out var estabelecimentoId))
        {
            throw new ForbiddenException("Não foi possível determinar o estabelecimento para verificar permissões.");
        }

        return (usuarioId, estabelecimentoId);
    }
}
