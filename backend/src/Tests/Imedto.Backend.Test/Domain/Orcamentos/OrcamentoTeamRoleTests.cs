using Imedto.Backend.Domain.Orcamentos.Catalogos;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Orcamentos;

[TestFixture]
public class OrcamentoTeamRoleTests
{
    [Test]
    public void Criar_ComValoresValidos_PreencheCamposEAtiva()
    {
        var sut = OrcamentoTeamRole.Criar(1, " Cirurgião principal ", null, " Dra. Beatriz ",
            TipoHonorarioTeamRole.Percentual, 60m, " procedimento ");
        Assert.That(sut.Papel, Is.EqualTo("Cirurgião principal"));
        Assert.That(sut.NomePadrao, Is.EqualTo("Dra. Beatriz"));
        Assert.That(sut.TipoHonorario, Is.EqualTo(TipoHonorarioTeamRole.Percentual));
        Assert.That(sut.Valor, Is.EqualTo(60m));
        Assert.That(sut.BaseCalculo, Is.EqualTo("procedimento"));
        Assert.That(sut.Ativo, Is.True);
    }

    [Test]
    public void Criar_PercentualMaiorQue100_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            OrcamentoTeamRole.Criar(1, "Cirurgião", null, null, TipoHonorarioTeamRole.Percentual, 101m, "procedimento"));
        Assert.That(ex!.Message, Does.Contain("100%"));
    }

    [Test]
    public void Criar_ValorNegativo_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() =>
            OrcamentoTeamRole.Criar(1, "Cirurgião", null, null, TipoHonorarioTeamRole.Fixo, -1m, "por cirurgia"));
    }

    [Test]
    public void Criar_FixoMaiorQue100_AceitaNormalmente()
    {
        var sut = OrcamentoTeamRole.Criar(1, "Instrumentadora", null, null, TipoHonorarioTeamRole.Fixo, 350m, "por cirurgia");
        Assert.That(sut.Valor, Is.EqualTo(350m));
    }

    [Test]
    public void Criar_EstabelecimentoInvalido_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() =>
            OrcamentoTeamRole.Criar(0, "Cirurgião", null, null, TipoHonorarioTeamRole.Fixo, 100m, "procedimento"));
    }

    [Test]
    public void Atualizar_AlteraCampos_EAtualizaTimestamp()
    {
        var sut = OrcamentoTeamRole.Criar(1, "Cirurgião", null, null, TipoHonorarioTeamRole.Percentual, 30m, "procedimento");
        sut.Atualizar("Primeiro auxiliar", null, "Dr. X", TipoHonorarioTeamRole.Fixo, 400m, "por cirurgia");
        Assert.That(sut.Papel, Is.EqualTo("Primeiro auxiliar"));
        Assert.That(sut.TipoHonorario, Is.EqualTo(TipoHonorarioTeamRole.Fixo));
        Assert.That(sut.Valor, Is.EqualTo(400m));
        Assert.That(sut.AtualizadaEm, Is.Not.Null);
    }

    [Test]
    public void Inativar_DepoisDeAtivo_DesativaERegistraTimestamp()
    {
        var sut = OrcamentoTeamRole.Criar(1, "Cirurgião", null, null, TipoHonorarioTeamRole.Percentual, 50m, "procedimento");
        sut.Inativar();
        Assert.That(sut.Ativo, Is.False);
        Assert.That(sut.AtualizadaEm, Is.Not.Null);
    }
}
