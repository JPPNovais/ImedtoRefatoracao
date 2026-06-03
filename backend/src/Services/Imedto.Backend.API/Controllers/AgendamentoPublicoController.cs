using Imedto.Backend.Application.Agendamentos.Commands;
using Imedto.Backend.Contracts.Agendamentos.Commands;
using Imedto.Backend.Contracts.Agendamentos.Queries;
using Imedto.Backend.Contracts.Agendamentos.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Imedto.Backend.API.Controllers;

/// <summary>
/// Endpoints anônimos do fluxo de confirmação de presença via link público (Fase 2).
///
/// <para>Segurança:</para>
/// <list type="bullet">
///   <item><c>[AllowAnonymous]</c> — autenticação é o próprio token (256 bits de entropia).</item>
///   <item><c>[EnableRateLimiting("agendamentos-publico")]</c> — 10 req/min por IP, anti-enumeração.</item>
///   <item>Todos os erros (token inválido / expirado / cancelado) devolvem <b>410 Gone</b>
///         com a mesma mensagem genérica, para não vazar se o token existe ou não (CA19).</item>
/// </list>
///
/// <para>LGPD (CA17/CA23): payload retorna apenas estab.NomeFantasia, profissional.NomeCompleto,
/// TipoServico e data/hora. Sem paciente_id, estabelecimento_id, CPF ou e-mail.</para>
/// </summary>
[AllowAnonymous]
[ApiController]
[Produces("application/json")]
[EnableRateLimiting("agendamentos-publico")]
public class AgendamentoPublicoController : ControllerBase
{
    private readonly ICommandBus _commandBus;
    private readonly IRequestBus _requestBus;

    public AgendamentoPublicoController(ICommandBus commandBus, IRequestBus requestBus)
    {
        _commandBus = commandBus;
        _requestBus = requestBus;
    }

    /// <summary>
    /// Consultar resumo do agendamento via token público (CA17/CA23).
    /// 200 retorna apenas dados mínimos sem PII. 410 quando token inválido/expirado/cancelado.
    /// </summary>
    [HttpGet("api/publico/agendamentos/confirmar/{token}")]
    [ProducesResponseType(typeof(ConfirmacaoPublicaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<IActionResult> Consultar(string token)
    {
        try
        {
            var dto = await _requestBus.Query<ConsultarConfirmacaoPublicaQuery, ConfirmacaoPublicaDto>(
                new ConsultarConfirmacaoPublicaQuery
                {
                    Token = token,
                    IpOrigem = ResolverIp(),
                    UserAgent = ResolverUserAgent(),
                });
            return Ok(dto);
        }
        catch (BusinessException ex)
        {
            // CA19: qualquer erro de negócio aqui é "link inválido/expirado/cancelado" — 410 genérico.
            return StatusCode(StatusCodes.Status410Gone, new
            {
                tipo = "LinkInvalido",
                mensagem = ex.Message,
            });
        }
    }

    /// <summary>
    /// Confirmar presença via token público (CA18/CA20).
    /// Idempotência: já confirmado → 200 "Presença já confirmada". Erro → 410 genérico.
    /// </summary>
    [HttpPost("api/publico/agendamentos/confirmar/{token}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<IActionResult> Confirmar(string token)
    {
        var cmd = new ConfirmarPresencaPublicaCommand
        {
            Token = token,
            IpOrigem = ResolverIp(),
            UserAgent = ResolverUserAgent(),
        };

        try
        {
            await _commandBus.Send(cmd);
        }
        catch (BusinessException ex) when (ex.Message == ConfirmarPresencaPublicaCommandHandler.MensagemLinkInvalido)
        {
            // CA19: 410 genérico idêntico para token inválido/expirado/cancelado.
            return StatusCode(StatusCodes.Status410Gone, new
            {
                tipo = "LinkInvalido",
                mensagem = ex.Message,
            });
        }

        // CA20: idempotência — já confirmado → 200 sem alterar estado.
        if (cmd.Resultado == ResultadoConfirmacaoPresenca.JaConfirmado)
        {
            return Ok(new
            {
                resultado = "ja_confirmado",
                mensagem = "Presença já confirmada. Você pode fechar esta página.",
            });
        }

        return Ok(new
        {
            resultado = "confirmado",
            mensagem = "Presença confirmada com sucesso! Você pode fechar esta página.",
        });
    }

    private string? ResolverIp()
    {
        var fwd = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(fwd))
        {
            var primeiro = fwd.Split(',')[0].Trim();
            if (!string.IsNullOrEmpty(primeiro)) return primeiro;
        }
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private string? ResolverUserAgent()
        => Request.Headers["User-Agent"].ToString();
}
