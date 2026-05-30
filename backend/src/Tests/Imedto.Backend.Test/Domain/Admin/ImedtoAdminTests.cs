using Imedto.Backend.Domain.Admin;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Admin;

/// <summary>
/// Cobre as invariantes do aggregate ImedtoAdmin.
/// </summary>
[TestFixture]
public class ImedtoAdminTests
{
    [Test]
    public void Criar_EmailVazio_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            ImedtoAdmin.Criar("", "Admin", "hash", true, null));
        Assert.That(ex!.Message, Does.Contain("E-mail"));
    }

    [Test]
    public void Criar_NomeVazio_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            ImedtoAdmin.Criar("a@b.com", "", "hash", true, null));
        Assert.That(ex!.Message, Does.Contain("Nome"));
    }

    [Test]
    public void Criar_DadosValidos_AdminAtivo()
    {
        var admin = ImedtoAdmin.Criar("admin@imedto.com", "Admin", "hash123", true, null);

        Assert.That(admin.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(admin.Email, Is.EqualTo("admin@imedto.com"));
        Assert.That(admin.Ativo, Is.True);
        Assert.That(admin.ForcePasswordReset, Is.True);
        Assert.That(admin.UltimoLoginEm, Is.Null);
    }

    [Test]
    public void Criar_EmailNormalizadoParaMinusculas()
    {
        var admin = ImedtoAdmin.Criar("ADMIN@IMEDTO.COM", "Admin", "hash", false, null);
        Assert.That(admin.Email, Is.EqualTo("admin@imedto.com"));
    }

    [Test]
    public void RegistrarLogin_AtualizaUltimoLoginEm()
    {
        var admin = ImedtoAdmin.Criar("a@b.com", "A", "h", false, null);
        var antes = DateTimeOffset.UtcNow;

        admin.RegistrarLogin();

        Assert.That(admin.UltimoLoginEm, Is.Not.Null);
        Assert.That(admin.UltimoLoginEm!.Value, Is.GreaterThanOrEqualTo(antes));
    }

    [Test]
    public void AtualizarSenha_HashVazio_LancaBusinessException()
    {
        var admin = ImedtoAdmin.Criar("a@b.com", "A", "h", false, null);
        Assert.Throws<BusinessException>(() => admin.AtualizarSenha("", false));
    }

    [Test]
    public void Desativar_AdminJaInativo_LancaBusinessException()
    {
        var admin = ImedtoAdmin.Criar("a@b.com", "A", "h", false, null);
        admin.Desativar(Guid.NewGuid());
        // Tentar desativar novamente deve lançar.
        Assert.Throws<BusinessException>(() => admin.Desativar(Guid.NewGuid()));
    }

    [Test]
    public void Desativar_AdminAtivo_DesativaCorretamente()
    {
        var admin = ImedtoAdmin.Criar("a@b.com", "A", "h", false, null);
        var desativadoPor = Guid.NewGuid();

        admin.Desativar(desativadoPor);

        Assert.That(admin.Ativo, Is.False);
        Assert.That(admin.DesativadoEm, Is.Not.Null);
        Assert.That(admin.DesativadoPorAdminId, Is.EqualTo(desativadoPor));
    }

    // ── Smoke: Reativar ──────────────────────────────────────────────────────────

    [Test]
    public void Reativar_AdminInativo_ReativaCorretamente()
    {
        var admin = ImedtoAdmin.Criar("a@b.com", "A", "h", false, null);
        admin.Desativar(Guid.NewGuid());

        admin.Reativar(Guid.NewGuid());

        Assert.That(admin.Ativo, Is.True);
        Assert.That(admin.DesativadoEm, Is.Null);
        Assert.That(admin.DesativadoPorAdminId, Is.Null);
    }

    [Test]
    public void Reativar_AdminJaAtivo_LancaBusinessException()
    {
        var admin = ImedtoAdmin.Criar("a@b.com", "A", "h", false, null);

        var ex = Assert.Throws<BusinessException>(() => admin.Reativar(Guid.NewGuid()));
        Assert.That(ex!.Message, Does.Contain("já está ativo"));
    }

    // ── Smoke: AtualizarSenha (ResetSenha handler) ─────────────────────────────

    [Test]
    public void AtualizarSenha_HashValido_ForceResetTrue_MarcaParaReset()
    {
        var admin = ImedtoAdmin.Criar("a@b.com", "A", "hash_antigo", false, null);

        admin.AtualizarSenha("novo_hash_valido", forceReset: true);

        Assert.That(admin.SenhaHash, Is.EqualTo("novo_hash_valido"));
        Assert.That(admin.ForcePasswordReset, Is.True);
        Assert.That(admin.AtualizadoEm, Is.Not.Null);
    }

    [Test]
    public void ConcluirResetSenha_LimpaFlag_ForcePasswordResetFalse()
    {
        var admin = ImedtoAdmin.Criar("a@b.com", "A", "h", forcePasswordReset: true, null);

        admin.ConcluirResetSenha();

        Assert.That(admin.ForcePasswordReset, Is.False);
        Assert.That(admin.AtualizadoEm, Is.Not.Null);
    }
}
