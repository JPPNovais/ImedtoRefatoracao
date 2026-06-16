using Dapper;
using Imedto.Backend.Domain.Migracao;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Migracao;

public class DapperCatalogoCirurgiaLookup : IMigracaoCatalogoCirurgiaLookup
{
    private readonly AppReadConnectionString _cs;
    public DapperCatalogoCirurgiaLookup(AppReadConnectionString cs) => _cs = cs;

    public async Task<long?> ObterIdPorCodigoOuNulo(string codigo, long estabelecimentoId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_cs.Value);
        return await conn.QueryFirstOrDefaultAsync<long?>(
            "SELECT id FROM orcamento_catalogo_cirurgia WHERE codigo_interno = @Codigo AND estabelecimento_id = @EstId AND ativo = true LIMIT 1",
            new { Codigo = codigo, EstId = estabelecimentoId });
    }

    public async Task<long?> ObterIdPorNomeOuNulo(string descricao, long estabelecimentoId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_cs.Value);
        return await conn.QueryFirstOrDefaultAsync<long?>(
            "SELECT id FROM orcamento_catalogo_cirurgia WHERE lower(descricao) = lower(@Descricao) AND estabelecimento_id = @EstId AND ativo = true LIMIT 1",
            new { Descricao = descricao, EstId = estabelecimentoId });
    }
}

public class DapperCatalogoProdutoLookup : IMigracaoCatalogoProdutoLookup
{
    private readonly AppReadConnectionString _cs;
    public DapperCatalogoProdutoLookup(AppReadConnectionString cs) => _cs = cs;

    public async Task<long?> ObterIdPorCodigoOuNulo(string codigoSku, long estabelecimentoId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_cs.Value);
        return await conn.QueryFirstOrDefaultAsync<long?>(
            "SELECT id FROM orcamento_catalogo_produto WHERE codigo_sku = @Codigo AND estabelecimento_id = @EstId AND ativo = true LIMIT 1",
            new { Codigo = codigoSku, EstId = estabelecimentoId });
    }

    public async Task<long?> ObterIdPorNomeOuNulo(string nome, long estabelecimentoId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_cs.Value);
        return await conn.QueryFirstOrDefaultAsync<long?>(
            "SELECT id FROM orcamento_catalogo_produto WHERE lower(nome) = lower(@Nome) AND estabelecimento_id = @EstId AND ativo = true LIMIT 1",
            new { Nome = nome, EstId = estabelecimentoId });
    }
}
