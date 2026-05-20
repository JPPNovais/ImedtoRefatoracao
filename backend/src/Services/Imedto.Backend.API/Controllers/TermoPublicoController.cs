using Imedto.Backend.Application.Termos.Commands;
using Imedto.Backend.Application.Termos.Queries;
using Imedto.Backend.Contracts.Termos.Commands;
using Imedto.Backend.Contracts.Termos.Dtos;
using Imedto.Backend.Contracts.Termos.Queries;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Imedto.Backend.API.Controllers;

/// <summary>
/// Endpoints anônimos do fluxo de aceite/recusa via link público (Fase 4).
///
/// <para>Segurança:</para>
/// <list type="bullet">
///   <item><c>[AllowAnonymous]</c> — autenticação é o próprio token (256 bits de entropia).</item>
///   <item><c>[EnableRateLimiting("termos-publico")]</c> — 10 req/min por IP, anti-enumeração.</item>
///   <item>Todos os erros (token inválido / expirado / já respondido) devolvem <b>410 Gone</b>
///         com a mesma mensagem genérica, para não vazar se o token existe ou não.</item>
///   <item>Hash de integridade do snapshot validado a cada aceite (defense-in-depth).</item>
/// </list>
///
/// <para>LGPD: retornamos apenas estab.NomeFantasia, profissional.NomeCompleto, título e
/// conteúdo do termo. Sem paciente_id, sem CPF, sem e-mail.</para>
/// </summary>
[AllowAnonymous]
[ApiController]
[Produces("application/json")]
[EnableRateLimiting("termos-publico")]
public class TermoPublicoController : ControllerBase
{
    private readonly ICommandBus _commandBus;
    private readonly IRequestBus _requestBus;

    public TermoPublicoController(ICommandBus commandBus, IRequestBus requestBus)
    {
        _commandBus = commandBus;
        _requestBus = requestBus;
    }

    /// <summary>
    /// Visualizar conteúdo do termo via token público. 200 retorna apenas dados mínimos
    /// (sem PII do paciente). 410 quando token inválido/expirado/já respondido — mensagem
    /// genérica idêntica em todos os casos para evitar enumeração.
    /// </summary>
    [HttpGet("api/publico/termos/aceite/{token}")]
    [ProducesResponseType(typeof(TermoPublicoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<IActionResult> Visualizar(string token)
    {
        try
        {
            var dto = await _requestBus.Query<ObterTermoPublicoPorTokenQuery, TermoPublicoDto>(new ObterTermoPublicoPorTokenQuery
            {
                Token = token,
                IpOrigem = ResolverIp(),
                UserAgent = ResolverUserAgent(),
            });
            return Ok(dto);
        }
        catch (BusinessException ex)
        {
            // Qualquer erro de negócio aqui é "link inválido/expirado/respondido" — 410 genérico.
            return StatusCode(StatusCodes.Status410Gone, new
            {
                tipo = "LinkInvalido",
                mensagem = ex.Message,
            });
        }
    }

    /// <summary>
    /// Registrar aceite/recusa via token. Body: <c>{ aceito: bool, nomeConfirmado?: string }</c>.
    ///
    /// Idempotência: se o termo já foi respondido (independente de quando), devolve 200 com
    /// mensagem padrão "Termo já respondido" — NUNCA altera estado.
    /// </summary>
    [HttpPost("api/publico/termos/aceite/{token}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RegistrarResposta(string token, [FromBody] RegistrarRespostaRequest request)
    {
        if (request is null)
            return StatusCode(StatusCodes.Status410Gone, new { tipo = "LinkInvalido", mensagem = RegistrarRespostaPublicaTermoCommandHandler.MensagemLinkInvalido });

        var cmd = new RegistrarRespostaPublicaTermoCommand
        {
            TokenAceite = token,
            Aceito = request.Aceito,
            NomeConfirmado = request.NomeConfirmado,
            IpOrigem = ResolverIp(),
            UserAgent = ResolverUserAgent(),
        };

        try
        {
            await _commandBus.Send(cmd);
        }
        catch (BusinessException ex) when (ex.Message == RegistrarRespostaPublicaTermoCommandHandler.MensagemLinkInvalido)
        {
            return StatusCode(StatusCodes.Status410Gone, new
            {
                tipo = "LinkInvalido",
                mensagem = ex.Message,
            });
        }
        // Outras BusinessExceptions (nome incorreto) seguem o pipeline padrão → 422.

        if (cmd.Resultado == ResultadoRespostaPublica.JaRespondido)
        {
            return Ok(new
            {
                resultado = "ja_respondido",
                mensagem = "Termo já foi respondido. Você pode fechar esta página.",
            });
        }

        return Ok(new
        {
            resultado = "registrado",
            mensagem = request.Aceito
                ? "Termo aceito. Você pode fechar esta página."
                : "Recusa registrada. Você pode fechar esta página.",
        });
    }

    private string ResolverIp()
    {
        var fwd = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(fwd))
        {
            var primeiro = fwd.Split(',')[0].Trim();
            if (!string.IsNullOrEmpty(primeiro)) return primeiro;
        }
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private string ResolverUserAgent()
        => Request.Headers["User-Agent"].ToString();
}

public record RegistrarRespostaRequest(bool Aceito, string NomeConfirmado = null);
