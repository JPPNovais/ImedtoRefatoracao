namespace Imedto.Backend.Domain.Cobrancas;

public interface ITabelaPrecoConsultaRepository
{
    Task<TabelaPrecoConsulta?> ObterPorIdOuNulo(long id, long estabelecimentoId);

    /// <summary>
    /// Retorna o preço sugerido: primeiro pelo profissional, depois o padrão do estabelecimento.
    /// Retorna null se nenhum preço estiver configurado.
    /// </summary>
    Task<decimal?> ObterValorSugerido(long estabelecimentoId, Guid profissionalId);

    Task Salvar(TabelaPrecoConsulta tabela);
}
