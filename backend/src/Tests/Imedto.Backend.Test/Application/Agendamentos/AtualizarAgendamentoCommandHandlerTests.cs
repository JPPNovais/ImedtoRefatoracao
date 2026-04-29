using Imedto.Backend.Application.Agendamentos.Commands;
using Imedto.Backend.Contracts.Agendamentos.Commands;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Agendamentos;

[TestFixture]
public class AtualizarAgendamentoCommandHandlerTests
{
    private Mock<IAgendamentoRepository> _agendamentoRepo;
    private Mock<IVinculoRepository> _vinculoRepo;
    private AtualizarAgendamentoCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long AgendamentoId = 42;

    [SetUp]
    public void SetUp()
    {
        _agendamentoRepo = new Mock<IAgendamentoRepository>();
        _vinculoRepo = new Mock<IVinculoRepository>();
        _sut = new AtualizarAgendamentoCommandHandler(_agendamentoRepo.Object, _vinculoRepo.Object);
    }

    private Agendamento CriarAgendamentoExistente(Guid profissionalId)
    {
        var inicio = DateTime.UtcNow.AddHours(2);
        var agendamento = Agendamento.Criar(
            estabelecimentoId: EstabelecimentoId,
            pacienteId: 1,
            profissionalUsuarioId: profissionalId,
            criadoPorUsuarioId: Guid.NewGuid(),
            inicioPrevisto: inicio,
            fimPrevisto: inicio.AddHours(1),
            tipoServico: "Consulta",
            observacoes: null);

        // Força Id != 0 via reflection para simular entidade persistida
        typeof(Agendamento)
            .GetProperty(nameof(Agendamento.Id),
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!
            .SetValue(agendamento, AgendamentoId);

        return agendamento;
    }

    [Test]
    public async Task Handle_MesmoHorario_NaoDisparaConflito()
    {
        // Arrange
        var profissionalId = Guid.NewGuid();
        var inicio = DateTime.UtcNow.AddHours(3);
        var fim = inicio.AddHours(1);
        var agendamento = CriarAgendamentoExistente(profissionalId);

        _agendamentoRepo
            .Setup(r => r.ObterPorId(AgendamentoId))
            .ReturnsAsync(agendamento);

        // ExisteConflito retorna false quando recebe excluirAgendamentoId == AgendamentoId
        _agendamentoRepo
            .Setup(r => r.ExisteConflito(
                profissionalId, inicio, fim,
                excluirAgendamentoId: AgendamentoId))
            .ReturnsAsync(false);

        var cmd = new AtualizarAgendamentoCommand
        {
            AgendamentoId = AgendamentoId,
            EstabelecimentoId = EstabelecimentoId,
            ProfissionalUsuarioId = profissionalId,
            InicioPrevisto = inicio,
            FimPrevisto = fim,
            TipoServico = "Consulta"
        };

        // Act + Assert — não deve lançar
        Assert.DoesNotThrowAsync(() => _sut.Handle(cmd));

        _agendamentoRepo.Verify(r => r.Salvar(agendamento), Times.Once);
    }

    [Test]
    public async Task Handle_HorarioOcupado_LancaBusinessException()
    {
        // Arrange
        var profissionalId = Guid.NewGuid();
        var inicio = DateTime.UtcNow.AddHours(3);
        var fim = inicio.AddHours(1);
        var agendamento = CriarAgendamentoExistente(profissionalId);

        _agendamentoRepo
            .Setup(r => r.ObterPorId(AgendamentoId))
            .ReturnsAsync(agendamento);

        // ExisteConflito retorna true — há outro agendamento no horário
        _agendamentoRepo
            .Setup(r => r.ExisteConflito(
                profissionalId, inicio, fim,
                excluirAgendamentoId: AgendamentoId))
            .ReturnsAsync(true);

        var cmd = new AtualizarAgendamentoCommand
        {
            AgendamentoId = AgendamentoId,
            EstabelecimentoId = EstabelecimentoId,
            ProfissionalUsuarioId = profissionalId,
            InicioPrevisto = inicio,
            FimPrevisto = fim,
            TipoServico = "Consulta"
        };

        // Act + Assert
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));

        Assert.That(ex.Message, Does.Contain("horário").IgnoreCase);
    }

    [Test]
    public async Task Handle_HorarioOcupado_PassaExcluirAgendamentoIdCorretoParaConflito()
    {
        // Arrange
        var profissionalId = Guid.NewGuid();
        var inicio = DateTime.UtcNow.AddHours(3);
        var fim = inicio.AddHours(1);
        var agendamento = CriarAgendamentoExistente(profissionalId);

        _agendamentoRepo
            .Setup(r => r.ObterPorId(AgendamentoId))
            .ReturnsAsync(agendamento);

        _agendamentoRepo
            .Setup(r => r.ExisteConflito(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<long?>()))
            .ReturnsAsync(false);

        var cmd = new AtualizarAgendamentoCommand
        {
            AgendamentoId = AgendamentoId,
            EstabelecimentoId = EstabelecimentoId,
            ProfissionalUsuarioId = profissionalId,
            InicioPrevisto = inicio,
            FimPrevisto = fim,
            TipoServico = "Consulta"
        };

        // Act
        await _sut.Handle(cmd);

        // Assert — verifica que excluirAgendamentoId == cmd.AgendamentoId foi repassado
        _agendamentoRepo.Verify(r => r.ExisteConflito(
            profissionalId,
            inicio,
            fim,
            excluirAgendamentoId: AgendamentoId), Times.Once);
    }
}
