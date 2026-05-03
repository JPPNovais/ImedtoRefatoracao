using Imedto.Backend.Application.Prontuarios.Commands;
using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Prontuarios;

[TestFixture]
public class AdicionarVariavelPoolCommandHandlerTests
{
    private Mock<IProntuarioVariavelPoolRepository> _repo;
    private AdicionarVariavelPoolCommandHandler _sut;

    private const long EstabelecimentoId = 1;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IProntuarioVariavelPoolRepository>();
        _sut = new AdicionarVariavelPoolCommandHandler(_repo.Object);
    }

    private AdicionarVariavelPoolCommand Cmd(string tipo = "Alergia") => new()
    {
        EstabelecimentoId = EstabelecimentoId,
        Tipo = tipo,
        Nome = "Penicilina",
    };

    [Test]
    public async Task Handle_TipoValidoESemConflito_PersisteItem()
    {
        _repo.Setup(r => r.ExisteOutraComMesmoNome(EstabelecimentoId, TipoVariavelPool.Alergia, "Penicilina", 0))
             .ReturnsAsync(false);

        await _sut.Handle(Cmd());

        _repo.Verify(r => r.Salvar(It.Is<ProntuarioVariavelPool>(i =>
            i.Tipo == TipoVariavelPool.Alergia && i.Nome == "Penicilina")),
            Times.Once);
    }

    [Test]
    public void Handle_TipoInvalido_LancaBusinessException()
    {
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd("Outro")));
        Assert.That(ex.Message, Does.Contain("Tipo"));
    }

    [Test]
    public void Handle_NomeDuplicado_LancaBusinessException()
    {
        _repo.Setup(r => r.ExisteOutraComMesmoNome(EstabelecimentoId, TipoVariavelPool.Alergia, "Penicilina", 0))
             .ReturnsAsync(true);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("nome"));
    }
}
