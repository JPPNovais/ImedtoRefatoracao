using Dapper;
using Imedto.Backend.Contracts.Pacientes.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Contrato de leitura da query agregada de documentos (Dapper).
/// Interface localizada em Infrastructure (mesmo padrão de ReceitaQueryRepository).
/// </summary>
public interface IDocumentoQueryRepository
{
    Task<PaginaDocumentosDto> ListarDoPaciente(
        long pacienteId,
        long estabelecimentoId,
        int pagina,
        int tamanhoPagina,
        string? tipo,
        DateTime? dataInicio,
        DateTime? dataFim,
        string? busca);
}

/// <summary>
/// Read-side agregado de documentos clínicos finalizados (Dapper).
/// Realiza UNION ALL sobre receitas (status='Emitida'), atestados e pedidos de exame,
/// ordenado por data desc, paginado server-side. Filtros por tipo, período e busca
/// textual são aplicados ANTES do UNION em cada subconsulta (R8/R9).
/// Multi-tenant: todas as subconsultas filtram estabelecimento_id (R2/R12).
/// </summary>
public class DocumentoQueryRepository : IDocumentoQueryRepository
{
    private readonly string _connStr;

    public DocumentoQueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

    public async Task<PaginaDocumentosDto> ListarDoPaciente(
        long pacienteId,
        long estabelecimentoId,
        int pagina,
        int tamanhoPagina,
        string? tipo,
        DateTime? dataInicio,
        DateTime? dataFim,
        string? busca)
    {
        // Normaliza busca: espaços = no-op (R11)
        var buscaAtiva = !string.IsNullOrWhiteSpace(busca);
        var buscaNorm = buscaAtiva ? busca!.Trim() : null;

        // Construção das subconsultas habilitadas pelo filtro de tipo
        var incluirReceitas = tipo == null || tipo == "Receita";
        var incluirAtestados = tipo == null || tipo == "Atestado";
        var incluirPedidos   = tipo == null || tipo == "PedidoExame";

        // ── Subconsulta de receitas ────────────────────────────────────────
        // Só emitidas (R1). Busca: EXISTS contra receita_itens.medicamento (R8).
        var sqlReceitas = incluirReceitas ? $"""
            SELECT
                'Receita'                                              AS Tipo,
                r.id                                                   AS Id,
                CONCAT('Receita ', r.tipo)                             AS Titulo,
                COALESCE(r.emitida_em, r.criada_em)                    AS Data,
                u.nome_completo                                        AS ProfissionalNome
            FROM public.receitas r
            LEFT JOIN public.usuarios u ON u.id = r.profissional_usuario_id
            WHERE r.paciente_id       = @PacienteId
              AND r.estabelecimento_id = @EstabelecimentoId
              AND r.deletado_em IS NULL
              AND r.status = 'Emitida'
              {FiltroData("COALESCE(r.emitida_em, r.criada_em)", dataInicio, dataFim)}
              {(buscaAtiva ? """
              AND EXISTS (
                  SELECT 1 FROM public.receita_itens ri
                  WHERE ri.receita_id = r.id
                    AND unaccent(ri.medicamento) ILIKE unaccent('%' || @Busca || '%')
              )
              """ : "")}
            """ : null;

        // ── Subconsulta de atestados ───────────────────────────────────────
        // Sem status (já emitidos por natureza). Busca: tipo + conteudo (R8).
        var sqlAtestados = incluirAtestados ? $"""
            SELECT
                'Atestado'                                             AS Tipo,
                a.id                                                   AS Id,
                CONCAT('Atestado de ', a.tipo)                        AS Titulo,
                a.criado_em                                            AS Data,
                u.nome_completo                                        AS ProfissionalNome
            FROM public.atestados a
            LEFT JOIN public.usuarios u ON u.id = a.profissional_usuario_id
            WHERE a.paciente_id       = @PacienteId
              AND a.estabelecimento_id = @EstabelecimentoId
              AND a.deletado_em IS NULL
              {FiltroData("a.criado_em", dataInicio, dataFim)}
              {(buscaAtiva ? """
              AND (
                  unaccent(a.tipo)    ILIKE unaccent('%' || @Busca || '%')
                  OR unaccent(a.conteudo) ILIKE unaccent('%' || @Busca || '%')
              )
              """ : "")}
            """ : null;

        // ── Subconsulta de pedidos de exame ───────────────────────────────
        // Sem status (já emitidos por natureza). Busca: exames (JSONB→text) + indicação (R8).
        var sqlPedidos = incluirPedidos ? $"""
            SELECT
                'PedidoExame'                                          AS Tipo,
                p.id                                                   AS Id,
                CONCAT('Pedido de exame ', p.tipo)                    AS Titulo,
                p.criado_em                                            AS Data,
                u.nome_completo                                        AS ProfissionalNome
            FROM public.pedidos_exame p
            LEFT JOIN public.usuarios u ON u.id = p.profissional_usuario_id
            WHERE p.paciente_id       = @PacienteId
              AND p.estabelecimento_id = @EstabelecimentoId
              AND p.deletado_em IS NULL
              {FiltroData("p.criado_em", dataInicio, dataFim)}
              {(buscaAtiva ? """
              AND (
                  unaccent(p.exames::text)           ILIKE unaccent('%' || @Busca || '%')
                  OR unaccent(p.indicacao_clinica)   ILIKE unaccent('%' || @Busca || '%')
              )
              """ : "")}
            """ : null;

        // Monta UNION ALL apenas com as subconsultas ativas (R10)
        var subconsultas = new[] { sqlReceitas, sqlAtestados, sqlPedidos }
            .Where(s => s != null)
            .ToList();

        if (subconsultas.Count == 0)
        {
            // Nenhum tipo ativo → retorna vazio (não deve ocorrer com validação do handler)
            return new PaginaDocumentosDto
            {
                Itens = Enumerable.Empty<DocumentoResumoDto>(),
                Total = 0,
                Pagina = pagina,
                TamanhoPagina = tamanhoPagina,
            };
        }

        var unionSql = string.Join("\nUNION ALL\n", subconsultas);

        var sqlTotal = $"""
            SELECT COUNT(*) FROM (
            {unionSql}
            ) AS docs
            """;

        var sqlItens = $"""
            SELECT Tipo, Id, Titulo, Data, ProfissionalNome FROM (
            {unionSql}
            ) AS docs
            ORDER BY docs.data DESC
            LIMIT @Tamanho OFFSET @Offset
            """;

        var parametros = new
        {
            PacienteId = pacienteId,
            EstabelecimentoId = estabelecimentoId,
            DataInicio = dataInicio,
            DataFim = dataFim,
            Busca = buscaNorm,
            Tamanho = tamanhoPagina,
            Offset = (pagina - 1) * tamanhoPagina,
        };

        await using var conn = new NpgsqlConnection(_connStr);

        var total = await conn.ExecuteScalarAsync<int>(sqlTotal, parametros);
        var itens = await conn.QueryAsync<DocumentoResumoDto>(sqlItens, parametros);

        return new PaginaDocumentosDto
        {
            Itens = itens,
            Total = total,
            Pagina = pagina,
            TamanhoPagina = tamanhoPagina,
        };
    }

    /// <summary>
    /// Gera cláusula WHERE para filtro de período (inclusivo).
    /// Retorna string vazia se ambos ausentes.
    /// </summary>
    private static string FiltroData(string coluna, DateTime? inicio, DateTime? fim)
    {
        var partes = new List<string>();
        if (inicio.HasValue) partes.Add($"{coluna} >= @DataInicio");
        if (fim.HasValue)    partes.Add($"{coluna} <= @DataFim");
        return partes.Count == 0 ? "" : "AND " + string.Join(" AND ", partes);
    }
}
