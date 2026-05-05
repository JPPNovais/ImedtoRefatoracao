namespace Imedto.Backend.Domain.Salas;

public interface ISalaRepository
{
    /// <summary>
    /// Carrega a sala filtrando por <paramref name="estabelecimentoId"/>
    /// (defense-in-depth IDOR/LGPD). Retorna null se inexistente ou de outro tenant.
    /// </summary>
    Task<Sala?> ObterPorIdOuNulo(long id, long estabelecimentoId);

    Task<bool> ExisteOutraComMesmoNome(long estabelecimentoId, string nome, long ignorarId);
    Task Salvar(Sala sala);
    Task Excluir(Sala sala);
}
