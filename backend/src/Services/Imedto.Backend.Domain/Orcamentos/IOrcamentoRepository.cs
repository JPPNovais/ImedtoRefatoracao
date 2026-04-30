namespace Imedto.Backend.Domain.Orcamentos;

public interface IOrcamentoRepository
{
    Task<Orcamento> ObterPorId(long id);

    /// <summary>
    /// Carrega o aggregate completo (itens + equipe + implantes + formas + cirurgias +
    /// internação + anestesia). Usado por todos os handlers que mutam o aggregate
    /// (Atualizar/Aprovar/Recusar/Enviar/Cancelar) para garantir que invariantes que
    /// dependem das collections (ex: ValidarIntegridade) possam rodar.
    /// </summary>
    Task<Orcamento> ObterPorIdCompleto(long id);

    Task Salvar(Orcamento orcamento);
}
