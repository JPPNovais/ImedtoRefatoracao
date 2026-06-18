using Dapper;
using Imedto.Backend.Contracts.Automacoes.Queries;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.SharedKernel.Cqrs;
using Npgsql;

namespace Imedto.Backend.Application.Automacoes.Queries;

public class ObterConfiguracaoAutomacaoQueryHandlers : IRequestHandler<ObterConfiguracaoAutomacaoQuery, ConfiguracaoAutomacaoDto>
{
    private readonly string _connStr;

    public ObterConfiguracaoAutomacaoQueryHandlers(AppReadConnectionString conn)
        => _connStr = conn.Value;

    public async Task<ConfiguracaoAutomacaoDto> Handle(ObterConfiguracaoAutomacaoQuery query)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        var row = await conn.QuerySingleOrDefaultAsync<ConfiguracaoAutomacaoDto>(
            """
            SELECT
                lembretes_habilitados           AS LembretesHabilitados,
                lembretes_whatsapp_habilitados  AS LembretesWhatsappHabilitados,
                horas_antecedencia_lembrete     AS HorasAntecedenciaLembrete,
                expiracao_orcamentos_habilitada AS ExpiracaoOrcamentosHabilitada,
                email_remetente                 AS EmailRemetente
            FROM configuracoes_automacao
            WHERE estabelecimento_id = @EstabelecimentoId
            """,
            new { query.EstabelecimentoId });

        return row ?? new ConfiguracaoAutomacaoDto
        {
            LembretesHabilitados = false,
            LembretesWhatsappHabilitados = false,
            HorasAntecedenciaLembrete = 24,
            ExpiracaoOrcamentosHabilitada = true
        };
    }
}
