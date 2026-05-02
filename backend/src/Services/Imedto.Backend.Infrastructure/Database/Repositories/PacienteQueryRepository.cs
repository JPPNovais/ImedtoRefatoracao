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

        const string sqlCount = """
            SELECT count(*)
            FROM   public.pacientes
            WHERE  estabelecimento_id = @EstabelecimentoId
              AND  deletado_em IS NULL
              AND  (@Busca::text IS NULL OR nome_completo ILIKE '%' || @Busca || '%'
                                    OR (@BuscaNumerica IS NOT NULL AND cpf LIKE @BuscaNumerica || '%'))
            """;

        const string sqlItens = """
            SELECT  id               AS Id,
                    nome_completo    AS NomeCompleto,
                    cpf              AS Cpf,
                    data_nascimento  AS DataNascimento,
                    telefone         AS Telefone,
                    criado_em        AS CriadoEm
            FROM    public.pacientes
            WHERE   estabelecimento_id = @EstabelecimentoId
              AND   deletado_em IS NULL
              AND   (@Busca IS NULL OR nome_completo ILIKE '%' || @Busca || '%'
                                     OR (@BuscaNumerica IS NOT NULL AND cpf LIKE @BuscaNumerica || '%'))
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

    public async Task<PacienteDto> ObterPorId(long pacienteId, long estabelecimentoId)
    {
        // Minimizado (LGPD): sem estabelecimento_id (front nao usa, amplia IDOR)
        // e sem atualizado_em (sem uso no front).
        const string sql = """
            SELECT  id                  AS Id,
                    nome_completo       AS NomeCompleto,
                    cpf                 AS Cpf,
                    data_nascimento     AS DataNascimento,
                    genero              AS Genero,
                    telefone            AS Telefone,
                    email               AS Email,
                    endereco            AS Endereco,
                    observacoes         AS Observacoes,
                    criado_em           AS CriadoEm
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
