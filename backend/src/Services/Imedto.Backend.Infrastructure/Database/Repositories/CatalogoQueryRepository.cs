using Dapper;
using Imedto.Backend.Contracts.Catalogo.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class CatalogoQueryRepository
{
    private readonly string _connStr;

    public CatalogoQueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

    public async Task<IEnumerable<ProfissaoListadaDto>> ListarProfissoes(bool apenasAtivas)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT
                p.id            AS Id,
                p.nome          AS Nome,
                p.conselho_sigla AS ConselhoSigla,
                p.ativo         AS Ativo
            FROM profissoes p
            WHERE (@ApenasAtivas::boolean IS NULL OR NOT @ApenasAtivas::boolean OR p.ativo = true)
            ORDER BY p.nome
            """;

        return await conn.QueryAsync<ProfissaoListadaDto>(sql, new { ApenasAtivas = apenasAtivas });
    }

    public async Task<IEnumerable<EspecialidadeListadaDto>> ListarEspecialidades(long profissaoId, bool apenasAtivas)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT
                e.id            AS Id,
                e.profissao_id  AS ProfissaoId,
                p.nome          AS ProfissaoNome,
                e.nome          AS Nome,
                e.ativo         AS Ativo
            FROM especialidades e
            JOIN profissoes p ON p.id = e.profissao_id
            WHERE e.profissao_id = @ProfissaoId
              AND (@ApenasAtivas::boolean IS NULL OR NOT @ApenasAtivas::boolean OR e.ativo = true)
            ORDER BY e.nome
            """;

        return await conn.QueryAsync<EspecialidadeListadaDto>(sql, new
        {
            ProfissaoId = profissaoId,
            ApenasAtivas = apenasAtivas
        });
    }

    public async Task<IEnumerable<RegiaoCatalogoDto>> ListarRegioesCatalogo(string? vista, bool apenasAtivas)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT
                r.id              AS Id,
                r.codigo          AS Codigo,
                r.nome            AS Nome,
                r.pai_codigo      AS PaiCodigo,
                r.nivel           AS Nivel,
                r.vista           AS Vista,
                r.template_texto  AS TemplateTexto,
                r.svg_coords      AS SvgCoordsJson,
                r.ordem           AS Ordem,
                r.lateralidade    AS Lateralidade,
                r.ativo           AS Ativo
            FROM regioes_anatomicas_catalogo r
            WHERE (@Vista::text IS NULL OR r.vista = @Vista)
              AND (@ApenasAtivas::boolean IS NULL OR NOT @ApenasAtivas::boolean OR r.ativo = true)
            ORDER BY r.nivel, r.ordem, r.nome
            """;

        return await conn.QueryAsync<RegiaoCatalogoDto>(sql, new { Vista = vista, ApenasAtivas = apenasAtivas });
    }
}
