using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.API.Filters;
using Imedto.Backend.Domain.Assinaturas;
using Imedto.Backend.Domain.Ia;
using System.Text.Json;

namespace Imedto.Backend.API.Controllers;

[ApiController]
[Route("api/ia")]
[Authorize]
[FeatureGate(Features.Ia)]
public class IaController : ControllerBase
{
    private readonly IIaService _ia;

    public IaController(IIaService ia) => _ia = ia;

    [HttpPost("sugestao-secao")]
    public async Task SugestaoSecao([FromBody] SugestaoSecaoProntuarioRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.SecaoAlvoTitulo))
        {
            Response.StatusCode = 400;
            await Response.WriteAsync("SecaoAlvoTitulo é obrigatório.", ct);
            return;
        }

        Response.ContentType = "text/event-stream; charset=utf-8";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        try
        {
            await foreach (var chunk in _ia.SugerirSecaoProntuarioAsync(request, ct))
            {
                var data = JsonSerializer.Serialize(new { text = chunk });
                await Response.WriteAsync($"data: {data}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }
        }
        catch (InvalidOperationException ex)
        {
            var erro = JsonSerializer.Serialize(new { erro = ex.Message });
            await Response.WriteAsync($"data: {erro}\n\n", ct);
        }

        await Response.WriteAsync("data: [DONE]\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }
}
