using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Npgsql;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Domain.Assinaturas;
using Imedto.Backend.Infrastructure;

namespace Imedto.Backend.Infrastructure.Assinaturas;

/// <summary>
/// Implementação de <see cref="IAssinaturaService"/>. Cache em memória de 1 minuto para
/// evitar 1 query por request gated.
///
/// F3 (briefing 2026-06-11_003): fonte de dados migrada da estrutura legada
/// (assinaturas/planos, bigint) para a estrutura nova (imedto_assinaturas/imedto_planos, uuid).
/// O estado derivado (ativo/bloqueado/feature) é calculado pelo Domain.Admin — sem leitura
/// do enum legado StatusAssinatura.
///
/// Política fail-closed: erro ao consultar = bloqueia. Preferimos bloquear a feature do que
/// liberar indevidamente em caso de falha técnica.
/// </summary>
public class AssinaturaService : IAssinaturaService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(1);

    private readonly IImedtoAssinaturaRepository _assinaturaRepo;
    private readonly IImedtoPlanoRepository _planoRepo;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AssinaturaService> _logger;
    private readonly string _connectionString;

    public AssinaturaService(
        IImedtoAssinaturaRepository assinaturaRepo,
        IImedtoPlanoRepository planoRepo,
        IMemoryCache cache,
        ILogger<AssinaturaService> logger,
        AppReadConnectionString connection)
    {
        _assinaturaRepo = assinaturaRepo;
        _planoRepo = planoRepo;
        _cache = cache;
        _logger = logger;
        _connectionString = connection.Value;
    }

    public async Task<bool> TenantTemFeature(long estabelecimentoId, string feature, CancellationToken ct = default)
    {
        if (estabelecimentoId <= 0 || string.IsNullOrWhiteSpace(feature)) return false;

        var chave = $"assinatura:feature:{estabelecimentoId}:{feature.ToLowerInvariant()}";
        if (_cache.TryGetValue<bool>(chave, out var cached))
            return cached;

        var liberado = await ResolverFeature(estabelecimentoId, feature, ct);
        _cache.Set(chave, liberado, CacheTtl);
        return liberado;
    }

    public async Task<bool> TenantEstaAtivo(long estabelecimentoId, CancellationToken ct = default)
    {
        if (estabelecimentoId <= 0) return false;

        var chave = $"assinatura:ativo:{estabelecimentoId}";
        if (_cache.TryGetValue<bool>(chave, out var cached))
            return cached;

        var ativo = await ResolverAtivo(estabelecimentoId, ct);
        _cache.Set(chave, ativo, CacheTtl);
        return ativo;
    }

    public async Task<ResultadoFeature> AvaliarFeature(long estabelecimentoId, string feature, CancellationToken ct = default)
    {
        if (estabelecimentoId <= 0 || string.IsNullOrWhiteSpace(feature))
            return ResultadoFeature.AssinaturaInativa;

        try
        {
            var assinatura = await _assinaturaRepo.ObterVigenteDoEstabelecimentoAsync(estabelecimentoId, ct);
            if (assinatura is null || !assinatura.EstaAtiva())
                return ResultadoFeature.AssinaturaInativa;

            var plano = await _planoRepo.ObterPorIdAsync(assinatura.PlanoId, ct);
            if (plano is null || !plano.TemFeature(feature))
                return ResultadoFeature.FeatureNaoIncluida;

            return ResultadoFeature.Liberada;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Falha ao avaliar feature gating. Estabelecimento={EstabelecimentoId} Feature={Feature}",
                estabelecimentoId, feature);
            // Fail-closed: erro técnico vira AssinaturaInativa para forçar usuário a checar planos.
            return ResultadoFeature.AssinaturaInativa;
        }
    }

    private async Task<bool> ResolverFeature(long estabelecimentoId, string feature, CancellationToken ct)
    {
        var resultado = await AvaliarFeature(estabelecimentoId, feature, ct);
        return resultado == ResultadoFeature.Liberada;
    }

    private async Task<bool> ResolverAtivo(long estabelecimentoId, CancellationToken ct)
    {
        try
        {
            var assinatura = await _assinaturaRepo.ObterVigenteDoEstabelecimentoAsync(estabelecimentoId, ct);
            return assinatura is not null && assinatura.EstaAtiva();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Falha ao resolver status ativo. Estabelecimento={EstabelecimentoId}",
                estabelecimentoId);
            return false;
        }
    }

    public void InvalidarCache(long estabelecimentoId)
    {
        if (estabelecimentoId <= 0) return;
        // Remove a chave "ativo" e todas as chaves "feature:{X}" do estabelecimento.
        // IMemoryCache não suporta busca por prefixo, então enumeramos as features
        // conhecidas — adicionar nova feature exige listar aqui (defense em depth: se
        // esquecer, a feature nova só leva 1min pra refletir).
        _cache.Remove($"assinatura:ativo:{estabelecimentoId}");
        foreach (var feature in new[]
        {
            Features.Receitas, Features.ExameFisico, Features.ProcedimentosCirurgicos,
            Features.OrcamentoCompleto, Features.Ia, Features.RelatoriosAvancados,
            Features.AutomacoesIlimitadas, Features.AnexosIlimitados,
        })
        {
            _cache.Remove($"assinatura:feature:{estabelecimentoId}:{feature.ToLowerInvariant()}");
        }
    }

    public async Task<bool> LimiteAtingidoAsync(long estabelecimentoId, string recurso, CancellationToken ct = default)
    {
        if (estabelecimentoId <= 0) return true;

        var assinatura = await _assinaturaRepo.ObterVigenteDoEstabelecimentoAsync(estabelecimentoId, ct);
        if (assinatura is null) return true;

        var plano = await _planoRepo.ObterPorIdAsync(assinatura.PlanoId, ct);
        if (plano is null) return true;

        var limite = recurso switch
        {
            "profissionais" => plano.ObterLimiteProfissionais(),
            "pacientes" => plano.ObterLimitePacientes(),
            _ => throw new ArgumentException($"Recurso desconhecido: {recurso}", nameof(recurso))
        };

        if (limite is null) return false; // ilimitado

        var atual = recurso switch
        {
            "profissionais" => await ContarProfissionaisAtivos(estabelecimentoId, ct),
            "pacientes" => await ContarPacientesAtivos(estabelecimentoId, ct),
            _ => 0
        };

        return atual >= limite.Value;
    }

    // Conta vínculos ativos + 1 (dono). O dono nunca tem vínculo na tabela
    // vinculo_profissional_estabelecimento (regra de negócio validada no ConvidarProfissionalCommandHandler),
    // então somar 1 é seguro e correto.
    private async Task<int> ContarProfissionaisAtivos(long estabelecimentoId, CancellationToken ct)
    {
        const string sql = """
            SELECT COUNT(*)::int
            FROM vinculo_profissional_estabelecimento
            WHERE estabelecimento_id = @EstabelecimentoId
              AND status = 'Ativo'
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var vinculos = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql,
            new { EstabelecimentoId = estabelecimentoId }, cancellationToken: ct));

        return vinculos + 1; // +1 pelo dono
    }

    private async Task<int> ContarPacientesAtivos(long estabelecimentoId, CancellationToken ct)
    {
        const string sql = """
            SELECT COUNT(*)::int
            FROM pacientes
            WHERE estabelecimento_id = @EstabelecimentoId
              AND deletado_em IS NULL
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql,
            new { EstabelecimentoId = estabelecimentoId }, cancellationToken: ct));
    }
}
