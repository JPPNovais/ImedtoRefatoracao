namespace Imedto.Backend.Domain.Orcamentos;

public interface IOrcamentoRepository
{
    /// <summary>
    /// Carrega o orçamento filtrando por <paramref name="estabelecimentoId"/>
    /// (defense-in-depth IDOR/LGPD). Retorna null se inexistente ou de outro tenant.
    /// </summary>
    Task<Orcamento?> ObterPorIdOuNulo(long id, long estabelecimentoId);

    /// <summary>
    /// Carrega o aggregate completo (itens + equipe + implantes + formas + cirurgias +
    /// internação + anestesia) filtrando por <paramref name="estabelecimentoId"/>.
    /// Usado por todos os handlers que mutam o aggregate (Atualizar/Aprovar/Recusar/Enviar/Cancelar)
    /// para garantir que invariantes que dependem das collections (ex: ValidarIntegridade) possam rodar.
    /// Retorna null se inexistente ou de outro tenant.
    /// </summary>
    Task<Orcamento?> ObterPorIdCompletoOuNulo(long id, long estabelecimentoId);

    Task Salvar(Orcamento orcamento);
}
