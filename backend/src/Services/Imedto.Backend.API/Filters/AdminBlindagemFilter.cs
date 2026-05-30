using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Imedto.Backend.Infrastructure.Admin;

namespace Imedto.Backend.API.Filters;

/// <summary>
/// Blindagem cruzada (CA9): rejeita com 403 qualquer JWT com claim
/// <c>imedto_admin = "true"</c> tentando acessar endpoints FORA de <c>/api/admin/*</c>.
///
/// Razão: admin global NÃO é usuário de tenant — nunca deve operar o app cliente.
/// O inverso (JWT de usuário comum em /api/admin/*) é bloqueado pela policy
/// <c>ImedtoAdmin</c> aplicada nos controllers de admin.
///
/// Este filter é global (registrado em Program.cs). Não afeta rotas /api/admin/*
/// nem rotas anônimas (sem autenticação).
/// </summary>
public class AdminBlindagemFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            await next();
            return;
        }

        var path = context.HttpContext.Request.Path.Value ?? string.Empty;

        // Rotas /api/admin/* são tratadas pelos controllers admin — não interferir.
        if (path.StartsWith("/api/admin/", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        // JWT com claim imedto_admin tentando acessar rota normal = 403.
        var isAdmin = user.FindFirst(ImedtoAdminTokenIssuer.AdminClaim)?.Value == "true";
        if (isAdmin)
        {
            context.Result = new ObjectResult(new
            {
                tipo = "AcessoNegado",
                mensagem = "Acesso negado."
            })
            { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }

        await next();
    }
}
