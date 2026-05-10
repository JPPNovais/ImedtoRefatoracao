using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.SharedKernel.Tenancy;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Imedto.Backend.API.Controllers;

/// <summary>
/// Endpoint utilitário que retorna o contexto de tenant atual. Serve para o frontend
/// confirmar qual estabelecimento está ativo e o papel do usuário nele, além de ser
/// o menor teste possível do <c>RequiresEstabelecimentoAttribute</c>.
/// </summary>
[Authorize]
[RequiresEstabelecimento]
[ApiController]
[Route("api/tenant")]
[Produces("application/json")]
public class TenantController : ControllerBase
{
    private readonly ICurrentTenantAccessor _tenant;
    private readonly AppDbContext _db;

    public TenantController(ICurrentTenantAccessor tenant, AppDbContext db)
    {
        _tenant = tenant;
        _db = db;
    }

    /// <summary>Retorna o tenant ativo (exige header <c>X-Estabelecimento-Id</c> válido).</summary>
    [HttpGet("contexto")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Contexto() => Ok(new
    {
        estabelecimentoId = _tenant.EstabelecimentoId,
        usuarioId = _tenant.UsuarioId,
        papel = _tenant.Papel
    });

    /// <summary>
    /// Permissões granulares do usuário no tenant ativo. Usado pelo front para
    /// revalidar a UI (sidebar, botões) sem reload depois que o Dono altera o modelo
    /// do vínculo OU quando o evento SignalR <c>permissoes-alteradas</c> chega.
    /// Dono → arrays vazios + papel "Dono" → front interpreta como "tudo".
    /// </summary>
    [HttpGet("me/permissoes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MinhasPermissoes(CancellationToken ct)
    {
        // Dono passa pelo gate de tenant com papel "Dono" — sem precisar consultar o modelo.
        if (string.Equals(_tenant.Papel, "Dono", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new
            {
                papel = _tenant.Papel,
                permissoes = Array.Empty<string>(),
                permissoesExtras = Array.Empty<string>(),
            });
        }

        // Profissional: lê o JSONB do modelo de permissão do vínculo ativo.
        var resultado = await _db.Database
            .SqlQuery<PermissoesRow>($"""
                SELECT  COALESCE(mp.permissoes::text, '[]')         AS "PermissoesJson",
                        COALESCE(mp.permissoes_extras::text, '[]')  AS "PermissoesExtrasJson"
                FROM    public.vinculo_profissional_estabelecimento v
                LEFT JOIN public.modelo_permissao_estabelecimento mp ON mp.id = v.modelo_permissao_id
                WHERE   v.profissional_usuario_id = {_tenant.UsuarioId}
                  AND   v.estabelecimento_id      = {_tenant.EstabelecimentoId}
                  AND   v.status                  = 'Ativo'
                LIMIT 1
                """)
            .FirstOrDefaultAsync(ct);

        var permissoes = resultado is null
            ? Array.Empty<string>()
            : (JsonSerializer.Deserialize<List<string>>(resultado.PermissoesJson) ?? new()).ToArray();
        var extras = resultado is null
            ? Array.Empty<string>()
            : (JsonSerializer.Deserialize<List<string>>(resultado.PermissoesExtrasJson) ?? new()).ToArray();

        return Ok(new
        {
            papel = _tenant.Papel,
            permissoes,
            permissoesExtras = extras,
        });
    }

    private sealed class PermissoesRow
    {
        public string PermissoesJson { get; set; } = "[]";
        public string PermissoesExtrasJson { get; set; } = "[]";
    }
}
