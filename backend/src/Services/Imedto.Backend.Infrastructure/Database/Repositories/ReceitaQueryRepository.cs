using Dapper;
using Imedto.Backend.Contracts.Receitas.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Contrato de leitura de receitas (Dapper). A interface vive aqui (Infrastructure)
/// para evitar circularidade: Application depende de Infrastructure mas o oposto
/// não, e os DTOs do contrato moram em <c>Imedto.Backend.Contracts</c>. Manter
/// próximo da implementação reflete o pattern usado em <c>ProntuarioAnexoQueryRepository</c>.
/// </summary>
public interface IReceitaQueryRepository
{
    Task<PaginaReceitasDto> ListarDoPaciente(long pacienteId, long estabelecimentoId, int pagina, int tamanhoPagina);
    Task<ReceitaDto?> ObterCompleta(long receitaId, long estabelecimentoId);
    Task<ConfiguracaoReceitaDto?> ObterConfiguracao(long estabelecimentoId);
    // ListarFavoritos removido — endpoint GET /api/receitas/medicamentos-favoritos
    // nao tinha consumidor no front. Decisao Fase 1.
    Task<int> ContarEmitidasNoMes(long estabelecimentoId, int ano, int mes);
}

/// <summary>
/// Read-side de receitas (Dapper). Sempre filtra por <c>estabelecimento_id</c>
/// e <c>deletado_em IS NULL</c>. Operações:
/// <list type="bullet">
///   <item><see cref="ListarDoPaciente"/> — paginada, sem itens (para a lista resumida).</item>
///   <item><see cref="ObterCompleta"/> — receita + itens via multi-mapping (1 + 1 round-trips).</item>
///   <item><see cref="ContarEmitidasNoMes"/> — indicador de volume.</item>
/// </list>
/// </summary>
public class ReceitaQueryRepository : IReceitaQueryRepository
{
    private readonly string _connStr;

    public ReceitaQueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

    public async Task<PaginaReceitasDto> ListarDoPaciente(
        long pacienteId,
        long estabelecimentoId,
        int pagina,
        int tamanhoPagina)
    {
        const string sqlTotal = """
            SELECT COUNT(*)
            FROM   public.receitas r
            WHERE  r.paciente_id = @PacienteId
              AND  r.estabelecimento_id = @EstabelecimentoId
              AND  r.deletado_em IS NULL
            """;

        // Ordena rascunhos (emitida_em IS NULL) primeiro pela data de criação, e
        // emitidas pela data de emissão. NULLS FIRST coloca rascunhos no topo —
        // o profissional vê o que está em andamento antes do histórico.
        const string sqlItens = """
            SELECT  r.id                  AS Id,
                    r.paciente_id         AS PacienteId,
                    r.prontuario_id       AS ProntuarioId,
                    r.tipo                AS Tipo,
                    r.tipo_notificacao    AS TipoNotificacao,
                    r.status              AS Status,
                    r.emitida_em          AS EmitidaEm,
                    r.validade_ate        AS ValidadeAte,
                    r.requer_retencao     AS RequerRetencao,
                    (SELECT COUNT(*) FROM public.receita_itens ri WHERE ri.receita_id = r.id) AS QuantidadeItens,
                    u.nome_completo       AS ProfissionalNome
            FROM    public.receitas r
            LEFT JOIN public.usuarios u ON u.id = r.profissional_usuario_id
            WHERE   r.paciente_id = @PacienteId
              AND   r.estabelecimento_id = @EstabelecimentoId
              AND   r.deletado_em IS NULL
            ORDER BY COALESCE(r.emitida_em, r.criada_em) DESC
            LIMIT   @Tamanho OFFSET @Offset
            """;

        await using var conn = new NpgsqlConnection(_connStr);
        var total = await conn.ExecuteScalarAsync<int>(sqlTotal, new
        {
            PacienteId = pacienteId,
            EstabelecimentoId = estabelecimentoId
        });

        var itens = await conn.QueryAsync<ReceitaResumoDto>(sqlItens, new
        {
            PacienteId = pacienteId,
            EstabelecimentoId = estabelecimentoId,
            Tamanho = tamanhoPagina,
            Offset = (pagina - 1) * tamanhoPagina
        });

        return new PaginaReceitasDto
        {
            Itens = itens,
            Total = total,
            Pagina = pagina,
            TamanhoPagina = tamanhoPagina
        };
    }

    public async Task<ReceitaDto?> ObterCompleta(long receitaId, long estabelecimentoId)
    {
        // Multi-mapping (Receita + Item via JOIN).
        // Itens podem não existir? Não — fábrica exige ≥1. Mas LEFT JOIN cobre o caso
        // de inconsistência em produção (algum item deletado manualmente).
        const string sql = """
            SELECT  r.id                  AS Id,
                    r.prontuario_id       AS ProntuarioId,
                    r.paciente_id         AS PacienteId,
                    r.estabelecimento_id  AS EstabelecimentoId,
                    r.profissional_usuario_id AS ProfissionalUsuarioId,
                    u.nome_completo       AS ProfissionalNome,
                    r.tipo                AS Tipo,
                    r.tipo_notificacao    AS TipoNotificacao,
                    r.status              AS Status,
                    r.emitida_em          AS EmitidaEm,
                    r.validade_ate        AS ValidadeAte,
                    r.requer_retencao     AS RequerRetencao,
                    r.assinatura_digital_status AS AssinaturaDigitalStatus,
                    r.observacoes         AS Observacoes,
                    r.cancelada_em        AS CanceladaEm,
                    r.motivo_cancelamento AS MotivoCancelamento,
                    ri.id                 AS Id,
                    ri.ordem              AS Ordem,
                    ri.medicamento        AS Medicamento,
                    ri.posologia          AS Posologia,
                    ri.quantidade         AS Quantidade,
                    ri.via_administracao  AS Via,
                    ri.observacao         AS Observacao,
                    ri.concentracao       AS Concentracao,
                    ri.forma_farmaceutica AS FormaFarmaceutica,
                    ri.duracao            AS Duracao
            FROM    public.receitas r
            LEFT JOIN public.usuarios u  ON u.id = r.profissional_usuario_id
            LEFT JOIN public.receita_itens ri ON ri.receita_id = r.id
            WHERE   r.id = @Id
              AND   r.estabelecimento_id = @EstabelecimentoId
              AND   r.deletado_em IS NULL
            ORDER BY ri.ordem
            """;

        await using var conn = new NpgsqlConnection(_connStr);

        ReceitaDto? receita = null;
        var itens = new List<ItemReceitaDto>();

        await conn.QueryAsync<ReceitaDto, ItemReceitaDto, ReceitaDto>(
            sql,
            (r, i) =>
            {
                receita ??= r;
                if (i is not null && i.Id != 0)
                    itens.Add(i);
                return r;
            },
            new { Id = receitaId, EstabelecimentoId = estabelecimentoId },
            splitOn: "Id");

        if (receita is null) return null;
        receita.Itens = itens;
        return receita;
    }

    public async Task<ConfiguracaoReceitaDto?> ObterConfiguracao(long estabelecimentoId)
    {
        const string sql = """
            SELECT  estabelecimento_id  AS EstabelecimentoId,
                    cabecalho_html      AS CabecalhoHtml,
                    rodape_html         AS RodapeHtml,
                    modelo_padrao_id    AS ModeloPadraoId,
                    emissor_padrao      AS EmissorPadrao,
                    atualizada_em       AS AtualizadaEm
            FROM    public.receitas_configuracao_estabelecimento
            WHERE   estabelecimento_id = @EstabelecimentoId
            """;

        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.QuerySingleOrDefaultAsync<ConfiguracaoReceitaDto>(sql, new
        {
            EstabelecimentoId = estabelecimentoId
        });
    }

    public async Task<int> ContarEmitidasNoMes(long estabelecimentoId, int ano, int mes)
    {
        // Filtra emitida_em IS NOT NULL para excluir rascunhos do indicador.
        const string sql = """
            SELECT COUNT(*)
            FROM   public.receitas
            WHERE  estabelecimento_id = @EstabelecimentoId
              AND  deletado_em IS NULL
              AND  emitida_em IS NOT NULL
              AND  date_part('year', emitida_em)  = @Ano
              AND  date_part('month', emitida_em) = @Mes
            """;

        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            EstabelecimentoId = estabelecimentoId,
            Ano = ano,
            Mes = mes
        });
    }
}
