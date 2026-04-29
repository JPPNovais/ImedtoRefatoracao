using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Assinaturas;
using Imedto.Backend.Infrastructure.Database;

namespace Imedto.Backend.Infrastructure.Assinaturas;

/// <summary>
/// Hosted service que roda uma vez na startup e popula o catálogo de planos se a tabela
/// <c>planos</c> estiver vazia. Idempotente — se já há ao menos um plano, não faz nada
/// (gestão posterior dos planos fica a cargo de console admin futuro). Mantém a inicialização
/// do trial dependente apenas do nome "Trial", sem precisar de coordenação com migrations.
///
/// Por que hosted service e não migration: precos/limites podem variar por ambiente, e não
/// queremos que cada deploy recrie/atualize linhas — a tabela <c>planos</c> é catálogo vivo,
/// não DDL. Fica mais previsível ter o seed em código que respeita o aggregate.
/// </summary>
public class SeedPlanosHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SeedPlanosHostedService> _logger;

    public SeedPlanosHostedService(IServiceScopeFactory scopeFactory, ILogger<SeedPlanosHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var sp = scope.ServiceProvider;
            var db = sp.GetRequiredService<AppDbContext>();
            var repo = sp.GetRequiredService<IPlanoRepository>();

            var jaExiste = await db.Planos.AsNoTracking().AnyAsync(cancellationToken);
            if (jaExiste)
            {
                _logger.LogDebug("[SeedPlanos] Catálogo de planos já populado — nada a fazer.");
                return;
            }

            foreach (var seed in PlanosSeed)
            {
                var plano = Plano.Criar(
                    nome: seed.Nome,
                    precoMensal: seed.PrecoMensal,
                    limiteProfissionais: seed.LimiteProfissionais,
                    limitePacientes: seed.LimitePacientes,
                    features: seed.Features,
                    ordem: seed.Ordem);
                await repo.Salvar(plano);
                _logger.LogInformation("[SeedPlanos] Plano '{Nome}' inserido (id {Id}).", plano.Nome, plano.Id);
            }
        }
        catch (Exception ex)
        {
            // Não derrubar o boot — o app deve subir mesmo se o seed falhar (banco indisponível
            // momentaneamente, por exemplo). Próxima inicialização tenta de novo.
            _logger.LogError(ex, "[SeedPlanos] Falha ao popular catálogo de planos.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Catálogo inicial. Os preços de Pro/Enterprise são placeholders — a definição final
    /// virá com o módulo de billing (fase posterior). Os limites e a lista de features são
    /// derivados do produto/plano de migração descrito no item 2.7 da Fase 2.
    /// </summary>
    private static readonly IReadOnlyList<PlanoSeed> PlanosSeed = new List<PlanoSeed>
    {
        new(
            Nome: "Free",
            PrecoMensal: 0m,
            LimiteProfissionais: 2,
            LimitePacientes: 50,
            Features: Array.Empty<string>(),
            Ordem: 0),

        new(
            Nome: "Trial",
            PrecoMensal: 0m,
            LimiteProfissionais: 10,
            LimitePacientes: 200,
            Features: new[]
            {
                Features.Receitas,
                Features.ExameFisico,
                Features.ProcedimentosCirurgicos,
                Features.OrcamentoCompleto,
                Features.Ia,
                Features.RelatoriosAvancados,
                Features.AutomacoesIlimitadas,
                Features.AnexosIlimitados
            },
            Ordem: 1),

        new(
            Nome: "Pro",
            PrecoMensal: 199m,
            LimiteProfissionais: 10,
            LimitePacientes: 500,
            Features: new[]
            {
                Features.Receitas,
                Features.ExameFisico,
                Features.OrcamentoCompleto,
                Features.RelatoriosAvancados,
                Features.AutomacoesIlimitadas
            },
            Ordem: 2),

        new(
            Nome: "Enterprise",
            PrecoMensal: 499m,
            LimiteProfissionais: null,
            LimitePacientes: null,
            Features: new[]
            {
                Features.Receitas,
                Features.ExameFisico,
                Features.ProcedimentosCirurgicos,
                Features.OrcamentoCompleto,
                Features.Ia,
                Features.RelatoriosAvancados,
                Features.AutomacoesIlimitadas,
                Features.AnexosIlimitados
            },
            Ordem: 3),
    };

    private sealed record PlanoSeed(
        string Nome,
        decimal PrecoMensal,
        int? LimiteProfissionais,
        int? LimitePacientes,
        IReadOnlyList<string> Features,
        int Ordem);
}
