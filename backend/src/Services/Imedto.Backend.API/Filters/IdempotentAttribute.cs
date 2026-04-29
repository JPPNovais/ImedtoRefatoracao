namespace Imedto.Backend.API.Filters;

/// <summary>
/// Marca um endpoint para participar do mecanismo de idempotência.
/// Quando presente, o <see cref="IdempotencyFilter"/> inspeciona o header
/// <c>Idempotency-Key</c> e retorna a resposta cacheada em caso de request repetida.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class IdempotentAttribute : Attribute { }
