using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Imedto.Backend.SharedKernel.Tenancy;

/// <summary>
/// Exige que o papel do usuário no tenant seja um dos papéis listados.
/// Deve ser usado junto com <see cref="RequiresEstabelecimentoAttribute"/>, que já popula
/// o <see cref="ICurrentTenantAccessor"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class RequiresPapelAttribute : Attribute, IAsyncActionFilter
{
    private readonly TenantPapel[] _papeisPermitidos;

    public RequiresPapelAttribute(params TenantPapel[] papeisPermitidos)
    {
        if (papeisPermitidos.Length == 0)
            throw new ArgumentException("Informe ao menos um papel.", nameof(papeisPermitidos));
        _papeisPermitidos = papeisPermitidos;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var tenant = context.HttpContext.RequestServices.GetRequiredService<ICurrentTenantAccessor>();

        if (!tenant.TemTenantDefinido)
        {
            context.Result = new ObjectResult(new { mensagem = "Acesso negado: tenant não definido." })
            {
                StatusCode = 403
            };
            return;
        }

        if (!Enum.TryParse<TenantPapel>(tenant.Papel, out var papelAtual) ||
            !_papeisPermitidos.Contains(papelAtual))
        {
            context.Result = new ObjectResult(new
            {
                tipo = "PermissaoInsuficiente",
                mensagem = "Seu perfil de acesso não permite esta operação."
            })
            {
                StatusCode = 403
            };
            return;
        }

        await next();
    }
}
