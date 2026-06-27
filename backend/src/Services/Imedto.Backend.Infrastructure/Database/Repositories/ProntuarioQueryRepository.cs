using Dapper;
using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.SharedKernel.Tenancy;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Read-side do prontuário (Dapper): retorna o prontuário + timeline de evoluções
/// em consultas agregadas. Todas escopadas por <c>estabelecimento_id</c>.
/// </summary>
public class ProntuarioQueryRepository
{
    private readonly string _connectionString;

    public ProntuarioQueryRepository(AppReadConnectionString connection)
    {
        _connectionString = connection.Value;
    }

    public async Task<ProntuarioCompletoDto> ObterDoPaciente(long pacienteId, long estabelecimentoId, int tamanhoTimeline)
        => await ObterDoPacienteGated(pacienteId, estabelecimentoId, tamanhoTimeline, Guid.Empty, TenantPapel.Profissional);

    /// <summary>
    /// Retorna o prontuário + evoluções + alertas gated (R2 LGPD).
    /// O campo <c>Alertas</c> é preenchido apenas para Dono ou Profissional que
    /// atendeu/está atendendo o paciente; para os demais, retorna vazio.
    /// </summary>
    public async Task<ProntuarioCompletoDto> ObterDoPacienteGated(
        long pacienteId,
        long estabelecimentoId,
        int tamanhoTimeline,
        Guid solicitanteUsuarioId,
        TenantPapel papel)
    {
        // SELECT minimizado (LGPD): sem paciente_id/estabelecimento_id (vem da rota)
        // e autor_usuario_id (Guid auth interno — front nao usa).
        const string sqlPront = """
            SELECT  p.id                         AS Id,
                    p.modelo_de_prontuario_id    AS ModeloDeProntuarioId,
                    m.nome                       AS ModeloNome,
                    m.estrutura                  AS ModeloEstrutura,
                    p.criado_em                  AS CriadoEm,
                    p.atualizado_em              AS AtualizadoEm
            FROM    public.prontuarios p
            JOIN    public.modelo_de_prontuario m ON m.id = p.modelo_de_prontuario_id
            WHERE   p.paciente_id = @PacienteId
              AND   p.estabelecimento_id = @EstabelecimentoId
              AND   p.deletado_em IS NULL
            """;

        // Gating de autoria (R1 briefing 2026-06-27_001): Profissional vê só as próprias evoluções;
        // Dono vê todas. @Papel = 'Dono' bypassa o predicado de autoria (R4).
        const string sqlEvo = """
            SELECT  e.id                             AS Id,
                    e.prontuario_id                  AS ProntuarioId,
                    e.autor_usuario_id               AS AutorUsuarioId,
                    u.nome_completo                  AS AutorNome,
                    mdp.nome                         AS ModeloNome,
                    e.conteudo                       AS Conteudo,
                    e.modelo_snapshot                AS ModeloSnapshot,
                    e.modelo_de_prontuario_id_origem AS ModeloDeProntuarioIdOrigem,
                    e.criada_em                      AS CriadaEm
            FROM    public.prontuario_evolucoes e
            LEFT JOIN public.usuarios u ON u.id = e.autor_usuario_id
            LEFT JOIN public.modelo_de_prontuario mdp ON mdp.id = e.modelo_de_prontuario_id_origem
            WHERE   e.prontuario_id = @ProntuarioId
              AND   e.deletado_em IS NULL
              AND   (@Papel = 'Dono' OR e.autor_usuario_id = @UsuarioId)
            ORDER BY e.criada_em DESC
            LIMIT   @Tamanho
            """;

        // Alertas: gated por R2 LGPD — Dono ou Profissional que atendeu/está atendendo.
        // EXISTS duplo: evolução autorada + agendamento ativo com check-in.
        // Para Dono: sem verificação adicional — vê sempre.
        // Para demais papeis (Recepcionista etc.): array vazio — indistinguível de "sem alerta".
        const string sqlAlertas = """
            SELECT COALESCE(pac.alertas, ARRAY[]::text[])
            FROM   public.pacientes pac
            WHERE  pac.id = @PacienteId
              AND  pac.estabelecimento_id = @EstabelecimentoId
              AND  pac.deletado_em IS NULL
              AND (
                    -- Profissional já atendeu: tem evolução autorada por ele neste prontuário
                    EXISTS (
                        SELECT 1
                        FROM   public.prontuario_evolucoes pe
                        JOIN   public.prontuarios pr ON pr.id = pe.prontuario_id
                        WHERE  pr.paciente_id = @PacienteId
                          AND  pr.estabelecimento_id = @EstabelecimentoId
                          AND  pr.deletado_em IS NULL
                          AND  pe.autor_usuario_id = @UsuarioId
                          AND  pe.deletado_em IS NULL
                    )
                    OR
                    -- Profissional está atendendo agora: agendamento ativo com check-in
                    EXISTS (
                        SELECT 1
                        FROM   public.agendamentos ag
                        WHERE  ag.paciente_id = @PacienteId
                          AND  ag.estabelecimento_id = @EstabelecimentoId
                          AND  ag.profissional_usuario_id = @UsuarioId
                          AND  ag.check_in_em IS NOT NULL
                          AND  ag.status NOT IN ('Concluido', 'Cancelado', 'Expirado')
                    )
              )
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var prontuario = await conn.QuerySingleOrDefaultAsync<ProntuarioDto>(sqlPront, new
        {
            PacienteId = pacienteId,
            EstabelecimentoId = estabelecimentoId
        });

        if (prontuario is null) return null;

        var evolucoes = await conn.QueryAsync<EvolucaoDto>(sqlEvo, new
        {
            ProntuarioId = prontuario.Id,
            Tamanho = Math.Clamp(tamanhoTimeline, 1, 500),
            UsuarioId = solicitanteUsuarioId,
            Papel = papel.ToString(),
        });

        // Gating de alertas: Dono vê sempre; Profissional só com vínculo de atendimento.
        // Recepcionista e outros papeis: array vazio + PodeGerirAlertas=false (falha-fechada — R5/CA12).
        string[] alertas;
        bool podeGerirAlertas;
        if (papel == TenantPapel.Dono)
        {
            var alertasDono = await conn.QuerySingleOrDefaultAsync<string[]>(
                """
                SELECT COALESCE(alertas, ARRAY[]::text[])
                FROM   public.pacientes
                WHERE  id = @PacienteId AND estabelecimento_id = @EstabelecimentoId AND deletado_em IS NULL
                """,
                new { PacienteId = pacienteId, EstabelecimentoId = estabelecimentoId });
            alertas = alertasDono ?? [];
            // Dono sempre pode gerir (CA11).
            podeGerirAlertas = true;
        }
        else if (papel == TenantPapel.Profissional && solicitanteUsuarioId != Guid.Empty)
        {
            // sqlAlertas retorna não-nulo apenas quando o profissional tem vínculo.
            // Se retornou null → sem vínculo → array vazio E não pode gerir (CA12).
            var alertasProfissional = await conn.QuerySingleOrDefaultAsync<string[]>(
                sqlAlertas,
                new { PacienteId = pacienteId, EstabelecimentoId = estabelecimentoId, UsuarioId = solicitanteUsuarioId });
            podeGerirAlertas = alertasProfissional is not null;
            alertas = alertasProfissional ?? [];
        }
        else
        {
            // Recepcionista ou papel desconhecido: falha-fechada — sem alertas, não pode gerir (R5/CA12).
            alertas = [];
            podeGerirAlertas = false;
        }

        return new ProntuarioCompletoDto
        {
            Prontuario = prontuario,
            Evolucoes = evolucoes,
            Alertas = alertas,
            PodeGerirAlertas = podeGerirAlertas,
        };
    }

    /// <summary>
    /// Verifica se o usuário tem vínculo de atendimento com o paciente (R2 LGPD):
    /// (a) já autou alguma evolução no prontuário do paciente, OU
    /// (b) tem agendamento ativo com check-in nesse paciente.
    /// Ambas as checagens filtram por <c>estabelecimento_id</c> (multi-tenant).
    /// </summary>
    public virtual async Task<bool> VerificarVinculoAtendimento(long pacienteId, long estabelecimentoId, Guid usuarioId)
    {
        const string sql = """
            SELECT EXISTS (
                -- Profissional já atendeu: tem evolução autorada por ele
                SELECT 1
                FROM   public.prontuario_evolucoes pe
                JOIN   public.prontuarios pr ON pr.id = pe.prontuario_id
                WHERE  pr.paciente_id = @PacienteId
                  AND  pr.estabelecimento_id = @EstabelecimentoId
                  AND  pr.deletado_em IS NULL
                  AND  pe.autor_usuario_id = @UsuarioId
                  AND  pe.deletado_em IS NULL
                UNION ALL
                -- Profissional está atendendo agora: agendamento ativo com check-in
                SELECT 1
                FROM   public.agendamentos ag
                WHERE  ag.paciente_id = @PacienteId
                  AND  ag.estabelecimento_id = @EstabelecimentoId
                  AND  ag.profissional_usuario_id = @UsuarioId
                  AND  ag.check_in_em IS NOT NULL
                  AND  ag.status NOT IN ('Concluido', 'Cancelado', 'Expirado')
                LIMIT 1
            )
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<bool>(sql, new
        {
            PacienteId = pacienteId,
            EstabelecimentoId = estabelecimentoId,
            UsuarioId = usuarioId
        });
    }

    /// <summary>
    /// Listagem paginada das evoluções do prontuário do paciente. Retorna lista vazia
    /// + total 0 quando o paciente ainda não tem prontuário (o front exibe o CTA).
    /// Gated por autor-ou-dono (R1/R6 briefing 2026-06-27_001).
    /// </summary>
    public virtual async Task<PaginaEvolucoesDto> ListarEvolucoesPaginadas(
        long pacienteId,
        long estabelecimentoId,
        int pagina,
        int tamanhoPagina,
        Guid solicitanteUsuarioId,
        TenantPapel papel)
    {
        const string sqlProntId = """
            SELECT  p.id
            FROM    public.prontuarios p
            WHERE   p.paciente_id = @PacienteId
              AND   p.estabelecimento_id = @EstabelecimentoId
              AND   p.deletado_em IS NULL
            """;

        // Gating: @Papel = 'Dono' bypassa; caso contrário filtra por autor.
        const string sqlTotal = """
            SELECT COUNT(*)
            FROM   public.prontuario_evolucoes e
            WHERE  e.prontuario_id = @ProntuarioId
              AND  e.deletado_em IS NULL
              AND  (@Papel = 'Dono' OR e.autor_usuario_id = @UsuarioId)
            """;

        const string sqlItens = """
            SELECT  e.id                             AS Id,
                    e.prontuario_id                  AS ProntuarioId,
                    e.autor_usuario_id               AS AutorUsuarioId,
                    u.nome_completo                  AS AutorNome,
                    mdp.nome                         AS ModeloNome,
                    e.conteudo                       AS Conteudo,
                    e.modelo_snapshot                AS ModeloSnapshot,
                    e.modelo_de_prontuario_id_origem AS ModeloDeProntuarioIdOrigem,
                    e.criada_em                      AS CriadaEm
            FROM    public.prontuario_evolucoes e
            LEFT JOIN public.usuarios u ON u.id = e.autor_usuario_id
            LEFT JOIN public.modelo_de_prontuario mdp ON mdp.id = e.modelo_de_prontuario_id_origem
            WHERE   e.prontuario_id = @ProntuarioId
              AND   e.deletado_em IS NULL
              AND   (@Papel = 'Dono' OR e.autor_usuario_id = @UsuarioId)
            ORDER BY e.criada_em DESC
            LIMIT   @Tamanho OFFSET @Offset
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var prontuarioId = await conn.QuerySingleOrDefaultAsync<long?>(sqlProntId, new
        {
            PacienteId = pacienteId,
            EstabelecimentoId = estabelecimentoId
        });

        if (prontuarioId is null)
        {
            return new PaginaEvolucoesDto
            {
                Itens = Array.Empty<EvolucaoDto>(),
                Total = 0,
                Pagina = pagina,
                TamanhoPagina = tamanhoPagina,
            };
        }

        var parametrosGated = new
        {
            ProntuarioId = prontuarioId.Value,
            UsuarioId = solicitanteUsuarioId,
            Papel = papel.ToString(),
        };

        var total = await conn.ExecuteScalarAsync<int>(sqlTotal, parametrosGated);

        var itens = await conn.QueryAsync<EvolucaoDto>(sqlItens, new
        {
            ProntuarioId = prontuarioId.Value,
            UsuarioId = solicitanteUsuarioId,
            Papel = papel.ToString(),
            Tamanho = tamanhoPagina,
            Offset = (pagina - 1) * tamanhoPagina,
        });

        return new PaginaEvolucoesDto
        {
            Itens = itens,
            Total = total,
            Pagina = pagina,
            TamanhoPagina = tamanhoPagina,
        };
    }

    /// <summary>
    /// Conta evoluções não-deletadas do prontuário do paciente visíveis ao solicitante.
    /// Gated por autor-ou-dono (R1/R6 briefing 2026-06-27_001) — coerente com <see cref="ListarEvolucoesPaginadas"/>.
    /// 0 se ainda não tem prontuário ou o solicitante não tem evoluções próprias.
    /// </summary>
    public virtual async Task<int> ContarEvolucoes(
        long pacienteId,
        long estabelecimentoId,
        Guid solicitanteUsuarioId,
        TenantPapel papel)
    {
        const string sql = """
            SELECT COUNT(*)::int
            FROM   public.prontuario_evolucoes pe
            JOIN   public.prontuarios p ON p.id = pe.prontuario_id
            WHERE  p.paciente_id = @PacienteId
              AND  p.estabelecimento_id = @EstabelecimentoId
              AND  p.deletado_em IS NULL
              AND  pe.deletado_em IS NULL
              AND  (@Papel = 'Dono' OR pe.autor_usuario_id = @UsuarioId)
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            PacienteId = pacienteId,
            EstabelecimentoId = estabelecimentoId,
            UsuarioId = solicitanteUsuarioId,
            Papel = papel.ToString(),
        });
    }
}
