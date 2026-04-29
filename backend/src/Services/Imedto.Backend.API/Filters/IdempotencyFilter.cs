using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Imedto.Backend.Domain.Idempotency;

namespace Imedto.Backend.API.Filters;

/// <summary>
/// Filtro global que implementa idempotência para endpoints marcados com <see cref="IdempotentAttribute"/>.
///
/// Comportamento:
/// - Sem header <c>Idempotency-Key</c> → executa normalmente.
/// - Com header + chave nova → executa, persiste status + body, TTL 24h.
/// - Com header + chave existente + mesmo hash → retorna response cacheada (sem efeito colateral).
/// - Com header + chave existente + hash diferente → 409 Conflict.
/// </summary>
public class IdempotencyFilter : IAsyncActionFilter
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);

    private readonly IIdempotencyRepository _repo;

    public IdempotencyFilter(IIdempotencyRepository repo)
    {
        _repo = repo;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Só age em endpoints marcados com [Idempotent]
        var temAtributo = context.ActionDescriptor.EndpointMetadata
            .OfType<IdempotentAttribute>()
            .Any();

        if (!temAtributo)
        {
            await next();
            return;
        }

        var key = context.HttpContext.Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(key))
        {
            await next();
            return;
        }

        if (key.Length > 80)
        {
            context.Result = new BadRequestObjectResult(new
            {
                mensagem = "Idempotency-Key não pode ter mais de 80 caracteres."
            });
            return;
        }

        var hashPayload = await ComputarHashAsync(context.HttpContext.Request);

        var registroExistente = await _repo.ObterPorKey(key);

        if (registroExistente is not null)
        {
            if (registroExistente.EstaExpirado())
            {
                // Expirado — trata como nova requisição (remove o antigo e processa)
                // A limpeza em massa é feita pelo job. Aqui apenas deixamos prosseguir.
            }
            else if (registroExistente.HashPayload != hashPayload)
            {
                context.Result = new ConflictObjectResult(new
                {
                    mensagem = "A chave de idempotência já foi usada com um payload diferente."
                });
                return;
            }
            else
            {
                // Mesmo key + mesmo hash → retorna resposta cacheada
                context.HttpContext.Response.StatusCode = registroExistente.StatusCode;
                context.Result = new ContentResult
                {
                    StatusCode = registroExistente.StatusCode,
                    ContentType = "application/json",
                    Content = registroExistente.ResponseJson
                };
                return;
            }
        }

        // Executa a action normalmente
        var executedContext = await next();

        // Captura status e body para persistir
        if (executedContext.Result is ObjectResult objectResult)
        {
            var statusCode = objectResult.StatusCode ?? 200;
            var responseJson = JsonSerializer.Serialize(objectResult.Value);

            var registro = IdempotencyKey.Registrar(key, hashPayload, statusCode, responseJson, Ttl);
            await _repo.Salvar(registro);
        }
        else if (executedContext.Result is StatusCodeResult statusCodeResult)
        {
            var registro = IdempotencyKey.Registrar(key, hashPayload, statusCodeResult.StatusCode, "{}", Ttl);
            await _repo.Salvar(registro);
        }
    }

    private static async Task<string> ComputarHashAsync(HttpRequest request)
    {
        request.EnableBuffering();
        using var ms = new MemoryStream();
        await request.Body.CopyToAsync(ms);
        request.Body.Position = 0;

        var bytes = ms.ToArray();
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
