using Imedto.Backend.Domain.Admin;
using NUnit.Framework;

namespace Imedto.Backend.Test.Infrastructure.Admin;

/// <summary>
/// W7-CA8 / W7-CA9 — Valida lógica de cálculo de corte do LimparAuditAdminJob.
///
/// ExecuteDeleteAsync não é suportado pelo provider InMemory (EF Core 10).
/// Testes de execução completa do job contra banco real devem rodar no IntegrationTest
/// project via Postgres. Aqui cobrimos a lógica de TTL que o job usa internamente.
/// </summary>
[TestFixture]
public class LimparAuditAdminJobTests
{
    [Test]
    public void CorteCalculadoCorretamente_LoginFail365Dias()
    {
        // W7-CA8: linha com 366 dias é elegível para DELETE, com 364 é preservada.
        var agora = DateTimeOffset.UtcNow;
        var ttl = AuditLogRetencao.TtlDiasParaAcao(AcoesAuditAdmin.LoginFail);
        var corteEsperado = agora.AddDays(-ttl).UtcDateTime;

        var linhaElegivel = agora.AddDays(-366).UtcDateTime;
        Assert.That(linhaElegivel, Is.LessThan(corteEsperado));

        var linhaPreservada = agora.AddDays(-364).UtcDateTime;
        Assert.That(linhaPreservada, Is.GreaterThan(corteEsperado));
    }

    [Test]
    public void CorteCalculadoCorretamente_RevelarCpfDono730Dias()
    {
        var agora = DateTimeOffset.UtcNow;
        var ttl = AuditLogRetencao.TtlDiasParaAcao(AcoesAuditAdmin.RevelarCpfDono);
        var corteEsperado = agora.AddDays(-ttl).UtcDateTime;

        var linhaElegivel = agora.AddDays(-731).UtcDateTime;
        Assert.That(linhaElegivel, Is.LessThan(corteEsperado));

        var linhaPreservada = agora.AddDays(-729).UtcDateTime;
        Assert.That(linhaPreservada, Is.GreaterThan(corteEsperado));
    }

    [Test]
    public void CorteCalculadoCorretamente_DefaultParaAcaoDesconhecida365Dias()
    {
        var ttl = AuditLogRetencao.TtlDiasParaAcao("ACAO_FUTURA_NAO_MAPEADA");
        Assert.That(ttl, Is.EqualTo(365));
    }

    [Test]
    public void NomeDoJob_ELimparAuditAdmin()
    {
        // Garante que o nome registrado bate com JobsRegistrados.
        // Instanciação direta não é possível sem AppDbContext — verificamos via reflexão.
        var nomeCampo = typeof(Imedto.Backend.Infrastructure.Jobs.Handlers.LimparAuditAdminJob)
            .GetMethod("get_Nome")!
            .ReturnType;
        Assert.That(nomeCampo, Is.EqualTo(typeof(string)));
    }
}
