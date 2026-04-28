using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Contracts.Profissionais.Commands;
using Imedto.Backend.Contracts.Profissionais.Queries;
using Imedto.Backend.Contracts.Profissionais.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.API.Controllers;

/// <summary>Cadastro profissional do usuário autenticado (1:1 com a conta).</summary>
[Authorize]
[ApiController]
[Route("api/profissional")]
[Produces("application/json")]
[RequestSizeLimit(5 * 1024 * 1024)]
public class ProfissionalController : ControllerBase
{
    private readonly ICommandBus _commandBus;
    private readonly IRequestBus _requestBus;

    public ProfissionalController(ICommandBus commandBus, IRequestBus requestBus)
    {
        _commandBus = commandBus;
        _requestBus = requestBus;
    }

    /// <summary>Retorna o cadastro profissional do próprio usuário, ou 404 se não tiver.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ProfissionalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterMe()
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);

        var dto = await _requestBus.Query<ObterProfissionalMeQuery, ProfissionalDto>(
            new ObterProfissionalMeQuery { UsuarioId = userId });

        if (dto is null)
            return NotFound();

        return Ok(dto);
    }

    /// <summary>Cria o cadastro profissional do usuário autenticado (1:1).</summary>
    /// <response code="201">Cadastrado.</response>
    /// <response code="422">Dados inválidos, já cadastrado ou registro duplicado.</response>
    [HttpPost("me")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CadastrarMe([FromBody] CadastrarProfissionalRequest request)
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);

        await _commandBus.Send(new CadastrarProfissionalCommand
        {
            UsuarioId = userId,
            Conselho = request.Conselho,
            Uf = request.Uf,
            NumeroRegistro = request.NumeroRegistro,
            Especialidade = request.Especialidade,
            Bio = request.Bio
        });

        return Created(string.Empty, null);
    }

    /// <summary>Atualiza o cadastro profissional do próprio usuário.</summary>
    [HttpPut("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AtualizarMe([FromBody] AtualizarProfissionalRequest request)
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);

        await _commandBus.Send(new AtualizarProfissionalCommand
        {
            UsuarioId = userId,
            Conselho = request.Conselho,
            Uf = request.Uf,
            NumeroRegistro = request.NumeroRegistro,
            Especialidade = request.Especialidade,
            Bio = request.Bio
        });

        return NoContent();
    }

    /// <summary>Faz upload da foto de perfil do profissional autenticado (multipart).</summary>
    /// <remarks>Aceita JPG, PNG, WebP e GIF até 2 MB. Retorna a URL pública atualizada.</remarks>
    /// <response code="200">Foto atualizada — corpo: <c>{ fotoUrl }</c>.</response>
    /// <response code="422">Arquivo inválido (vazio, tipo não suportado ou maior que 2 MB).</response>
    [HttpPut("me/foto")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AlterarFoto([FromForm] IFormFile arquivo)
    {
        var validacao = ValidarFoto(arquivo);
        if (validacao is { } erro)
            return UnprocessableEntity(new { tipo = "ErroDeNegocio", mensagem = erro });

        var userId = Guid.Parse(User.FindFirst("sub")!.Value);
        var ext = Path.GetExtension(arquivo.FileName);

        await using var stream = arquivo.OpenReadStream();
        await _commandBus.Send(new AlterarFotoProfissionalCommand
        {
            UsuarioId = userId,
            MimeType = arquivo.ContentType,
            Extensao = ext,
            Conteudo = stream
        });

        var dto = await _requestBus.Query<ObterProfissionalMeQuery, ProfissionalDto>(
            new ObterProfissionalMeQuery { UsuarioId = userId });

        return Ok(new { fotoUrl = dto?.FotoUrl });
    }

    private static string ValidarFoto(IFormFile arquivo)
    {
        if (arquivo is null || arquivo.Length == 0)
            return "Arquivo vazio.";
        if (arquivo.Length > 2 * 1024 * 1024)
            return "Foto deve ter no máximo 2 MB.";

        var mimesAceitos = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
        if (!mimesAceitos.Contains(arquivo.ContentType?.ToLowerInvariant()))
            return "Formato não suportado. Use JPG, PNG, WebP ou GIF.";

        return null;
    }
}

public record CadastrarProfissionalRequest(
    string Conselho, string Uf, string NumeroRegistro, string Especialidade, string Bio);

public record AtualizarProfissionalRequest(
    string Conselho, string Uf, string NumeroRegistro, string Especialidade, string Bio);
