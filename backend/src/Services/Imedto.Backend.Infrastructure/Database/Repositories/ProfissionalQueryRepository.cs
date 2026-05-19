using Dapper;
using Imedto.Backend.Contracts.Profissionais.Queries.Results;
using Imedto.Backend.Domain.Common;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class ProfissionalQueryRepository
{
    private readonly string _connectionString;
    private readonly IFotoStorageService _fotoStorage;

    public ProfissionalQueryRepository(AppReadConnectionString connection, IFotoStorageService fotoStorage)
    {
        _connectionString = connection.Value;
        _fotoStorage = fotoStorage;
    }

    public async Task<ProfissionalDto> ObterPorUsuario(Guid usuarioId)
    {
        const string sql = """
            SELECT  usuario_id      AS UsuarioId,
                    conselho        AS Conselho,
                    uf              AS Uf,
                    numero_registro AS NumeroRegistro,
                    especialidade   AS Especialidade,
                    bio             AS Bio,
                    foto_url        AS FotoUrl,
                    criado_em       AS CriadoEm,
                    atualizado_em   AS AtualizadoEm
            FROM    public.profissionais
            WHERE   usuario_id = @UsuarioId
              AND   deletado_em IS NULL
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var dto = await conn.QuerySingleOrDefaultAsync<ProfissionalDto>(sql, new { UsuarioId = usuarioId });
        if (dto is not null)
            dto.FotoUrl = _fotoStorage.GerarUrlLeitura(dto.FotoUrl);
        return dto;
    }
}
