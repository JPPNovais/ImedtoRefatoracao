namespace Imedto.Backend.Domain.Orcamentos;

public interface IOrcamentoRepository
{
    Task<Orcamento> ObterPorId(long id);

    /// <summary>Carrega o aggregate apenas com itens (compatibilidade — orçamento simples).</summary>
    Task<Orcamento> ObterPorIdComItens(long id);

    /// <summary>
    /// Carrega o aggregate completo (itens + equipe + implantes + formas). Usado pelos
    /// handlers de <c>AtualizarOrcamentoCompletoCommand</c>.
    /// </summary>
    Task<Orcamento> ObterPorIdCompleto(long id);

    Task Salvar(Orcamento orcamento);
}
