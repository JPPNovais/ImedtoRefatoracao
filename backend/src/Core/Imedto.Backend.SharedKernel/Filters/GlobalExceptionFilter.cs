using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.SharedKernel.Filters;

/// <summary>
/// Captura exceções globalmente e retorna respostas padronizadas.
/// Registre em Program.cs: builder.Services.AddControllers(o => o.Filters.Add&lt;GlobalExceptionFilter&gt;())
/// </summary>
public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;

    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is BusinessException businessEx)
        {
            context.Result = new UnprocessableEntityObjectResult(new
            {
                tipo = "ErroDeNegocio",
                mensagem = businessEx.Message
            });
            context.ExceptionHandled = true;
            return;
        }

        _logger.LogError(context.Exception, "Erro não tratado: {Mensagem}", context.Exception.Message);

        context.Result = new ObjectResult(new
        {
            tipo = "ErroInterno",
            mensagem = "Ocorreu um erro interno. Por favor, tente novamente."
        })
        {
            StatusCode = 500
        };
        context.ExceptionHandled = true;
    }
}
