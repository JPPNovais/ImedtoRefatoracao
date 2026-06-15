namespace Imedto.Backend.Domain.Migracao;

public interface IMigracaoCatalogoCirurgiaLookup
{
    Task<long?> ObterIdPorCodigoOuNulo(string codigo, long estabelecimentoId, CancellationToken ct = default);
    Task<long?> ObterIdPorNomeOuNulo(string descricao, long estabelecimentoId, CancellationToken ct = default);
}

public interface IMigracaoCatalogoProdutoLookup
{
    Task<long?> ObterIdPorCodigoOuNulo(string codigoSku, long estabelecimentoId, CancellationToken ct = default);
    Task<long?> ObterIdPorNomeOuNulo(string nome, long estabelecimentoId, CancellationToken ct = default);
}
