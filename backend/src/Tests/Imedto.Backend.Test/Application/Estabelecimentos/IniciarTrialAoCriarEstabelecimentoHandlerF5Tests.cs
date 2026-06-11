using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Imedto.Backend.Application.Estabelecimentos.Events;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Domain.Estabelecimentos.Events;
using Imedto.Backend.Infrastructure.Database;

namespace Imedto.Backend.Test.Application.Estabelecimentos;

/// <summary>
/// Cobre CA34 e CA35 do briefing 2026-06-11_003 F5:
/// IniciarTrialAoCriarEstabelecimentoHandler grava na estrutura nova (imedto_assinaturas)
/// usando a config global editável (imedto_config_trial).
/// </summary>
[TestFixture]
public class IniciarTrialAoCriarEstabelecimentoHandlerF5Tests
{
    private static readonly Guid _planoTrialId = new("00000000-0000-0000-0000-000000000001");
    private const long _estabelecimentoId = 42L;

    private AppDbContext _db = null!;
    private Mock<IImedtoConfigTrialRepository> _configTrialRepoMock = null!;
    private Mock<IImedtoAssinaturaRepository> _assinaturaRepoMock = null!;
    private IniciarTrialAoCriarEstabelecimentoHandler _handler = null!;

    private EstabelecimentoCriadoEvent Evento() =>
        new(_estabelecimentoId, Guid.NewGuid(), "Clínica Teste");

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _configTrialRepoMock = new Mock<IImedtoConfigTrialRepository>();
        _assinaturaRepoMock = new Mock<IImedtoAssinaturaRepository>();

        _handler = new IniciarTrialAoCriarEstabelecimentoHandler(
            _configTrialRepoMock.Object,
            _assinaturaRepoMock.Object,
            _db,
            NullLogger<IniciarTrialAoCriarEstabelecimentoHandler>.Instance);
    }

    /// <summary>
    /// CA34: trial_habilitado=true e duração=14 → cria ImedtoAssinatura na nova estrutura
    /// com expira_em ≈ now+14d, plano configurado, sem escrever na legada.
    /// </summary>
    [Test]
    public async Task Handle_TrialHabilitado_CriaAssinaturaNaNovaEstrutura()
    {
        var config = ImedtoConfigTrial.CriarPadrao(_planoTrialId);
        // Padrão: DuracaoTrialDias=14, TrialHabilitado=true.
        _configTrialRepoMock.Setup(r => r.ObterAsync(It.IsAny<CancellationToken>()))
                             .ReturnsAsync(config);

        ImedtoAssinatura? assinaturaCriada = null;
        _assinaturaRepoMock
            .Setup(r => r.Adicionar(It.IsAny<ImedtoAssinatura>()))
            .Callback<ImedtoAssinatura>(a => assinaturaCriada = a);

        var antes = DateTimeOffset.UtcNow;
        await _handler.Handle(Evento());
        var depois = DateTimeOffset.UtcNow;

        _assinaturaRepoMock.Verify(r => r.Adicionar(It.IsAny<ImedtoAssinatura>()), Times.Once,
            "Deve adicionar exatamente uma ImedtoAssinatura na nova estrutura.");

        Assert.That(assinaturaCriada, Is.Not.Null);
        Assert.That(assinaturaCriada!.EstabelecimentoId, Is.EqualTo(_estabelecimentoId));
        Assert.That(assinaturaCriada.PlanoId, Is.EqualTo(_planoTrialId));
        Assert.That(assinaturaCriada.FimEm, Is.Null, "Vigência deve estar aberta (fim_em IS NULL).");
        Assert.That(assinaturaCriada.SuspensaEm, Is.Null, "Não deve nascer suspensa.");
        Assert.That(assinaturaCriada.ExpiraEm, Is.Not.Null, "Trial deve ter data de expiração.");

        // expira_em deve ser now+14d (tolerância de 2 segundos de execução do teste).
        var expiraEsperadaMin = antes.AddDays(14);
        var expiraEsperadaMax = depois.AddDays(14);
        Assert.That(assinaturaCriada.ExpiraEm!.Value, Is.InRange(expiraEsperadaMin, expiraEsperadaMax),
            "expira_em deve ser now+14 dias conforme config.");
    }

    /// <summary>
    /// CA35: config alterada para duração=30 → expira_em = now+30d.
    /// </summary>
    [Test]
    public async Task Handle_TrialHabilitadoDuracao30_ExpiraEmNovaMaisTrintaDias()
    {
        var config = ImedtoConfigTrial.CriarPadrao(_planoTrialId);
        config.Atualizar(_planoTrialId, duracaoTrialDias: 30, trialHabilitado: true, adminId: null);

        _configTrialRepoMock.Setup(r => r.ObterAsync(It.IsAny<CancellationToken>()))
                             .ReturnsAsync(config);

        ImedtoAssinatura? assinaturaCriada = null;
        _assinaturaRepoMock
            .Setup(r => r.Adicionar(It.IsAny<ImedtoAssinatura>()))
            .Callback<ImedtoAssinatura>(a => assinaturaCriada = a);

        var antes = DateTimeOffset.UtcNow;
        await _handler.Handle(Evento());
        var depois = DateTimeOffset.UtcNow;

        Assert.That(assinaturaCriada, Is.Not.Null);
        var expiraEsperadaMin = antes.AddDays(30);
        var expiraEsperadaMax = depois.AddDays(30);
        Assert.That(assinaturaCriada!.ExpiraEm!.Value, Is.InRange(expiraEsperadaMin, expiraEsperadaMax),
            "expira_em deve ser now+30 dias conforme config atualizada.");
    }

    /// <summary>
    /// CA35 (complemento): trial_habilitado=false → nasce sem vigência (estado BLOQUEADO).
    /// Nenhuma ImedtoAssinatura é criada.
    /// </summary>
    [Test]
    public async Task Handle_TrialDesabilitado_NaoCriaAssinatura()
    {
        var config = ImedtoConfigTrial.CriarPadrao(_planoTrialId);
        config.Atualizar(_planoTrialId, duracaoTrialDias: 14, trialHabilitado: false, adminId: null);

        _configTrialRepoMock.Setup(r => r.ObterAsync(It.IsAny<CancellationToken>()))
                             .ReturnsAsync(config);

        await _handler.Handle(Evento());

        _assinaturaRepoMock.Verify(r => r.Adicionar(It.IsAny<ImedtoAssinatura>()), Times.Never,
            "Com trial desabilitado, nenhuma ImedtoAssinatura deve ser criada — nasce BLOQUEADO.");
    }

    /// <summary>
    /// Config ausente (seed não rodou) → handler retorna sem criar assinatura (sem exceção).
    /// </summary>
    [Test]
    public async Task Handle_ConfigAusente_RetornaSemCriarAssinatura()
    {
        _configTrialRepoMock.Setup(r => r.ObterAsync(It.IsAny<CancellationToken>()))
                             .ReturnsAsync((ImedtoConfigTrial?)null);

        // Não deve lançar exceção.
        await _handler.Handle(Evento());

        _assinaturaRepoMock.Verify(r => r.Adicionar(It.IsAny<ImedtoAssinatura>()), Times.Never);
    }
}
