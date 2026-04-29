namespace Imedto.Backend.Domain.Assinaturas;

public interface IPlanoRepository
{
    Task<Plano?> ObterPorIdOuNulo(long id);

    /// <summary>Retorna o plano cujo nome bate (case-insensitive). Usado pelo seed e pelo handler de trial.</summary>
    Task<Plano?> ObterPorNomeOuNulo(string nome);

    Task Salvar(Plano plano);
}
