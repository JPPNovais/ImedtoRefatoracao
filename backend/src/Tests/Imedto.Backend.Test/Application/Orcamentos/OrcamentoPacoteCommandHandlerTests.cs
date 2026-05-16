using Imedto.Backend.Application.Orcamentos.Catalogos;
using Imedto.Backend.Contracts.Orcamentos.Catalogos.Commands;
using Imedto.Backend.Domain.Orcamentos.Catalogos;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Orcamentos;

[TestFixture]
public class OrcamentoPacoteCommandHandlerTests
{
    private const long EstabA = 1;

    [Test]
    public async Task Criar_ProcedimentoDeOutroEstab_LancaBusinessException()
    {
        var pacoteRepo = new Mock<IOrcamentoPacoteRepository>();
        var cirRepo = new Mock<ICatalogoCirurgiaRepository>();
        var prodRepo = new Mock<ICatalogoProdutoRepository>();
        var teamRepo = new Mock<IOrcamentoTeamRoleRepository>();
        var anestRepo = new Mock<IOrcamentoAnestesistaRepository>();

        cirRepo.Setup(r => r.ObterPorIdOuNulo(99, EstabA)).ReturnsAsync((CatalogoCirurgia?)null);

        var sut = new CriarOrcamentoPacoteCommandHandler(
            pacoteRepo.Object, cirRepo.Object, prodRepo.Object, teamRepo.Object, anestRepo.Object);

        var ex = Assert.ThrowsAsync<BusinessException>(async () =>
            await sut.Handle(new CriarOrcamentoPacoteCommand
            {
                EstabelecimentoId = EstabA,
                Nome = "Pacote X",
                ProcedimentoIds = new List<long> { 99 }
            }));

        Assert.That(ex!.Message, Is.EqualTo("Procedimento não encontrado."));
        pacoteRepo.Verify(r => r.Salvar(It.IsAny<OrcamentoPacote>()), Times.Never);
        await Task.CompletedTask;
    }

    [Test]
    public async Task Criar_AnestesistaDeOutroEstab_LancaBusinessException()
    {
        var pacoteRepo = new Mock<IOrcamentoPacoteRepository>();
        var cirRepo = new Mock<ICatalogoCirurgiaRepository>();
        var prodRepo = new Mock<ICatalogoProdutoRepository>();
        var teamRepo = new Mock<IOrcamentoTeamRoleRepository>();
        var anestRepo = new Mock<IOrcamentoAnestesistaRepository>();

        anestRepo.Setup(r => r.ObterPorIdOuNulo(77, EstabA)).ReturnsAsync((OrcamentoAnestesista?)null);

        var sut = new CriarOrcamentoPacoteCommandHandler(
            pacoteRepo.Object, cirRepo.Object, prodRepo.Object, teamRepo.Object, anestRepo.Object);

        var ex = Assert.ThrowsAsync<BusinessException>(async () =>
            await sut.Handle(new CriarOrcamentoPacoteCommand
            {
                EstabelecimentoId = EstabA, Nome = "Pacote X", AnestesistaId = 77
            }));
        Assert.That(ex!.Message, Is.EqualTo("Anestesista não encontrado."));
        await Task.CompletedTask;
    }

    [Test]
    public async Task Remover_PacoteDeOutroEstab_LancaBusinessException()
    {
        var pacoteRepo = new Mock<IOrcamentoPacoteRepository>();
        pacoteRepo.Setup(r => r.ObterPorIdOuNulo(50, EstabA)).ReturnsAsync((OrcamentoPacote?)null);
        var sut = new RemoverOrcamentoPacoteCommandHandler(pacoteRepo.Object);
        var ex = Assert.ThrowsAsync<BusinessException>(async () =>
            await sut.Handle(new RemoverOrcamentoPacoteCommand { Id = 50, EstabelecimentoId = EstabA }));
        Assert.That(ex!.Message, Is.EqualTo("Pacote não encontrado."));
        await Task.CompletedTask;
    }

    [Test]
    public async Task RemoverProcedimento_ComPacoteAtivoReferenciando_LancaBusinessException()
    {
        var cirRepo = new Mock<ICatalogoCirurgiaRepository>();
        var pacoteRepo = new Mock<IOrcamentoPacoteRepository>();
        var cirurgia = CatalogoCirurgia.Criar(EstabA, "Colecistectomia", 4800m, 90);
        cirRepo.Setup(r => r.ObterPorIdOuNulo(10, EstabA)).ReturnsAsync(cirurgia);
        pacoteRepo.Setup(r => r.ExistePacoteAtivoComProcedimento(10, EstabA)).ReturnsAsync(true);

        var sut = new RemoverCatalogoCirurgiaCommandHandler(cirRepo.Object, pacoteRepo.Object);
        var ex = Assert.ThrowsAsync<BusinessException>(async () =>
            await sut.Handle(new RemoverCatalogoCirurgiaCommand { Id = 10, EstabelecimentoId = EstabA }));
        Assert.That(ex!.Message, Does.Contain("pacote"));
        cirRepo.Verify(r => r.Salvar(It.IsAny<CatalogoCirurgia>()), Times.Never);
        await Task.CompletedTask;
    }

    [Test]
    public async Task RemoverProcedimento_SemPacoteReferenciando_Inativa()
    {
        var cirRepo = new Mock<ICatalogoCirurgiaRepository>();
        var pacoteRepo = new Mock<IOrcamentoPacoteRepository>();
        var cirurgia = CatalogoCirurgia.Criar(EstabA, "Apendicectomia", 3800m, 60);
        cirRepo.Setup(r => r.ObterPorIdOuNulo(20, EstabA)).ReturnsAsync(cirurgia);
        pacoteRepo.Setup(r => r.ExistePacoteAtivoComProcedimento(20, EstabA)).ReturnsAsync(false);

        var sut = new RemoverCatalogoCirurgiaCommandHandler(cirRepo.Object, pacoteRepo.Object);
        await sut.Handle(new RemoverCatalogoCirurgiaCommand { Id = 20, EstabelecimentoId = EstabA });
        Assert.That(cirurgia.Ativo, Is.False);
        cirRepo.Verify(r => r.Salvar(cirurgia), Times.Once);
    }
}
