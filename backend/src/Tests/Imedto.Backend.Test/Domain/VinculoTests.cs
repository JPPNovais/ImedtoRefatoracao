using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain;

[TestFixture]
public class VinculoTests
{
    private static VinculoProfissionalEstabelecimento CriarConvite()
    {
        return VinculoProfissionalEstabelecimento.Convidar(
            profissionalUsuarioId: Guid.NewGuid(),
            estabelecimentoId: 1,
            modeloPermissaoId: 1,
            convidadoPorUsuarioId: Guid.NewGuid());
    }

    [Test]
    public void Convidar_Valido_StatusConvidado()
    {
        var v = CriarConvite();
        Assert.That(v.Status, Is.EqualTo(VinculoStatus.Convidado));
    }

    [Test]
    public void Convidar_ProfissionalVazio_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            VinculoProfissionalEstabelecimento.Convidar(
                Guid.Empty, 1, 1, Guid.NewGuid()));
        Assert.That(ex.Message, Does.Contain("Profissional"));
    }

    [Test]
    public void Convidar_ProfissionalIgualConvidador_LancaBusinessException()
    {
        var id = Guid.NewGuid();
        var ex = Assert.Throws<BusinessException>(() =>
            VinculoProfissionalEstabelecimento.Convidar(id, 1, 1, id));
        Assert.That(ex.Message, Does.Contain("a si mesmo"));
    }

    [Test]
    public void Aceitar_StatusConvidado_MudaParaAtivo()
    {
        var v = CriarConvite();
        v.Aceitar();
        Assert.That(v.Status, Is.EqualTo(VinculoStatus.Ativo));
    }

    [Test]
    public void Aceitar_JaAtivo_LancaBusinessException()
    {
        var v = CriarConvite();
        v.Aceitar();
        var ex = Assert.Throws<BusinessException>(() => v.Aceitar());
        Assert.That(ex.Message, Does.Contain("pendentes podem ser aceitos"));
    }

    [Test]
    public void Inativar_StatusAtivo_MudaParaInativo()
    {
        var v = CriarConvite();
        v.Aceitar();
        v.Inativar();
        Assert.That(v.Status, Is.EqualTo(VinculoStatus.Inativo));
    }

    [Test]
    public void Inativar_JaInativo_LancaBusinessException()
    {
        var v = CriarConvite();
        v.Aceitar();
        v.Inativar();
        var ex = Assert.Throws<BusinessException>(() => v.Inativar());
        Assert.That(ex.Message, Does.Contain("Vínculo já está inativo"));
    }

    [Test]
    public void AtualizarModeloPermissao_StatusInativo_LancaBusinessException()
    {
        var v = CriarConvite();
        v.Aceitar();
        v.Inativar();
        var ex = Assert.Throws<BusinessException>(() => v.AtualizarModeloPermissao(2));
        Assert.That(ex.Message, Does.Contain("inativo"));
    }
}
