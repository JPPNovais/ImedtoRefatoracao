namespace Imedto.Backend.Domain.Prontuarios;

public interface IProntuarioVariavelPoolRepository
{
    /// <summary>
    /// Carrega a opção do pool filtrando por <paramref name="estabelecimentoId"/>
    /// (defense-in-depth IDOR/LGPD). Retorna null se inexistente ou de outro tenant.
    /// Itens padrão-do-sistema (estabelecimento_id IS NULL) NÃO são retornados — quem precisa
    /// editar isso usa ferramenta admin separada.
    /// </summary>
    Task<ProntuarioVariavelPool?> ObterPorIdOuNulo(long id, long estabelecimentoId);

    /// <summary>
    /// Verifica duplicidade considerando padrão-sistema OU do estabelecimento.
    /// A comparação usa <see cref="NormalizadorPool.Normalizar"/> (trim + lower + sem acento).
    /// </summary>
    Task<bool> ExisteOutraComMesmoNome(long estabelecimentoId, TipoVariavelPool tipo, string nome, long ignorarId);

    /// <summary>
    /// Carrega todos os itens ativos (padrão-sistema + do estabelecimento) de um tipo para
    /// comparação de dedup em memória. Usado pela extração automática ao salvar evolução.
    /// </summary>
    Task<IReadOnlyList<ProntuarioVariavelPool>> ListarAtivosPorTipo(long estabelecimentoId, TipoVariavelPool tipo);

    Task Salvar(ProntuarioVariavelPool item);
    Task Excluir(ProntuarioVariavelPool item);
}
