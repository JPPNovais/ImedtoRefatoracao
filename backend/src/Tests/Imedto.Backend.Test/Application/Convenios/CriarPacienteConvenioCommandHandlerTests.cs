using Imedto.Backend.Application.PacienteConvenios.Commands;
using Imedto.Backend.Contracts.PacienteConvenios.Commands;
using Imedto.Backend.Domain.Convenios;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.PacienteConvenios;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Imedto.Backend.Test.Application.Convenios;

/// <summary>
/// Testa CriarPacienteConvenio — CA138 (plano de outro convênio → 422)
/// + multi-tenant (paciente/convênio do tenant).
/// </summary>
[TestFixture]
public class CriarPacienteConvenioCommandHandlerTests
{
    private Mock<IPacienteConvenioRepository> _repo;
    private Mock<IConvenioRepository> _convenioRepo;
    private Mock<IPacienteRepository> _pacienteRepo;
    private CriarPacienteConvenioCommandHandler _sut;

    private const long EstabId = 1L;
    private const long PacienteId = 20L;
    private const long ConvenioId = 5L;
    private const long PlanoId = 3L;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IPacienteConvenioRepository>();
        _convenioRepo = new Mock<IConvenioRepository>();
        _pacienteRepo = new Mock<IPacienteRepository>();
        _sut = new CriarPacienteConvenioCommandHandler(
            _repo.Object, _convenioRepo.Object, _pacienteRepo.Object);
    }

    private CriarPacienteConvenioCommand Cmd(long? planoId = null) => new()
    {
        PacienteId = PacienteId,
        EstabelecimentoId = EstabId,
        UsuarioSolicitanteId = Guid.NewGuid(),
        ConvenioId = ConvenioId,
        PlanoId = planoId,
        NumeroCarteirinha = "98765",
        Validade = null,
    };

    private static Paciente PacienteExistente()
        => Paciente.Cadastrar(EstabId, "Teste Paciente", "12345678909",
            new DateTime(1990, 1, 1), GeneroPaciente.Masculino,
            null, null, null, null);

    private static Convenio ConvenioComPlano(long planoId)
    {
        var c = Convenio.Criar(EstabId, "Unimed", null);
        var plano = c.AdicionarPlano("Fácil");
        // Simula Id gerado pelo banco via reflection (padrão nos testes do projeto)
        typeof(Imedto.Backend.SharedKernel.Domain.Entity)
            .GetProperty("Id")!
            .SetValue(plano, planoId);
        return c;
    }

    // ── Multi-tenant: paciente de outro tenant → 422 genérico ───────────────

    [Test]
    public void Handle_PacienteDeOutroTenant_LancaGenerica()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabId)).ReturnsAsync((Paciente?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não encontrado"));
        _repo.Verify(r => r.Salvar(It.IsAny<PacienteConvenio>()), Times.Never);
    }

    // ── Multi-tenant: convênio de outro tenant → 422 genérico ───────────────

    [Test]
    public void Handle_ConvenioDeOutroTenant_LancaGenerica()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabId)).ReturnsAsync(PacienteExistente());
        _convenioRepo.Setup(r => r.ObterPorIdOuNulo(ConvenioId, EstabId)).ReturnsAsync((Convenio?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não encontrado"));
        _repo.Verify(r => r.Salvar(It.IsAny<PacienteConvenio>()), Times.Never);
    }

    // ── CA138: plano de outro convênio → 422 ────────────────────────────────

    [Test]
    public void Handle_PlanoDeOutroConvenio_LancaBusinessException()
    {
        // Convenio existe mas não tem o plano 999
        var c = Convenio.Criar(EstabId, "Unimed", null);
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabId)).ReturnsAsync(PacienteExistente());
        _convenioRepo.Setup(r => r.ObterPorIdOuNulo(ConvenioId, EstabId)).ReturnsAsync(c);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd(planoId: 999)));
        Assert.That(ex.Message, Does.Contain("Plano não encontrado"));
        _repo.Verify(r => r.Salvar(It.IsAny<PacienteConvenio>()), Times.Never);
    }

    // ── Happy path: sem plano ────────────────────────────────────────────────

    [Test]
    public async Task Handle_SemPlano_CriaCarteirinha()
    {
        var c = Convenio.Criar(EstabId, "Unimed", null);
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabId)).ReturnsAsync(PacienteExistente());
        _convenioRepo.Setup(r => r.ObterPorIdOuNulo(ConvenioId, EstabId)).ReturnsAsync(c);
        PacienteConvenio? salva = null;
        _repo.Setup(r => r.Salvar(It.IsAny<PacienteConvenio>()))
            .Callback<PacienteConvenio>(pc => salva = pc)
            .Returns(Task.CompletedTask);

        await _sut.Handle(Cmd(planoId: null));

        _repo.Verify(r => r.Salvar(It.IsAny<PacienteConvenio>()), Times.Once);
        Assert.That(salva!.ConvenioId, Is.EqualTo(ConvenioId));
        Assert.That(salva.PlanoId, Is.Null);
    }

    // ── Happy path: com plano válido ─────────────────────────────────────────

    [Test]
    public async Task Handle_PlanoValido_CriaCarteirinha()
    {
        var c = ConvenioComPlano(PlanoId);
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabId)).ReturnsAsync(PacienteExistente());
        _convenioRepo.Setup(r => r.ObterPorIdOuNulo(ConvenioId, EstabId)).ReturnsAsync(c);

        await _sut.Handle(Cmd(planoId: PlanoId));

        _repo.Verify(r => r.Salvar(It.IsAny<PacienteConvenio>()), Times.Once);
    }
}
