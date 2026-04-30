using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Imedto.Backend.Domain.Assinaturas;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.API.Filters;

/// <summary>
/// Filter de gating de feature por tenant. Aplica em ações que exigem plano com a feature
/// informada — exemplo: <c>[FeatureGate("ia")]</c>. Resolve <see cref="IAssinaturaService"/>
/// via <c>IServiceProvider</c> da request (cache de 1 min interno).
///
/// Pré-condições:
/// <list type="bullet">
/// <item>A action precisa estar atrás de <see cref="RequiresEstabelecimentoAttribute"/> — o gate
/// usa <see cref="ICurrentTenantAccessor.EstabelecimentoId"/> populado por ele.</item>
/// <item>Não aplicar em rotas essenciais (auth, me, notificações, assinatura).</item>
/// </list>
///
/// Resposta de bloqueio: HTTP 402 (Payment Required) com payload genérico — o frontend
/// renderiza upsell. Mensagem propositalmente genérica (LGPD: nada de PII e nada de "qual plano você
/// tem hoje" para evitar enumerar tenants).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class FeatureGateAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _feature;

    public FeatureGateAttribute(string feature)
    {
        if (string.IsNullOrWhiteSpace(feature))
            throw new ArgumentException("Feature obrigatória.", nameof(feature));
        _feature = feature.Trim();
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var sp = context.HttpContext.RequestServices;
        var tenant = sp.GetRequiredService<ICurrentTenantAccessor>();

        if (!tenant.TemTenantDefinido)
        {
            // Sem tenant não temos como avaliar — exige que [RequiresEstabelecimento] esteja antes.
            context.Result = new BadRequestObjectResult(new
            {
                tipo = "TenantAusente",
                mensagem = "Requisição sem contexto de estabelecimento."
            });
            return;
        }

        var assinatura = sp.GetRequiredService<IAssinaturaService>();
        var resultado = await assinatura.AvaliarFeature(
            tenant.EstabelecimentoId, _feature, context.HttpContext.RequestAborted);

        if (resultado == ResultadoFeature.AssinaturaInativa)
        {
            // Trial expirado / Suspensa / Cancelada / Expirada / sem assinatura.
            // Frontend escuta esse tipo e redireciona para /assinatura-expirada.
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

        if (resultado == ResultadoFeature.FeatureNaoIncluida)
        {
            // Plano atual não inclui a feature — frontend abre modal de upsell.
            context.Result = new ObjectResult(new
            {
                tipo = "FeatureBloqueada",
                mensagem = "Esta funcionalidade requer um plano superior.",
                feature = _feature
            })
            {
                StatusCode = StatusCodes.Status402PaymentRequired
            };
            return;
        }

        await next();
    }
}
