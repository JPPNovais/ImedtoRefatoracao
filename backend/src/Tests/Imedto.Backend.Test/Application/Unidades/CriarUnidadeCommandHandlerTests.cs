using Imedto.Backend.Application.Unidades.Commands;
using Imedto.Backend.Contracts.Unidades.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Unidades;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Unidades;

[TestFixture]
public class CriarUnidadeCommandHandlerTests
{
    private Mock<IUnidadeRepository> _unidades;
    private Mock<IEstabelecimentoRepository> _estabRepo;
    private CriarUnidadeCommandHandler _sut;

    private readonly Guid _donoId = Guid.NewGuid();
    private readonly Guid _outroId = Guid.NewGuid();
    private const long EstabelecimentoId = 1;

    [SetUp]
    public void SetUp()
    {
        _unidades = new Mock<IUnidadeRepository>();
        _estabRepo = new Mock<IEstabelecimentoRepository>();
        _sut = new CriarUnidadeCommandHandler(_unidades.Object, _estabRepo.Object);
    }

    private Estabelecimento Estab()
    {
        var e = Estabelecimento.Criar(_donoId, "Clinica", null, null, null, null);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(e, EstabelecimentoId);
        return e;
    }

    private CriarUnidadeCommand Cmd(bool principal = false, Guid? solicitante = null) => new()
    {
        EstabelecimentoId = EstabelecimentoId,
        UsuarioSolicitanteId = solicitante ?? _donoId,
        Nome = "Filial Norte",
        IsPrincipal = principal,
        Cep = "01310100", Logradouro = "Rua A", Numero = "1",
        Complemento = "", Bairro = "Centro", Cidade = "SP", Estado = "SP",
        Telefone = "11999998888",
    };

    [Test]
    public async Task Handle_DonoCriaUnidade_Persiste()
    {
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(Estab());
        _unidades.Setup(r => r.ExisteOutraComMesmoNome(EstabelecimentoId, "Filial Norte", 0))
                 .ReturnsAsync(false);

        await _sut.Handle(Cmd());

        _unidades.Verify(r => r.Salvar(It.IsAny<UnidadeEstabelecimento>()), Times.Once);
    }

    [Test]
    public async Task Handle_NovaPrincipalComOutraJaPrincipal_DesmarcaAnteriorEAplicaNova()
    {
        var anterior = UnidadeEstabelecimento.Criar(EstabelecimentoId, "Matriz", true,
            new EnderecoUnidadeInput("", "", "", "", "", "", ""), null);
        // Setar Id real para Equals do Entity (que compara por Id) distinguir das instancias.
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(anterior, 50L);

        _estabRepo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(Estab());
        _unidades.Setup(r => r.ExisteOutraComMesmoNome(EstabelecimentoId, "Filial Norte", 0))
                 .ReturnsAsync(false);
        _unidades.Setup(r => r.ObterPrincipalDoEstabelecimento(EstabelecimentoId))
                 .ReturnsAsync(anterior);

        await _sut.Handle(Cmd(principal: true));

        Assert.That(anterior.IsPrincipal, Is.False, "Anterior deve perder a flag.");
        _unidades.Verify(r => r.Salvar(anterior), Times.Once);
        _unidades.Verify(r => r.Salvar(It.Is<UnidadeEstabelecimento>(u => u != anterior && u.IsPrincipal)),
            Times.Once);
    }

    [Test]
    public void Handle_NaoEhDono_LancaBusinessException()
    {
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(Estab());

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd(solicitante: _outroId)));
        Assert.That(ex.Message, Does.Contain("dono"));
    }

    [Test]
    public void Handle_NomeDuplicado_LancaBusinessException()
    {
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(Estab());
        _unidades.Setup(r => r.ExisteOutraComMesmoNome(EstabelecimentoId, "Filial Norte", 0))
                 .ReturnsAsync(true);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("nome"));
    }

    [Test]
    public void Handle_EstabelecimentoInexistente_LancaBusinessException()
    {
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync((Estabelecimento)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("Estabelecimento"));
    }
}
