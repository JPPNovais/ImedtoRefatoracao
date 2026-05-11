namespace Imedto.Backend.SharedKernel.Domain;

/// <summary>
/// Falha de autorização (papel/permissão insuficiente). Mapeada pelo
/// <c>GlobalExceptionFilter</c> para HTTP 403 Forbidden — não confundir com
/// <see cref="BusinessException"/>, que representa regra de negócio (422).
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}
