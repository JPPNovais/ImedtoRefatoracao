namespace Imedto.Backend.Domain.Atestados;

public interface IAtestadoRepository
{
    /// <summary>
    /// Carrega o atestado filtrando por <paramref name="estabelecimentoId"/>
    /// (defense-in-depth IDOR/LGPD). Retorna null se inexistente ou de outro tenant.
    /// Ignora registros soft-deleted.
    /// </summary>
    Task<Atestado?> ObterPorIdOuNulo(long id, long estabelecimentoId);
    Task Salvar(Atestado atestado);
}

public interface IModeloAtestadoRepository
{
    Task<ModeloAtestado?> ObterPorIdOuNulo(long id, long estabelecimentoId);
    Task<IReadOnlyList<ModeloAtestado>> ListarPorEstabelecimento(long estabelecimentoId);
    Task Salvar(ModeloAtestado modelo);
    Task Excluir(ModeloAtestado modelo);
}
