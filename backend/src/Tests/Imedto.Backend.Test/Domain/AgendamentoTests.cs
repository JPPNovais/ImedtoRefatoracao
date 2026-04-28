using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain;

[TestFixture]
public class AgendamentoTests
{
    private static Agendamento CriarValido()
    {
        var inicio = DateTime.UtcNow.AddHours(2);
        return Agendamento.Criar(
            estabelecimentoId: 1,
            pacienteId: 1,
            profissionalUsuarioId: Guid.NewGuid(),
            criadoPorUsuarioId: Guid.NewGuid(),
            inicioPrevisto: inicio,
            fimPrevisto: inicio.AddHours(1),
            tipoServico: "Consulta",
            observacoes: null);
    }

    [Test]
    public void Criar_Valido_StatusAgendado()
    {
        var a = CriarValido();
        Assert.That(a.Status, Is.EqualTo(AgendamentoStatus.Agendado));
    }

    [Test]
    public void Criar_InicioAposTermino_LancaBusinessException()
    {
        var base_ = DateTime.UtcNow.AddHours(2);
        var ex = Assert.Throws<BusinessException>(() =>
            Agendamento.Criar(1, 1, Guid.NewGuid(), Guid.NewGuid(),
                base_.AddHours(1), base_, "Consulta", null));
        Assert.That(ex.Message, Does.Contain("início deve ser anterior"));
    }

    [Test]
    public void Criar_TipoServicoVazio_LancaBusinessException()
    {
        var inicio = DateTime.UtcNow.AddHours(2);
        var ex = Assert.Throws<BusinessException>(() =>
            Agendamento.Criar(1, 1, Guid.NewGuid(), Guid.NewGuid(),
                inicio, inicio.AddHours(1), "", null));
        Assert.That(ex.Message, Does.Contain("serviço é obrigatório"));
    }

    [Test]
    public void Confirmar_StatusAgendado_MudaParaConfirmado()
    {
        var a = CriarValido();
        a.Confirmar();
        Assert.That(a.Status, Is.EqualTo(AgendamentoStatus.Confirmado));
    }

    [Test]
    public void Confirmar_JaConfirmado_LancaBusinessException()
    {
        var a = CriarValido();
        a.Confirmar();
        var ex = Assert.Throws<BusinessException>(() => a.Confirmar());
        Assert.That(ex.Message, Does.Contain("Agendado"));
    }

    [Test]
    public void Cancelar_StatusAgendado_MudaParaCancelado()
    {
        var a = CriarValido();
        a.Cancelar("Paciente solicitou.");
        Assert.That(a.Status, Is.EqualTo(AgendamentoStatus.Cancelado));
    }

    [Test]
    public void Cancelar_SemMotivo_LancaBusinessException()
    {
        var a = CriarValido();
        var ex = Assert.Throws<BusinessException>(() => a.Cancelar(""));
        Assert.That(ex.Message, Does.Contain("Motivo"));
    }

    [Test]
    public void Cancelar_JaCancelado_LancaBusinessException()
    {
        var a = CriarValido();
        a.Cancelar("Motivo.");
        var ex = Assert.Throws<BusinessException>(() => a.Cancelar("outro"));
        Assert.That(ex.Message, Does.Contain("cancelado"));
    }

    [Test]
    public void Concluir_StatusConfirmado_MudaParaConcluido()
    {
        var a = CriarValido();
        a.Confirmar();
        a.Concluir();
        Assert.That(a.Status, Is.EqualTo(AgendamentoStatus.Concluido));
    }

    [Test]
    public void Concluir_StatusCancelado_LancaBusinessException()
    {
        var a = CriarValido();
        a.Cancelar("Motivo.");
        var ex = Assert.Throws<BusinessException>(() => a.Concluir());
        Assert.That(ex.Message, Does.Contain("cancelado"));
    }

    [Test]
    public void Atualizar_StatusCancelado_LancaBusinessException()
    {
        var a = CriarValido();
        a.Cancelar("Motivo.");
        var novoInicio = DateTime.UtcNow.AddHours(3);
        var ex = Assert.Throws<BusinessException>(() =>
            a.Atualizar(Guid.NewGuid(), novoInicio, novoInicio.AddHours(1), "Consulta", null));
        Assert.That(ex.Message, Does.Contain("cancelado"));
    }

    [Test]
    public void Atualizar_TrocaProfissional_AtualizaCampo()
    {
        var a = CriarValido();
        var novoProfissional = Guid.NewGuid();
        var novoInicio = DateTime.UtcNow.AddHours(3);
        a.Atualizar(novoProfissional, novoInicio, novoInicio.AddHours(1), "Retorno", "obs");
        Assert.That(a.ProfissionalUsuarioId, Is.EqualTo(novoProfissional));
        Assert.That(a.TipoServico, Is.EqualTo("Retorno"));
        Assert.That(a.Observacoes, Is.EqualTo("obs"));
    }

    [Test]
    public void Atualizar_ProfissionalVazio_LancaBusinessException()
    {
        var a = CriarValido();
        var novoInicio = DateTime.UtcNow.AddHours(3);
        var ex = Assert.Throws<BusinessException>(() =>
            a.Atualizar(Guid.Empty, novoInicio, novoInicio.AddHours(1), "Consulta", null));
        Assert.That(ex.Message, Does.Contain("Profissional"));
    }

    [Test]
    public void Atualizar_NovoInicioNoPassado_LancaBusinessException()
    {
        var a = CriarValido();
        var novoInicio = DateTime.UtcNow.AddHours(-2);
        var ex = Assert.Throws<BusinessException>(() =>
            a.Atualizar(Guid.NewGuid(), novoInicio, novoInicio.AddHours(1), "Consulta", null));
        Assert.That(ex.Message, Does.Contain("passado"));
    }

    [Test]
    public void Atualizar_AgendamentoJaOcorreu_LancaBusinessException()
    {
        var a = CriarValido();
        // Burla a factory para simular um agendamento cuja data de início já passou.
        var prop = typeof(Agendamento).GetProperty(nameof(Agendamento.InicioPrevisto))!;
        prop.SetValue(a, DateTime.UtcNow.AddDays(-1));

        var novoInicio = DateTime.UtcNow.AddHours(3);
        var ex = Assert.Throws<BusinessException>(() =>
            a.Atualizar(Guid.NewGuid(), novoInicio, novoInicio.AddHours(1), "Consulta", null));
        Assert.That(ex.Message, Does.Contain("já ocorreu"));
    }
}
