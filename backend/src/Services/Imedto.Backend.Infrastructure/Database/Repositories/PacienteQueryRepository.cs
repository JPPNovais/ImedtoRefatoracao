using Dapper;
using Imedto.Backend.Contracts.Pacientes.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Read-side de Pacientes (Dapper). Todas as queries são obrigatoriamente escopadas
/// por <c>estabelecimento_id</c> para garantir isolamento multi-tenant.
/// </summary>
public class PacienteQueryRepository
{
    private readonly string _connectionString;

    public PacienteQueryRepository(AppReadConnectionString connection)
    {
        _connectionString = connection.Value;
    }

    public async Task<PaginaPacientesDto> Listar(
        long estabelecimentoId,
        string busca,
        int pagina,
        int tamanhoPagina)
    {
        pagina = Math.Max(pagina, 1);
        tamanhoPagina = Math.Clamp(tamanhoPagina, 1, 100);
        var offset = (pagina - 1) * tamanhoPagina;

        var buscaSanitizada = string.IsNullOrWhiteSpace(busca)
            ? null
            : busca.Trim();
        var buscaNumerica = buscaSanitizada is null
            ? null
            : new string(buscaSanitizada.Where(char.IsDigit).ToArray());
        if (string.IsNullOrEmpty(buscaNumerica)) buscaNumerica = null;

        // Para usar o indice GIN trigram (ix_pacientes_nome_completo_trgm — ver migration
        // 20260503004122_pacientes_indice_trigram_nome.sql), o ILIKE precisa comparar a
        // EXPRESSAO indexada nos dois lados: imutable_unaccent(lower(nome_completo)).
        // Sem isso, o planner faz seq scan filtrado pelo estabelecimento_id (B-tree).
        const string sqlCount = """
            SELECT count(*)
            FROM   public.pacientes
            WHERE  estabelecimento_id = @EstabelecimentoId
              AND  deletado_em IS NULL
              AND  (@Busca::text IS NULL OR public.imutable_unaccent(lower(nome_completo)) ILIKE '%' || public.imutable_unaccent(lower(@Busca)) || '%'
                                    OR (@BuscaNumerica IS NOT NULL AND cpf LIKE @BuscaNumerica || '%')
                                    OR (@Busca IS NOT NULL AND lower(documento_internacional) LIKE lower(@Busca) || '%'))
            """;

        const string sqlItens = """
            SELECT  id                                       AS Id,
                    nome_completo                            AS NomeCompleto,
                    cpf                                      AS Cpf,
                    documento_internacional                  AS DocumentoInternacional,
                    data_nascimento                          AS DataNascimento,
                    telefone                                 AS Telefone,
                    criado_em                                AS CriadoEm,
                    COALESCE(tags, ARRAY[]::text[])          AS Tags,
                    coalesce(array_length(alertas, 1), 0)    AS QtdAlertas
            FROM    public.pacientes
            WHERE   estabelecimento_id = @EstabelecimentoId
              AND   deletado_em IS NULL
              AND   (@Busca IS NULL OR public.imutable_unaccent(lower(nome_completo)) ILIKE '%' || public.imutable_unaccent(lower(@Busca)) || '%'
                                     OR (@BuscaNumerica IS NOT NULL AND cpf LIKE @BuscaNumerica || '%')
                                     OR (@Busca IS NOT NULL AND lower(documento_internacional) LIKE lower(@Busca) || '%'))
            ORDER BY nome_completo
            LIMIT  @Tamanho
            OFFSET @Offset
            """;

        var parametros = new
        {
            EstabelecimentoId = estabelecimentoId,
            Busca = buscaSanitizada,
            BuscaNumerica = buscaNumerica,
            Tamanho = tamanhoPagina,
            Offset = offset
        };

        await using var conn = new NpgsqlConnection(_connectionString);
        var total = await conn.ExecuteScalarAsync<int>(sqlCount, parametros);
        var itens = await conn.QueryAsync<PacienteListaItemDto>(sqlItens, parametros);

        return new PaginaPacientesDto
        {
            Itens = itens,
            Total = total,
            Pagina = pagina,
            TamanhoPagina = tamanhoPagina
        };
    }

    /// <summary>
    /// Autocomplete de paciente (LGPD: o DTO retorna somente id + nome). Sem busca →
    /// retorna os últimos cadastrados; com busca → filtra por nome (índice GIN trigram),
    /// CPF (prefixo numérico) ou documento internacional — mesma cobertura do Listar.
    /// Limite máximo enforced em 30 (proteção contra exfiltração).
    /// </summary>
    public async Task<IReadOnlyList<PacienteBuscaRapidaDto>> BuscaRapida(
        long estabelecimentoId,
        string q,
        int limite)
    {
        var lim = Math.Clamp(limite, 1, 30);
        var buscaSanitizada = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        var buscaNumerica = buscaSanitizada is null
            ? null
            : new string(buscaSanitizada.Where(char.IsDigit).ToArray());
        if (string.IsNullOrEmpty(buscaNumerica)) buscaNumerica = null;

        const string sql = """
            SELECT  id            AS Id,
                    nome_completo AS NomeCompleto
            FROM    public.pacientes
            WHERE   estabelecimento_id = @EstabelecimentoId
              AND   deletado_em IS NULL
              AND   (@Busca::text IS NULL
                     OR public.imutable_unaccent(lower(nome_completo))
                        ILIKE '%' || public.imutable_unaccent(lower(@Busca)) || '%'
                     OR (@BuscaNumerica IS NOT NULL AND cpf LIKE @BuscaNumerica || '%')
                     OR (@Busca IS NOT NULL AND lower(documento_internacional) LIKE lower(@Busca) || '%'))
            ORDER BY CASE WHEN @Busca IS NULL THEN criado_em END DESC,
                     nome_completo
            LIMIT  @Limite
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var itens = await conn.QueryAsync<PacienteBuscaRapidaDto>(sql, new
        {
            EstabelecimentoId = estabelecimentoId,
            Busca = buscaSanitizada,
            BuscaNumerica = buscaNumerica,
            Limite = lim
        });
        return itens.AsList();
    }

    /// <summary>
    /// Carrega o pacote LGPD Art. 18 — todos os campos do paciente + metadados
    /// de tratamento (criado/atualizado/deletado/anonimizado). Inclui registros
    /// soft-deletados, ja que o titular tem direito a portabilidade do historico.
    /// </summary>
    public async Task<PacienteExportPessoalDto> ObterParaExportLgpd(long pacienteId, long estabelecimentoId)
    {
        const string sql = """
            SELECT  id                          AS Id,
                    nome_completo               AS NomeCompleto,
                    cpf                         AS Cpf,
                    documento_internacional     AS DocumentoInternacional,
                    data_nascimento             AS DataNascimento,
                    genero                      AS Genero,
                    telefone                    AS Telefone,
                    email                       AS Email,
                    endereco                    AS Endereco,
                    observacoes                 AS Observacoes,
                    criado_em                   AS CriadoEm,
                    atualizado_em               AS AtualizadoEm,
                    deletado_em                 AS DeletadoEm,
                    deletado_por_usuario_id     AS DeletadoPorUsuarioId,
                    anonimizado_em              AS AnonimizadoEm,
                    anonimizado_por_usuario_id  AS AnonimizadoPorUsuarioId
            FROM    public.pacientes
            WHERE   id = @PacienteId
              AND   estabelecimento_id = @EstabelecimentoId
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QuerySingleOrDefaultAsync<PacienteExportPessoalDto>(sql, new
        {
            PacienteId = pacienteId,
            EstabelecimentoId = estabelecimentoId
        });
    }

    /// <summary>
    /// Retorna KPIs agregados (total + novos do mês corrente) usados no header
    /// da lista de pacientes. Uma única ida ao banco, sem joins pesados.
    /// </summary>
    public async Task<PacienteStatsDto> ObterStats(long estabelecimentoId)
    {
        const string sql = """
            SELECT
                count(*) FILTER (WHERE deletado_em IS NULL)                                      AS Total,
                count(*) FILTER (WHERE deletado_em IS NULL
                                   AND criado_em >= date_trunc('month', current_date))           AS NovosMesCorrente
            FROM public.pacientes
            WHERE estabelecimento_id = @EstabelecimentoId
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QuerySingleAsync<PacienteStatsDto>(sql, new
        {
            EstabelecimentoId = estabelecimentoId
        });
    }

    public async Task<PacienteDto> ObterPorId(long pacienteId, long estabelecimentoId)
    {
        // Minimizado (LGPD): sem estabelecimento_id (front nao usa, amplia IDOR)
        // e sem atualizado_em (sem uso no front).
        const string sql = """
            SELECT  id                                      AS Id,
                    nome_completo                           AS NomeCompleto,
                    cpf                                     AS Cpf,
                    documento_internacional                 AS DocumentoInternacional,
                    data_nascimento                         AS DataNascimento,
                    genero                                  AS Genero,
                    telefone                                AS Telefone,
                    email                                   AS Email,
                    endereco                                AS Endereco,
                    observacoes                             AS Observacoes,
                    COALESCE(tags, ARRAY[]::text[])         AS Tags,
                    COALESCE(alertas, ARRAY[]::text[])      AS Alertas,
                    criado_em                               AS CriadoEm,
                    whatsapp_lembrete_opt_in                AS WhatsappLembreteOptIn
            FROM    public.pacientes
            WHERE   id = @PacienteId
              AND   estabelecimento_id = @EstabelecimentoId
              AND   deletado_em IS NULL
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QuerySingleOrDefaultAsync<PacienteDto>(sql, new
        {
            PacienteId = pacienteId,
            EstabelecimentoId = estabelecimentoId
        });
    }
}
