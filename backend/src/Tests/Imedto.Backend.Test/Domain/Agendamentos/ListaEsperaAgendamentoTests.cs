using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Agendamentos;

[TestFixture]
public class ListaEsperaAgendamentoTests
{
    private static ListaEsperaAgendamento CriarValida() =>
        ListaEsperaAgendamento.Criar(
            estabelecimentoId: 1L,
            pacienteId: 100L,
            motivo: " Retorno cardiologico ",
            profissionalPreferidoId: Guid.NewGuid(),
            criadoPorUsuarioId: Guid.NewGuid(),
            prioridade: ListaEsperaPrioridade.Urgente,
            preferenciaPeriodo: ListaEsperaPreferenciaPeriodo.Manha);

    [Test]
    public void Criar_Valida_NormalizaEStateOk()
    {
        var le = CriarValida();
        Assert.That(le.EstabelecimentoId, Is.EqualTo(1L));
        Assert.That(le.PacienteId, Is.EqualTo(100L));
        Assert.That(le.Motivo, Is.EqualTo("Retorno cardiologico"));
        Assert.That(le.Prioridade, Is.EqualTo(ListaEsperaPrioridade.Urgente));
        Assert.That(le.PreferenciaPeriodo, Is.EqualTo(ListaEsperaPreferenciaPeriodo.Manha));
        Assert.That(le.AtendidoEm, Is.Null);
        Assert.That(le.AtendidoPorAgendamentoId, Is.Null);
        Assert.That(le.CriadoEm, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public void Criar_DefaultsRotinaEQualquer()
    {
        var le = ListaEsperaAgendamento.Criar(1L, 100L, "X", null, Guid.NewGuid());
        Assert.That(le.Prioridade, Is.EqualTo(ListaEsperaPrioridade.Rotina));
        Assert.That(le.PreferenciaPeriodo, Is.EqualTo(ListaEsperaPreferenciaPeriodo.Qualquer));
        Assert.That(le.ProfissionalPreferidoId, Is.Null);
    }

    [Test]
    public void Criar_EstabelecimentoZero_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() =>
            ListaEsperaAgendamento.Criar(0L, 100L, "X", null, Guid.NewGuid()));
    }

    [Test]
    public void Criar_PacienteZero_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() =>
            ListaEsperaAgendamento.Criar(1L, 0L, "X", null, Guid.NewGuid()));
    }

    [Test]
    public void Criar_MotivoVazio_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() =>
            ListaEsperaAgendamento.Criar(1L, 100L, " ", null, Guid.NewGuid()));
    }

    // ----- Encaixar -----

    [Test]
    public void Encaixar_Valido_MarcaAtendida()
    {
        var le = CriarValida();
        le.Encaixar(500L);

        Assert.That(le.AtendidoEm, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(5)));
        Assert.That(le.AtendidoPorAgendamentoId, Is.EqualTo(500L));
    }

    [Test]
    public void Encaixar_AgendamentoZero_LancaBusinessException()
    {
        var le = CriarValida();
        Assert.Throws<BusinessException>(() => le.Encaixar(0L));
    }

    [Test]
    public void Encaixar_JaAtendida_LancaBusinessException()
    {
        var le = CriarValida();
        le.Encaixar(500L);
        var ex = Assert.Throws<BusinessException>(() => le.Encaixar(501L));
        Assert.That(ex.Message, Does.Contain("já foi atendida"));
    }
}
