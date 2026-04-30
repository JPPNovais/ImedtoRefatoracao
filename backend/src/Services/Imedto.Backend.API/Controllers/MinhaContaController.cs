using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Contracts.Lgpd.Commands;
using Imedto.Backend.Contracts.Lgpd.Queries;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.API.Controllers;

/// <summary>
/// Endpoints de "minha conta" do titular autenticado.
/// Implementa os direitos LGPD de portabilidade (GET) e exclusão/esquecimento (DELETE).
/// </summary>
[Authorize]
[ApiController]
[Route("api/minha-conta")]
[Produces("application/json")]
public class MinhaContaController : ControllerBase
{
    private readonly ICommandBus _commandBus;
    private readonly IRequestBus _requestBus;

    public MinhaContaController(ICommandBus commandBus, IRequestBus requestBus)
    {
        _commandBus = commandBus;
        _requestBus = requestBus;
    }

    /// <summary>
    /// Exporta os dados de conta do titular (Art. 18 LGPD — direito de portabilidade).
    ///
    /// Retorna dados de conta, vínculos, notificações e consentimentos.
    /// Dados clínicos profundos (prontuário, receitas) são omitidos nesta versão — TODO 4.3-V2.
    /// </summary>
    [HttpGet("exportar-dados")]
    [ProducesResponseType(typeof(MeusDadosLgpdDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportarDados()
    {
        var dados = await _requestBus.Query<ExportarMeusDadosQuery, MeusDadosLgpdDto>(
            new ExportarMeusDadosQuery { UsuarioId = ObterUsuarioId() });
        return Ok(dados);
    }

    /// <summary>
    /// Anonimiza a conta do titular (Art. 18 LGPD — direito ao esquecimento).
    ///
    /// Não realiza exclusão física: substitui PII por valores neutros e registra em audit.
    /// O frontend deve chamar revoke de sessão no Supabase e redirecionar para logout.
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AnonimizarConta()
    {
        await _commandBus.Send(new AnonimizarMinhaContaCommand
        {
            UsuarioId = ObterUsuarioId()
        });

        // 204 sem corpo. O frontend interpreta este status como sinal para revogar a sessão
        // no Supabase e redirecionar para /login — o token ainda é válido até expirar ou revoke.
        return NoContent();
    }

    private Guid ObterUsuarioId() => Guid.Parse(User.FindFirst("sub")!.Value);
}
