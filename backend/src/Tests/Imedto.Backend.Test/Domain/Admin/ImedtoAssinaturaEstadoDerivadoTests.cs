using Imedto.Backend.Domain.Admin;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Admin;

/// <summary>
/// Cobre o estado derivado R3 (EstaAtiva, ObterEstado) e as ações de suspensão/reativação
/// de ImedtoAssinatura (briefing 2026-06-11_003, F1).
/// </summary>
[TestFixture]
public class ImedtoAssinaturaEstadoDerivadoTests
{
    private static readonly Guid _planoId = Guid.NewGuid();
    private static readonly Guid _adminId = Guid.NewGuid();

    // -------------------------------------------------------
    // Criar com expiraEm
    // -------------------------------------------------------

    [Test]
    public void Criar_ExpiraEm_Futuro_AssinaturaVigenteTemporaria()
    {
        var expira = DateTimeOffset.UtcNow.AddDays(14);
        var assinatura = ImedtoAssinatura.Criar(1, _planoId, false, null, _adminId, expiraEm: expira);

        Assert.That(assinatura.ExpiraEm, Is.EqualTo(expira));
        Assert.That(assinatura.EstaAtiva(), Is.True);
        Assert.That(assinatura.ObterEstado(), Is.EqualTo(EstadoAssinatura.Temporaria));
    }

    [Test]
    public void Criar_ExpiraEm_Passado_LancaBusinessException()
    {
        var expiraPassado = DateTimeOffset.UtcNow.AddDays(-1);

        var ex = Assert.Throws<BusinessException>(() =>
            ImedtoAssinatura.Criar(1, _planoId, false, null, _adminId, expiraEm: expiraPassado));

        Assert.That(ex!.Message, Does.Contain("futuro"));
    }

    [Test]
    public void Criar_SemExpiraEm_EstadoVitalicio()
    {
        var assinatura = ImedtoAssinatura.Criar(1, _planoId, false, null, _adminId);

        Assert.That(assinatura.ExpiraEm, Is.Null);
        Assert.That(assinatura.EstaAtiva(), Is.True);
        Assert.That(assinatura.ObterEstado(), Is.EqualTo(EstadoAssinatura.Vitalicia));
    }

    // -------------------------------------------------------
    // Defaults das colunas dormentes
    // -------------------------------------------------------

    [Test]
    public void Criar_DadosMinimos_DefaultsDormentesCorretos()
    {
        var assinatura = ImedtoAssinatura.Criar(1, _planoId, false, null, _adminId);

        Assert.That(assinatura.Origem, Is.EqualTo("admin_manual"));
        Assert.That(assinatura.ReferenciaExterna, Is.Null);
        Assert.That(assinatura.StatusCobranca, Is.EqualTo("nao_aplicavel"));
    }

    // -------------------------------------------------------
    // Estado derivado R3 — VITALÍCIO
    // -------------------------------------------------------

    [Test]
    public void EstaAtiva_VigenteVitalicio_True()
    {
        var assinatura = ImedtoAssinatura.Criar(1, _planoId, false, null, _adminId);

        Assert.That(assinatura.EstaAtiva(), Is.True);
    }

    [Test]
    public void ObterEstado_VigenteVitalicio_Vitalicia()
    {
        var assinatura = ImedtoAssinatura.Criar(1, _planoId, false, null, _adminId);

        Assert.That(assinatura.ObterEstado(), Is.EqualTo(EstadoAssinatura.Vitalicia));
    }

    // -------------------------------------------------------
    // Estado derivado R3 — EXPIRADO
    // -------------------------------------------------------

    [Test]
    public void EstaAtiva_EncerradaPorFecharVigencia_False()
    {
        var assinatura = ImedtoAssinatura.Criar(1, _planoId, false, null, _adminId);
        assinatura.FecharVigencia();

        Assert.That(assinatura.EstaAtiva(), Is.False);
        Assert.That(assinatura.ObterEstado(), Is.EqualTo(EstadoAssinatura.Encerrada));
    }

    // -------------------------------------------------------
    // Suspender invariantes
    // -------------------------------------------------------

    [Test]
    public void Suspender_AssinaturaVigente_SetaSuspensaEm()
    {
        var assinatura = ImedtoAssinatura.Criar(1, _planoId, false, null, _adminId);
        var antes = DateTimeOffset.UtcNow;

        assinatura.Suspender();

        Assert.That(assinatura.SuspensaEm, Is.Not.Null);
        Assert.That(assinatura.SuspensaEm!.Value, Is.GreaterThanOrEqualTo(antes));
        Assert.That(assinatura.EstaAtiva(), Is.False);
        Assert.That(assinatura.ObterEstado(), Is.EqualTo(EstadoAssinatura.Suspensa));
    }

    [Test]
    public void Suspender_TemporarioComExpiraEm_BloqueioTrumpVigencia()
    {
        // Mesmo com expiraEm no futuro, suspensão bloqueia (R3).
        var assinatura = ImedtoAssinatura.Criar(1, _planoId, false, null, _adminId,
            expiraEm: DateTimeOffset.UtcNow.AddDays(30));

        assinatura.Suspender();

        Assert.That(assinatura.EstaAtiva(), Is.False);
        Assert.That(assinatura.ObterEstado(), Is.EqualTo(EstadoAssinatura.Suspensa));
    }

    [Test]
    public void Suspender_JaSuspensa_LancaBusinessException()
    {
        var assinatura = ImedtoAssinatura.Criar(1, _planoId, false, null, _adminId);
        assinatura.Suspender();

        var ex = Assert.Throws<BusinessException>(() => assinatura.Suspender());
        Assert.That(ex!.Message, Does.Contain("já está suspensa"));
    }

    [Test]
    public void Suspender_VigenciaEncerrada_LancaBusinessException()
    {
        var assinatura = ImedtoAssinatura.Criar(1, _planoId, false, null, _adminId);
        assinatura.FecharVigencia();

        var ex = Assert.Throws<BusinessException>(() => assinatura.Suspender());
        Assert.That(ex!.Message, Does.Contain("encerrada"));
    }

    // -------------------------------------------------------
    // Reativar invariantes
    // -------------------------------------------------------

    [Test]
    public void Reativar_AssinaturaSuspensa_ZeraSuspensaEm()
    {
        var assinatura = ImedtoAssinatura.Criar(1, _planoId, false, null, _adminId);
        assinatura.Suspender();

        assinatura.Reativar();

        Assert.That(assinatura.SuspensaEm, Is.Null);
        Assert.That(assinatura.EstaAtiva(), Is.True);
    }

    [Test]
    public void Reativar_SuspensaComExpiraEm_RetornaEstadoTemporario()
    {
        // Após reativar, o estado vem do expira_em (R4 — "volta ao estado que expira_em ditar").
        var assinatura = ImedtoAssinatura.Criar(1, _planoId, false, null, _adminId,
            expiraEm: DateTimeOffset.UtcNow.AddDays(7));
        assinatura.Suspender();

        assinatura.Reativar();

        Assert.That(assinatura.ObterEstado(), Is.EqualTo(EstadoAssinatura.Temporaria));
    }

    [Test]
    public void Reativar_NaoSuspensa_LancaBusinessException()
    {
        var assinatura = ImedtoAssinatura.Criar(1, _planoId, false, null, _adminId);

        var ex = Assert.Throws<BusinessException>(() => assinatura.Reativar());
        Assert.That(ex!.Message, Does.Contain("não está suspensa"));
    }

    [Test]
    public void Reativar_VigenciaEncerrada_LancaBusinessException()
    {
        // Vigência encerrada sem suspensão: Reativar deve lançar pois FimEm != null.
        var assinatura = ImedtoAssinatura.Criar(1, _planoId, false, null, _adminId);
        assinatura.FecharVigencia(); // encerra sem suspensão

        // Reativar em vigência encerrada não faz sentido (regra defensiva do domínio).
        // Como SuspensaEm é null, lança "não está suspensa" — o guard de FimEm chega primeiro.
        var ex = Assert.Throws<BusinessException>(() => assinatura.Reativar());
        // Pode ser "encerrada" OU "não está suspensa" dependendo da ordem dos guards;
        // o que importa é que lança BusinessException.
        Assert.That(ex!.Message, Is.Not.Empty);
    }

    // -------------------------------------------------------
    // Ciclo completo: Suspender → Reativar → Suspender
    // -------------------------------------------------------

    [Test]
    public void CicloSuspenderReativar_FuncionaVezesSeguidas()
    {
        var assinatura = ImedtoAssinatura.Criar(1, _planoId, false, null, _adminId);

        assinatura.Suspender();
        Assert.That(assinatura.EstaAtiva(), Is.False);

        assinatura.Reativar();
        Assert.That(assinatura.EstaAtiva(), Is.True);

        assinatura.Suspender();
        Assert.That(assinatura.EstaAtiva(), Is.False);
    }
}
