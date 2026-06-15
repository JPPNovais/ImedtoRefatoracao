using System.Text.Json;
using Imedto.Backend.Application.Migracao.Jobs;
using Imedto.Backend.Application.Prontuarios.Commands;
using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Domain.Prontuarios.Pendencias;
using Imedto.Backend.Infrastructure.Storage;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Migracao;

/// <summary>
/// Testes do CarregarOnda2JobHandler (briefing 2026-06-15_001 — Marco 5).
///
/// Cobrem:
/// - CA13 — bloqueio quando Onda 1 ainda ativa para o tenant.
/// - CA14 — vínculo por CPF e rejeição sem identificador; nunca cria paciente.
/// - CA15 — prontuário sem estrutura → anexo histórico; com conteudo_json → evolução.
/// - CA16 — BusinessException do domínio bloqueia registro de receita inválida.
/// - CA21 — audit é gerado pelos handlers de prontuário (implícito via AutorSistemaId).
/// - CA4/CA20 — motivo de rejeição sem PII.
/// </summary>
[TestFixture]
public class CarregarOnda2JobHandlerTests
{
    // Fakes dos handlers — classes concretas têm construtores complexos.
    // Subclasses sobrecarregam Handle() para capturar chamadas sem dependências reais.

    private sealed class FakeIniciarProntuario : IniciarProntuarioCommandHandler
    {
        public List<IniciarProntuarioCommand> Chamadas { get; } = new();
        public Exception? LancarExcecao { get; set; }

        public FakeIniciarProntuario() : base(
            Mock.Of<IProntuarioRepository>(),
            Mock.Of<IPacienteRepository>(),
            Mock.Of<IModeloDeProntuarioRepository>(),
            Mock.Of<IProntuarioAcessoLogService>(),
            Mock.Of<IEventBus>()) { }

        public override Task Handle(IniciarProntuarioCommand cmd)
        {
            Chamadas.Add(cmd);
            if (LancarExcecao is not null) throw LancarExcecao;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRegistrarEvolucao : RegistrarEvolucaoCommandHandler
    {
        public List<RegistrarEvolucaoCommand> Chamadas { get; } = new();
        public Exception? LancarExcecao { get; set; }
        public long EvolucaoIdRetornado { get; set; } = 77L;

        // PoolExtratorEvolucao e PendenciaExtratorEvolucao são classes concretas — instanciar
        // com repos mockados; nunca são chamadas porque Handle() é sobrescrito.
        public FakeRegistrarEvolucao() : base(
            Mock.Of<IProntuarioRepository>(),
            Mock.Of<IProntuarioEvolucaoRepository>(),
            Mock.Of<IPacienteRepository>(),
            Mock.Of<IModeloDeProntuarioRepository>(),
            Mock.Of<IProntuarioAcessoLogService>(),
            Mock.Of<IEventBus>(),
            new PoolExtratorEvolucao(Mock.Of<IProntuarioVariavelPoolRepository>()),
            new PendenciaExtratorEvolucao(Mock.Of<IPendenciaAtendimentoRepository>())) { }

        public override Task Handle(RegistrarEvolucaoCommand cmd)
        {
            Chamadas.Add(cmd);
            cmd.EvolucaoIdCriada = EvolucaoIdRetornado;
            if (LancarExcecao is not null) throw LancarExcecao;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAdicionarAnexo : AdicionarAnexoCommandHandler
    {
        public List<AdicionarAnexoCommand> Chamadas { get; } = new();
        public long AnexoIdRetornado { get; set; } = 333L;

        public FakeAdicionarAnexo() : base(
            Mock.Of<IProntuarioRepository>(),
            Mock.Of<IProntuarioAnexoRepository>(),
            Mock.Of<IAnexoStorageService>(),
            Mock.Of<IPacienteRepository>(),
            Mock.Of<IProntuarioAcessoLogService>(),
            Options.Create(new StorageOptions
            {
                TamanhoMaxMb = 10,
                MimeTypesPermitidos = ["text/plain", "application/pdf"]
            })) { }

        public override Task Handle(AdicionarAnexoCommand cmd)
        {
            Chamadas.Add(cmd);
            cmd.AnexoIdCriado = AnexoIdRetornado;
            return Task.CompletedTask;
        }
    }

    private Mock<IMigracaoJobRepository>      _jobRepo;
    private Mock<IMigracaoRegistroRepository> _registroRepo;
    private Mock<IMigracaoPacienteLookup>     _pacienteLookup;
    private Mock<IProntuarioRepository>       _prontuarioRepo;
    private FakeIniciarProntuario             _iniciarProntuario;
    private FakeRegistrarEvolucao             _registrarEvolucao;
    private FakeAdicionarAnexo                _adicionarAnexo;

    private CarregarOnda2JobHandler _sut;

    private const long EstabelecimentoId = 42;
    private const long JobId             = 200;
    private const long PacienteId        = 99;
    private const long ProntuarioId      = 55;

    [SetUp]
    public void SetUp()
    {
        _jobRepo           = new Mock<IMigracaoJobRepository>();
        _registroRepo      = new Mock<IMigracaoRegistroRepository>();
        _pacienteLookup    = new Mock<IMigracaoPacienteLookup>();
        _prontuarioRepo    = new Mock<IProntuarioRepository>();
        _iniciarProntuario = new FakeIniciarProntuario();
        _registrarEvolucao = new FakeRegistrarEvolucao();
        _adicionarAnexo    = new FakeAdicionarAnexo();

        _sut = new CarregarOnda2JobHandler(
            _jobRepo.Object,
            _registroRepo.Object,
            _pacienteLookup.Object,
            _prontuarioRepo.Object,
            _iniciarProntuario,
            _registrarEvolucao,
            _adicionarAnexo,
            NullLogger<CarregarOnda2JobHandler>.Instance);
    }

    private MigracaoJob CriarJobMigrandoOnda2()
    {
        var adminId = Guid.NewGuid();
        var job = MigracaoJob.Criar(EstabelecimentoId, Guid.NewGuid(), "iClinic", MigracaoJob.OndaProntuario);
        job.RegistrarArquivoRecebido("migracao/42/200/prontuarios.zip");
        job.MarcarMapaEmRevisao();
        job.MarcarPreviewPronto(adminId);
        job.MarcarMigrando(adminId);
        return job;
    }

    private static MigracaoRegistro CriarRegistro(string entidade, object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        return MigracaoRegistro.Criar(JobId, EstabelecimentoId, entidade, json);
    }

    private void SetupJobMigrando(MigracaoJob job)
    {
        _jobRepo.Setup(r => r.ObterMaisAntigoMigrandoOnda2OuNulo(default)).ReturnsAsync(job);
        _jobRepo.Setup(r => r.ExisteOnda1AtivaParaTenant(EstabelecimentoId, default)).ReturnsAsync(false);
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), default)).Returns(Task.CompletedTask);
        _registroRepo.Setup(r => r.Salvar(It.IsAny<MigracaoRegistro>(), default)).Returns(Task.CompletedTask);
    }

    // ─── CA13 — bloqueio quando Onda 1 ainda ativa ──────────────────────────────

    [Test]
    public async Task ExecutarAsync_QuandoOnda1AindaAtiva_BlockeiaSemProcessar()
    {
        // Arrange — CA13: existe job de Onda 1 em andamento para o mesmo tenant
        var job = CriarJobMigrandoOnda2();
        _jobRepo.Setup(r => r.ObterMaisAntigoMigrandoOnda2OuNulo(default)).ReturnsAsync(job);
        _jobRepo.Setup(r => r.ExisteOnda1AtivaParaTenant(EstabelecimentoId, default)).ReturnsAsync(true);

        // Act
        await _sut.ExecutarAsync(default);

        // Assert — não deve ter processado nenhum registro; status do job não muda
        _registroRepo.Verify(r => r.ListarPorJob(It.IsAny<long>(), default), Times.Never,
            "CA13: job bloqueado não deve listar registros.");
        _jobRepo.Verify(r => r.Salvar(It.IsAny<MigracaoJob>(), default), Times.Never,
            "CA13: job bloqueado não deve ser salvo (fica para próxima rodada do scheduler).");
        Assert.That(job.Status, Is.EqualTo(MigracaoJob.StatusMigrando),
            "CA13: status permanece 'migrando' (não concluí, não falhou).");
    }

    // ─── CA13 — sem job na fila Onda 2, não faz nada ───────────────────────────

    [Test]
    public async Task ExecutarAsync_QuandoNenhumJobOnda2_NaoFazNada()
    {
        _jobRepo.Setup(r => r.ObterMaisAntigoMigrandoOnda2OuNulo(default)).ReturnsAsync((MigracaoJob?)null);

        await _sut.ExecutarAsync(default);

        _registroRepo.Verify(r => r.ListarPorJob(It.IsAny<long>(), default), Times.Never);
    }

    // ─── CA14 — CPF resolve para paciente com prontuário → evolução criada ──────

    [Test]
    public async Task EvolucaoEstruturada_ComCpfEProntuario_CriaEvolucao()
    {
        // Arrange
        var job = CriarJobMigrandoOnda2();
        var reg = CriarRegistro("prontuario_evolucao", new
        {
            cpf = "12345678901",
            conteudo_json = "{\"queixa\":\"dor de cabeça\"}"
        });

        SetupJobMigrando(job);
        _registroRepo.Setup(r => r.ListarPorJob(job.Id, default)).ReturnsAsync([reg]);
        _pacienteLookup.Setup(l => l.ObterPorCpfOuNulo("12345678901", EstabelecimentoId, default))
                       .ReturnsAsync(new PacienteMigracaoInfo(PacienteId, ProntuarioId));

        // Act
        await _sut.ExecutarAsync(default);

        // Assert
        Assert.That(reg.Status, Is.EqualTo("importado_criado"), "CA14: evolução estruturada deve ser criada.");
        Assert.That(reg.EntidadeAlvoId, Is.EqualTo(77L), "CA14: EntidadeAlvoId = id da evolução criada.");
        Assert.That(_registrarEvolucao.Chamadas, Has.Count.EqualTo(1),
            "CA21: handler de evolução chamado exatamente uma vez.");
        var cmd = _registrarEvolucao.Chamadas[0];
        Assert.That(cmd.PacienteId, Is.EqualTo(PacienteId), "CA14: PacienteId correto.");
        Assert.That(cmd.EstabelecimentoId, Is.EqualTo(EstabelecimentoId), "CA2: EstabelecimentoId do job.");
        Assert.That(cmd.AutorUsuarioId, Is.EqualTo(CarregarOnda2JobHandler.AutorSistemaId),
            "CA21: audit usa AutorSistemaId (sem usuário real).");
    }

    // ─── CA14 — sem CPF/documento → rejeitado ("paciente não identificado") ──────

    [Test]
    public async Task EvolucaoEstruturada_SemIdentificador_Rejeita_SemPii()
    {
        // Arrange — CA14 + CA4: sem cpf nem documento_internacional → rejeição sem PII
        var job = CriarJobMigrandoOnda2();
        var reg = CriarRegistro("prontuario_evolucao", new
        {
            conteudo_json = "{\"queixa\":\"tosse\"}"
            // sem cpf nem documento_internacional
        });

        SetupJobMigrando(job);
        _registroRepo.Setup(r => r.ListarPorJob(job.Id, default)).ReturnsAsync([reg]);

        // Act
        await _sut.ExecutarAsync(default);

        // Assert
        Assert.That(reg.Status, Is.EqualTo("rejeitado"), "CA14: sem identificador → rejeitado.");
        Assert.That(reg.MotivoRejeicao, Is.EqualTo("paciente não identificado"),
            "CA4: motivo genérico sem PII.");
        Assert.That(_iniciarProntuario.Chamadas, Is.Empty,
            "CA14: nunca cria prontuário para paciente não identificado.");
        Assert.That(job.Status, Is.EqualTo("concluido_com_erros"));
    }

    // ─── CA14 — CPF não resolve (paciente não existe no tenant) ─────────────────

    [Test]
    public async Task EvolucaoEstruturada_CpfNaoResolvido_Rejeita()
    {
        // Arrange — CA14: CPF presente mas não existe no tenant
        var job = CriarJobMigrandoOnda2();
        var reg = CriarRegistro("prontuario_evolucao", new
        {
            cpf = "99999999999",
            conteudo_json = "{\"queixa\":\"febre\"}"
        });

        SetupJobMigrando(job);
        _registroRepo.Setup(r => r.ListarPorJob(job.Id, default)).ReturnsAsync([reg]);
        _pacienteLookup.Setup(l => l.ObterPorCpfOuNulo("99999999999", EstabelecimentoId, default))
                       .ReturnsAsync((PacienteMigracaoInfo?)null);

        // Act
        await _sut.ExecutarAsync(default);

        // Assert
        Assert.That(reg.Status, Is.EqualTo("rejeitado"), "CA14: CPF não encontrado → rejeitado.");
        Assert.That(reg.MotivoRejeicao, Is.EqualTo("paciente não identificado"),
            "CA4: mensagem genérica sem PII.");
        Assert.That(_iniciarProntuario.Chamadas, Is.Empty,
            "CA14: nunca cria paciente a partir do prontuário.");
    }

    // ─── CA15 — origem sem estrutura (prontuario_anexo) → anexo histórico ───────

    [Test]
    public async Task AnexoHistorico_SemEstrutura_CriaAnexo_NaoEvolucao()
    {
        // Arrange — CA15: entidade prontuario_anexo → nunca cria evolução estruturada
        var job = CriarJobMigrandoOnda2();
        var reg = CriarRegistro("prontuario_anexo", new
        {
            cpf = "12345678901",
            texto_livre = "Paciente apresentou melhora significativa após 3 sessões.",
            nome_arquivo = "consulta_2020.txt"
        });

        SetupJobMigrando(job);
        _registroRepo.Setup(r => r.ListarPorJob(job.Id, default)).ReturnsAsync([reg]);
        _pacienteLookup.Setup(l => l.ObterPorCpfOuNulo("12345678901", EstabelecimentoId, default))
                       .ReturnsAsync(new PacienteMigracaoInfo(PacienteId, ProntuarioId));

        // Act
        await _sut.ExecutarAsync(default);

        // Assert
        Assert.That(reg.Status, Is.EqualTo("importado_criado"), "CA15: anexo histórico criado.");
        Assert.That(_registrarEvolucao.Chamadas, Is.Empty,
            "CA15: não deve criar evolução estruturada para prontuario_anexo.");
        Assert.That(_adicionarAnexo.Chamadas, Has.Count.EqualTo(1));
        var cmdAnexo = _adicionarAnexo.Chamadas[0];
        Assert.That(cmdAnexo.PacienteId, Is.EqualTo(PacienteId));
        Assert.That(cmdAnexo.EstabelecimentoId, Is.EqualTo(EstabelecimentoId));
        Assert.That(cmdAnexo.MimeType, Is.EqualTo("text/plain"));
        Assert.That(cmdAnexo.AutorUsuarioId, Is.EqualTo(CarregarOnda2JobHandler.AutorSistemaId),
            "CA21: audit via AdicionarAnexoCommandHandler com AutorSistemaId.");
    }

    // ─── CA15 — evolução SEM conteudo_json → vira anexo histórico (não evolução) ─

    [Test]
    public async Task EvolucaoSemConteudoJson_ViraAnexo_NaoEvolucaoFalsa()
    {
        // Arrange — CA15: prontuario_evolucao sem conteudo_json → não deve fabricar evolução
        var job = CriarJobMigrandoOnda2();
        var reg = CriarRegistro("prontuario_evolucao", new
        {
            cpf = "11122233344",
            texto_livre = "Atendimento realizado em 2019. Paciente sem queixas relevantes."
            // sem conteudo_json → importado como anexo histórico
        });

        SetupJobMigrando(job);
        _registroRepo.Setup(r => r.ListarPorJob(job.Id, default)).ReturnsAsync([reg]);
        _pacienteLookup.Setup(l => l.ObterPorCpfOuNulo("11122233344", EstabelecimentoId, default))
                       .ReturnsAsync(new PacienteMigracaoInfo(PacienteId, ProntuarioId));

        // Act
        await _sut.ExecutarAsync(default);

        // Assert
        Assert.That(reg.Status, Is.EqualTo("importado_criado"), "CA15: importado como anexo.");
        Assert.That(_registrarEvolucao.Chamadas, Is.Empty,
            "CA15: não deve criar evolução sem estrutura identificável.");
        Assert.That(_adicionarAnexo.Chamadas, Has.Count.EqualTo(1));
    }

    // ─── CA16 — receita com combinação inválida → BusinessException → rejeitado ─

    [Test]
    public async Task EvolucaoComReceitaInvalida_BusinessException_Rejeita()
    {
        // Arrange — CA16: RegistrarEvolucaoCommand lança BusinessException (combinação tipo+notificação)
        var job = CriarJobMigrandoOnda2();
        var reg = CriarRegistro("prontuario_evolucao", new
        {
            cpf = "55566677788",
            conteudo_json = "{\"receita\":{\"tipo\":\"ControlladoB\",\"notificacao\":\"A\"}}"
        });

        SetupJobMigrando(job);
        _registroRepo.Setup(r => r.ListarPorJob(job.Id, default)).ReturnsAsync([reg]);
        _pacienteLookup.Setup(l => l.ObterPorCpfOuNulo("55566677788", EstabelecimentoId, default))
                       .ReturnsAsync(new PacienteMigracaoInfo(PacienteId, ProntuarioId));

        // CA16: domínio rejeita com BusinessException
        _registrarEvolucao.LancarExcecao =
            new BusinessException("Receita: combinação de tipo e notificação inválida.");

        // Act
        await _sut.ExecutarAsync(default);

        // Assert
        Assert.That(reg.Status, Is.EqualTo("rejeitado"), "CA16: BusinessException → rejeitado.");
        Assert.That(reg.MotivoRejeicao, Does.Contain("tipo e notificação").IgnoreCase,
            "CA16: motivo descreve o problema sem PII do paciente.");
        Assert.That(job.Status, Is.EqualTo("concluido_com_erros"));
    }

    // ─── CA14 — documento internacional resolve vínculo ─────────────────────────

    [Test]
    public async Task EvolucaoEstruturada_ComDocumentoInternacional_CriaEvolucao()
    {
        // Arrange — CA14: quando CPF ausente mas doc_internacional presente
        var job = CriarJobMigrandoOnda2();
        var reg = CriarRegistro("prontuario_evolucao", new
        {
            documento_internacional = "PASSP-US-123456",
            conteudo_json = "{\"observacao\":\"Paciente estrangeiro\"}"
        });

        SetupJobMigrando(job);
        _registroRepo.Setup(r => r.ListarPorJob(job.Id, default)).ReturnsAsync([reg]);
        _pacienteLookup.Setup(l => l.ObterPorDocumentoInternacionalOuNulo("PASSP-US-123456", EstabelecimentoId, default))
                       .ReturnsAsync(new PacienteMigracaoInfo(PacienteId, ProntuarioId));

        // Act
        await _sut.ExecutarAsync(default);

        // Assert
        Assert.That(reg.Status, Is.EqualTo("importado_criado"));
        _pacienteLookup.Verify(l => l.ObterPorCpfOuNulo(It.IsAny<string>(), It.IsAny<long>(), default), Times.Never,
            "CA14: CPF não tenta busca quando ausente no payload.");
        _pacienteLookup.Verify(l => l.ObterPorDocumentoInternacionalOuNulo("PASSP-US-123456", EstabelecimentoId, default), Times.Once);
    }

    // ─── Multi-tenant (CA2) — sempre usa estabelecimentoId do job ────────────────

    [Test]
    public async Task Execucao_UsaEstabelecimentoIdDoJob_NaoOutroTenant()
    {
        // Arrange
        var job = CriarJobMigrandoOnda2();
        var reg = CriarRegistro("prontuario_evolucao", new
        {
            cpf = "10203040506",
            conteudo_json = "{\"q\":\"test\"}"
        });

        SetupJobMigrando(job);
        _registroRepo.Setup(r => r.ListarPorJob(job.Id, default)).ReturnsAsync([reg]);
        _pacienteLookup.Setup(l => l.ObterPorCpfOuNulo(It.IsAny<string>(), EstabelecimentoId, default))
                       .ReturnsAsync(new PacienteMigracaoInfo(PacienteId, ProntuarioId));

        // Act
        await _sut.ExecutarAsync(default);

        // Assert — todas as chamadas usaram EstabelecimentoId do job (42)
        _pacienteLookup.Verify(
            l => l.ObterPorCpfOuNulo(It.IsAny<string>(), EstabelecimentoId, default),
            Times.Once,
            "CA2: lookup usa estabelecimentoId do job, não valor fixo ou de outro tenant.");
        Assert.That(_registrarEvolucao.Chamadas[0].EstabelecimentoId, Is.EqualTo(EstabelecimentoId),
            "CA2: command de evolução carrega EstabelecimentoId do job.");
    }

    // ─── Entidade desconhecida → pulada (não rejeita o job) ─────────────────────

    [Test]
    public async Task EntidadeDesconhecida_MarcaComoNaoSuportada_NaoFalhaJob()
    {
        var job = CriarJobMigrandoOnda2();
        var reg = CriarRegistro("entidade_onda1_aqui", new { cpf = "000" });

        SetupJobMigrando(job);
        _registroRepo.Setup(r => r.ListarPorJob(job.Id, default)).ReturnsAsync([reg]);

        await _sut.ExecutarAsync(default);

        Assert.That(reg.Status, Is.EqualTo("pulado"));
        Assert.That(reg.MotivoRejeicao, Is.EqualTo("entidade não suportada nesta onda"));
        Assert.That(job.Status, Is.EqualTo("concluido"),
            "Entidade pulada não conta como rejeição — job concluído normalmente.");
    }

    // ─── CA15 — paciente sem prontuário → inicia antes de criar evolução ─────────

    [Test]
    public async Task EvolucaoEstruturada_PacienteSemProntuario_IniciaECriaEvolucao()
    {
        // Arrange — CA15: ProntuarioId = null → handler inicia o prontuário antes da evolução
        var job = CriarJobMigrandoOnda2();
        var reg = CriarRegistro("prontuario_evolucao", new
        {
            cpf = "77788899900",
            conteudo_json = "{\"pressao\":\"120/80\"}"
        });

        SetupJobMigrando(job);
        _registroRepo.Setup(r => r.ListarPorJob(job.Id, default)).ReturnsAsync([reg]);

        // Paciente existe mas SEM prontuário ainda
        _pacienteLookup.Setup(l => l.ObterPorCpfOuNulo("77788899900", EstabelecimentoId, default))
                       .ReturnsAsync(new PacienteMigracaoInfo(PacienteId, ProntuarioId: null));
        _pacienteLookup.Setup(l => l.ObterIdModeloPadraoProntuarioOuNulo(EstabelecimentoId, default))
                       .ReturnsAsync(1L);

        // Prontuário "iniciado" — ObterPorPaciente retorna um fake com Id
        var prontuarioFake = Prontuario.Iniciar(PacienteId, EstabelecimentoId, 1L);
        typeof(Prontuario).GetProperty("Id")!.SetValue(prontuarioFake, ProntuarioId);
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
                       .ReturnsAsync(prontuarioFake);

        // Act
        await _sut.ExecutarAsync(default);

        // Assert — iniciou prontuário e depois criou evolução
        Assert.That(_iniciarProntuario.Chamadas, Has.Count.EqualTo(1),
            "CA15: inicia prontuário quando paciente não tem ainda.");
        var cmdIniciar = _iniciarProntuario.Chamadas[0];
        Assert.That(cmdIniciar.PacienteId, Is.EqualTo(PacienteId));
        Assert.That(cmdIniciar.EstabelecimentoId, Is.EqualTo(EstabelecimentoId));
        Assert.That(cmdIniciar.ModeloDeProntuarioId, Is.EqualTo(1L));
        Assert.That(cmdIniciar.SolicitanteUsuarioId, Is.EqualTo(CarregarOnda2JobHandler.AutorSistemaId));
        Assert.That(reg.Status, Is.EqualTo("importado_criado"));
    }
}
