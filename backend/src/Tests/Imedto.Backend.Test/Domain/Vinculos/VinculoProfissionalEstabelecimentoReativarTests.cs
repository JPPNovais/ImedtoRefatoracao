using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.Domain.Vinculos.Events;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Vinculos;

[TestFixture]
public class VinculoProfissionalEstabelecimentoReativarTests
{
    private static VinculoProfissionalEstabelecimento CriarVinculoInativo()
    {
        var vinculo = VinculoProfissionalEstabelecimento.Convidar(
            profissionalUsuarioId: Guid.NewGuid(),
            estabelecimentoId: 1,
            modeloPermissaoId: 1,
            convidadoPorUsuarioId: Guid.NewGuid());
        vinculo.Aceitar();
        vinculo.Inativar();
        return vinculo;
    }

    [Test]
    public void ReativarComoConvite_VinculoInativo_StatusViraConvidado()
    {
        var vinculo = CriarVinculoInativo();

        vinculo.ReativarComoConvite(novoModeloPermissaoId: 2, convidadoPorUsuarioId: Guid.NewGuid());

        Assert.That(vinculo.Status, Is.EqualTo(VinculoStatus.Convidado));
    }

    [Test]
    public void ReativarComoConvite_VinculoInativo_AtualizaModeloPermissaoId()
    {
        var vinculo = CriarVinculoInativo();

        vinculo.ReativarComoConvite(novoModeloPermissaoId: 99, convidadoPorUsuarioId: Guid.NewGuid());

        Assert.That(vinculo.ModeloPermissaoId, Is.EqualTo(99));
    }

    [Test]
    public void ReativarComoConvite_VinculoInativo_ZeraAceitoEmEInativadoEm()
    {
        var vinculo = CriarVinculoInativo();
        Assert.That(vinculo.AceitoEm, Is.Not.Null, "pré-condição: deve ter AceitoEm antes da reativação");
        Assert.That(vinculo.InativadoEm, Is.Not.Null, "pré-condição: deve ter InativadoEm antes da reativação");

        vinculo.ReativarComoConvite(2, Guid.NewGuid());

        Assert.That(vinculo.AceitoEm, Is.Null);
        Assert.That(vinculo.InativadoEm, Is.Null);
    }

    [Test]
    public void ReativarComoConvite_VinculoInativo_AtualizaConvidadoPorEConvidadoEm()
    {
        var vinculo = CriarVinculoInativo();
        var novoConvidador = Guid.NewGuid();
        var antes = DateTime.UtcNow;

        vinculo.ReativarComoConvite(2, novoConvidador);

        Assert.That(vinculo.ConvidadoPorUsuarioId, Is.EqualTo(novoConvidador));
        Assert.That(vinculo.ConvidadoEm, Is.GreaterThanOrEqualTo(antes));
    }

    [Test]
    public void ReativarComoConvite_VinculoInativo_AdicionaProfissionalConvidadoEvent()
    {
        var vinculo = CriarVinculoInativo();

        vinculo.ReativarComoConvite(2, Guid.NewGuid());

        Assert.That(vinculo.DomainEvents, Has.One.TypeOf<ProfissionalConvidadoEvent>());
    }

    [Test]
    public void ReativarComoConvite_VinculoAtivo_LancaBusinessException()
    {
        var vinculo = VinculoProfissionalEstabelecimento.Convidar(
            Guid.NewGuid(), 1, 1, Guid.NewGuid());
        vinculo.Aceitar();

        var ex = Assert.Throws<BusinessException>(() =>
            vinculo.ReativarComoConvite(2, Guid.NewGuid()));

        Assert.That(ex.Message, Does.Contain("inativos").IgnoreCase);
    }

    [Test]
    public void ReativarComoConvite_VinculoConvidado_LancaBusinessException()
    {
        var vinculo = VinculoProfissionalEstabelecimento.Convidar(
            Guid.NewGuid(), 1, 1, Guid.NewGuid());

        var ex = Assert.Throws<BusinessException>(() =>
            vinculo.ReativarComoConvite(2, Guid.NewGuid()));

        Assert.That(ex.Message, Does.Contain("inativos").IgnoreCase);
    }

    [Test]
    public void ReativarComoConvite_ModeloPermissaoIdZero_LancaBusinessException()
    {
        var vinculo = CriarVinculoInativo();

        var ex = Assert.Throws<BusinessException>(() =>
            vinculo.ReativarComoConvite(novoModeloPermissaoId: 0, Guid.NewGuid()));

        Assert.That(ex.Message, Does.Contain("Modelo de permissão").IgnoreCase);
    }

    [Test]
    public void ReativarComoConvite_ConvidadoPorUsuarioIdVazio_LancaBusinessException()
    {
        var vinculo = CriarVinculoInativo();

        var ex = Assert.Throws<BusinessException>(() =>
            vinculo.ReativarComoConvite(2, Guid.Empty));

        Assert.That(ex.Message, Does.Contain("obrigatório").IgnoreCase);
    }
}
