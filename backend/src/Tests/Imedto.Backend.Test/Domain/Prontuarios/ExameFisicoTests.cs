using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Prontuarios;

[TestFixture]
public class ExameFisicoTests
{
    private static readonly Guid ProfissionalId = Guid.NewGuid();

    private static ExameFisico RegistrarValido(IEnumerable<ExameFisico.RegiaoInput>? regioes = null)
        => ExameFisico.Registrar(
            evolucaoId: 1,
            prontuarioId: 1,
            pacienteId: 1,
            estabelecimentoId: 1,
            realizadoPorUsuarioId: ProfissionalId,
            realizadoEm: DateTime.UtcNow.AddMinutes(-5),
            dadosGeraisJson: null,
            observacoesGerais: null,
            regioes: regioes ?? Enumerable.Empty<ExameFisico.RegiaoInput>());

    private static ExameFisico.RegiaoInput CriarRegiao(string codigo)
        => new(codigo, null, Lateralidade.NaoAplicavel, "Normal", SeveridadeExame.Normal, 0);

    [Test]
    public void Registrar_ComTresRegioes_ExameCriadoComRegioes()
    {
        var regioes = new[]
        {
            CriarRegiao("torax"),
            CriarRegiao("abdomen"),
            CriarRegiao("cabeca")
        };

        var exame = RegistrarValido(regioes);

        Assert.That(exame.Regioes, Has.Count.EqualTo(3));
    }

    [Test]
    public void Registrar_SemRegioes_ExameCriadoSemRegioes()
    {
        var exame = RegistrarValido();

        Assert.That(exame.Regioes, Is.Empty);
    }

    [Test]
    public void Registrar_EvolucaoIdZero_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            ExameFisico.Registrar(0, 1, 1, 1, ProfissionalId, null, null, null,
                Enumerable.Empty<ExameFisico.RegiaoInput>()));

        Assert.That(ex.Message, Does.Contain("Evolução é obrigatória"));
    }

    [Test]
    public void Registrar_DataFutura_LancaBusinessException()
    {
        var dataFutura = DateTime.UtcNow.AddHours(1);

        var ex = Assert.Throws<BusinessException>(() =>
            ExameFisico.Registrar(1, 1, 1, 1, ProfissionalId, dataFutura, null, null,
                Enumerable.Empty<ExameFisico.RegiaoInput>()));

        Assert.That(ex.Message, Does.Contain("futura"));
    }

    [Test]
    public void AdicionarRegiao_CodigoNovo_RegiaoAdicionada()
    {
        var exame = RegistrarValido();

        exame.AdicionarRegiao(CriarRegiao("torax"));

        Assert.That(exame.Regioes, Has.Count.EqualTo(1));
        Assert.That(exame.Regioes.First().RegiaoCodigo, Is.EqualTo("torax"));
    }

    [Test]
    public void AdicionarRegiao_CodigoDuplicado_LancaBusinessException()
    {
        var exame = RegistrarValido([CriarRegiao("torax")]);

        var ex = Assert.Throws<BusinessException>(() =>
            exame.AdicionarRegiao(CriarRegiao("torax")));

        Assert.That(ex.Message, Does.Contain("torax"));
    }

    [Test]
    public void AdicionarRegiao_CodigoDuplicadoCaseDiferente_LancaBusinessException()
    {
        var exame = RegistrarValido([CriarRegiao("TORAX")]);

        var ex = Assert.Throws<BusinessException>(() =>
            exame.AdicionarRegiao(CriarRegiao("torax")));

        Assert.That(ex.Message, Does.Contain("torax"));
    }

    [Test]
    public void AtualizarRegiao_RegiaoExistente_AchadosAtualizados()
    {
        var exame = RegistrarValido([CriarRegiao("torax")]);

        exame.AtualizarRegiao("torax", "Diminuição do murmúrio", SeveridadeExame.Alterado, Lateralidade.Esquerda);

        var regiao = exame.Regioes.First(r => r.RegiaoCodigo == "torax");
        Assert.That(regiao.Achados, Is.EqualTo("Diminuição do murmúrio"));
        Assert.That(regiao.Severidade, Is.EqualTo(SeveridadeExame.Alterado));
        Assert.That(regiao.Lateralidade, Is.EqualTo(Lateralidade.Esquerda));
    }

    [Test]
    public void AtualizarRegiao_RegiaoNaoEncontrada_LancaBusinessException()
    {
        var exame = RegistrarValido();

        var ex = Assert.Throws<BusinessException>(() =>
            exame.AtualizarRegiao("abdomen", "Dor", null, Lateralidade.NaoAplicavel));

        Assert.That(ex.Message, Does.Contain("não encontrada"));
    }

    [Test]
    public void RemoverRegiao_RegiaoExistente_RegiaoRemovidaDaColecao()
    {
        var exame = RegistrarValido([CriarRegiao("torax"), CriarRegiao("abdomen")]);

        exame.RemoverRegiao("torax");

        Assert.That(exame.Regioes, Has.Count.EqualTo(1));
        Assert.That(exame.Regioes.First().RegiaoCodigo, Is.EqualTo("abdomen"));
    }

    [Test]
    public void RemoverRegiao_RegiaoNaoEncontrada_LancaBusinessException()
    {
        var exame = RegistrarValido();

        var ex = Assert.Throws<BusinessException>(() =>
            exame.RemoverRegiao("torax"));

        Assert.That(ex.Message, Does.Contain("não encontrada"));
    }

    [Test]
    public void AdicionarRegiao_ExameDeletado_LancaBusinessException()
    {
        var exame = RegistrarValido();
        exame.MarcarComoDeletado(ProfissionalId);

        var ex = Assert.Throws<BusinessException>(() =>
            exame.AdicionarRegiao(CriarRegiao("torax")));

        Assert.That(ex.Message, Does.Contain("deletado"));
    }
}
