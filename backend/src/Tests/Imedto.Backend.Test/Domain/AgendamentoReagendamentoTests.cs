using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.Agendamentos.Events;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain;

/// <summary>
/// Testes de domínio para as regras R1-R7 de reagendamento:
/// reset Confirmado→Agendado, zerar lembrete, disparo de evento e bloqueios.
/// Cada teste mapeia para um CA do briefing 2026-06-02_001.
/// </summary>
[TestFixture]
public class AgendamentoReagendamentoTests
{
    private static readonly Guid ProfissionalA = Guid.NewGuid();
    private static readonly Guid ProfissionalB = Guid.NewGuid();

    private static Agendamento CriarConfirmado(Guid profissionalId)
    {
        var inicio = DateTime.UtcNow.AddHours(2);
        var a = Agendamento.Criar(
            estabelecimentoId: 1,
            pacienteId: 10,
            profissionalUsuarioId: profissionalId,
            criadoPorUsuarioId: Guid.NewGuid(),
            inicioPrevisto: inicio,
            fimPrevisto: inicio.AddHours(1),
            tipoServico: "Consulta",
            observacoes: null);
        a.Confirmar();
        return a;
    }

    private static Agendamento CriarAgendado(Guid profissionalId)
    {
        var inicio = DateTime.UtcNow.AddHours(2);
        return Agendamento.Criar(
            estabelecimentoId: 1,
            pacienteId: 10,
            profissionalUsuarioId: profissionalId,
            criadoPorUsuarioId: Guid.NewGuid(),
            inicioPrevisto: inicio,
            fimPrevisto: inicio.AddHours(1),
            tipoServico: "Consulta",
            observacoes: null);
    }

    // ── CA1: reset por horário ──────────────────────────────────────────────

    [Test]
    public void Atualizar_ConfirmadoMudaInicio_StatusVoltaAgendado()
    {
        // CA1: Confirmado + mudança de InicioPrevisto → Status volta a Agendado.
        var a = CriarConfirmado(ProfissionalA);
        var novoInicio = DateTime.UtcNow.AddHours(4);

        a.Atualizar(ProfissionalA, novoInicio, novoInicio.AddHours(1), "Consulta", null);

        Assert.That(a.Status, Is.EqualTo(AgendamentoStatus.Agendado));
    }

    [Test]
    public void Atualizar_ConfirmadoMudaInicio_LembretePorEmailViraFalse()
    {
        // CA1/CA15: reset zera LembretePorEmailEnviado.
        var a = CriarConfirmado(ProfissionalA);
        a.MarcarLembretePorEmailEnviado();
        var novoInicio = DateTime.UtcNow.AddHours(4);

        a.Atualizar(ProfissionalA, novoInicio, novoInicio.AddHours(1), "Consulta", null);

        Assert.That(a.LembretePorEmailEnviado, Is.False);
    }

    [Test]
    public void Atualizar_ConfirmadoMudaInicio_AnexaAgendamentoReagendadoEvent()
    {
        // CA1: domínio anexa AgendamentoReagendadoEvent ao resetar.
        var a = CriarConfirmado(ProfissionalA);
        var novoInicio = DateTime.UtcNow.AddHours(4);

        a.Atualizar(ProfissionalA, novoInicio, novoInicio.AddHours(1), "Consulta", null);

        Assert.That(a.DomainEvents.OfType<AgendamentoReagendadoEvent>().Count(), Is.EqualTo(1));
    }

    // ── CA2: reset por profissional ─────────────────────────────────────────

    [Test]
    public void Atualizar_ConfirmadoMudaProfissional_StatusVoltaAgendado()
    {
        // CA2: Confirmado + mudança de ProfissionalUsuarioId → Status volta a Agendado.
        var a = CriarConfirmado(ProfissionalA);
        var inicio = a.InicioPrevisto; // mantém horário

        a.Atualizar(ProfissionalB, inicio, inicio.AddHours(1), "Consulta", null);

        Assert.That(a.Status, Is.EqualTo(AgendamentoStatus.Agendado));
    }

    [Test]
    public void Atualizar_ConfirmadoMudaProfissional_AnexaEvento()
    {
        // CA2: evento disparado ao mudar profissional.
        var a = CriarConfirmado(ProfissionalA);
        var inicio = a.InicioPrevisto;

        a.Atualizar(ProfissionalB, inicio, inicio.AddHours(1), "Consulta", null);

        Assert.That(a.DomainEvents.OfType<AgendamentoReagendadoEvent>().Count(), Is.EqualTo(1));
    }

    // ── CA3: editar sem reset ───────────────────────────────────────────────

    [Test]
    public void Atualizar_ConfirmadoMudaSoObservacoes_StatusPermaneceConfirmado()
    {
        // CA3: mudar só Observacoes/TipoServico com mesmo horário/profissional → sem reset.
        var a = CriarConfirmado(ProfissionalA);
        var inicio = a.InicioPrevisto;

        a.Atualizar(ProfissionalA, inicio, inicio.AddHours(1), "Retorno", "nova obs");

        Assert.That(a.Status, Is.EqualTo(AgendamentoStatus.Confirmado));
    }

    [Test]
    public void Atualizar_ConfirmadoMudaSoObservacoes_LembreteNaoZerado()
    {
        // CA3: lembrete permanece inalterado quando não há mudança de horário/profissional.
        var a = CriarConfirmado(ProfissionalA);
        a.MarcarLembretePorEmailEnviado();
        var inicio = a.InicioPrevisto;

        a.Atualizar(ProfissionalA, inicio, inicio.AddHours(1), "Retorno", "nova obs");

        Assert.That(a.LembretePorEmailEnviado, Is.True);
    }

    [Test]
    public void Atualizar_ConfirmadoMudaSoObservacoes_NenhumEventoAnexado()
    {
        // CA3: sem evento quando não há mudança de horário/profissional.
        var a = CriarConfirmado(ProfissionalA);
        var inicio = a.InicioPrevisto;

        a.Atualizar(ProfissionalA, inicio, inicio.AddHours(1), "Retorno", "nova obs");

        Assert.That(a.DomainEvents.OfType<AgendamentoReagendadoEvent>(), Is.Empty);
    }

    // ── CA4: origem Agendado — informativo, sem reset ───────────────────────

    [Test]
    public void Atualizar_AgendadoMudaHorario_StatusPermaneceAgendado()
    {
        // CA4/R5: Agendado + mudança de horário → status NÃO muda (já pendente).
        var a = CriarAgendado(ProfissionalA);
        var novoInicio = DateTime.UtcNow.AddHours(5);

        a.Atualizar(ProfissionalA, novoInicio, novoInicio.AddHours(1), "Consulta", null);

        Assert.That(a.Status, Is.EqualTo(AgendamentoStatus.Agendado));
    }

    [Test]
    public void Atualizar_AgendadoMudaHorario_LembretePorEmailViraFalse()
    {
        // CA4/R6: Agendado + mudança de horário → zerar lembrete.
        var a = CriarAgendado(ProfissionalA);
        a.MarcarLembretePorEmailEnviado();
        var novoInicio = DateTime.UtcNow.AddHours(5);

        a.Atualizar(ProfissionalA, novoInicio, novoInicio.AddHours(1), "Consulta", null);

        Assert.That(a.LembretePorEmailEnviado, Is.False);
    }

    [Test]
    public void Atualizar_AgendadoMudaHorario_AnexaAgendamentoReagendadoEvent()
    {
        // CA4/R5: mesmo sendo Agendado, mudança de horário dispara evento informativo.
        var a = CriarAgendado(ProfissionalA);
        var novoInicio = DateTime.UtcNow.AddHours(5);

        a.Atualizar(ProfissionalA, novoInicio, novoInicio.AddHours(1), "Consulta", null);

        Assert.That(a.DomainEvents.OfType<AgendamentoReagendadoEvent>().Count(), Is.EqualTo(1));
    }

    [Test]
    public void Atualizar_AgendadoMudaSoObservacoes_LembreteNaoZerado()
    {
        // R6: Agendado + mudança só de obs/tipo → lembrete NÃO zera.
        var a = CriarAgendado(ProfissionalA);
        a.MarcarLembretePorEmailEnviado();
        var inicio = a.InicioPrevisto;

        a.Atualizar(ProfissionalA, inicio, inicio.AddHours(1), "Retorno", "nova obs");

        Assert.That(a.LembretePorEmailEnviado, Is.True);
    }

    // ── CA6: reagendar várias vezes ─────────────────────────────────────────

    [Test]
    public void Atualizar_ConfirmadoReagendadoReconfirmadoReagendado_StatusAgendadoNaUltimaVez()
    {
        // CA6: N iterações de reset+reconfirmação não corrompem estado.
        var a = CriarConfirmado(ProfissionalA);
        var inicio1 = DateTime.UtcNow.AddHours(4);

        // 1ª remarcação → volta a Agendado
        a.Atualizar(ProfissionalA, inicio1, inicio1.AddHours(1), "Consulta", null);
        Assert.That(a.Status, Is.EqualTo(AgendamentoStatus.Agendado));

        // Reconfirmação manual
        a.Confirmar();
        Assert.That(a.Status, Is.EqualTo(AgendamentoStatus.Confirmado));

        // 2ª remarcação → volta a Agendado novamente
        var inicio2 = DateTime.UtcNow.AddHours(6);
        a.Atualizar(ProfissionalA, inicio2, inicio2.AddHours(1), "Consulta", null);
        Assert.That(a.Status, Is.EqualTo(AgendamentoStatus.Agendado));
    }

    // ── CA7: bloqueio Cancelado/Concluído ───────────────────────────────────

    [Test]
    public void Atualizar_StatusCancelado_LancaBusinessException()
    {
        // CA7: Cancelado → 422.
        var a = CriarAgendado(ProfissionalA);
        a.Cancelar("Motivo.");
        var novoInicio = DateTime.UtcNow.AddHours(3);

        var ex = Assert.Throws<BusinessException>(() =>
            a.Atualizar(ProfissionalA, novoInicio, novoInicio.AddHours(1), "Consulta", null));

        Assert.That(ex.Message, Does.Contain("cancelado"));
    }

    [Test]
    public void Atualizar_StatusConcluido_LancaBusinessException()
    {
        // CA7: Concluído → 422.
        var a = CriarAgendado(ProfissionalA);
        a.Concluir();
        var novoInicio = DateTime.UtcNow.AddHours(3);

        var ex = Assert.Throws<BusinessException>(() =>
            a.Atualizar(ProfissionalA, novoInicio, novoInicio.AddHours(1), "Consulta", null));

        Assert.That(ex.Message, Does.Contain("concluído"));
    }

    // ── CA15: lembrete redispara no novo horário ────────────────────────────

    [Test]
    public void Atualizar_LembreteJaEnviado_ZeraAoReagendar()
    {
        // CA15: lembrete_por_email_enviado=true → vira false ao reagendar.
        var a = CriarConfirmado(ProfissionalA);
        a.MarcarLembretePorEmailEnviado();
        Assert.That(a.LembretePorEmailEnviado, Is.True); // pré-condição

        var novoInicio = DateTime.UtcNow.AddHours(4);
        a.Atualizar(ProfissionalA, novoInicio, novoInicio.AddHours(1), "Consulta", null);

        Assert.That(a.LembretePorEmailEnviado, Is.False);
    }

    // ── Evento — conteúdo SEM PII ───────────────────────────────────────────

    [Test]
    public void Atualizar_EventoAnexado_ContemApenasIdsENovoHorario()
    {
        // CA4/CA10: o evento não carrega dados pessoais — só IDs e novo InicioPrevisto.
        var a = CriarConfirmado(ProfissionalA);
        var novoInicio = DateTime.UtcNow.AddHours(4);

        a.Atualizar(ProfissionalA, novoInicio, novoInicio.AddHours(1), "Consulta", null);

        var ev = a.DomainEvents.OfType<AgendamentoReagendadoEvent>().Single();
        // IDs presentes
        Assert.That(ev.EstabelecimentoId, Is.EqualTo(1));
        Assert.That(ev.PacienteId, Is.EqualTo(10));
        Assert.That(ev.ProfissionalUsuarioId, Is.EqualTo(ProfissionalA));
        Assert.That(ev.NovoInicioPrevisto, Is.EqualTo(novoInicio));
        // Sem PII: record não tem campos de CPF/nome/telefone
        var tipo = typeof(AgendamentoReagendadoEvent);
        Assert.That(tipo.GetProperty("Cpf"), Is.Null);
        Assert.That(tipo.GetProperty("Nome"), Is.Null);
        Assert.That(tipo.GetProperty("Email"), Is.Null);
    }
}
