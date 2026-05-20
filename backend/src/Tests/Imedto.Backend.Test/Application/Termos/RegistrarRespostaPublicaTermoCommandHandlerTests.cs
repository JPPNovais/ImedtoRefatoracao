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

/// <summary>
/// Tests do handler de aceite/recusa público (Fase 4). Garante:
///   - Token inválido / expirado / termo não pendente: mensagem genérica
///     ([[link-invalido]] — todos os erros retornam a mesma frase).
///   - Idempotência: termo já assinado retorna RespondidoAgora=false, sem mudar estado.
///   - Hash de integridade divergente: erro técnico (InvalidOperationException).
///   - Nome confirmado: case/acento insensitivo.
///   - Eventos disparados ao aceitar/recusar (notificação do emissor).
/// </summary>
[TestFixture]
public class RegistrarRespostaPublicaTermoCommandHandlerTests
{
    private Mock<ITermoEmitidoRepository> _termoRepo = null!;
    private Mock<IPacienteRepository> _pacienteRepo = null!;
    private Mock<IEventBus> _eventBus = null!;
    private RegistrarRespostaPublicaTermoCommandHandler _sut = null!;

    private const long EstabId = 1;
    private const long PacienteId = 10;
    private const long TermoId = 99;

    [SetUp]
    public void SetUp()
    {
        _termoRepo = new Mock<ITermoEmitidoRepository>();
        _pacienteRepo = new Mock<IPacienteRepository>();
        _eventBus = new Mock<IEventBus>();
        _sut = new RegistrarRespostaPublicaTermoCommandHandler(_termoRepo.Object, _pacienteRepo.Object, _eventBus.Object);
    }

    private TermoEmitido CriarTermoPendente(string snapshot = "<p>termo</p>")
    {
        var termo = TermoEmitido.Emitir(
            pacienteId: PacienteId,
            estabelecimentoId: EstabId,
            termoModeloId: 5,
            versaoModelo: 1,
            conteudoResolvidoHtml: snapshot,
            conteudoResolvidoTexto: "termo",
            assinaturaTipo: AssinaturaTipo.AceiteLink,
            emitidoPorUsuarioId: Guid.NewGuid(),
            ttlLinkPublico: TimeSpan.FromDays(30));
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(termo, TermoId);
        return termo;
    }

    private RegistrarRespostaPublicaTermoCommand Cmd(string token, bool aceito = true, string nome = null) => new()
    {
        TokenAceite = token,
        Aceito = aceito,
        NomeConfirmado = nome,
        IpOrigem = "127.0.0.1",
        UserAgent = "test-ua",
    };

    [Test]
    public void Handle_TokenVazio_LancaMensagemGenerica()
    {
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd(" ")));
        Assert.That(ex!.Message, Is.EqualTo(RegistrarRespostaPublicaTermoCommandHandler.MensagemLinkInvalido));
    }

    [Test]
    public void Handle_TokenInexistente_LancaMensagemGenerica()
    {
        _termoRepo.Setup(r => r.ObterPorTokenOuNulo("tk")).ReturnsAsync((TermoEmitido)null);
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd("tk")));
        Assert.That(ex!.Message, Is.EqualTo(RegistrarRespostaPublicaTermoCommandHandler.MensagemLinkInvalido));
    }

    [Test]
    public async Task Handle_TermoJaAssinado_RetornaIdempotenteSemMudarEstado()
    {
        var termo = CriarTermoPendente();
        termo.RegistrarAceitePublico("1.2.3.4", "ua-anterior");
        _termoRepo.Setup(r => r.ObterPorTokenOuNulo("tk")).ReturnsAsync(termo);

        var cmd = Cmd("tk");
        await _sut.Handle(cmd);

        Assert.That(cmd.Resultado, Is.EqualTo(ResultadoRespostaPublica.JaRespondido));
        _termoRepo.Verify(r => r.Salvar(It.IsAny<TermoEmitido>()), Times.Never);
        _termoRepo.Verify(r => r.SalvarAcessoLog(It.IsAny<TermoEmitidoAcessoLog>()), Times.Once);
        _eventBus.Verify(b => b.Publish(It.IsAny<IDomainEvent>()), Times.Never);
    }

    [Test]
    public void Handle_HashIntegridadeDivergente_LancaInvalidOperation()
    {
        var termo = CriarTermoPendente();
        // Mexer no snapshot depois de gerado simula corrupção do banco.
        typeof(TermoEmitido).GetProperty(nameof(TermoEmitido.ConteudoSnapshotHtml))!
            .SetValue(termo, "<p>conteudo-alterado</p>");
        _termoRepo.Setup(r => r.ObterPorTokenOuNulo("tk")).ReturnsAsync(termo);

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.Handle(Cmd("tk")));
    }

    [Test]
    public async Task Handle_AceiteValido_MudaStatusEDisparaEvento()
    {
        var termo = CriarTermoPendente();
        _termoRepo.Setup(r => r.ObterPorTokenOuNulo("tk")).ReturnsAsync(termo);

        var cmd = Cmd("tk", aceito: true);
        await _sut.Handle(cmd);

        Assert.That(termo.Status, Is.EqualTo(StatusTermoEmitido.Assinado));
        Assert.That(cmd.Resultado, Is.EqualTo(ResultadoRespostaPublica.RespondidoAgora));
        _termoRepo.Verify(r => r.Salvar(termo), Times.Once);
        _eventBus.Verify(b => b.Publish(It.Is<IDomainEvent>(e => e is TermoAssinadoEvent)), Times.Once);
    }

    [Test]
    public async Task Handle_RecusaValida_MudaStatusEDisparaEvento()
    {
        var termo = CriarTermoPendente();
        _termoRepo.Setup(r => r.ObterPorTokenOuNulo("tk")).ReturnsAsync(termo);

        var cmd = Cmd("tk", aceito: false);
        await _sut.Handle(cmd);

        Assert.That(termo.Status, Is.EqualTo(StatusTermoEmitido.Recusado));
        _eventBus.Verify(b => b.Publish(It.Is<IDomainEvent>(e => e is TermoRecusadoEvent)), Times.Once);
    }

    [Test]
    public async Task Handle_NomeConfirmadoIgnoraAcentoCase()
    {
        var termo = CriarTermoPendente();
        var paciente = Paciente.Cadastrar(EstabId, "João da Silva", null, null,
            GeneroPaciente.NaoInformado, null, null, null, null);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(paciente, PacienteId);
        _termoRepo.Setup(r => r.ObterPorTokenOuNulo("tk")).ReturnsAsync(termo);
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabId)).ReturnsAsync(paciente);

        await _sut.Handle(Cmd("tk", nome: "JOAO DA SILVA"));

        Assert.That(termo.Status, Is.EqualTo(StatusTermoEmitido.Assinado));
    }

    [Test]
    public void Handle_NomeConfirmadoIncorreto_Lanca422()
    {
        var termo = CriarTermoPendente();
        var paciente = Paciente.Cadastrar(EstabId, "João da Silva", null, null,
            GeneroPaciente.NaoInformado, null, null, null, null);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(paciente, PacienteId);
        _termoRepo.Setup(r => r.ObterPorTokenOuNulo("tk")).ReturnsAsync(termo);
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabId)).ReturnsAsync(paciente);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd("tk", nome: "Maria Souza")));
        Assert.That(ex!.Message, Does.Contain("nome completo"));
    }

    [Test]
    public void NomesEquivalentes_IgnoraAcentoCaseEEspacosMultiplos()
    {
        Assert.That(RegistrarRespostaPublicaTermoCommandHandler.NomesEquivalentes("João  da Silva", "joao da silva"), Is.True);
        Assert.That(RegistrarRespostaPublicaTermoCommandHandler.NomesEquivalentes("Ana Pérez", "Ana Perez"), Is.True);
        Assert.That(RegistrarRespostaPublicaTermoCommandHandler.NomesEquivalentes("João Silva", "Maria Silva"), Is.False);
    }
}
