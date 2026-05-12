using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Imedto.Backend.Domain.Assinaturas;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.API.Filters;

/// <summary>
/// Bloqueia a action quando a assinatura do estabelecimento atual NÃO está ativa
/// (Trial expirado, Suspensa, Cancelada, Expirada ou inexistente). Diferente do
/// <see cref="FeatureGateAttribute"/>: este filter não exige feature específica —
/// gate genérico para endpoints "core" que não estão atrás de FeatureGate (ex:
/// agendamento, paciente, financeiro). Defense-in-depth do guard do front (router
/// já redireciona pra /assinatura-expirada, mas chamadas API diretas precisam
/// também ser bloqueadas).
///
/// Pré-condição: <see cref="RequiresEstabelecimentoAttribute"/> populando o tenant.
///
/// Resposta de bloqueio: HTTP 402 (Payment Required) — mesma forma do FeatureGate
/// para o interceptor do front lidar uniforme (modal upsell / redirect).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RequiresAssinaturaAtivaAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var sp = context.HttpContext.RequestServices;
        var tenant = sp.GetRequiredService<ICurrentTenantAccessor>();

        if (!tenant.TemTenantDefinido)
        {
            context.Result = new BadRequestObjectResult(new
            {
                tipo = "TenantAusente",
                mensagem = "Requisição sem contexto de estabelecimento."
            });
            return;
        }

        var assinatura = sp.GetRequiredService<IAssinaturaService>();
        var ativo = await assinatura.TenantEstaAtivo(
            tenant.EstabelecimentoId, context.HttpContext.RequestAborted);

        if (!ativo)
        {
            context.Result = new ObjectResult(new
            {
                tipo = "AssinaturaInativa",
                mensagem = "Sua assinatura está inativa. Renove para continuar usando o Imedto."
            })
            {
                StatusCode = StatusCodes.Status402PaymentRequired
            };
            return;
        }

        await next();
    }
}
