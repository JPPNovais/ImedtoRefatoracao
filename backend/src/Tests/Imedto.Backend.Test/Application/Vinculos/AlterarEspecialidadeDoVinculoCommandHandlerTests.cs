using Imedto.Backend.Application.Vinculos.Commands;
using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Vinculos;

[TestFixture]
public class AlterarEspecialidadeDoVinculoCommandHandlerTests
{
    private Mock<IVinculoRepository> _vinculoRepo;
    private Mock<IEstabelecimentoRepository> _estabRepo;
    private AlterarEspecialidadeDoVinculoCommandHandler _sut;

    private readonly Guid _donoId = Guid.NewGuid();
    private readonly Guid _outroId = Guid.NewGuid();
    private const long EstabelecimentoId = 1;
    private const long VinculoId = 50;

    [SetUp]
    public void SetUp()
    {
        _vinculoRepo = new Mock<IVinculoRepository>();
        _estabRepo = new Mock<IEstabelecimentoRepository>();
        _sut = new AlterarEspecialidadeDoVinculoCommandHandler(_vinculoRepo.Object, _estabRepo.Object);
    }

    private VinculoProfissionalEstabelecimento VinculoAtivo()
    {
        var v = VinculoProfissionalEstabelecimento.Convidar(
            Guid.NewGuid(), EstabelecimentoId, 1L, _donoId);
        v.Aceitar();
        return v;
    }

    private VinculoProfissionalEstabelecimento VinculoConvidado() =>
        VinculoProfissionalEstabelecimento.Convidar(Guid.NewGuid(), EstabelecimentoId, 1L, _donoId);

    private VinculoProfissionalEstabelecimento VinculoInativo()
    {
        var v = VinculoAtivo();
        v.Inativar();
        return v;
    }

    private Estabelecimento Estab() =>
        Estabelecimento.Criar(_donoId, "Clinica", null, null, null, null);

    private AlterarEspecialidadeDoVinculoCommand Cmd(string? especialidade = "Dermatologia", Guid? solicitante = null) => new()
    {
        VinculoId = VinculoId,
        EstabelecimentoId = EstabelecimentoId,
        Especialidade = especialidade,
        UsuarioSolicitanteId = solicitante ?? _donoId,
    };

    // ─── CA5 — caminho feliz ───────────────────────────────────────────────────

    [Test]
    public async Task Handle_DonoAlteraEspecialidade_GravaNovosValor()
    {
        var v = VinculoAtivo();
        _vinculoRepo.Setup(r => r.ObterPorIdNoEstabelecimentoOuNulo(VinculoId, EstabelecimentoId)).ReturnsAsync(v);
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(Estab());

        await _sut.Handle(Cmd("Dermatologia"));

        Assert.That(v.EspecialidadeConvidada, Is.EqualTo("Dermatologia"));
        _vinculoRepo.Verify(r => r.Salvar(v), Times.Once);
    }

    // ─── CA6 — limpar campo ────────────────────────────────────────────────────

    [Test]
    public async Task Handle_DonoLimpaEspecialidade_GravaNull()
    {
        var v = VinculoAtivo();
        _vinculoRepo.Setup(r => r.ObterPorIdNoEstabelecimentoOuNulo(VinculoId, EstabelecimentoId)).ReturnsAsync(v);
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(Estab());

        await _sut.Handle(Cmd(especialidade: null));

        Assert.That(v.EspecialidadeConvidada, Is.Null);
        _vinculoRepo.Verify(r => r.Salvar(v), Times.Once);
    }

    [Test]
    public async Task Handle_DonoEnviaStringVazia_GravaNull()
    {
        var v = VinculoAtivo();
        _vinculoRepo.Setup(r => r.ObterPorIdNoEstabelecimentoOuNulo(VinculoId, EstabelecimentoId)).ReturnsAsync(v);
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(Estab());

        await _sut.Handle(Cmd(especialidade: "   "));

        Assert.That(v.EspecialidadeConvidada, Is.Null);
        _vinculoRepo.Verify(r => r.Salvar(v), Times.Once);
    }

    // ─── CA7 — RBAC: não é Dono ────────────────────────────────────────────────

    [Test]
    public void Handle_NaoEhDono_LancaBusinessException()
    {
        var v = VinculoAtivo();
        _vinculoRepo.Setup(r => r.ObterPorIdNoEstabelecimentoOuNulo(VinculoId, EstabelecimentoId)).ReturnsAsync(v);
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(Estab());

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd(solicitante: _outroId)));
        Assert.That(ex.Message, Does.Contain("dono"));
    }

    // ─── CA8 — multi-tenant ────────────────────────────────────────────────────

    [Test]
    public void Handle_VinculoCrossTenant_LancaMensagemGenerica()
    {
        // Repo retorna null para vínculo de outro tenant — falha-fechada.
        _vinculoRepo.Setup(r => r.ObterPorIdNoEstabelecimentoOuNulo(VinculoId, EstabelecimentoId))
            .ReturnsAsync((VinculoProfissionalEstabelecimento?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Is.EqualTo("Vínculo não encontrado."));
        // Curto-circuito: estab repo não deve ser consultado.
        _estabRepo.Verify(r => r.ObterPorId(It.IsAny<long>()), Times.Never);
    }

    [Test]
    public void Handle_VinculoInexistente_LancaBusinessException()
    {
        _vinculoRepo.Setup(r => r.ObterPorIdNoEstabelecimentoOuNulo(VinculoId, EstabelecimentoId))
            .ReturnsAsync((VinculoProfissionalEstabelecimento?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não encontrado"));
    }

    // ─── CA10 — estados Convidado e Inativo ────────────────────────────────────

    [Test]
    public async Task Handle_VinculoConvidado_PermiteAtualizarEspecialidade()
    {
        var v = VinculoConvidado();
        _vinculoRepo.Setup(r => r.ObterPorIdNoEstabelecimentoOuNulo(VinculoId, EstabelecimentoId)).ReturnsAsync(v);
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(Estab());

        await _sut.Handle(Cmd("Acupuntura"));

        Assert.That(v.EspecialidadeConvidada, Is.EqualTo("Acupuntura"));
        _vinculoRepo.Verify(r => r.Salvar(v), Times.Once);
    }

    [Test]
    public async Task Handle_VinculoInativo_PermiteAtualizarEspecialidade()
    {
        var v = VinculoInativo();
        _vinculoRepo.Setup(r => r.ObterPorIdNoEstabelecimentoOuNulo(VinculoId, EstabelecimentoId)).ReturnsAsync(v);
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(Estab());

        await _sut.Handle(Cmd("Ortopedia"));

        Assert.That(v.EspecialidadeConvidada, Is.EqualTo("Ortopedia"));
        _vinculoRepo.Verify(r => r.Salvar(v), Times.Once);
    }

    // ─── CA12 — normalização ──────────────────────────────────────────────────

    [Test]
    public void Handle_EspecialidadeComMaisDe200Chars_LancaBusinessException()
    {
        var v = VinculoAtivo();
        _vinculoRepo.Setup(r => r.ObterPorIdNoEstabelecimentoOuNulo(VinculoId, EstabelecimentoId)).ReturnsAsync(v);
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(Estab());

        var especialidadeLonga = new string('A', 201);
        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd(especialidade: especialidadeLonga)));
    }

    [Test]
    public async Task Handle_EspecialidadeComEspacosNasPontas_NormalizeToTrim()
    {
        var v = VinculoAtivo();
        _vinculoRepo.Setup(r => r.ObterPorIdNoEstabelecimentoOuNulo(VinculoId, EstabelecimentoId)).ReturnsAsync(v);
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(Estab());

        await _sut.Handle(Cmd(especialidade: "  Cardiologia  "));

        Assert.That(v.EspecialidadeConvidada, Is.EqualTo("Cardiologia"));
    }

    // ─── CA11 — mensagem genérica (LGPD) ─────────────────────────────────────

    [Test]
    public void Handle_VinculoNaoEncontrado_MensagemNaoRevealaTenant()
    {
        _vinculoRepo.Setup(r => r.ObterPorIdNoEstabelecimentoOuNulo(VinculoId, EstabelecimentoId))
            .ReturnsAsync((VinculoProfissionalEstabelecimento?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        // Mensagem não deve revelar existência de vínculo em outro tenant.
        Assert.That(ex.Message, Does.Not.Contain("outro estabelecimento"));
        Assert.That(ex.Message, Does.Not.Contain("tenant"));
        Assert.That(ex.Message, Is.EqualTo("Vínculo não encontrado."));
    }
}
