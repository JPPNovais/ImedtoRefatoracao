using Imedto.Backend.Application.Admin.Migracao;
using Imedto.Backend.Contracts.Admin.Migracao;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Admin.Migracao;

[TestFixture]
public class SalvarMapaRevisadoCommandHandlerTests
{
    private Mock<IMigracaoMapaRepository> _mapaRepo;
    private SalvarMapaRevisadoCommandHandler _sut;
    private static readonly Guid AdminId = Guid.NewGuid();
    private const long JobId = 10;
    private const string Entidade = "paciente";

    [SetUp]
    public void SetUp()
    {
        _mapaRepo = new Mock<IMigracaoMapaRepository>();
        _sut = new SalvarMapaRevisadoCommandHandler(_mapaRepo.Object);
    }

    [Test]
    public async Task Handle_MapaExistente_RevisaESalva()
    {
        var mapa = MigracaoMapa.Criar(JobId, 42, Entidade, "{\"de_para\":{},\"confianca\":0.5,\"duvidas\":[]}");

        // Sem bloco → usa ObterPorJobEntidadeBlocoAdminOuNulo com nomeBlocoOrigem = "".
        _mapaRepo.Setup(r => r.ObterPorJobEntidadeBlocoAdminOuNulo(
                JobId, Entidade, string.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mapa);

        var cmd = new SalvarMapaRevisadoCommand
        {
            JobId            = JobId,
            Entidade         = Entidade,
            DePara           = new Dictionary<string, string> { ["nome"] = "nome", ["cpf"] = "cpf" },
            RevisadoPorUsuarioId = AdminId,
        };

        await _sut.Handle(cmd);

        _mapaRepo.Verify(r => r.Salvar(mapa, It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(mapa.RevisadoPorUsuarioId, Is.EqualTo(AdminId));
        Assert.That(mapa.RevisadoEm, Is.Not.Null);
    }

    [Test]
    public void Handle_MapaNaoEncontrado_LancaBusinessException()
    {
        _mapaRepo.Setup(r => r.ObterPorJobEntidadeBlocoAdminOuNulo(
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MigracaoMapa?)null);

        var cmd = new SalvarMapaRevisadoCommand
        {
            JobId = JobId, Entidade = Entidade,
            DePara = [], RevisadoPorUsuarioId = AdminId,
        };

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        Assert.That(ex!.Message, Does.Contain("não encontrado"));
    }

    [Test]
    public void Handle_AdminIdVazio_LancaBusinessException()
    {
        var cmd = new SalvarMapaRevisadoCommand
        {
            JobId = JobId, Entidade = Entidade,
            DePara = [], RevisadoPorUsuarioId = Guid.Empty,
        };

        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
    }

    // ─── Addendum 4 (Tipo A — regressão do bug corrigido) ────────────────────

    [Test]
    public async Task Handle_ComNomeBlocoOrigem_UsaMetodoAdminSemGuardDeTenant()
    {
        // Verifica CA77: operador salva mapa de dump aninhado — NomeBlocoOrigem preenchido.
        // O bug era: ObterPorJobEntidadeBlocoOuNulo (com guard) era chamado com estabelecimentoId=0 → 500.
        const string bloco = "clientes";
        var mapaJson = "{\"de_para\":{},\"confianca\":0.5,\"duvidas\":[],\"entidade_classificada\":\"agendamento\"}";
        var mapa = MigracaoMapa.Criar(JobId, 42, Entidade, mapaJson, bloco);

        _mapaRepo.Setup(r => r.ObterPorJobEntidadeBlocoAdminOuNulo(
                JobId, Entidade, bloco, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mapa);

        var cmd = new SalvarMapaRevisadoCommand
        {
            JobId                  = JobId,
            Entidade               = Entidade,
            NomeBlocoOrigem        = bloco,
            EntidadeReclassificada = "paciente", // operador corrige de agendamento → paciente
            DePara                 = new Dictionary<string, string> { ["nome"] = "nome" },
            Ignorado               = false,
            RevisadoPorUsuarioId   = AdminId,
        };

        // Não deve lançar InvalidOperationException nem InvalidCastException.
        await _sut.Handle(cmd);

        // Handler deve usar ObterPorJobEntidadeBlocoAdminOuNulo (sem guard de tenant).
        _mapaRepo.Verify(r => r.ObterPorJobEntidadeBlocoAdminOuNulo(
            JobId, Entidade, bloco, It.IsAny<CancellationToken>()), Times.Once);

        // Deve ter chamado Salvar com o mapa revisado.
        _mapaRepo.Verify(r => r.Salvar(mapa, It.IsAny<CancellationToken>()), Times.Once);

        // O mapa deve conter entidade_operador = "paciente" no JSON.
        Assert.That(mapa.MapaJson, Does.Contain("\"entidade_operador\""));
        Assert.That(mapa.MapaJson, Does.Contain("paciente"));
        // entidade_classificada original (agendamento) é preservada.
        Assert.That(mapa.MapaJson, Does.Contain("\"entidade_classificada\""));
        Assert.That(mapa.MapaJson, Does.Contain("agendamento"));
    }

    [Test]
    public async Task Handle_ComCamposJsonPreservados_NaoLancaInvalidCastException()
    {
        // Verifica que JsonElement não causa InvalidCastException ao ler confianca_classificacao/encoding_suspeito.
        // Bug anterior: Convert.ToDouble(JsonElement) → InvalidCastException.
        const string mapaJsonComCampos = """
            {
                "de_para": {},
                "confianca": 0.85,
                "duvidas": [],
                "entidade_classificada": "agendamento",
                "confianca_classificacao": 0.9,
                "encoding_suspeito": true,
                "eh_config": false
            }
            """;
        var mapa = MigracaoMapa.Criar(JobId, 42, Entidade, mapaJsonComCampos);

        _mapaRepo.Setup(r => r.ObterPorJobEntidadeBlocoAdminOuNulo(
                JobId, Entidade, string.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mapa);

        var cmd = new SalvarMapaRevisadoCommand
        {
            JobId            = JobId,
            Entidade         = Entidade,
            DePara           = [],
            RevisadoPorUsuarioId = AdminId,
        };

        // Não deve lançar.
        Assert.DoesNotThrowAsync(() => _sut.Handle(cmd));

        // Campos preservados no JSON de saída.
        await _sut.Handle(cmd);
        Assert.That(mapa.MapaJson, Does.Contain("\"confianca_classificacao\""));
        Assert.That(mapa.MapaJson, Does.Contain("0.9"));
        Assert.That(mapa.MapaJson, Does.Contain("\"encoding_suspeito\":true"));
    }

    [Test]
    public void Handle_EntidadeReclassificadaInvalida_LancaBusinessException()
    {
        // CA77: entidade fora da lista canônica → 422.
        var cmd = new SalvarMapaRevisadoCommand
        {
            JobId                  = JobId,
            Entidade               = Entidade,
            EntidadeReclassificada = "entidade_inventada",
            DePara                 = [],
            RevisadoPorUsuarioId   = AdminId,
        };

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        Assert.That(ex!.Message, Does.Contain("inválida"));
    }
}
