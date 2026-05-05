using Dapper;
using Imedto.Backend.Contracts.Agendamentos.Queries.Results;
using Imedto.Backend.SharedKernel.Domain;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class ListaEsperaQueryRepository
{
    private readonly string _connStr;
    public ListaEsperaQueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

    public async Task<PaginaListaEsperaDto> Listar(long estabelecimentoId, int pagina, int tamanhoPagina)
    {
        if (pagina < 1) throw new BusinessException("Página deve ser maior ou igual a 1.");
        if (tamanhoPagina < 1 || tamanhoPagina > 100)
            throw new BusinessException("Tamanho da página deve estar entre 1 e 100.");

        var offset = (pagina - 1) * tamanhoPagina;

        await using var conn = new NpgsqlConnection(_connStr);
        // Filtra apenas entradas não atendidas. Calcula tempo decorrido em minutos.
        const string sql = """
            SELECT count(*)
            FROM   lista_espera_agendamento le
            WHERE  le.estabelecimento_id = @EstabelecimentoId
              AND  le.atendido_em IS NULL;

            SELECT
                le.id                            AS Id,
                le.paciente_id                   AS PacienteId,
                pac.nome_completo                AS PacienteNome,
                pac.telefone                     AS PacienteTelefone,
                le.motivo                        AS Motivo,
                le.profissional_preferido_id     AS ProfissionalPreferidoId,
                COALESCE(u.nome_completo, u.email) AS ProfissionalPreferidoNome,
                le.prioridade                    AS Prioridade,
                le.preferencia_periodo           AS PreferenciaPeriodo,
                le.criado_em                     AS CriadoEm,
                EXTRACT(EPOCH FROM (NOW() - le.criado_em))::int / 60 AS MinutosDesdeQueEntrou
            FROM lista_espera_agendamento le
            JOIN pacientes pac ON pac.id = le.paciente_id
            LEFT JOIN usuarios u ON u.id = le.profissional_preferido_id
            WHERE le.estabelecimento_id = @EstabelecimentoId
              AND le.atendido_em IS NULL
            ORDER BY
                CASE le.prioridade
                    WHEN 'Urgente' THEN 0
                    WHEN 'Prioritario' THEN 1
                    ELSE 2
                END,
                le.criado_em ASC
            LIMIT  @Tamanho
            OFFSET @Offset;
            """;

        var parametros = new
        {
            EstabelecimentoId = estabelecimentoId,
            Tamanho = tamanhoPagina,
            Offset = offset
        };

        await using var multi = await conn.QueryMultipleAsync(sql, parametros);
        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<ListaEsperaItemDto>();

        return new PaginaListaEsperaDto
        {
            Itens = itens.ToList(),
            Total = total,
            Pagina = pagina,
            TamanhoPagina = tamanhoPagina
        };
    }
}
