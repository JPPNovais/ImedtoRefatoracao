using Dapper;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Interface Dapper de leitura para assinatura digital ICP-Brasil.
/// Separada de <see cref="AssinaturaQueryRepository"/> (que é sobre planos/assinaturas do tenant).
/// </summary>
public interface IAssinaturaDigitalQueryRepository
{
    Task<StatusAssinaturaDigitalInfo?> ObterStatusAsync(long receitaId, long estabelecimentoId);
}

public sealed record StatusAssinaturaDigitalInfo(
    string Status,
    string? PdfAssinadoS3Key);

/// <summary>
/// Leitura Dapper de status de assinatura digital. Singleton — sem estado de request.
/// </summary>
public class AssinaturaDigitalQueryRepository : IAssinaturaDigitalQueryRepository
{
    private readonly string _connStr;

    public AssinaturaDigitalQueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

    public async Task<StatusAssinaturaDigitalInfo?> ObterStatusAsync(long receitaId, long estabelecimentoId)
    {
        const string sql = """
            SELECT  assinatura_digital_status AS Status,
                    pdf_assinado_s3_key       AS PdfAssinadoS3Key
            FROM    public.receitas
            WHERE   id = @ReceitaId
              AND   estabelecimento_id = @EstabelecimentoId
              AND   deletado_em IS NULL
            """;

        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.QuerySingleOrDefaultAsync<StatusAssinaturaDigitalInfo>(sql, new
        {
            ReceitaId = receitaId,
            EstabelecimentoId = estabelecimentoId,
        });
    }
}
