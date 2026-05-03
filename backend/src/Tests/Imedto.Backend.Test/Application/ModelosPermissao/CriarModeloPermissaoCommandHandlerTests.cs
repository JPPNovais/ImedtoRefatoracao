using Imedto.Backend.Application.ModelosPermissao.Commands;
using Imedto.Backend.Contracts.ModelosPermissao.Commands;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.ModelosPermissao;

[TestFixture]
public class CriarModeloPermissaoCommandHandlerTests
{
    private Mock<IModeloPermissaoRepository> _repo;
    private CriarModeloPermissaoCommandHandler _sut;

    private const long EstabelecimentoId = 1;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IModeloPermissaoRepository>();
        _sut = new CriarModeloPermissaoCommandHandler(_repo.Object);
    }

    private CriarModeloPermissaoCommand Cmd(string tipo = "Profissional") => new()
    {
        EstabelecimentoId = EstabelecimentoId,
        Nome = "Coordenacao",
        TipoAcesso = tipo,
        Permissoes = new[] { "agenda", "pacientes" },
    };

    [Test]
    public async Task Handle_TudoValido_PersisteEPropagaIdCriado()
    {
        _repo.Setup(r => r.Salvar(It.IsAny<ModeloPermissaoEstabelecimento>()))
             .Callback<ModeloPermissaoEstabelecimento>(m =>
                 typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(m, 99L))
             .Returns(Task.CompletedTask);

        var cmd = Cmd();
        await _sut.Handle(cmd);

        Assert.That(cmd.ModeloIdCriado, Is.EqualTo(99L));
        _repo.Verify(r => r.Salvar(It.IsAny<ModeloPermissaoEstabelecimento>()), Times.Once);
    }

    [Test]
    public void Handle_TipoAcessoInvalido_LancaBusinessException()
    {
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd(tipo: "OutroTipo")));
        Assert.That(ex.Message, Does.Contain("TipoAcesso inválido"));
        _repo.Verify(r => r.Salvar(It.IsAny<ModeloPermissaoEstabelecimento>()), Times.Never);
    }
}
