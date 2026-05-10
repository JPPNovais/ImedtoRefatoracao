using Microsoft.AspNetCore.Mvc.Filters;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.API.Filters;

/// <summary>
/// Verifica se o usuário autenticado possui a ação granular (formato "area.acao"
/// do <see cref="CatalogoPermissoes"/>) no estabelecimento da request. Dono do
/// estabelecimento sempre passa — a lógica fica em
/// <see cref="IModeloPermissaoRepository.UsuarioTemAcao"/>.
///
/// Aplica-se em controllers atrás de <see cref="RequiresEstabelecimentoAttribute"/>
/// (lê tenant via <see cref="ICurrentTenantAccessor"/>). Se o tenant não estiver
/// definido, falha com mensagem genérica.
///
/// Resposta de bloqueio: HTTP 422 via <see cref="BusinessException"/>. Mensagem
/// genérica — sem detalhar qual permissão faltou (defesa contra enumeration).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class RequiresAcaoAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _area;
    private readonly string? _acao;

    /// <param name="area">Área do catálogo (ex: "agenda", "pacientes").</param>
    /// <param name="acao">
    /// Ação granular (ex: "ver", "criar", "editar"). Quando null/vazio, qualquer ação
    /// dentro da área concede acesso.
    /// </param>
    public RequiresAcaoAttribute(string area, string? acao = null)
    {
        if (string.IsNullOrWhiteSpace(area))
            throw new ArgumentException("Área é obrigatória.", nameof(area));
        _area = area.Trim();
        _acao = string.IsNullOrWhiteSpace(acao) ? null : acao.Trim();
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
    {
        var sp = ctx.HttpContext.RequestServices;
        var tenant = sp.GetRequiredService<ICurrentTenantAccessor>();

        if (!tenant.TemTenantDefinido)
            throw new BusinessException("Tenant não definido — adicione [RequiresEstabelecimento] na action.");

        var modelo = sp.GetRequiredService<IModeloPermissaoRepository>();
        var temAcao = await modelo.UsuarioTemAcao(
            tenant.UsuarioId,
            tenant.EstabelecimentoId,
            _area,
            _acao,
            ctx.HttpContext.RequestAborted);

        if (!temAcao)
            throw new BusinessException("Você não tem permissão para esta operação.");

        await next();
    }
}
