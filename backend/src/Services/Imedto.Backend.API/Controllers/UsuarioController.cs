using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Contracts.Usuarios.Commands;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.API.Controllers;

/// <summary>Gerenciamento do próprio perfil do usuário autenticado.</summary>
[Authorize]
[ApiController]
[Route("api/usuario")]
[Produces("application/json")]
public class UsuarioController : ControllerBase
{
    private readonly ICommandBus _commandBus;

    public UsuarioController(ICommandBus commandBus)
    {
        _commandBus = commandBus;
    }

    /// <summary>Atualização parcial do próprio perfil (nome, telefone).</summary>
    /// <response code="204">Perfil atualizado.</response>
    [HttpPatch("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AtualizarPerfil([FromBody] AtualizarPerfilRequest request)
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);

        await _commandBus.Send(new AtualizarPerfilUsuarioCommand
        {
            UsuarioId = userId,
            NomeCompleto = request.NomeCompleto,
            Telefone = request.Telefone
        });

        return NoContent();
    }

    /// <summary>Finaliza o onboarding (preenche nome, CPF e marca usuário como ativo).</summary>
    /// <response code="204">Onboarding concluído.</response>
    /// <response code="422">CPF já cadastrado em outra conta ou dados inválidos.</response>
    [HttpPost("me/onboarding")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CompletarOnboarding([FromBody] OnboardingRequest request)
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);

        await _commandBus.Send(new CompletarOnboardingUsuarioCommand
        {
            UsuarioId = userId,
            NomeCompleto = request.NomeCompleto,
            Cpf = request.Cpf,
            Telefone = request.Telefone
        });

        return NoContent();
    }
}

public record AtualizarPerfilRequest(string NomeCompleto, string Telefone);
public record OnboardingRequest(string NomeCompleto, string Cpf, string Telefone);
