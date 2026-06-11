using System.Text.Json;
using Dapper;
using Imedto.Backend.Contracts.Prontuarios.Queries;
using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Npgsql;

namespace Imedto.Backend.Application.Prontuarios.Queries;

/// <summary>
/// Retorna o snapshot de procedimentos indicados de uma evolução (F5/R2).
/// Multi-tenant: verifica se a evolução pertence ao prontuário do estabelecimento ativo.
/// Itens legado sem catalogoCirurgiaId são ignorados (CA99).
/// Singleton: query Dapper pura sem efeito colateral.
/// </summary>
public class ObterProcedimentosIndicadosQueryHandler
    : IRequestHandler<ObterProcedimentosIndicadosQuery, IEnumerable<ProcedimentoIndicadoDto>>
{
    private readonly string _connStr;

    public ObterProcedimentosIndicadosQueryHandler(AppReadConnectionString conn)
        => _connStr = conn.Value;

    public async Task<IEnumerable<ProcedimentoIndicadoDto>> Handle(ObterProcedimentosIndicadosQuery query)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        // Carrega o conteudo_json da evolução, garantindo que pertença ao tenant correto
        // via JOIN prontuario → paciente → estabelecimento (multi-tenant falha-fechada R2/CA110).
        const string sql = """
            SELECT e.conteudo_json
            FROM prontuario_evolucoes e
            JOIN prontuarios p ON p.id = e.prontuario_id
            WHERE e.id = @EvolucaoId
              AND p.estabelecimento_id = @EstabelecimentoId
            LIMIT 1;
            """;

        var conteudoJson = await conn.QuerySingleOrDefaultAsync<string>(sql, new
        {
            EvolucaoId = query.EvolucaoId,
            EstabelecimentoId = query.EstabelecimentoId,
        });

        // Evolução inexistente ou de outro tenant → "Não encontrado" genérico (CA110).
        if (conteudoJson is null)
            throw new BusinessException("Não encontrado.");

        return ExtrairProcedimentosIndicados(conteudoJson);
    }

    /// <summary>
    /// Extrai seção "procedimentos-indicados" do ConteudoJson.
    /// Reusa o mesmo contrato de parsing do MarcarProcedimentoRealizadoCommandHandler.
    /// Itens sem catalogoCirurgiaId (legado) são ignorados (CA99).
    /// </summary>
    private static List<ProcedimentoIndicadoDto> ExtrairProcedimentosIndicados(string conteudoJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(conteudoJson);
            if (!doc.RootElement.TryGetProperty("procedimentos-indicados", out var secao))
                return new List<ProcedimentoIndicadoDto>();

            if (!secao.TryGetProperty("procedimentos", out var arr) ||
                arr.ValueKind != JsonValueKind.Array)
                return new List<ProcedimentoIndicadoDto>();

            var resultado = new List<ProcedimentoIndicadoDto>();
            foreach (var item in arr.EnumerateArray())
            {
                if (!item.TryGetProperty("catalogoCirurgiaId", out var idEl)) continue;
                if (idEl.ValueKind == JsonValueKind.Null) continue;

                var catalogoCirurgiaId = idEl.GetInt64();
                var descricao = item.TryGetProperty("descricao", out var dEl) ? dEl.GetString() ?? "" : "";
                var valor = item.TryGetProperty("valor", out var vEl) && vEl.ValueKind == JsonValueKind.Number
                    ? vEl.GetDecimal() : 0m;

                resultado.Add(new ProcedimentoIndicadoDto
                {
                    CatalogoCirurgiaId = catalogoCirurgiaId,
                    Descricao = descricao,
                    Valor = valor,
                });
            }
            return resultado;
        }
        catch
        {
            return new List<ProcedimentoIndicadoDto>();
        }
    }
}
