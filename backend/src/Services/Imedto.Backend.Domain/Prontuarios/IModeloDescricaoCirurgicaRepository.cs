namespace Imedto.Backend.Domain.Prontuarios;

public interface IModeloDescricaoCirurgicaRepository
{
    /// <summary>
    /// Carrega o modelo filtrando por <paramref name="estabelecimentoId"/>
    /// (defense-in-depth IDOR/LGPD). Retorna null se inexistente ou de outro tenant.
    /// Modelos padrão-do-sistema (estabelecimento_id IS NULL) NÃO são retornados — imutáveis pelo tenant.
    /// </summary>
    Task<ModeloDescricaoCirurgica?> ObterPorIdOuNulo(long id, long estabelecimentoId);

    /// <summary>
    /// Verifica duplicidade de título dentro do escopo visível ao tenant
    /// (padrão-sistema + do estabelecimento). Usa <see cref="NormalizadorPool.Normalizar"/>
    /// para comparação insensível a acento, sem unaccent no Postgres.
    /// </summary>
    Task<bool> ExisteOutroComMesmoTitulo(long estabelecimentoId, string titulo, long ignorarId);

    Task Salvar(ModeloDescricaoCirurgica modelo);
    Task Excluir(ModeloDescricaoCirurgica modelo);
}
