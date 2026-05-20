using System.Text.Json;
using Dapper;
using Imedto.Backend.Contracts.Estabelecimentos.Queries.Results;
using Imedto.Backend.Domain.Common;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Read repository (Dapper) para projeções de Estabelecimento em DTO.
/// </summary>
public class EstabelecimentoQueryRepository
{
    private readonly string _connectionString;
    private readonly IFotoStorageService _fotoStorage;

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public EstabelecimentoQueryRepository(AppReadConnectionString connection, IFotoStorageService fotoStorage)
    {
        _connectionString = connection.Value;
        _fotoStorage = fotoStorage;
    }

    /// <summary>
    /// Lista estabelecimentos onde o usuário é dono OU tem vínculo profissional ativo.
    /// </summary>
    public async Task<IEnumerable<EstabelecimentoDto>> ListarPorUsuario(Guid usuarioId)
    {
        // SELECT minimizado (LGPD): dono_usuario_id removido (Guid auth interno;
        // front diferencia Dono via PapelDoUsuario).
        const string sql = """
            SELECT  e.id                          AS Id,
                    e.nome_fantasia               AS NomeFantasia,
                    e.razao_social                AS RazaoSocial,
                    e.cnpj                        AS Cnpj,
                    e.telefone                    AS Telefone,
                    e.endereco                    AS Endereco,
                    e.cidade                      AS Cidade,
                    e.estado                      AS Estado,
                    e.foto_url                    AS FotoUrl,
                    e.status                      AS Status,
                    e.criado_em                   AS CriadoEm,
                    'Dono'                        AS PapelDoUsuario,
                    e.horario_inicio              AS HorarioInicio,
                    e.horario_fim                 AS HorarioFim,
                    e.duracao_consulta_padrao_minutos     AS DuracaoConsultaPadraoMinutos,
                    e.intervalo_entre_consultas_minutos   AS IntervaloEntreConsultasMinutos,
                    e.dias_semana_funcionamento::text AS DiasSemanaJson,
                    e.horarios_bloqueados::text       AS HorariosBloqueadosJson,
                    e.datas_bloqueadas::text          AS DatasBloqueadasJson,
                    '[]'::text                        AS PermissoesJson,
                    '[]'::text                        AS PermissoesExtrasJson
            FROM    public.estabelecimentos e
            WHERE   e.dono_usuario_id = @UsuarioId
            UNION
            SELECT  e.id,
                    e.nome_fantasia,
                    e.razao_social,
                    e.cnpj,
                    e.telefone,
                    e.endereco,
                    e.cidade,
                    e.estado,
                    e.foto_url,
                    e.status,
                    e.criado_em,
                    'Profissional',
                    e.horario_inicio,
                    e.horario_fim,
                    e.duracao_consulta_padrao_minutos,
                    e.intervalo_entre_consultas_minutos,
                    e.dias_semana_funcionamento::text,
                    e.horarios_bloqueados::text,
                    e.datas_bloqueadas::text,
                    COALESCE(mp.permissoes::text, '[]'),
                    COALESCE(mp.permissoes_extras::text, '[]')
            FROM    public.estabelecimentos e
            JOIN    public.vinculo_profissional_estabelecimento v ON v.estabelecimento_id = e.id
            LEFT JOIN public.modelo_permissao_estabelecimento mp ON mp.id = v.modelo_permissao_id
            WHERE   v.profissional_usuario_id = @UsuarioId
              AND   v.status = 'Ativo'
            ORDER BY NomeFantasia
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var rows = await conn.QueryAsync<EstabelecimentoLinha>(sql, new { UsuarioId = usuarioId });

        return rows.Select(r => new EstabelecimentoDto
        {
            Id = r.Id,
            NomeFantasia = r.NomeFantasia,
            RazaoSocial = r.RazaoSocial,
            Cnpj = r.Cnpj,
            Telefone = r.Telefone,
            Endereco = r.Endereco,
            Cidade = r.Cidade,
            Estado = r.Estado,
            FotoUrl = _fotoStorage.GerarUrlLeitura(r.FotoUrl),
            Status = r.Status,
            CriadoEm = r.CriadoEm,
            PapelDoUsuario = r.PapelDoUsuario,
            HorarioInicio = r.HorarioInicio,
            HorarioFim = r.HorarioFim,
            DuracaoConsultaPadraoMinutos = r.DuracaoConsultaPadraoMinutos,
            IntervaloEntreConsultasMinutos = r.IntervaloEntreConsultasMinutos,
            DiasSemanaFuncionamento = JsonSerializer.Deserialize<List<int>>(r.DiasSemanaJson ?? "[]") ?? new(),
            HorariosBloqueados = JsonSerializer.Deserialize<List<HorarioBloqueadoDto>>(r.HorariosBloqueadosJson ?? "[]", _jsonOpts) ?? new(),
            DatasBloqueadas = JsonSerializer.Deserialize<List<DataBloqueadaDto>>(r.DatasBloqueadasJson ?? "[]", _jsonOpts) ?? new(),
            Permissoes = JsonSerializer.Deserialize<List<string>>(r.PermissoesJson ?? "[]") ?? new(),
            PermissoesExtras = JsonSerializer.Deserialize<List<string>>(r.PermissoesExtrasJson ?? "[]") ?? new(),
        });
    }

    /// <summary>
    /// Verifica se o CNPJ (apenas dígitos) já está em uso por algum estabelecimento.
    /// Usado pelo onboarding/validação inline.
    /// </summary>
    public async Task<bool> ExisteCnpj(string cnpjDigitos)
    {
        if (string.IsNullOrEmpty(cnpjDigitos)) return false;

        const string sql = """
            SELECT EXISTS(SELECT 1 FROM public.estabelecimentos WHERE cnpj = @cnpj)
        """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<bool>(sql, new { cnpj = cnpjDigitos });
    }

    /// <summary>
    /// Retorna apenas os campos de funcionamento necessários para calcular disponibilidade.
    /// </summary>
    public async Task<ConfiguracaoFuncionamentoDto?> ObterConfiguracaoFuncionamento(long estabelecimentoId)
    {
        // horario_inicio/fim são castados para ::text para evitar problemas de mapeamento
        // do tipo PostgreSQL 'time' com Dapper — parse manual garante compatibilidade.
        const string sql = """
            SELECT  horario_inicio::text                 AS HorarioInicioStr,
                    horario_fim::text                    AS HorarioFimStr,
                    duracao_consulta_padrao_minutos      AS DuracaoConsultaPadraoMinutos,
                    intervalo_entre_consultas_minutos    AS IntervaloEntreConsultasMinutos,
                    dias_semana_funcionamento::text      AS DiasSemanaJson,
                    horarios_bloqueados::text            AS HorariosBloqueadosJson,
                    datas_bloqueadas::text               AS DatasBloqueadasJson
            FROM    public.estabelecimentos
            WHERE   id = @Id
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var linha = await conn.QuerySingleOrDefaultAsync<ConfiguracaoFuncionamentoLinha>(sql, new { Id = estabelecimentoId });
        if (linha is null) return null;

        return new ConfiguracaoFuncionamentoDto
        {
            HorarioInicio = TimeOnly.TryParse(linha.HorarioInicioStr, out var hi) ? hi : new TimeOnly(8, 0),
            HorarioFim = TimeOnly.TryParse(linha.HorarioFimStr, out var hf) ? hf : new TimeOnly(18, 0),
            DuracaoConsultaPadraoMinutos = linha.DuracaoConsultaPadraoMinutos,
            IntervaloEntreConsultasMinutos = linha.IntervaloEntreConsultasMinutos,
            DiasSemanaFuncionamento = JsonSerializer.Deserialize<List<int>>(linha.DiasSemanaJson ?? "[1,2,3,4,5]") ?? new(),
            HorariosBloqueados = JsonSerializer.Deserialize<List<HorarioBloqueadoDispo>>(linha.HorariosBloqueadosJson ?? "[]", _jsonOpts) ?? new(),
            DatasBloqueadas = JsonSerializer.Deserialize<List<DataBloqueadaDispo>>(linha.DatasBloqueadasJson ?? "[]", _jsonOpts) ?? new(),
        };
    }

    private class ConfiguracaoFuncionamentoLinha
    {
        public string HorarioInicioStr { get; set; } = "08:00:00";
        public string HorarioFimStr { get; set; } = "18:00:00";
        public int DuracaoConsultaPadraoMinutos { get; set; } = 30;
        public int IntervaloEntreConsultasMinutos { get; set; } = 0;
        public string DiasSemanaJson { get; set; } = "[1,2,3,4,5]";
        public string HorariosBloqueadosJson { get; set; } = "[]";
        public string DatasBloqueadasJson { get; set; } = "[]";
    }

    private class EstabelecimentoLinha
    {
        public long Id { get; set; }
        public string NomeFantasia { get; set; }
        public string RazaoSocial { get; set; }
        public string Cnpj { get; set; }
        public string Telefone { get; set; }
        public string Endereco { get; set; }
        public string Cidade { get; set; }
        public string Estado { get; set; }
        public string FotoUrl { get; set; }
        public string Status { get; set; }
        public DateTime CriadoEm { get; set; }
        public string PapelDoUsuario { get; set; }
        public TimeOnly HorarioInicio { get; set; }
        public TimeOnly HorarioFim { get; set; }
        public int DuracaoConsultaPadraoMinutos { get; set; }
        public int IntervaloEntreConsultasMinutos { get; set; }
        public string DiasSemanaJson { get; set; }
        public string HorariosBloqueadosJson { get; set; }
        public string DatasBloqueadasJson { get; set; }
        public string PermissoesJson { get; set; } = "[]";
        public string PermissoesExtrasJson { get; set; } = "[]";
    }
}
