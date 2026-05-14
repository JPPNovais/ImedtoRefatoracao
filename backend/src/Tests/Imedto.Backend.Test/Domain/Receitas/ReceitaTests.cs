using Imedto.Backend.Domain.Receitas;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Receitas;

[TestFixture]
public class ReceitaTests
{
    private static IEnumerable<(string, string, string?, ViaAdministracao?, string?)> UmItem()
        => [("Dipirona 500mg", "1 comprimido a cada 8h", null, null, null)];

    [Test]
    public void Emitir_ComItemValido_RetornaReceitaComStatusEmitida()
    {
        var receita = Receita.Emitir(1, 1, Guid.NewGuid(), 1, TipoReceita.Comum, null, null, UmItem());

        Assert.That(receita.Status, Is.EqualTo(StatusReceita.Emitida));
        Assert.That(receita.Itens, Has.Count.EqualTo(1));
    }

    /// <summary>
    /// Bug #3 — toda receita emitida pelo Imedto hoje nasce como
    /// <see cref="StatusAssinaturaDigital.NaoAssinada"/>. Front exibe banner
    /// avisando que precisa imprimir + assinar manualmente. Regulatorio CFM 2.299/2021.
    /// </summary>
    [Test]
    public void Emitir_Default_AssinaturaDigitalStatusEhNaoAssinada()
    {
        var receita = Receita.Emitir(1, 1, Guid.NewGuid(), 1, TipoReceita.Comum, null, null, UmItem());

        Assert.That(receita.AssinaturaDigitalStatus, Is.EqualTo(StatusAssinaturaDigital.NaoAssinada));
    }

    [Test]
    public void IniciarRascunho_Default_AssinaturaDigitalStatusEhNaoAssinada()
    {
        var rascunho = Receita.IniciarRascunho(
            prontuarioId: 1, pacienteId: 1, profissionalUsuarioId: Guid.NewGuid(),
            estabelecimentoId: 1, tipo: TipoReceita.Comum, tipoNotificacao: null,
            observacoes: null, validadeAte: null, itens: null);

        Assert.That(rascunho.AssinaturaDigitalStatus, Is.EqualTo(StatusAssinaturaDigital.NaoAssinada));
    }

    [Test]
    public void Emitir_ComMultiplosItens_OrdemSequencialCorreta()
    {
        var itens = new[]
        {
            ("Dipirona 500mg", "a cada 8h", (string?)null, (ViaAdministracao?)null, (string?)null),
            ("Ibuprofeno 400mg", "a cada 12h", (string?)null, (ViaAdministracao?)null, (string?)null),
            ("Omeprazol 20mg", "1x ao dia", (string?)null, (ViaAdministracao?)null, (string?)null),
        };

        var receita = Receita.Emitir(1, 1, Guid.NewGuid(), 1, TipoReceita.Comum, null, null, itens);

        Assert.That(receita.Itens[0].Ordem, Is.EqualTo(0));
        Assert.That(receita.Itens[1].Ordem, Is.EqualTo(1));
        Assert.That(receita.Itens[2].Ordem, Is.EqualTo(2));
    }

    [Test]
    public void Emitir_SemItens_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Receita.Emitir(1, 1, Guid.NewGuid(), 1, TipoReceita.Comum, null, null,
                Array.Empty<(string, string, string?, ViaAdministracao?, string?)>()));

        Assert.That(ex.Message, Does.Contain("ao menos um medicamento"));
    }

    [Test]
    public void Emitir_Controlada_SemValidadeAte_LancaBusinessException()
    {
        // Notificação Especial (controle especial / Lista C5): prazo livre — caller
        // precisa informar a validade. Para A/B/C o aggregate auto-calcula via ANVISA.
        var ex = Assert.Throws<BusinessException>(() =>
            Receita.Emitir(1, 1, Guid.NewGuid(), 1, TipoReceita.Controlada, null, null, UmItem(), TipoNotificacao.Especial));

        Assert.That(ex.Message, Does.Contain("validade"));
    }

    [Test]
    public void Emitir_Controlada_ComValidadePassada_LancaBusinessException()
    {
        var validadePassada = DateTime.UtcNow.AddDays(-1);

        var ex = Assert.Throws<BusinessException>(() =>
            Receita.Emitir(1, 1, Guid.NewGuid(), 1, TipoReceita.Controlada, null, validadePassada, UmItem(), TipoNotificacao.B));

        Assert.That(ex.Message, Does.Contain("futura"));
    }

    [Test]
    public void Emitir_Controlada_ComValidadeFutura_Sucesso()
    {
        var validadeFutura = DateTime.UtcNow.AddDays(30);

        var receita = Receita.Emitir(1, 1, Guid.NewGuid(), 1, TipoReceita.Controlada, null, validadeFutura, UmItem(), TipoNotificacao.B);

        Assert.That(receita.Status, Is.EqualTo(StatusReceita.Emitida));
        Assert.That(receita.ValidadeAte, Is.EqualTo(validadeFutura));
        Assert.That(receita.TipoNotificacao, Is.EqualTo(TipoNotificacao.B));
    }

    [Test]
    public void Cancelar_ReceitaEmitida_MudaStatusERegistraMotivo()
    {
        var receita = Receita.Emitir(1, 1, Guid.NewGuid(), 1, TipoReceita.Comum, null, null, UmItem());

        receita.Cancelar("Medicamento errado");

        Assert.That(receita.Status, Is.EqualTo(StatusReceita.Cancelada));
        Assert.That(receita.MotivoCancelamento, Is.EqualTo("Medicamento errado"));
        Assert.That(receita.CanceladaEm, Is.Not.Null);
    }

    [Test]
    public void Cancelar_ReceitaJaCancelada_LancaBusinessException()
    {
        var receita = Receita.Emitir(1, 1, Guid.NewGuid(), 1, TipoReceita.Comum, null, null, UmItem());
        receita.Cancelar("Erro");

        var ex = Assert.Throws<BusinessException>(() => receita.Cancelar("Segundo cancelamento"));

        Assert.That(ex.Message, Does.Contain("emitidas podem ser canceladas"));
    }

    [Test]
    public void Cancelar_SemMotivo_LancaBusinessException()
    {
        var receita = Receita.Emitir(1, 1, Guid.NewGuid(), 1, TipoReceita.Comum, null, null, UmItem());

        var ex = Assert.Throws<BusinessException>(() => receita.Cancelar("   "));

        Assert.That(ex.Message, Does.Contain("Motivo"));
    }

    [Test]
    public void MarcarComoDeletado_PrimeiraVez_RegistraDeletadoEm()
    {
        var receita = Receita.Emitir(1, 1, Guid.NewGuid(), 1, TipoReceita.Comum, null, null, UmItem());
        var usuarioId = Guid.NewGuid();

        receita.MarcarComoDeletado(usuarioId);

        Assert.That(receita.DeletadoEm, Is.Not.Null);
        Assert.That(receita.DeletadoPorUsuarioId, Is.EqualTo(usuarioId));
    }

    [Test]
    public void MarcarComoDeletado_JaDeletada_LancaBusinessException()
    {
        var receita = Receita.Emitir(1, 1, Guid.NewGuid(), 1, TipoReceita.Comum, null, null, UmItem());
        var usuarioId = Guid.NewGuid();
        receita.MarcarComoDeletado(usuarioId);

        var ex = Assert.Throws<BusinessException>(() => receita.MarcarComoDeletado(usuarioId));

        Assert.That(ex.Message, Does.Contain("já está deletada"));
    }

    [Test]
    public void Emitir_ProntuarioInvalido_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Receita.Emitir(0, 1, Guid.NewGuid(), 1, TipoReceita.Comum, null, null, UmItem()));

        Assert.That(ex.Message, Does.Contain("Prontuário"));
    }

    [Test]
    public void Emitir_ProfissionalVazio_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Receita.Emitir(1, 1, Guid.Empty, 1, TipoReceita.Comum, null, null, UmItem()));

        Assert.That(ex.Message, Does.Contain("Profissional"));
    }
}
