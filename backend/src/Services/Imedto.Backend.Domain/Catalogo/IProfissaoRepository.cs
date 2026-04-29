namespace Imedto.Backend.Domain.Catalogo;

public interface IProfissaoRepository
{
    Task<Profissao> ObterPorId(long id);
    Task<Profissao?> ObterPorIdOuNulo(long id);
    Task Salvar(Profissao profissao);
}
