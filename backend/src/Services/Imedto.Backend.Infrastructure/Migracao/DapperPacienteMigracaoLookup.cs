using Dapper;
using Imedto.Backend.Domain.Migracao;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Migracao;

/// <summary>
/// Implementação Dapper de <see cref="IMigracaoPacienteLookup"/> — leitura otimizada
/// para resolução de vínculo paciente↔prontuário durante a carga da Onda 2.
///
/// Singleton-safe: abre conexão por chamada (pool de NpgsqlConnection).
/// Sem PII nos resultados — retorna apenas IDs.
/// </summary>
public class DapperPacienteMigracaoLookup : IMigracaoPacienteLookup
{
    private readonly AppReadConnectionString _cs;

    public DapperPacienteMigracaoLookup(AppReadConnectionString cs) => _cs = cs;

    public async Task<PacienteMigracaoInfo?> ObterPorCpfOuNulo(
        string cpf, long estabelecimentoId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_cs.Value);
        // JOIN com prontuarios para trazer prontuario_id em 1 query (CA21 — precisamos do id para o audit log)
        var row = await conn.QueryFirstOrDefaultAsync<(long pacienteId, long? prontuarioId)?>(
            @"SELECT p.id AS pacienteId, pr.id AS prontuarioId
              FROM pacientes p
              LEFT JOIN prontuarios pr ON pr.paciente_id = p.id AND pr.estabelecimento_id = @EstId AND pr.deletado_em IS NULL
              WHERE p.cpf = @Cpf
                AND p.estabelecimento_id = @EstId
                AND p.deletado_em IS NULL
              LIMIT 1",
            new { Cpf = cpf, EstId = estabelecimentoId });

        return row.HasValue ? new PacienteMigracaoInfo(row.Value.pacienteId, row.Value.prontuarioId) : null;
    }

    public async Task<PacienteMigracaoInfo?> ObterPorDocumentoInternacionalOuNulo(
        string doc, long estabelecimentoId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_cs.Value);
        var row = await conn.QueryFirstOrDefaultAsync<(long pacienteId, long? prontuarioId)?>(
            @"SELECT p.id AS pacienteId, pr.id AS prontuarioId
              FROM pacientes p
              LEFT JOIN prontuarios pr ON pr.paciente_id = p.id AND pr.estabelecimento_id = @EstId AND pr.deletado_em IS NULL
              WHERE p.documento_internacional = @Doc
                AND p.estabelecimento_id = @EstId
                AND p.deletado_em IS NULL
              LIMIT 1",
            new { Doc = doc, EstId = estabelecimentoId });

        return row.HasValue ? new PacienteMigracaoInfo(row.Value.pacienteId, row.Value.prontuarioId) : null;
    }

    public async Task<long?> ObterIdPorNomeOuNulo(
        string nome, long estabelecimentoId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_cs.Value);
        // Retorna null se ambíguo (mais de 1 resultado) — agendamento será pulado com aviso.
        // ILIKE = case-insensitive no Postgres; sem PII no log (CA4).
        var ids = await conn.QueryAsync<long>(
            @"SELECT id FROM pacientes
              WHERE LOWER(nome_completo) = LOWER(@Nome)
                AND estabelecimento_id = @EstId
                AND deletado_em IS NULL
              LIMIT 2",
            new { Nome = nome, EstId = estabelecimentoId });

        var list = ids.ToList();
        return list.Count == 1 ? list[0] : null;
    }

    public async Task<long?> ObterIdModeloPadraoProntuarioOuNulo(
        long estabelecimentoId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_cs.Value);
        // Prioridade: modelo padrão do sistema ativo (eh_padrao_sistema = true) antes do próprio do tenant.
        // Nome da tabela: modelo_de_prontuario (singular) — conforme ModeloDeProntuarioConfiguration.
        return await conn.QueryFirstOrDefaultAsync<long?>(
            @"SELECT id FROM modelo_de_prontuario
              WHERE ativo = true
                AND (eh_padrao_sistema = true OR estabelecimento_id = @EstId)
              ORDER BY eh_padrao_sistema DESC
              LIMIT 1",
            new { EstId = estabelecimentoId });
    }
}
