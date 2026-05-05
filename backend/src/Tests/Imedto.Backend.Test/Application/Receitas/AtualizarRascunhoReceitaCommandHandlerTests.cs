using Imedto.Backend.Application.Receitas.Commands;
using Imedto.Backend.Contracts.Receitas.Commands;
using Imedto.Backend.Domain.Receitas;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Receitas;

[TestFixture]
public class AtualizarRascunhoReceitaCommandHandlerTests
{
    private Mock<IReceitaRepository> _receitaRepo;
    private AtualizarRascunhoReceitaCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long ReceitaId = 99;
    private readonly Guid _profissionalId = Guid.NewGuid();
    private readonly Guid _outroProfissional = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _receitaRepo = new Mock<IReceitaRepository>();
        _sut = new AtualizarRascunhoReceitaCommandHandler(_receitaRepo.Object);
    }

    private static Receita Rascunho(long estabId, Guid prof) =>
        Receita.IniciarRascunho(
            prontuarioId: 200L, pacienteId: 100L,
            profissionalUsuarioId: prof, estabelecimentoId: estabId,
            tipo: TipoReceita.Comum, tipoNotificacao: null,
            observacoes: null, validadeAte: null,
            itens: new[] { new Receita.ItemReceitaInput("Med", "Pos", null, null, null) });

    private AtualizarRascunhoReceitaCommand Cmd(Guid? solicitante = null) => new()
    {
        ReceitaId = ReceitaId,
        EstabelecimentoId = EstabelecimentoId,
        SolicitanteUsuarioId = solicitante ?? _profissionalId,
        Observacoes = "Nova obs",
        Itens = new() { new() { Medicamento = "Dipirona", Posologia = "1 cp" } },
    };

    [Test]
    public async Task Handle_ProprioProfissional_AtualizaRascunho()
    {
        var receita = Rascunho(EstabelecimentoId, _profissionalId);
        _receitaRepo.Setup(r => r.ObterPorIdOuNulo(ReceitaId, EstabelecimentoId)).ReturnsAsync(receita);

        await _sut.Handle(Cmd());

        Assert.That(receita.Observacoes, Is.EqualTo("Nova obs"));
        _receitaRepo.Verify(r => r.Salvar(receita), Times.Once);
    }

    [Test]
    public void Handle_DeOutroTenant_LancaBusinessException()
    {
        _receitaRepo.Setup(r => r.ObterPorIdOuNulo(ReceitaId, EstabelecimentoId))
                    .ReturnsAsync((Receita?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não encontrada"));
    }

    [Test]
    public void Handle_OutroProfissional_LancaBusinessException()
    {
        _receitaRepo.Setup(r => r.ObterPorIdOuNulo(ReceitaId, EstabelecimentoId))
                    .ReturnsAsync(Rascunho(EstabelecimentoId, _profissionalId));

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd(solicitante: _outroProfissional)));
        Assert.That(ex.Message, Does.Contain("profissional responsável"));
        _receitaRepo.Verify(r => r.Salvar(It.IsAny<Receita>()), Times.Never);
    }
}
