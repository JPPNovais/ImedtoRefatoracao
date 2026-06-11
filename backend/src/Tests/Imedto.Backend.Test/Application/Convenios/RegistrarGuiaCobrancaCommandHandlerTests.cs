using Imedto.Backend.Application.Cobrancas.Commands;
using Imedto.Backend.Contracts.Cobrancas.Commands;
using Imedto.Backend.Domain.Cobrancas;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Convenios;

/// <summary>
/// Testa RegistrarGuiaCobranca — CA148 (guia em Particular → 422) + multi-tenant.
/// </summary>
[TestFixture]
public class RegistrarGuiaCobrancaCommandHandlerTests
{
    private Mock<ICobrancaRepository> _repo;
    private RegistrarGuiaCobrancaCommandHandler _sut;

    private const long EstabId = 1L;
    private const long CobrancaId = 100L;
    private static readonly Guid UsuarioId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<ICobrancaRepository>();
        _sut = new RegistrarGuiaCobrancaCommandHandler(_repo.Object);
    }

    private static RegistrarGuiaCobrancaCommand Cmd(string guia = "G-001")
        => new()
        {
            CobrancaId = CobrancaId,
            EstabelecimentoId = EstabId,
            UsuarioSolicitanteId = UsuarioId,
            GuiaNumero = guia,
            GuiaSenha = null,
            GuiaAutorizadaEm = null,
        };

    private static Cobranca CobrancaConvenio()
        => Cobranca.CriarParaConsulta(EstabId, 10L, 500L,
            TipoAtendimento.Convenio, 0m, "Consulta", UsuarioId, convenioId: 55L);

    private static Cobranca CobrancaParticular()
        => Cobranca.CriarParaConsulta(EstabId, 10L, 500L,
            TipoAtendimento.Particular, 200m, "Consulta", UsuarioId);

    // ── Multi-tenant: cobrança de outro tenant → 422 genérico ────────────────

    [Test]
    public void Handle_CobrancaNaoEncontrada_LancaGenerica()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(CobrancaId, EstabId)).ReturnsAsync((Cobranca?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não encontrada"));
        _repo.Verify(r => r.Salvar(It.IsAny<Cobranca>()), Times.Never);
    }

    // ── CA148: guia em cobrança Particular → 422 ────────────────────────────

    [Test]
    public void Handle_CobrancaParticular_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(CobrancaId, EstabId)).ReturnsAsync(CobrancaParticular());

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("convênio"));
        _repo.Verify(r => r.Salvar(It.IsAny<Cobranca>()), Times.Never);
    }

    // ── Happy path: guia registrada com sucesso ───────────────────────────────

    [Test]
    public async Task Handle_CobrancaConvenio_RegistraGuia()
    {
        var c = CobrancaConvenio();
        _repo.Setup(r => r.ObterPorIdOuNulo(CobrancaId, EstabId)).ReturnsAsync(c);

        await _sut.Handle(new RegistrarGuiaCobrancaCommand
        {
            CobrancaId = CobrancaId,
            EstabelecimentoId = EstabId,
            UsuarioSolicitanteId = UsuarioId,
            GuiaNumero = "G-2024-001",
            GuiaSenha = "Abc123",
            GuiaAutorizadaEm = DateOnly.FromDateTime(DateTime.Today),
        });

        Assert.That(c.GuiaNumero, Is.EqualTo("G-2024-001"));
        Assert.That(c.GuiaSenha, Is.EqualTo("Abc123"));
        _repo.Verify(r => r.Salvar(c), Times.Once);
    }

    // ── Falha-fechada: repo chamado com estabelecimentoId correto ────────────

    [Test]
    public void Handle_SempreFiltroTenant()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(CobrancaId, EstabId)).ReturnsAsync((Cobranca?)null);

        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));

        _repo.Verify(r => r.ObterPorIdOuNulo(CobrancaId, EstabId), Times.Once);
        _repo.Verify(r => r.ObterPorIdOuNulo(It.IsAny<long>(), 0), Times.Never);
    }
}
