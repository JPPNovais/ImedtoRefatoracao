namespace Imedto.Backend.Domain.Idempotency;

public interface IIdempotencyRepository
{
    Task<IdempotencyKey?> ObterPorKey(string key);
    Task Salvar(IdempotencyKey k);
    Task RemoverExpiradosAsync();
}
