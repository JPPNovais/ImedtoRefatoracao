using Imedto.Backend.Application.Termos.Commands;
using Imedto.Backend.Contracts.Termos.Commands;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Termos;
using Imedto.Backend.Domain.Termos.Events;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Termos;

/// <summary>
/// Testes do handler pós briefing 2026-06-12_002:
/// - AssinaturaTipo fixo (PdfAnexado) — removido da emissão.
/// - TokenAceiteGerado removido do command.
/// - EvolucaoId adicionado ao command.
/// </summary>
[TestFixture]
public class EmitirTermoCommandHandlerTests
{
    private Mock<ITermoEmitidoRepository> _termoRepo = null!;
    private Mock<ITermoModeloRepository> _modeloRepo = null!;
    private Mock<IPacienteRepository> _pacienteRepo = null!;
    private Mock<IVinculoRepository> _vinculoRepo = null!;
    private Mock<ITermoResolverDeVariaveis> _resolver = null!;
    private Mock<ITermoHtmlSanitizer> _sanitizer = null!;
    private Mock<ITermoTextoExtractor> _texto = null!;
    private Mock<ITermoAuditLogger> _audit = null!;
    private Mock<IEventBus> _eventBus = null!;
    private EmitirTermoCommandHandler _sut = null!;

    private const long EstabId = 1;
    private const long PacienteId = 10;
    private const long ModeloId = 99;
    private readonly Guid _emissor = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _termoRepo = new Mock<ITermoEmitidoRepository>();
        _modeloRepo = new Mock<ITermoModeloRepository>();
        _pacienteRepo = new Mock<IPacienteRepository>();
        _vinculoRepo = new Mock<IVinculoRepository>();
        _resolver = new Mock<ITermoResolverDeVariaveis>();
        _sanitizer = new Mock<ITermoHtmlSanitizer>();
        _texto = new Mock<ITermoTextoExtractor>();
        _audit = new Mock<ITermoAuditLogger>();
        _eventBus = new Mock<IEventBus>();
        _sut = new EmitirTermoCommandHandler(
            _termoRepo.Object, _modeloRepo.Object, _pacienteRepo.Object,
            _vinculoRepo.Object, _resolver.Object, _sanitizer.Object,
            _texto.Object, _audit.Object, _eventBus.Object);
    }

    private static Paciente PacienteAtivo()
    {
        var p = Paciente.Cadastrar(EstabId, "Paciente", null, null,
            GeneroPaciente.NaoInformado, null, "paciente@local", null, null);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(p, PacienteId);
        return p;
    }

    private static TermoModelo ModeloAtivo()
    {
        var m = TermoModelo.CriarDoEstabelecimento(EstabId, Guid.NewGuid(), CategoriaTermo.Lgpd, "Termo teste", "<p>x</p>");
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(m, ModeloId);
        return m;
    }

    private EmitirTermoCommand Cmd(long? evolucaoId = null) => new()
    {
        PacienteId = PacienteId,
        EstabelecimentoId = EstabId,
        EmissorUsuarioId = _emissor,
        ModeloId = ModeloId,
        EvolucaoId = evolucaoId,
    };

    private void SetupPacienteEModelo()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabId)).ReturnsAsync(PacienteAtivo());
        _modeloRepo.Setup(r => r.ObterPorIdDoEstabelecimentoOuNulo(ModeloId, EstabId)).ReturnsAsync(ModeloAtivo());
        _resolver.Setup(r => r.ResolverAsync(It.IsAny<string>(), It.IsAny<ContextoDeVariaveis>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<p>resolvido</p>");
        _sanitizer.Setup(s => s.Sanitizar(It.IsAny<string>())).Returns("<p>resolvido</p>");
        _texto.Setup(t => t.Extrair(It.IsAny<string>())).Returns("resolvido");
    }

    private void SetupSalvar(long id = 7L)
    {
        _termoRepo.Setup(r => r.Salvar(It.IsAny<TermoEmitido>()))
            .Callback<TermoEmitido>(t => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(t, id))
            .Returns(Task.CompletedTask);
    }

    // ── Fluxo válido ──────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_FluxoValido_PersisteSnapshotEPublicaEvento()
    {
        SetupPacienteEModelo();
        SetupSalvar(7L);

        var cmd = Cmd();
        await _sut.Handle(cmd);

        Assert.That(cmd.TermoEmitidoId, Is.EqualTo(7L));
        _eventBus.Verify(b => b.Publish(It.Is<IDomainEvent>(e => e is TermoEmitidoEvent)), Times.Once);
        _audit.Verify(a => a.RegistrarAsync(EstabId, _emissor, "termo-emitido", "TermoEmitido", 7L,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_ComEvolucaoId_VinculaEvolucaoNoTermo()
    {
        SetupPacienteEModelo();
        SetupSalvar(7L);

        TermoEmitido? capturado = null;
        _termoRepo.Setup(r => r.Salvar(It.IsAny<TermoEmitido>()))
            .Callback<TermoEmitido>(t =>
            {
                typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(t, 7L);
                capturado = t;
            })
            .Returns(Task.CompletedTask);

        var cmd = Cmd(evolucaoId: 42L);
        await _sut.Handle(cmd);

        Assert.That(capturado!.EvolucaoId, Is.EqualTo(42L));
    }

    // ── Erros de negócio ──────────────────────────────────────────────────────

    [Test]
    public void Handle_PacienteCrossTenant_LancaMensagemGenerica()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabId)).ReturnsAsync((Paciente)null);
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex!.Message, Is.EqualTo("Paciente não encontrado."));
    }

    [Test]
    public void Handle_ModeloNaoEncontradoEmTenantNemEmPadroes_LancaMensagemGenerica()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabId)).ReturnsAsync(PacienteAtivo());
        _modeloRepo.Setup(r => r.ObterPorIdDoEstabelecimentoOuNulo(ModeloId, EstabId)).ReturnsAsync((TermoModelo)null);
        _modeloRepo.Setup(r => r.ObterPadraoDoSistemaPorIdOuNulo(ModeloId)).ReturnsAsync((TermoModelo)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex!.Message, Is.EqualTo("Modelo de termo não encontrado."));
    }

    // ── Modelo padrão do sistema ───────────────────────────────────────────────

    [Test]
    public async Task Handle_ModeloPadraoDoSistema_AceitaEmissaoSemPrecisarClonar()
    {
        var padrao = TermoModelo.CriarPadraoDoSistema(CategoriaTermo.Lgpd, "Padrão LGPD", "<p>x</p>");
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(padrao, ModeloId);

        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabId)).ReturnsAsync(PacienteAtivo());
        _modeloRepo.Setup(r => r.ObterPorIdDoEstabelecimentoOuNulo(ModeloId, EstabId)).ReturnsAsync((TermoModelo)null);
        _modeloRepo.Setup(r => r.ObterPadraoDoSistemaPorIdOuNulo(ModeloId)).ReturnsAsync(padrao);
        _resolver.Setup(r => r.ResolverAsync(It.IsAny<string>(), It.IsAny<ContextoDeVariaveis>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<p>x</p>");
        _sanitizer.Setup(s => s.Sanitizar(It.IsAny<string>())).Returns("<p>x</p>");
        _texto.Setup(t => t.Extrair(It.IsAny<string>())).Returns("x");
        SetupSalvar(11L);

        await _sut.Handle(Cmd());
        _termoRepo.Verify(r => r.Salvar(It.IsAny<TermoEmitido>()), Times.Once);
    }

    // ── Regressão: EvolucaoId não descartado no mapeamento controller→command ──

    /// <summary>
    /// Regressão do bug identificado pelo QA: EmitirTermoRequest não declarava EvolucaoId,
    /// então o campo era descartado silenciosamente no controller antes de chegar ao handler.
    /// Este teste valida que o aggregate persiste o EvolucaoId quando o command o carrega.
    /// </summary>
    [Test]
    public async Task Handle_EvolucaoIdNoCommand_ChegaNoAggregateEEhPersistido()
    {
        SetupPacienteEModelo();

        TermoEmitido? capturado = null;
        _termoRepo.Setup(r => r.Salvar(It.IsAny<TermoEmitido>()))
            .Callback<TermoEmitido>(t =>
            {
                typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(t, 99L);
                capturado = t;
            })
            .Returns(Task.CompletedTask);

        // Simula o payload que o controller montaria ao receber EvolucaoId=55 do frontend.
        var cmd = new EmitirTermoCommand
        {
            PacienteId = PacienteId,
            EstabelecimentoId = EstabId,
            EmissorUsuarioId = _emissor,
            ModeloId = ModeloId,
            EvolucaoId = 55L,
        };
        await _sut.Handle(cmd);

        Assert.That(capturado, Is.Not.Null, "Aggregate deve ter sido salvo.");
        Assert.That(capturado!.EvolucaoId, Is.EqualTo(55L),
            "EvolucaoId deve chegar ao aggregate sem ser descartado.");
    }

    // ── Profissional ──────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_SemProfissionalUsuarioId_NaoValidaVinculo()
    {
        SetupPacienteEModelo();
        SetupSalvar(1L);

        ContextoDeVariaveis? capturado = null;
        _resolver.Setup(r => r.ResolverAsync(It.IsAny<string>(), It.IsAny<ContextoDeVariaveis>(), It.IsAny<CancellationToken>()))
            .Callback<string, ContextoDeVariaveis, CancellationToken>((_, c, _) => capturado = c)
            .ReturnsAsync("<p>x</p>");

        await _sut.Handle(Cmd());

        Assert.That(capturado!.ProfissionalUsuarioId, Is.Null);
        _vinculoRepo.Verify(v => v.PodeAtuarComoProfissional(It.IsAny<Guid>(), It.IsAny<long>()), Times.Never);
    }

    [Test]
    public async Task Handle_ComProfissionalUsuarioIdValido_ValidaVinculoEPropaga()
    {
        var profissionalId = Guid.NewGuid();
        SetupPacienteEModelo();
        _vinculoRepo.Setup(v => v.PodeAtuarComoProfissional(profissionalId, EstabId)).ReturnsAsync(true);
        SetupSalvar(2L);

        ContextoDeVariaveis? capturado = null;
        _resolver.Setup(r => r.ResolverAsync(It.IsAny<string>(), It.IsAny<ContextoDeVariaveis>(), It.IsAny<CancellationToken>()))
            .Callback<string, ContextoDeVariaveis, CancellationToken>((_, c, _) => capturado = c)
            .ReturnsAsync("<p>x</p>");

        var cmd = Cmd();
        cmd.ProfissionalUsuarioId = profissionalId;
        await _sut.Handle(cmd);

        Assert.That(capturado!.ProfissionalUsuarioId, Is.EqualTo(profissionalId));
        _vinculoRepo.Verify(v => v.PodeAtuarComoProfissional(profissionalId, EstabId), Times.Once);
    }

    [Test]
    public void Handle_ComProfissionalUsuarioIdInvalido_LancaMensagemGenerica()
    {
        var profissionalId = Guid.NewGuid();
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabId)).ReturnsAsync(PacienteAtivo());
        _modeloRepo.Setup(r => r.ObterPorIdDoEstabelecimentoOuNulo(ModeloId, EstabId)).ReturnsAsync(ModeloAtivo());
        _vinculoRepo.Setup(v => v.PodeAtuarComoProfissional(profissionalId, EstabId)).ReturnsAsync(false);

        var cmd = Cmd();
        cmd.ProfissionalUsuarioId = profissionalId;

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        Assert.That(ex!.Message, Is.EqualTo("Profissional inválido."));
        _termoRepo.Verify(r => r.Salvar(It.IsAny<TermoEmitido>()), Times.Never);
    }
}
