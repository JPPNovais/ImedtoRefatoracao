using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Imedto.Backend.SharedKernel.Tenancy;

/// <summary>
/// Action filter que exige o header <c>X-Estabelecimento-Id</c>, valida se o usuário
/// autenticado é dono OU tem vínculo ativo e popula o <see cref="ICurrentTenantAccessor"/>
/// da request. Quando falha:
/// - 401 se o usuário não está autenticado;
/// - 400 se o header está ausente/inválido;
/// - 403 se o usuário não tem acesso ao estabelecimento;
/// - 404 se o estabelecimento não existe.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class RequiresEstabelecimentoAttribute : Attribute, IAsyncActionFilter
{
    public const string HeaderName = "X-Estabelecimento-Id";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;

        var subClaim = http.User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(subClaim) || !Guid.TryParse(subClaim, out var usuarioId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var headerValue = http.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(headerValue) || !long.TryParse(headerValue, out var estabelecimentoId))
        {
            context.Result = new BadRequestObjectResult(new
            {
                tipo = "TenantAusente",
                mensagem = $"Header obrigatório: {HeaderName}."
            });
            return;
        }

        var resolver = http.RequestServices.GetRequiredService<ITenantAccessResolver>();
        var papel = await resolver.ResolverPapelAsync(usuarioId, estabelecimentoId);

        if (papel == TenantPapel.NaoEncontrado)
        {
            context.Result = new NotFoundObjectResult(new { mensagem = "Estabelecimento não encontrado." });
            return;
        }

        if (papel == TenantPapel.SemAcesso)
        {
            context.Result = new ObjectResult(new
            {
                tipo = "SemAcesso",
                mensagem = "Você não tem acesso a este estabelecimento."
            })
            {
                StatusCode = 403
            };
            return;
        }

        var tenant = (CurrentTenantAccessor)http.RequestServices.GetRequiredService<ICurrentTenantAccessor>();
        tenant.Definir(estabelecimentoId, usuarioId, papel.ToString());

        await next();
    }
}

public enum TenantPapel
{
    NaoEncontrado,
    SemAcesso,
    Dono,
    Profissional,
    Recepcionista
}

/// <summary>
/// Resolve o papel de um usuário em um estabelecimento. Implementado na Infrastructure
/// (tem acesso a repositórios). Abstraído aqui para o filter viver no SharedKernel sem
/// depender de Infrastructure diretamente.
/// </summary>
public interface ITenantAccessResolver
{
    Task<TenantPapel> ResolverPapelAsync(Guid usuarioId, long estabelecimentoId);
}
