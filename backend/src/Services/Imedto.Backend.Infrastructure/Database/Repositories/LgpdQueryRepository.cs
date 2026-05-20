using Dapper;
using Imedto.Backend.Contracts.Lgpd.Queries;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Read repository (Dapper) para exportação de dados LGPD.
/// Retorna apenas os campos estritamente necessários para cada tela (minimização).
/// </summary>
public class LgpdQueryRepository
{
    private readonly string _connectionString;

    public LgpdQueryRepository(AppReadConnectionString connection)
    {
        _connectionString = connection.Value;
    }

    public async Task<MeusDadosLgpdDto> ExportarMeusDados(Guid usuarioId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);

        // Dados do usuário
        const string sqlUsuario = """
            SELECT  id              AS UsuarioId,
                    email           AS Email,
                    nome_completo   AS NomeCompleto,
                    criado_em       AS CriadoEm,
                    ultimo_acesso_em AS UltimoAcessoEm
            FROM    public.usuarios
            WHERE   id = @UsuarioId
            """;

        var usuario = await conn.QuerySingleOrDefaultAsync<UsuarioExportDto>(sqlUsuario, new { UsuarioId = usuarioId });
        if (usuario is null)
            return null;

        // Profissional vinculado (se existir)
        const string sqlProf = """
            SELECT  u.id            AS Id,
                    u.nome_completo AS NomeCompleto,
                    p.numero_registro AS Crm
            FROM    public.profissionais p
            JOIN    public.usuarios u ON u.id = p.usuario_id
            WHERE   p.usuario_id = @UsuarioId
            """;

        var profissional = await conn.QuerySingleOrDefaultAsync<ProfissionalResumidoDto>(sqlProf, new { UsuarioId = usuarioId });

        // Vínculos com estabelecimentos
        const string sqlVinculos = """
            SELECT  e.id            AS EstabelecimentoId,
                    e.nome_fantasia AS NomeEstabelecimento,
                    v.status        AS Status,
                    v.aceito_em     AS VinculadoEm
            FROM    public.vinculo_profissional_estabelecimento v
            JOIN    public.estabelecimentos e ON e.id = v.estabelecimento_id
            WHERE   v.profissional_usuario_id = @UsuarioId
            ORDER BY v.aceito_em DESC
            """;

        var vinculos = await conn.QueryAsync<VinculoResumidoDto>(sqlVinculos, new { UsuarioId = usuarioId });

        // Notificações (sem conteúdo clínico — apenas título e metadados)
        const string sqlNotif = """
            SELECT  id          AS Id,
                    titulo      AS Titulo,
                    lida        AS Lida,
                    criada_em   AS CriadaEm
            FROM    public.notificacoes
            WHERE   usuario_id = @UsuarioId
            ORDER BY criada_em DESC
            LIMIT   200
            """;

        var notificacoes = await conn.QueryAsync<NotificacaoResumidaDto>(sqlNotif, new { UsuarioId = usuarioId });

        // Consentimentos LGPD são exibidos quando o usuário também é paciente — ligação
        // formal entre paciente e usuário ainda não existe no schema (Paciente não tem
        // usuario_id). Enquanto não há vínculo, devolvemos lista vazia: o módulo
        // legado lgpd_consentimentos foi arquivado na Fase 5 de Termos de Consentimento.
        // Quando paciente_usuario existir, popular daqui via termo_emitido (categoria=lgpd, assinado).
        var consentimentos = Array.Empty<ConsentimentoDto>();

        return new MeusDadosLgpdDto
        {
            UsuarioId = usuario.UsuarioId,
            Email = usuario.Email,
            NomeCompleto = usuario.NomeCompleto,
            CriadoEm = usuario.CriadoEm,
            UltimoAcessoEm = usuario.UltimoAcessoEm,
            Profissional = profissional,
            Vinculos = vinculos,
            Notificacoes = notificacoes,
            Consentimentos = consentimentos
        };
    }

    // DTO interno — não exposto ao Contracts.
    private sealed class UsuarioExportDto
    {
        public Guid UsuarioId { get; init; }
        public string Email { get; init; }
        public string NomeCompleto { get; init; }
        public DateTime CriadoEm { get; init; }
        public DateTime? UltimoAcessoEm { get; init; }
    }
}
