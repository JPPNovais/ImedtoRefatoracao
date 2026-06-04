using Imedto.Backend.Application.Vinculos.Commands;
using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Vinculos;

[TestFixture]
public class AlterarProfissaoEspecialidadeDoVinculoCommandHandlerTests
{
    private Mock<IVinculoRepository> _vinculoRepo;
    private Mock<IEstabelecimentoRepository> _estabRepo;
    private Mock<CatalogoQueryRepository> _catalogoRepo;
    private AlterarProfissaoEspecialidadeDoVinculoCommandHandler _sut;

    private readonly Guid _donoId = Guid.NewGuid();
    private readonly Guid _outroId = Guid.NewGuid();
    private const long EstabelecimentoId = 1;
    private const long VinculoId = 50;
    private const long ProfissaoId = 10;

    [SetUp]
    public void SetUp()
    {
        _vinculoRepo = new Mock<IVinculoRepository>();
        _estabRepo = new Mock<IEstabelecimentoRepository>();
        _catalogoRepo = new Mock<CatalogoQueryRepository>(
            new Imedto.Backend.Infrastructure.AppReadConnectionString("Host=localhost;Database=fake"));
        _sut = new AlterarProfissaoEspecialidadeDoVinculoCommandHandler(
            _vinculoRepo.Object, _estabRepo.Object, _catalogoRepo.Object);
    }

    private VinculoProfissionalEstabelecimento VinculoAtivo()
    {
        var v = VinculoProfissionalEstabelecimento.Convidar(
            Guid.NewGuid(), EstabelecimentoId, 1L, _donoId);
        v.Aceitar();
        return v;
    }

    private Estabelecimento Estab() =>
        Estabelecimento.Criar(_donoId, "Clinica", null, null, null, null);

    private AlterarProfissaoEspecialidadeDoVinculoCommand Cmd(
        long? profissaoId = ProfissaoId,
        string? especialidade = "Dermatologia",
        Guid? solicitante = null) => new()
    {
        VinculoId = VinculoId,
        EstabelecimentoId = EstabelecimentoId,
        ProfissaoId = profissaoId,
        Especialidade = especialidade,
        UsuarioSolicitanteId = solicitante ?? _donoId,
    };

    // ─── CA4 — persistência atômica (caminho feliz) ────────────────────────────

    [Test]
    public async Task Handle_DonoAlteraProfissaoEEspecialidade_GravaAmbosAtomicamente()
    {
        var v = VinculoAtivo();
        _vinculoRepo.Setup(r => r.ObterPorIdNoEstabelecimentoOuNulo(VinculoId, EstabelecimentoId)).ReturnsAsync(v);
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(Estab());
        _catalogoRepo.Setup(r => r.ExisteProfissaoAtiva(ProfissaoId)).ReturnsAsync(true);
        _catalogoRepo.Setup(r => r.ExisteEspecialidadeAtivaPorNome(ProfissaoId, "Dermatologia")).ReturnsAsync(true);

        await _sut.Handle(Cmd());

        Assert.Multiple(() =>
        {
            Assert.That(v.ProfissaoConvidadaId, Is.EqualTo(ProfissaoId));
            Assert.That(v.EspecialidadeConvidada, Is.EqualTo("Dermatologia"));
        });
        _vinculoRepo.Verify(r => r.Salvar(v), Times.Once);
    }

    // ─── CA3 — troca de profissão limpa especialidade ─────────────────────────

    [Test]
    public async Task Handle_TrocaProfissaoSemEspecialidade_LimpaEspecialidade()
    {
        var v = VinculoAtivo();
        _vinculoRepo.Setup(r => r.ObterPorIdNoEstabelecimentoOuNulo(VinculoId, EstabelecimentoId)).ReturnsAsync(v);
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(Estab());
        _catalogoRepo.Setup(r => r.ExisteProfissaoAtiva(ProfissaoId)).ReturnsAsync(true);

        await _sut.Handle(Cmd(profissaoId: ProfissaoId, especialidade: null));

        Assert.Multiple(() =>
        {
            Assert.That(v.ProfissaoConvidadaId, Is.EqualTo(ProfissaoId));
            Assert.That(v.EspecialidadeConvidada, Is.Null);
        });
    }

    // ─── CA7 — catálogo estrito: especialidade fora da profissão → 422 ─────────

    [Test]
    public void Handle_EspecialidadeForaDaCatalogoProfissao_LancaBusinessException()
    {
        var v = VinculoAtivo();
        _vinculoRepo.Setup(r => r.ObterPorIdNoEstabelecimentoOuNulo(VinculoId, EstabelecimentoId)).ReturnsAsync(v);
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(Estab());
        _catalogoRepo.Setup(r => r.ExisteProfissaoAtiva(ProfissaoId)).ReturnsAsync(true);
        _catalogoRepo.Setup(r => r.ExisteEspecialidadeAtivaPorNome(ProfissaoId, "EspecialidadeInexistente")).ReturnsAsync(false);

        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            _sut.Handle(Cmd(especialidade: "EspecialidadeInexistente")));
        Assert.That(ex.Message, Does.Contain("Especialidade não pertence à profissão selecionada ou está inativa."));
        _vinculoRepo.Verify(r => r.Salvar(It.IsAny<VinculoProfissionalEstabelecimento>()), Times.Never);
    }

    [Test]
    public void Handle_ProfissaoInativa_LancaBusinessException()
    {
        var v = VinculoAtivo();
        _vinculoRepo.Setup(r => r.ObterPorIdNoEstabelecimentoOuNulo(VinculoId, EstabelecimentoId)).ReturnsAsync(v);
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(Estab());
        _catalogoRepo.Setup(r => r.ExisteProfissaoAtiva(ProfissaoId)).ReturnsAsync(false);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("Profissão informada é inválida ou está inativa."));
    }

    [Test]
    public void Handle_EspecialidadeSemProfissao_LancaBusinessException()
    {
        var v = VinculoAtivo();
        _vinculoRepo.Setup(r => r.ObterPorIdNoEstabelecimentoOuNulo(VinculoId, EstabelecimentoId)).ReturnsAsync(v);
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(Estab());

        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            _sut.Handle(Cmd(profissaoId: null, especialidade: "Dermatologia")));
        Assert.That(ex.Message, Does.Contain("Profissão é obrigatória quando especialidade for informada."));
    }

    // ─── CA6 — RBAC: não é Dono → 403 ────────────────────────────────────────

    [Test]
    public void Handle_NaoEhDono_LancaBusinessException()
    {
        var v = VinculoAtivo();
        _vinculoRepo.Setup(r => r.ObterPorIdNoEstabelecimentoOuNulo(VinculoId, EstabelecimentoId)).ReturnsAsync(v);
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(Estab());

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd(solicitante: _outroId)));
        Assert.That(ex.Message, Does.Contain("dono"));
        _vinculoRepo.Verify(r => r.Salvar(It.IsAny<VinculoProfissionalEstabelecimento>()), Times.Never);
    }

    // ─── CA5 — multi-tenant: vínculo de outro tenant → mensagem genérica ──────

    [Test]
    public void Handle_VinculoCrossTenant_LancaMensagemGenerica()
    {
        // Repo retorna null para vínculo de outro tenant — falha-fechada.
        _vinculoRepo.Setup(r => r.ObterPorIdNoEstabelecimentoOuNulo(VinculoId, EstabelecimentoId))
            .ReturnsAsync((VinculoProfissionalEstabelecimento?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Is.EqualTo("Vínculo não encontrado."));
        // Curto-circuito: estab e catálogo não devem ser consultados.
        _estabRepo.Verify(r => r.ObterPorId(It.IsAny<long>()), Times.Never);
        _catalogoRepo.Verify(r => r.ExisteProfissaoAtiva(It.IsAny<long>()), Times.Never);
    }

    [Test]
    public void Handle_VinculoNaoEncontrado_MensagemNaoRevealaTenant()
    {
        _vinculoRepo.Setup(r => r.ObterPorIdNoEstabelecimentoOuNulo(VinculoId, EstabelecimentoId))
            .ReturnsAsync((VinculoProfissionalEstabelecimento?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Not.Contain("outro estabelecimento"));
        Assert.That(ex.Message, Does.Not.Contain("tenant"));
        Assert.That(ex.Message, Is.EqualTo("Vínculo não encontrado."));
    }

    // ─── Limpar profissão e especialidade ─────────────────────────────────────

    [Test]
    public async Task Handle_ProfissaoNulaEEspecialidadeNula_LimpaAmbos()
    {
        var v = VinculoAtivo();
        _vinculoRepo.Setup(r => r.ObterPorIdNoEstabelecimentoOuNulo(VinculoId, EstabelecimentoId)).ReturnsAsync(v);
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(Estab());

        await _sut.Handle(Cmd(profissaoId: null, especialidade: null));

        Assert.Multiple(() =>
        {
            Assert.That(v.ProfissaoConvidadaId, Is.Null);
            Assert.That(v.EspecialidadeConvidada, Is.Null);
        });
        _vinculoRepo.Verify(r => r.Salvar(v), Times.Once);
    }

    // ─── CA4 — não altera cadastro global (só o vínculo) ──────────────────────
    // O handler grava apenas no repo de vínculo (profissao_convidada_id + especialidade_convidada).
    // O cadastro global (tabela profissionais) não é tocado — a leitura usa COALESCE no Dapper.

    [Test]
    public async Task Handle_AlteraVinculo_GravaApenasNoVinculoNaoNoGlobal()
    {
        var v = VinculoAtivo();
        _vinculoRepo.Setup(r => r.ObterPorIdNoEstabelecimentoOuNulo(VinculoId, EstabelecimentoId)).ReturnsAsync(v);
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(Estab());
        _catalogoRepo.Setup(r => r.ExisteProfissaoAtiva(ProfissaoId)).ReturnsAsync(true);
        _catalogoRepo.Setup(r => r.ExisteEspecialidadeAtivaPorNome(ProfissaoId, "Dermatologia")).ReturnsAsync(true);

        await _sut.Handle(Cmd());

        // Campos gravados no aggregate do vínculo (não no profissional global).
        Assert.Multiple(() =>
        {
            Assert.That(v.ProfissaoConvidadaId, Is.EqualTo(ProfissaoId));
            Assert.That(v.EspecialidadeConvidada, Is.EqualTo("Dermatologia"));
        });
        // Salva apenas o vínculo — nunca o profissional global.
        _vinculoRepo.Verify(r => r.Salvar(v), Times.Once);
    }
}
