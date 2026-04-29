using Imedto.Backend.Domain.Cirurgias;
using Imedto.Backend.Domain.Cirurgias.Events;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;
using static Imedto.Backend.Domain.Cirurgias.ProcedimentoCirurgico;

namespace Imedto.Backend.Test.Domain.Cirurgias;

[TestFixture]
public class ProcedimentoCirurgicoTests
{
    private static readonly Guid CirurgiaoId = Guid.NewGuid();
    private static readonly Guid AnestesistaId = Guid.NewGuid();

    private static ProcedimentoCirurgico PlanejarComCirurgiao()
        => ProcedimentoCirurgico.Planejar(
            pacienteId: 1,
            prontuarioId: 1,
            estabelecimentoId: 1,
            agendamentoId: null,
            cirurgiaPrincipal: "Colecistectomia laparoscópica",
            cirurgiaCodigo: "51.23",
            dataAgendada: DateTime.UtcNow.AddDays(7),
            equipeInicial: [new EquipeInicialPayload(CirurgiaoId, PapelCirurgia.Cirurgiao)]);

    private static ProcedimentoCirurgico PlanejarSemEquipe()
        => ProcedimentoCirurgico.Planejar(
            pacienteId: 1,
            prontuarioId: 1,
            estabelecimentoId: 1,
            agendamentoId: null,
            cirurgiaPrincipal: "Colecistectomia laparoscópica",
            cirurgiaCodigo: null,
            dataAgendada: null,
            equipeInicial: []);

    [Test]
    public void Planejar_ComCirurgiaPrincipal_CriadoComStatusPlanejado()
    {
        var proc = PlanejarComCirurgiao();

        Assert.That(proc.Status, Is.EqualTo(StatusProcedimento.Planejado));
        Assert.That(proc.CirurgiaPrincipal, Is.EqualTo("Colecistectomia laparoscópica"));
        Assert.That(proc.Equipe, Has.Count.EqualTo(1));
    }

    [Test]
    public void Planejar_SemCirurgiaPrincipal_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            ProcedimentoCirurgico.Planejar(1, 1, 1, null, "  ", null, null, []));

        Assert.That(ex.Message, Does.Contain("Cirurgia principal é obrigatória"));
    }

    [Test]
    public void Planejar_PacienteZero_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            ProcedimentoCirurgico.Planejar(0, 1, 1, null, "Cirurgia X", null, null, []));

        Assert.That(ex.Message, Does.Contain("Paciente é obrigatório"));
    }

    [Test]
    public void Confirmar_ProcedimentoPlanejadoComEquipe_MudaParaConfirmado()
    {
        var proc = PlanejarComCirurgiao();

        proc.Confirmar();

        Assert.That(proc.Status, Is.EqualTo(StatusProcedimento.Confirmado));
    }

    [Test]
    public void Confirmar_ProcedimentoPlanejado_DispararaDomainEvent()
    {
        var proc = PlanejarComCirurgiao();

        proc.Confirmar();

        Assert.That(proc.DomainEvents, Has.Count.EqualTo(1));
        Assert.That(proc.DomainEvents.First(), Is.InstanceOf<ProcedimentoConfirmadoEvent>());
    }

    [Test]
    public void Confirmar_ProcedimentoCancelado_LancaBusinessException()
    {
        var proc = PlanejarComCirurgiao();
        proc.Cancelar("Paciente contraindicado");

        var ex = Assert.Throws<BusinessException>(() => proc.Confirmar());

        Assert.That(ex.Message, Does.Contain("planejados podem ser confirmados"));
    }

    [Test]
    public void Confirmar_SemEquipe_LancaBusinessException()
    {
        var proc = PlanejarSemEquipe();

        var ex = Assert.Throws<BusinessException>(() => proc.Confirmar());

        Assert.That(ex.Message, Does.Contain("Equipe é obrigatória"));
    }

    [Test]
    public void RegistrarRealizacao_SemCirurgiaoNaEquipe_LancaBusinessException()
    {
        var proc = ProcedimentoCirurgico.Planejar(
            1, 1, 1, null, "Cirurgia X", null, null,
            [new EquipeInicialPayload(AnestesistaId, PapelCirurgia.Anestesista)]);

        var ex = Assert.Throws<BusinessException>(() =>
            proc.RegistrarRealizacao(DateTime.UtcNow.AddMinutes(-30), null, null, null));

        Assert.That(ex.Message, Does.Contain("cirurgião"));
    }

    [Test]
    public void RegistrarRealizacao_ComCirurgiao_MudaParaRealizado()
    {
        var proc = PlanejarComCirurgiao();

        proc.RegistrarRealizacao(DateTime.UtcNow.AddMinutes(-30), "Sem intercorrências", null, null);

        Assert.That(proc.Status, Is.EqualTo(StatusProcedimento.Realizado));
        Assert.That(proc.DataRealizada, Is.Not.Null);
    }

    [Test]
    public void RegistrarRealizacao_DataFutura_LancaBusinessException()
    {
        var proc = PlanejarComCirurgiao();

        var ex = Assert.Throws<BusinessException>(() =>
            proc.RegistrarRealizacao(DateTime.UtcNow.AddHours(2), null, null, null));

        Assert.That(ex.Message, Does.Contain("futuro"));
    }

    [Test]
    public void Cancelar_ProcedimentoPlanejado_MudaParaCancelado()
    {
        var proc = PlanejarSemEquipe();

        proc.Cancelar("Paciente recusou");

        Assert.That(proc.Status, Is.EqualTo(StatusProcedimento.Cancelado));
        Assert.That(proc.MotivoCancelamento, Is.EqualTo("Paciente recusou"));
        Assert.That(proc.CanceladoEm, Is.Not.Null);
    }

    [Test]
    public void Cancelar_ProcedimentoJaRealizado_LancaBusinessException()
    {
        var proc = PlanejarComCirurgiao();
        proc.RegistrarRealizacao(DateTime.UtcNow.AddMinutes(-10), null, null, null);

        var ex = Assert.Throws<BusinessException>(() => proc.Cancelar("Tentativa tardia"));

        Assert.That(ex.Message, Does.Contain("já finalizado"));
    }

    [Test]
    public void AdicionarMembroEquipe_MesmoProfissionalMesmoRole_LancaBusinessException()
    {
        var proc = PlanejarComCirurgiao();

        var ex = Assert.Throws<BusinessException>(() =>
            proc.AdicionarMembroEquipe(CirurgiaoId, PapelCirurgia.Cirurgiao));

        Assert.That(ex.Message, Does.Contain("já está nessa função"));
    }

    [Test]
    public void AdicionarMembroEquipe_MesmoProfissionalPapelDiferente_Sucesso()
    {
        var proc = PlanejarComCirurgiao();

        proc.AdicionarMembroEquipe(CirurgiaoId, PapelCirurgia.Auxiliar);

        Assert.That(proc.Equipe, Has.Count.EqualTo(2));
    }
}
