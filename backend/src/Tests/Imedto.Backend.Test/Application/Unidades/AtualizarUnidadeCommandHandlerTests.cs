using Imedto.Backend.Application.Unidades.Commands;
using Imedto.Backend.Contracts.Unidades.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Unidades;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Unidades;

[TestFixture]
public class AtualizarUnidadeCommandHandlerTests
{
    private Mock<IUnidadeRepository> _unidades;
    private Mock<IEstabelecimentoRepository> _estabRepo;
    private AtualizarUnidadeCommandHandler _sut;

    private readonly Guid _donoId = Guid.NewGuid();
    private readonly Guid _outroId = Guid.NewGuid();
    private const long EstabelecimentoId = 1;
    private const long UnidadeId = 10;

    [SetUp]
    public void SetUp()
    {
        _unidades = new Mock<IUnidadeRepository>();
        _estabRepo = new Mock<IEstabelecimentoRepository>();
        _sut = new AtualizarUnidadeCommandHandler(_unidades.Object, _estabRepo.Object);
    }

    private Estabelecimento Estab()
    {
        var e = Estabelecimento.Criar(_donoId, "Clinica", null, null, null, null);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(e, EstabelecimentoId);
        return e;
    }

    private UnidadeEstabelecimento UnidadePrincipal(long id = UnidadeId, bool principal = true)
    {
        var u = UnidadeEstabelecimento.Criar(EstabelecimentoId, "Original", principal,
            new EnderecoUnidadeInput("", "", "", "", "", "", ""), null);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(u, id);
        return u;
    }

    private AtualizarUnidadeCommand Cmd(bool principal = true, Guid? solicitante = null) => new()
    {
        UnidadeId = UnidadeId,
        UsuarioSolicitanteId = solicitante ?? _donoId,
        Nome = "Atualizada",
        IsPrincipal = principal,
        Cep = "", Logradouro = "", Numero = "", Complemento = "",
        Bairro = "", Cidade = "", Estado = "",
        Telefone = null,
    };

    [Test]
    public async Task Handle_DonoAtualiza_PersisteAlteracoes()
    {
        var unidade = UnidadePrincipal();
        _unidades.Setup(r => r.ObterPorIdOuNulo(UnidadeId)).ReturnsAsync(unidade);
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(Estab());
        _unidades.Setup(r => r.ExisteOutraComMesmoNome(EstabelecimentoId, "Atualizada", UnidadeId))
                 .ReturnsAsync(false);

        await _sut.Handle(Cmd());

        Assert.That(unidade.Nome, Is.EqualTo("Atualizada"));
        _unidades.Verify(r => r.Salvar(unidade), Times.Once);
    }

    [Test]
    public async Task Handle_MarcaComoPrincipalQuandoOutraJaEhPrincipal_DesmarcaAnterior()
    {
        const long outroId = 99L;
        var unidade = UnidadePrincipal(principal: false);            // editada
        var anterior = UnidadePrincipal(id: outroId, principal: true); // principal atual

        _unidades.Setup(r => r.ObterPorIdOuNulo(UnidadeId)).ReturnsAsync(unidade);
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(Estab());
        _unidades.Setup(r => r.ExisteOutraComMesmoNome(It.IsAny<long>(), It.IsAny<string>(), UnidadeId))
                 .ReturnsAsync(false);
        _unidades.Setup(r => r.ObterPrincipalDoEstabelecimento(EstabelecimentoId)).ReturnsAsync(anterior);

        await _sut.Handle(Cmd(principal: true));

        Assert.That(anterior.IsPrincipal, Is.False);
        Assert.That(unidade.IsPrincipal, Is.True);
        _unidades.Verify(r => r.Salvar(anterior), Times.Once);
        _unidades.Verify(r => r.Salvar(unidade), Times.Once);
    }

    [Test]
    public void Handle_TentaDesmarcarUnicaPrincipal_LancaBusinessException()
    {
        var unidade = UnidadePrincipal(principal: true);
        _unidades.Setup(r => r.ObterPorIdOuNulo(UnidadeId)).ReturnsAsync(unidade);
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(Estab());
        _unidades.Setup(r => r.ExisteOutraComMesmoNome(It.IsAny<long>(), It.IsAny<string>(), UnidadeId))
                 .ReturnsAsync(false);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd(principal: false)));
        Assert.That(ex.Message, Does.Contain("principal"));
    }

    [Test]
    public void Handle_NaoEhDono_LancaBusinessException()
    {
        _unidades.Setup(r => r.ObterPorIdOuNulo(UnidadeId)).ReturnsAsync(UnidadePrincipal());
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(Estab());

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd(solicitante: _outroId)));
        Assert.That(ex.Message, Does.Contain("dono"));
    }
}
