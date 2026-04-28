namespace Imedto.Backend.SharedKernel.Domain;

/// <summary>
/// Exceção de negócio. Use no lugar de throw genérico para erros de domínio esperados.
/// Capturada pelo GlobalExceptionFilter e retornada como HTTP 422 (UnprocessableEntity).
/// </summary>
public class BusinessException : Exception
{
    public BusinessException(string message) : base(message) { }

    public BusinessException(string message, Exception inner) : base(message, inner) { }
}
