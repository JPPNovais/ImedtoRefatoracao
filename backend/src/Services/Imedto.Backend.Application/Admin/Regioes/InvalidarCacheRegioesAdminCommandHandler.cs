namespace Imedto.Backend.Application.Admin.Regioes;

/// <summary>
/// Invalida manualmente o cache do catálogo de regiões anatômicas (endpoint admin).
/// Reutiliza o mesmo CatalogoRegioesCacheInvalidador da invalidação automática — sem duplicar lógica.
/// Não persiste nada; não precisa de audit (sem mutação de dado de domínio — apenas limpa memória).
/// </summary>
public class InvalidarCacheRegioesAdminCommandHandler
{
    private readonly CatalogoRegioesCacheInvalidador _invalidador;

    public InvalidarCacheRegioesAdminCommandHandler(CatalogoRegioesCacheInvalidador invalidador)
    {
        _invalidador = invalidador;
    }

    public void Handle()
    {
        _invalidador.InvalidarTudo();
    }
}
