namespace Imedto.Backend.Domain.Idempotency;

/// <summary>
/// Aggregate que armazena a chave de idempotência de um command externo.
/// Garante que requests duplicados com a mesma <see cref="Key"/> retornem
/// a mesma resposta sem executar o efeito colateral novamente.
/// PK é <see cref="Key"/> (string) — não usa o Id numérico padrão.
/// </summary>
public class IdempotencyKey
{
    public virtual string Key { get; protected set; }
    public virtual string HashPayload { get; protected set; }
    public virtual int StatusCode { get; protected set; }
    public virtual string ResponseJson { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime ExpiraEm { get; protected set; }

    protected IdempotencyKey() { }

    public static IdempotencyKey Registrar(
        string key,
        string hashPayload,
        int statusCode,
        string responseJson,
        TimeSpan ttl)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Chave de idempotência é obrigatória.", nameof(key));
        if (string.IsNullOrWhiteSpace(hashPayload))
            throw new ArgumentException("Hash do payload é obrigatório.", nameof(hashPayload));

        var agora = DateTime.UtcNow;
        return new IdempotencyKey
        {
            Key = key.Trim(),
            HashPayload = hashPayload,
            StatusCode = statusCode,
            ResponseJson = responseJson ?? "{}",
            CriadoEm = agora,
            ExpiraEm = agora.Add(ttl)
        };
    }

    public bool EstaExpirado() => DateTime.UtcNow > ExpiraEm;
}
