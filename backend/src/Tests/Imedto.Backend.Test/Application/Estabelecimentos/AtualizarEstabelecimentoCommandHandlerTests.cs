using Imedto.Backend.Application.Estabelecimentos.Commands;
using Imedto.Backend.Contracts.Estabelecimentos.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Estabelecimentos;

[TestFixture]
public class AtualizarEstabelecimentoCommandHandlerTests
{
    private Mock<IEstabelecimentoRepository> _repo;
    private AtualizarEstabelecimentoCommandHandler _sut;

    private readonly Guid _donoId = Guid.NewGuid();
    private readonly Guid _outroId = Guid.NewGuid();
    private const long EstabelecimentoId = 1;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IEstabelecimentoRepository>();
        _sut = new AtualizarEstabelecimentoCommandHandler(_repo.Object);
    }

    private Estabelecimento Estab() =>
        Estabelecimento.Criar(_donoId, "Original", null, null, null, null);

    private AtualizarEstabelecimentoCommand Cmd(Guid? solicitante = null) => new()
    {
        EstabelecimentoId = EstabelecimentoId,
        UsuarioSolicitanteId = solicitante ?? _donoId,
        NomeFantasia = "Atualizado",
        RazaoSocial = "Imedto LTDA",
        Cnpj = "98.765.432/0001-10",
        Telefone = "11888887777",
        Endereco = "Rua B",
    };

    [Test]
    public async Task Handle_DonoAtualiza_PersisteAlteracoes()
    {
        var estab = Estab();
        _repo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(estab);
        _repo.Setup(r => r.ExisteCnpj("98765432000110", estab.Id)).ReturnsAsync(false);

        await _sut.Handle(Cmd());

        Assert.That(estab.NomeFantasia, Is.EqualTo("Atualizado"));
        Assert.That(estab.Cnpj, Is.EqualTo("98765432000110"));
        _repo.Verify(r => r.Salvar(estab), Times.Once);
    }

    [Test]
    public void Handle_NaoEhDono_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(Estab());

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd(solicitante: _outroId)));
        Assert.That(ex.Message, Does.Contain("dono"));
        _repo.Verify(r => r.Salvar(It.IsAny<Estabelecimento>()), Times.Never);
    }

    [Test]
    public void Handle_EstabelecimentoInexistente_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync((Estabelecimento)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não encontrado"));
    }

    [Test]
    public void Handle_CnpjJaUsadoPorOutroEstab_LancaBusinessException()
    {
        var estab = Estab();
        _repo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(estab);
        _repo.Setup(r => r.ExisteCnpj("98765432000110", estab.Id)).ReturnsAsync(true);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("CNPJ"));
    }
}
