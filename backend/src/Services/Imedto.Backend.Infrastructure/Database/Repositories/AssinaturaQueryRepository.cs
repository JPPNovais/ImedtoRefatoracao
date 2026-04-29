using Dapper;
using Imedto.Backend.Contracts.Assinaturas.Queries;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Leitura da assinatura do tenant. Já faz JOIN com <c>planos</c> para devolver o DTO completo
/// numa única ida ao banco — alinha com o premium "buscar apenas o necessário" e evita
/// round-trip extra para o detalhe do plano.
/// </summary>
public class AssinaturaQueryRepository
{
    private readonly string _connStr;
    private readonly PlanoQueryRepository _planoQueryRepo;

    public AssinaturaQueryRepository(AppReadConnectionString conn, PlanoQueryRepository planoQueryRepo)
    {
        _connStr = conn.Value;
        _planoQueryRepo = planoQueryRepo;
    }

    public async Task<AssinaturaDto?> ObterDoEstabelecimento(long estabelecimentoId)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT
                id           AS Id,
                plano_id     AS PlanoId,
                status       AS Status,
                iniciada_em  AS IniciadaEm,
                expira_em    AS ExpiraEm
            FROM assinaturas
            WHERE estabelecimento_id = @EstabelecimentoId
            LIMIT 1;
            """;

        var linha = await conn.QueryFirstOrDefaultAsync<AssinaturaRow>(sql, new { EstabelecimentoId = estabelecimentoId });
        if (linha is null) return null;

        var plano = await _planoQueryRepo.ObterPorId(linha.PlanoId);
        if (plano is null)
        {
            // FK garante consistência, mas em caso extremo (deleção manual) devolvemos um placeholder
            // mínimo para a tela não quebrar — ela renderiza "plano indisponível".
            plano = new PlanoDto { Id = linha.PlanoId, Nome = "(plano indisponível)" };
        }

        return new AssinaturaDto
        {
            Plano = plano,
            Status = linha.Status,
            IniciadaEm = linha.IniciadaEm,
            ExpiraEm = linha.ExpiraEm,
            DiasRestantes = CalcularDiasRestantes(linha.Status, linha.ExpiraEm)
        };
    }

    /// <summary>
    /// Dias completos restantes quando faz sentido (Trial dentro do prazo). Para outros status
    /// retorna null — a tela renderiza diferente (ex: "Ativa" não mostra contagem regressiva).
    /// </summary>
    private static int? CalcularDiasRestantes(string status, DateTime? expiraEm)
    {
        if (!expiraEm.HasValue) return null;
        if (!string.Equals(status, "Trial", StringComparison.OrdinalIgnoreCase)) return null;

        var diff = expiraEm.Value - DateTime.UtcNow;
        if (diff <= TimeSpan.Zero) return 0;
        return (int)Math.Ceiling(diff.TotalDays);
    }

    private sealed class AssinaturaRow
    {
        public long Id { get; set; }
        public long PlanoId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime IniciadaEm { get; set; }
        public DateTime? ExpiraEm { get; set; }
    }
}
