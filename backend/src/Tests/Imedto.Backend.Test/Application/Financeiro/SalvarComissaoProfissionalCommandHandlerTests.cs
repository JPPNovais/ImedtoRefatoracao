using Imedto.Backend.Application.Financeiro.Commands;
using Imedto.Backend.Contracts.Financeiro.Commands;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Financeiro;

/// <summary>
/// CA177 — Salvar config de comissão: somente Dono. Upsert por tipo (Consulta/Procedimento).
/// </summary>
[TestFixture]
public class SalvarComissaoProfissionalCommandHandlerTests
{
    private Mock<IConfigComissaoProfissionalRepository> _repo;
    private SalvarComissaoProfissionalCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private readonly Guid _profId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IConfigComissaoProfissionalRepository>();
        _sut = new SalvarComissaoProfissionalCommandHandler(_repo.Object);
    }

    private SalvarComissaoProfissionalCommand Cmd(bool ehDono = true,
        decimal? consulta = 35m, decimal? procedimento = 20m) => new()
    {
        EstabelecimentoId = EstabelecimentoId,
        ProfissionalUsuarioId = _profId,
        PercentualConsulta = consulta,
        PercentualProcedimento = procedimento,
        EhDono = ehDono
    };

    [Test]
    public async Task Handle_Dono_ConfigNaoExiste_CriaNovos()
    {
        _repo.Setup(r => r.ObterOuNulo(EstabelecimentoId, _profId, TipoComissao.Consulta))
             .ReturnsAsync((ConfigComissaoProfissional?)null);
        _repo.Setup(r => r.ObterOuNulo(EstabelecimentoId, _profId, TipoComissao.Procedimento))
             .ReturnsAsync((ConfigComissaoProfissional?)null);

        await _sut.Handle(Cmd());

        _repo.Verify(r => r.Salvar(It.Is<ConfigComissaoProfissional>(c =>
            c.Tipo == TipoComissao.Consulta && c.Percentual == 35m)), Times.Once);
        _repo.Verify(r => r.Salvar(It.Is<ConfigComissaoProfissional>(c =>
            c.Tipo == TipoComissao.Procedimento && c.Percentual == 20m)), Times.Once);
    }

    [Test]
    public async Task Handle_Dono_ConfigExiste_Atualiza()
    {
        var configExistente = ConfigComissaoProfissional.Criar(
            EstabelecimentoId, _profId, TipoComissao.Consulta, 10m);

        _repo.Setup(r => r.ObterOuNulo(EstabelecimentoId, _profId, TipoComissao.Consulta))
             .ReturnsAsync(configExistente);
        _repo.Setup(r => r.ObterOuNulo(EstabelecimentoId, _profId, TipoComissao.Procedimento))
             .ReturnsAsync((ConfigComissaoProfissional?)null);

        await _sut.Handle(Cmd(consulta: 50m, procedimento: 15m));

        // Deve atualizar o percentual do objeto existente.
        Assert.That(configExistente.Percentual, Is.EqualTo(50m));
        // Salvar é chamado duas vezes (1 para Consulta atualizada, 1 para Procedimento criado).
        _repo.Verify(r => r.Salvar(It.IsAny<ConfigComissaoProfissional>()), Times.Exactly(2));
    }

    [Test]
    public void Handle_NaoDono_LancaBusinessException()
    {
        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd(ehDono: false)));
    }

    [Test]
    public async Task Handle_SemPercentualConsulta_NaoSalvaConsulta()
    {
        _repo.Setup(r => r.ObterOuNulo(EstabelecimentoId, _profId, TipoComissao.Procedimento))
             .ReturnsAsync((ConfigComissaoProfissional?)null);

        await _sut.Handle(Cmd(consulta: null, procedimento: 20m));

        _repo.Verify(r => r.Salvar(It.Is<ConfigComissaoProfissional>(c =>
            c.Tipo == TipoComissao.Consulta)), Times.Never);
        _repo.Verify(r => r.Salvar(It.Is<ConfigComissaoProfissional>(c =>
            c.Tipo == TipoComissao.Procedimento)), Times.Once);
    }
}
