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

    Task<bool> ExisteOutraComMesmoNome(long estabelecimentoId, TipoVariavelPool tipo, string nome, long ignorarId);
    Task Salvar(ProntuarioVariavelPool item);
    Task Excluir(ProntuarioVariavelPool item);
}
