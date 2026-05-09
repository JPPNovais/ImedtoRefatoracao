using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.API.Filters;
using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Domain.Auth;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.API.Controllers;

/// <summary>Fluxos de convite e aceite de vínculo profissional×estabelecimento.</summary>
[Authorize]
[ApiController]
[Produces("application/json")]
public class VinculoController : ControllerBase
{
    private readonly ICommandBus _commandBus;
    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _env;

    public VinculoController(ICommandBus commandBus, IAuthService authService, IWebHostEnvironment env)
    {
        _commandBus = commandBus;
        _authService = authService;
        _env = env;
    }

    /// <summary>Dono ou usuário com permissão convida um profissional (por e-mail) para o estabelecimento.</summary>
    /// <remarks>
    /// Se o e-mail ainda não tem conta, ela é criada automaticamente e um magic link de
    /// invite é gerado. Em produção, o link é enviado por e-mail. Em desenvolvimento, o
    /// link é retornado no body para facilitar testes.
    /// </remarks>
    /// <response code="201">Convite criado. Em dev inclui <c>actionLink</c> no body.</response>
    /// <response code="422">Dados inválidos ou profissional já vinculado.</response>
    [HttpPost("/api/estabelecimento/{estabelecimentoId:long}/profissionais/convidar")]
    [RequiresPermissaoExtra(PermissoesExtras.GerirProfissionais, "estabelecimentoId")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ConvidarProfissional(long estabelecimentoId, [FromBody] ConvidarProfissionalRequest request)
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);

        // Controller orquestra: cria/identifica a conta de auth, depois dispara o command de domínio.
        var convite = await _authService.CriarConviteAsync(request.Email);
        var profUserId = Guid.Parse(convite.User.Id);

        await _commandBus.Send(new ConvidarProfissionalCommand
        {
            EstabelecimentoId = estabelecimentoId,
            ConvidadoPorUsuarioId = userId,
            ProfissionalUsuarioId = profUserId,
            ProfissionalEmail = convite.User.Email,
            ModeloPermissaoId = request.ModeloPermissaoId,
            Nome = request.Nome,
            Telefone = request.Telefone,
            Especialidade = request.Especialidade
        });

        if (_env.IsDevelopment())
        {
            return StatusCode(StatusCodes.Status201Created, new
            {
                profissionalUsuarioId = profUserId,
                email = convite.User.Email,
                jaExistia = convite.JaExistia,
                actionLink = convite.ActionLink
            });
        }

        return StatusCode(StatusCodes.Status201Created, null);
    }

    /// <summary>Profissional aceita um convite pendente.</summary>
    /// <response code="204">Convite aceito.</response>
    /// <response code="404">Vínculo não encontrado.</response>
    /// <response code="422">Convite já aceito/inativo ou usuário não é o convidado.</response>
    [HttpPost("/api/vinculo/{id:long}/aceitar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Aceitar(long id)
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);

        await _commandBus.Send(new AceitarConviteCommand
        {
            VinculoId = id,
            UsuarioSolicitanteId = userId
        });

        return NoContent();
    }

    /// <summary>Inativa um vínculo (dono ou o próprio profissional).</summary>
    /// <response code="204">Vínculo inativado.</response>
    /// <response code="404">Vínculo não encontrado.</response>
    /// <response code="422">Usuário sem permissão ou vínculo já inativo.</response>
    [HttpPost("/api/vinculo/{id:long}/inativar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Inativar(long id)
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);

        await _commandBus.Send(new InativarVinculoCommand
        {
            VinculoId = id,
            UsuarioSolicitanteId = userId
        });

        return NoContent();
    }
}

public record ConvidarProfissionalRequest(
    string Email,
    long? ModeloPermissaoId,
    string Nome = null,
    string Telefone = null,
    string Especialidade = null);
