using Imedto.Backend.Application.Pacientes.Queries;
using Imedto.Backend.Contracts.Pacientes.Queries;
using Imedto.Backend.Contracts.Pacientes.Queries.Results;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Acessos;

/// <summary>
/// Testes de unidade do handler ListarAcessosDoPacienteQueryHandlers.
/// Verificam: multi-tenant (CA8), audit LGPD (CA10), validações básicas.
/// </summary>
[TestFixture]
public class ListarAcessosDoPacienteQueryHandlerTests
{
    private Mock<IAcessoQueryRepository> _queryRepo;
    private Mock<IPacienteRepository> _pacienteRepo;
    private Mock<IPacienteAcessoLogService> _acessoLog;
    private ListarAcessosDoPacienteQueryHandlers _sut;

    private const long EstabelecimentoId = 1;
    private const long PacienteId = 100;
    private readonly Guid _solicitanteId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _queryRepo = new Mock<IAcessoQueryRepository>();
        _pacienteRepo = new Mock<IPacienteRepository>();
        _acessoLog = new Mock<IPacienteAcessoLogService>();
        _sut = new ListarAcessosDoPacienteQueryHandlers(
            _queryRepo.Object,
            _pacienteRepo.Object,
            _acessoLog.Object);
    }

    private static Paciente PacienteAtivo()
    {
        var p = Paciente.Cadastrar(EstabelecimentoId, "P", null, null,
            GeneroPaciente.NaoInformado, null, null, null, null);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(p, PacienteId);
        return p;
    }

    private ListarAcessosDoPacienteQuery Query() => new()
    {
        PacienteId = PacienteId,
        EstabelecimentoId = EstabelecimentoId,
        SolicitanteUsuarioId = _solicitanteId,
        Pagina = 1,
        TamanhoPagina = 20,
    };

    private PaginaAcessosDto PaginaVazia() => new()
    {
        Itens = Enumerable.Empty<AcessoPacienteResumoDto>(),
        Total = 0, Pagina = 1, TamanhoPagina = 20,
    };

    // ── Caminho feliz ──────────────────────────────────────────────────────

    [Test]
    public async Task Handle_PacienteExistente_RetornaPagina()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync(PacienteAtivo());
        var paginaEsperada = new PaginaAcessosDto
        {
            Itens = new[]
            {
                new AcessoPacienteResumoDto
                {
                    Quem = "Dr. Teste",
                    Quando = DateTime.UtcNow,
                    Recurso = "Cadastro/dados do paciente",
                    Acao = "Visualizou os dados",
                },
            },
            Total = 1, Pagina = 1, TamanhoPagina = 20,
        };
        _queryRepo.Setup(r => r.ListarDoPaciente(PacienteId, EstabelecimentoId, 1, 20))
                  .ReturnsAsync(paginaEsperada);

        var resultado = await _sut.Handle(Query());

        Assert.That(resultado.Total, Is.EqualTo(1));
        Assert.That(resultado.Itens.First().Quem, Is.EqualTo("Dr. Teste"));
    }

    // ── Multi-tenant (CA8/R2) ─────────────────────────────────────────────

    [Test]
    public void Handle_PacienteDeOutroTenant_LancaBusinessException()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync((Paciente)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Query()));

        Assert.That(ex.Message, Is.EqualTo("Paciente não encontrado."));
    }

    [Test]
    public void Handle_PacienteIdZero_LancaBusinessException()
    {
        var query = Query();
        query.PacienteId = 0;

        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(query));
    }

    // ── Audit LGPD (CA10/R4) ──────────────────────────────────────────────

    [Test]
    public async Task Handle_PacienteExistente_RegistraUmAcessoLeitura()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync(PacienteAtivo());
        _queryRepo.Setup(r => r.ListarDoPaciente(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>()))
                  .ReturnsAsync(PaginaVazia());

        await _sut.Handle(Query());

        _acessoLog.Verify(l => l.RegistrarAsync(
            PacienteId, _solicitanteId, EstabelecimentoId, TipoAcessoPaciente.Leitura, null),
            Times.Once);
    }

    [Test]
    public void Handle_PacienteNaoEncontrado_NaoRegistraAudit()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync((Paciente)null);

        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Query()));

        // Audit NÃO deve ser chamado quando o paciente não existe no tenant.
        _acessoLog.Verify(l => l.RegistrarAsync(
            It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<TipoAcessoPaciente>(), It.IsAny<string>()),
            Times.Never);
    }

    // ── Normalização de paginação ─────────────────────────────────────────

    [Test]
    public async Task Handle_PaginaZero_UsaPagina1()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync(PacienteAtivo());

        int paginaPassada = -1;
        _queryRepo.Setup(r => r.ListarDoPaciente(
            It.IsAny<long>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>()))
            .Callback((long _, long _, int pg, int _) => { paginaPassada = pg; })
            .ReturnsAsync(PaginaVazia());

        var query = Query();
        query.Pagina = 0;
        await _sut.Handle(query);

        Assert.That(paginaPassada, Is.EqualTo(1), "Página 0 deve ser normalizada para 1");
    }

    [Test]
    public async Task Handle_TamanhoExcessivo_LimitaA20()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync(PacienteAtivo());

        int tamanhoPassado = -1;
        _queryRepo.Setup(r => r.ListarDoPaciente(
            It.IsAny<long>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>()))
            .Callback((long _, long _, int _, int t) => { tamanhoPassado = t; })
            .ReturnsAsync(PaginaVazia());

        var query = Query();
        query.TamanhoPagina = 501;
        await _sut.Handle(query);

        Assert.That(tamanhoPassado, Is.EqualTo(20), "Tamanho > 500 deve ser normalizado para 20");
    }

    [Test]
    public async Task Handle_TamanhoPagina500_RepassaValorIntegro()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync(PacienteAtivo());

        int tamanhoPassado = -1;
        _queryRepo.Setup(r => r.ListarDoPaciente(
            It.IsAny<long>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>()))
            .Callback((long _, long _, int _, int t) => { tamanhoPassado = t; })
            .ReturnsAsync(PaginaVazia());

        var query = Query();
        query.TamanhoPagina = 500;
        await _sut.Handle(query);

        Assert.That(tamanhoPassado, Is.EqualTo(500), "TamanhoPagina=500 (teto do PDF export) deve chegar íntegro ao repositório");
    }
}
