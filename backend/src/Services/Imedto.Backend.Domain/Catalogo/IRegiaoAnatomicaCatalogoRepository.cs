namespace Imedto.Backend.Domain.Catalogo;

public interface IRegiaoAnatomicaCatalogoRepository
{
    Task<RegiaoAnatomicaCatalogo?> ObterPorCodigo(string codigo);
    Task Salvar(RegiaoAnatomicaCatalogo regiao);
}
