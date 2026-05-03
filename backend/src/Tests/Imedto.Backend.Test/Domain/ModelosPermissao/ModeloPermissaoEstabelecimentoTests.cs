using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.ModelosPermissao;

[TestFixture]
public class ModeloPermissaoEstabelecimentoTests
{
    private static ModeloPermissaoEstabelecimento CriarValido(bool ehPadrao = false)
    {
        if (ehPadrao)
            return ModeloPermissaoEstabelecimento.CriarPadroes(1L).First(m => m.Nome == "Admin");

        return ModeloPermissaoEstabelecimento.Criar(
            estabelecimentoId: 1L,
            nome: " Coordenacao ",
            tipoAcesso: TipoAcessoModelo.Profissional,
            permissoes: new[] { "agenda", "pacientes" },
            permissoesExtras: new[] { PermissoesExtras.AssistenteClinicoIa });
    }

    // ----- CriarPadroes -----

    [Test]
    public void CriarPadroes_RetornaTresModelosPadrao()
    {
        var padroes = ModeloPermissaoEstabelecimento.CriarPadroes(1L);

        Assert.That(padroes, Has.Count.EqualTo(3));
        Assert.That(padroes.Select(m => m.Nome),
            Is.EquivalentTo(new[] { "Admin", "Médico", "Recepção" }));
        Assert.That(padroes.All(m => m.EhPadrao), Is.True);
    }

    [Test]
    public void CriarPadroes_AdminTemPermissoesAdministrativas()
    {
        var admin = ModeloPermissaoEstabelecimento.CriarPadroes(1L).First(m => m.Nome == "Admin");
        Assert.That(admin.TemPermissaoExtra(PermissoesExtras.GerirPermissoes), Is.True);
        Assert.That(admin.TemPermissaoExtra(PermissoesExtras.GerirProfissionais), Is.True);
    }

    [Test]
    public void CriarPadroes_RecepcaoNaoTemPermissoesExtras()
    {
        var recepcao = ModeloPermissaoEstabelecimento.CriarPadroes(1L).First(m => m.Nome == "Recepção");
        Assert.That(recepcao.PermissoesExtrasLista, Is.Empty);
    }

    // ----- Criar -----

    [Test]
    public void Criar_Valido_NomeTrimadoENaoEhPadrao()
    {
        var m = CriarValido();
        Assert.That(m.Nome, Is.EqualTo("Coordenacao"));
        Assert.That(m.EhPadrao, Is.False);
        Assert.That(m.Permissoes, Is.EquivalentTo(new[] { "agenda", "pacientes" }));
        Assert.That(m.PermissoesExtrasLista, Is.EquivalentTo(new[] { PermissoesExtras.AssistenteClinicoIa }));
    }

    [Test]
    public void Criar_PermissoesNull_RetornaListasVazias()
    {
        var m = ModeloPermissaoEstabelecimento.Criar(1L, "X", TipoAcessoModelo.Profissional);
        Assert.That(m.Permissoes, Is.Empty);
        Assert.That(m.PermissoesExtrasLista, Is.Empty);
    }

    [Test]
    public void Criar_PermissoesDuplicadas_NormalizaSemDuplicar()
    {
        var m = ModeloPermissaoEstabelecimento.Criar(1L, "X", TipoAcessoModelo.Profissional,
            new[] { "agenda", "agenda", " agenda ", "pacientes" });
        Assert.That(m.Permissoes, Is.EquivalentTo(new[] { "agenda", "pacientes" }));
    }

    [Test]
    public void Criar_EstabelecimentoZero_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() =>
            ModeloPermissaoEstabelecimento.Criar(0L, "X", TipoAcessoModelo.Profissional));
    }

    [Test]
    public void Criar_NomeVazio_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() =>
            ModeloPermissaoEstabelecimento.Criar(1L, " ", TipoAcessoModelo.Profissional));
    }

    // ----- Atualizar -----

    [Test]
    public void Atualizar_Valido_AtualizaCampos()
    {
        var m = CriarValido();
        m.Atualizar("Novo Nome", TipoAcessoModelo.Recepcionista, new[] { "agenda" }, null);

        Assert.That(m.Nome, Is.EqualTo("Novo Nome"));
        Assert.That(m.TipoAcesso, Is.EqualTo(TipoAcessoModelo.Recepcionista));
        Assert.That(m.Permissoes, Is.EquivalentTo(new[] { "agenda" }));
        Assert.That(m.PermissoesExtrasLista, Is.Empty);
        Assert.That(m.AtualizadoEm, Is.Not.Null);
    }

    [Test]
    public void Atualizar_ModeloPadrao_LancaBusinessException()
    {
        var padrao = CriarValido(ehPadrao: true);
        var ex = Assert.Throws<BusinessException>(() =>
            padrao.Atualizar("Hack", TipoAcessoModelo.Profissional));
        Assert.That(ex.Message, Does.Contain("padrão"));
    }

    [Test]
    public void Atualizar_NomeVazio_LancaBusinessException()
    {
        var m = CriarValido();
        Assert.Throws<BusinessException>(() =>
            m.Atualizar(" ", TipoAcessoModelo.Profissional));
    }

    // ----- GarantirPodeExcluir -----

    [Test]
    public void GarantirPodeExcluir_NaoPadrao_NaoLanca()
    {
        var m = CriarValido();
        Assert.DoesNotThrow(() => m.GarantirPodeExcluir());
    }

    [Test]
    public void GarantirPodeExcluir_Padrao_LancaBusinessException()
    {
        var m = CriarValido(ehPadrao: true);
        Assert.Throws<BusinessException>(() => m.GarantirPodeExcluir());
    }

    // ----- TemPermissaoExtra -----

    [Test]
    public void TemPermissaoExtra_Existe_True()
    {
        var m = CriarValido();
        Assert.That(m.TemPermissaoExtra(PermissoesExtras.AssistenteClinicoIa), Is.True);
    }

    [Test]
    public void TemPermissaoExtra_NaoExiste_False()
    {
        var m = CriarValido();
        Assert.That(m.TemPermissaoExtra(PermissoesExtras.GerirPermissoes), Is.False);
    }

    [Test]
    public void TemPermissaoExtra_Vazio_False()
    {
        var m = CriarValido();
        Assert.That(m.TemPermissaoExtra(" "), Is.False);
    }

    // ----- AdicionarPermissaoExtra / RemoverPermissaoExtra -----

    [Test]
    public void AdicionarPermissaoExtra_NaoExistente_AdicionaELimpa()
    {
        var m = CriarValido();
        m.AdicionarPermissaoExtra(PermissoesExtras.GerirPermissoes);
        Assert.That(m.TemPermissaoExtra(PermissoesExtras.GerirPermissoes), Is.True);
        Assert.That(m.AtualizadoEm, Is.Not.Null);
    }

    [Test]
    public void AdicionarPermissaoExtra_Duplicada_NaoDuplica()
    {
        var m = CriarValido();
        m.AdicionarPermissaoExtra(PermissoesExtras.AssistenteClinicoIa);
        Assert.That(m.PermissoesExtrasLista.Count(p => p == PermissoesExtras.AssistenteClinicoIa),
            Is.EqualTo(1));
    }

    [Test]
    public void AdicionarPermissaoExtra_ModeloPadrao_LancaBusinessException()
    {
        var padrao = CriarValido(ehPadrao: true);
        Assert.Throws<BusinessException>(() =>
            padrao.AdicionarPermissaoExtra(PermissoesExtras.GerirPermissoes));
    }

    [Test]
    public void AdicionarPermissaoExtra_PermissaoVazia_LancaBusinessException()
    {
        var m = CriarValido();
        Assert.Throws<BusinessException>(() => m.AdicionarPermissaoExtra(" "));
    }

    [Test]
    public void RemoverPermissaoExtra_Existente_Remove()
    {
        var m = CriarValido();
        m.RemoverPermissaoExtra(PermissoesExtras.AssistenteClinicoIa);
        Assert.That(m.TemPermissaoExtra(PermissoesExtras.AssistenteClinicoIa), Is.False);
    }

    [Test]
    public void RemoverPermissaoExtra_Inexistente_NaoLanca()
    {
        var m = CriarValido();
        Assert.DoesNotThrow(() => m.RemoverPermissaoExtra(PermissoesExtras.GerirPermissoes));
    }

    [Test]
    public void RemoverPermissaoExtra_ModeloPadrao_LancaBusinessException()
    {
        var padrao = CriarValido(ehPadrao: true);
        Assert.Throws<BusinessException>(() =>
            padrao.RemoverPermissaoExtra(PermissoesExtras.AssistenteClinicoIa));
    }
}
