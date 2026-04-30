namespace Imedto.Backend.Domain.Catalogo;

public interface IProcedimentoCatalogoRepository
{
    Task<ProcedimentoCatalogo?> ObterPorIdOuNulo(long id);
    Task Salvar(ProcedimentoCatalogo procedimento);
}
