using Imedto.Backend.Application.Termos.Commands;
using Imedto.Backend.Contracts.Termos.Commands;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Termos;
using Imedto.Backend.Domain.Termos.Events;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Termos;

[TestFixture]
public class EmitirTermoCommandHandlerTests
{
    private Mock<ITermoEmitidoRepository> _termoRepo = null!;
    private Mock<ITermoModeloRepository> _modeloRepo = null!;
    private Mock<IPacienteRepository> _pacienteRepo = null!;
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
        _resolver = new Mock<ITermoResolverDeVariaveis>();
        _sanitizer = new Mock<ITermoHtmlSanitizer>();
        _texto = new Mock<ITermoTextoExtractor>();
        _audit = new Mock<ITermoAuditLogger>();
        _eventBus = new Mock<IEventBus>();
        _sut = new EmitirTermoCommandHandler(
            _termoRepo.Object, _modeloRepo.Object, _pacienteRepo.Object,
            _resolver.Object, _sanitizer.Object, _texto.Object,
            _audit.Object, _eventBus.Object);
    }

    private static Paciente PacienteAtivo()
    {
        var p = Paciente.Cadastrar(EstabId, "Paciente", null, null,
            GeneroPaciente.NaoInformado, null, null, null, null);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(p, PacienteId);
        return p;
    }

    private static TermoModelo ModeloAtivo()
    {
        var m = TermoModelo.CriarDoEstabelecimento(EstabId, Guid.NewGuid(), CategoriaTermo.Lgpd, "Termo teste", "<p>x</p>");
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(m, ModeloId);
        return m;
    }

    private EmitirTermoCommand Cmd(string tipo = "pdf_anexado") => new()
    {
        PacienteId = PacienteId,
        EstabelecimentoId = EstabId,
        EmissorUsuarioId = _emissor,
        ModeloId = ModeloId,
        AssinaturaTipo = tipo,
    };

    [Test]
    public async Task Handle_FluxoValido_PersisteSnapshotEPublicaEvento()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabId)).ReturnsAsync(PacienteAtivo());
        _modeloRepo.Setup(r => r.ObterPorIdDoEstabelecimentoOuNulo(ModeloId, EstabId)).ReturnsAsync(ModeloAtivo());
        _resolver.Setup(r => r.ResolverAsync(It.IsAny<string>(), It.IsAny<ContextoDeVariaveis>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<p>resolvido</p>");
        _sanitizer.Setup(s => s.Sanitizar("<p>resolvido</p>")).Returns("<p>resolvido</p>");
        _texto.Setup(t => t.Extrair(It.IsAny<string>())).Returns("resolvido");
        _termoRepo.Setup(r => r.Salvar(It.IsAny<TermoEmitido>()))
            .Callback<TermoEmitido>(t => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(t, 7L))
            .Returns(Task.CompletedTask);

        var cmd = Cmd();
        await _sut.Handle(cmd);

        Assert.That(cmd.TermoEmitidoId, Is.EqualTo(7L));
        Assert.That(cmd.TokenAceiteGerado, Is.Null);
        _eventBus.Verify(b => b.Publish(It.Is<IDomainEvent>(e => e is TermoEmitidoEvent)), Times.Once);
        _audit.Verify(a => a.RegistrarAsync(EstabId, _emissor, "termo-emitido", "TermoEmitido", 7L,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_AssinaturaAceiteLink_GeraTokenEnoCommand()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabId)).ReturnsAsync(PacienteAtivo());
        _modeloRepo.Setup(r => r.ObterPorIdDoEstabelecimentoOuNulo(ModeloId, EstabId)).ReturnsAsync(ModeloAtivo());
        _resolver.Setup(r => r.ResolverAsync(It.IsAny<string>(), It.IsAny<ContextoDeVariaveis>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<p>resolvido</p>");
        _sanitizer.Setup(s => s.Sanitizar(It.IsAny<string>())).Returns("<p>resolvido</p>");
        _texto.Setup(t => t.Extrair(It.IsAny<string>())).Returns("resolvido");
        _termoRepo.Setup(r => r.Salvar(It.IsAny<TermoEmitido>()))
            .Callback<TermoEmitido>(t => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(t, 8L))
            .Returns(Task.CompletedTask);

        var cmd = Cmd("aceite_link");
        await _sut.Handle(cmd);

        Assert.That(cmd.TokenAceiteGerado, Is.Not.Null.And.Not.Empty);
    }

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
        _termoRepo.Setup(r => r.Salvar(It.IsAny<TermoEmitido>()))
            .Callback<TermoEmitido>(t => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(t, 11L))
            .Returns(Task.CompletedTask);

        await _sut.Handle(Cmd());
        _termoRepo.Verify(r => r.Salvar(It.IsAny<TermoEmitido>()), Times.Once);
    }
}
