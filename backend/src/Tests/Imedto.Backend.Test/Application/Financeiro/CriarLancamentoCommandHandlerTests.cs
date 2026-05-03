using Imedto.Backend.Application.Financeiro.Commands;
using Imedto.Backend.Contracts.Financeiro.Commands;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.Domain.Financeiro.Events;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Financeiro;

[TestFixture]
public class CriarLancamentoCommandHandlerTests
{
    private Mock<ILancamentoRepository> _repo;
    private Mock<IEventBus> _events;
    private CriarLancamentoCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private readonly Guid _criadoPorId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<ILancamentoRepository>();
        _events = new Mock<IEventBus>();
        _sut = new CriarLancamentoCommandHandler(_repo.Object, _events.Object);
    }

    private CriarLancamentoCommand Cmd(string tipo = "Receita") => new()
    {
        EstabelecimentoId = EstabelecimentoId,
        Tipo = tipo,
        Descricao = "Consulta",
        Valor = 250m,
        DataVencimento = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
        Categoria = "Atendimento",
        CriadoPorUsuarioId = _criadoPorId,
    };

    [Test]
    public async Task Handle_TipoReceitaValido_PersisteLancamento()
    {
        await _sut.Handle(Cmd("Receita"));
        _repo.Verify(r => r.Salvar(It.Is<Lancamento>(l =>
            l.Tipo == TipoLancamento.Receita && l.Valor == 250m)),
            Times.Once);
    }

    [Test]
    public async Task Handle_TipoDespesaValido_PersisteLancamento()
    {
        await _sut.Handle(Cmd("Despesa"));
        _repo.Verify(r => r.Salvar(It.Is<Lancamento>(l => l.Tipo == TipoLancamento.Despesa)),
            Times.Once);
    }

    [Test]
    public void Handle_TipoInvalido_LancaBusinessException()
    {
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd("Outro")));
        Assert.That(ex.Message, Does.Contain("Tipo"));
        _repo.Verify(r => r.Salvar(It.IsAny<Lancamento>()), Times.Never);
    }
}
