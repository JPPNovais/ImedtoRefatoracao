using Imedto.Backend.Application.Unidades.Commands;
using Imedto.Backend.Contracts.Unidades.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Unidades;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Unidades;

[TestFixture]
public class DeletarUnidadeCommandHandlerTests
{
    private Mock<IUnidadeRepository> _unidades;
    private Mock<IEstabelecimentoRepository> _estabRepo;
    private DeletarUnidadeCommandHandler _sut;

    private readonly Guid _donoId = Guid.NewGuid();
    private readonly Guid _outroId = Guid.NewGuid();
    private const long EstabelecimentoId = 1;
    private const long UnidadeId = 10;

    [SetUp]
    public void SetUp()
    {
        _unidades = new Mock<IUnidadeRepository>();
        _estabRepo = new Mock<IEstabelecimentoRepository>();
        _sut = new DeletarUnidadeCommandHandler(_unidades.Object, _estabRepo.Object);
    }

    private Estabelecimento Estab()
    {
        var e = Estabelecimento.Criar(_donoId, "Clinica", null, null, null, null);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(e, EstabelecimentoId);
        return e;
    }

    private UnidadeEstabelecimento Unidade(bool principal = false)
    {
        var u = UnidadeEstabelecimento.Criar(EstabelecimentoId, "Filial", principal,
            new EnderecoUnidadeInput("", "", "", "", "", "", ""), null);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(u, UnidadeId);
        return u;
    }

    [Test]
    public async Task Handle_DonoExcluiNaoPrincipal_RemoveUnidade()
    {
        var unidade = Unidade(principal: false);
        _unidades.Setup(r => r.ObterPorIdOuNulo(UnidadeId, EstabelecimentoId)).ReturnsAsync(unidade);
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(Estab());

        await _sut.Handle(new DeletarUnidadeCommand
        {
            UnidadeId = UnidadeId,
            EstabelecimentoId = EstabelecimentoId,
            UsuarioSolicitanteId = _donoId,
        });

        _unidades.Verify(r => r.Excluir(unidade), Times.Once);
    }

    [Test]
    public void Handle_TentaExcluirPrincipal_LancaBusinessException()
    {
        _unidades.Setup(r => r.ObterPorIdOuNulo(UnidadeId, EstabelecimentoId)).ReturnsAsync(Unidade(principal: true));
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(Estab());

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new DeletarUnidadeCommand
        {
            UnidadeId = UnidadeId,
            EstabelecimentoId = EstabelecimentoId,
            UsuarioSolicitanteId = _donoId,
        }));
        Assert.That(ex.Message, Does.Contain("principal"));
        _unidades.Verify(r => r.Excluir(It.IsAny<UnidadeEstabelecimento>()), Times.Never);
    }

    [Test]
    public void Handle_NaoEhDono_LancaBusinessException()
    {
        _unidades.Setup(r => r.ObterPorIdOuNulo(UnidadeId, EstabelecimentoId)).ReturnsAsync(Unidade());
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(Estab());

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new DeletarUnidadeCommand
        {
            UnidadeId = UnidadeId,
            EstabelecimentoId = EstabelecimentoId,
            UsuarioSolicitanteId = _outroId,
        }));
        Assert.That(ex.Message, Does.Contain("dono"));
    }
}
