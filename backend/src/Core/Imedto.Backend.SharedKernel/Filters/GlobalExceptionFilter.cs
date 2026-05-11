using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.SharedKernel.Filters;

/// <summary>
/// Captura exceções globalmente e retorna respostas padronizadas.
/// Em Development inclui detalhe da exceção (tipo + mensagem + stack trace) para diagnóstico.
/// Em produção mantém mensagem genérica (LGPD/segurança).
/// Registre em Program.cs: builder.Services.AddControllers(o => o.Filters.Add&lt;GlobalExceptionFilter&gt;())
/// </summary>
public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
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

        if (context.Exception is ForbiddenException forbiddenEx)
        {
            context.Result = new ObjectResult(new
            {
                tipo = "SemPermissao",
                mensagem = forbiddenEx.Message
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            context.ExceptionHandled = true;
            return;
        }

        _logger.LogError(context.Exception, "Erro não tratado em {Path}: {Mensagem}",
            context.HttpContext.Request.Path, context.Exception.Message);

        object body = _env.IsDevelopment()
            ? new
            {
                tipo = "ErroInterno",
                mensagem = "Ocorreu um erro interno. Por favor, tente novamente.",
                detalhe = context.Exception.GetType().FullName + ": " + context.Exception.Message,
                inner = context.Exception.InnerException is null
                    ? null
                    : context.Exception.InnerException.GetType().FullName + ": " + context.Exception.InnerException.Message,
                stackTrace = context.Exception.StackTrace
            }
            : (object)new
            {
                tipo = "ErroInterno",
                mensagem = "Ocorreu um erro interno. Por favor, tente novamente."
            };

        context.Result = new ObjectResult(body) { StatusCode = 500 };
        context.ExceptionHandled = true;
    }
}
