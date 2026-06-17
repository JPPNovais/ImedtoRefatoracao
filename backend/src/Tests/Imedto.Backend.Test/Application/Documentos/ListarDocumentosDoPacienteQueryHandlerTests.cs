using Imedto.Backend.Application.Pacientes.Queries;
using Imedto.Backend.Contracts.Pacientes.Queries;
using Imedto.Backend.Contracts.Pacientes.Queries.Results;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Documentos;

/// <summary>
/// Testes de unidade do handler ListarDocumentosDoPacienteQueryHandlers.
/// Verificam: multi-tenant, audit LGPD, somente-emitidos (responsabilidade do repo),
/// R11 (busca vazia = no-op), R13 (busca não vai ao audit).
/// </summary>
[TestFixture]
public class ListarDocumentosDoPacienteQueryHandlerTests
{
    private Mock<IDocumentoQueryRepository> _queryRepo;
    private Mock<IPacienteRepository> _pacienteRepo;
    private Mock<IProntuarioRepository> _prontuarioRepo;
    private Mock<IProntuarioAcessoLogService> _acessoLog;
    private ListarDocumentosDoPacienteQueryHandlers _sut;

    private const long EstabelecimentoId = 1;
    private const long PacienteId = 100;
    private const long ProntuarioId = 200;
    private readonly Guid _solicitanteId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _queryRepo = new Mock<IDocumentoQueryRepository>();
        _pacienteRepo = new Mock<IPacienteRepository>();
        _prontuarioRepo = new Mock<IProntuarioRepository>();
        _acessoLog = new Mock<IProntuarioAcessoLogService>();
        _sut = new ListarDocumentosDoPacienteQueryHandlers(
            _queryRepo.Object,
            _pacienteRepo.Object,
            _prontuarioRepo.Object,
            _acessoLog.Object);
    }

    private static Paciente PacienteAtivo()
    {
        var p = Paciente.Cadastrar(EstabelecimentoId, "P", null, null,
            GeneroPaciente.NaoInformado, null, null, null, null);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(p, PacienteId);
        return p;
    }

    private static Prontuario ProntuarioJaIniciado()
    {
        var p = Prontuario.Iniciar(PacienteId, EstabelecimentoId, 1L);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(p, ProntuarioId);
        return p;
    }

    private ListarDocumentosDoPacienteQuery Query(string? busca = null, string? tipo = null) => new()
    {
        PacienteId = PacienteId,
        EstabelecimentoId = EstabelecimentoId,
        SolicitanteUsuarioId = _solicitanteId,
        Pagina = 1,
        TamanhoPagina = 10,
        Busca = busca,
        Tipo = tipo,
    };

    private PaginaDocumentosDto PaginaVazia() => new()
    {
        Itens = Enumerable.Empty<DocumentoResumoDto>(),
        Total = 0,
        Pagina = 1,
        TamanhoPagina = 10,
    };

    // ── Caminho feliz ──────────────────────────────────────────────────────

    [Test]
    public async Task Handle_PacienteExistente_RetornaPagina()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync(PacienteAtivo());
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
                       .ReturnsAsync(ProntuarioJaIniciado());
        var paginaEsperada = new PaginaDocumentosDto
        {
            Itens = new[] { new DocumentoResumoDto { Tipo = "Receita", Id = 1, Titulo = "Receita Comum", Data = DateTime.UtcNow } },
            Total = 1, Pagina = 1, TamanhoPagina = 10,
        };
        _queryRepo.Setup(r => r.ListarDoPaciente(
            PacienteId, EstabelecimentoId, 1, 10,
            null, null, null, null))
            .ReturnsAsync(paginaEsperada);

        var resultado = await _sut.Handle(Query());

        Assert.That(resultado.Total, Is.EqualTo(1));
        Assert.That(resultado.Itens.First().Tipo, Is.EqualTo("Receita"));
    }

    // ── Multi-tenant (CA10/R2) ─────────────────────────────────────────────

    [Test]
    public void Handle_PacienteDeOutroTenant_LancaBusinessException()
    {
        // Paciente não encontrado no tenant do solicitante → retorno nulo
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync((Paciente)null);

        var ex = Assert.ThrowsAsync<BusinessException>(
            () => _sut.Handle(Query()));

        Assert.That(ex.Message, Is.EqualTo("Paciente não encontrado."));
    }

    [Test]
    public void Handle_PacienteIdZero_LancaBusinessException()
    {
        var query = Query();
        query.PacienteId = 0;

        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(query));
    }

    // ── Audit LGPD (CA12/CA30) ─────────────────────────────────────────────

    [Test]
    public async Task Handle_ComProntuario_RegistraUmAcessoDeLeitura()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync(PacienteAtivo());
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
                       .ReturnsAsync(ProntuarioJaIniciado());
        _queryRepo.Setup(r => r.ListarDoPaciente(
            It.IsAny<long>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>()))
            .ReturnsAsync(PaginaVazia());

        await _sut.Handle(Query());

        _acessoLog.Verify(l => l.RegistrarAsync(
            ProntuarioId, _solicitanteId, EstabelecimentoId, TipoAcessoProntuario.Leitura),
            Times.Once);
    }

    [Test]
    public async Task Handle_SemProntuario_NaoRegistraAcessoNaoLancaExcecao()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync(PacienteAtivo());
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
                       .ReturnsAsync((Prontuario)null);
        _queryRepo.Setup(r => r.ListarDoPaciente(
            It.IsAny<long>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>()))
            .ReturnsAsync(PaginaVazia());

        await _sut.Handle(Query());

        _acessoLog.Verify(l => l.RegistrarAsync(
            It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<TipoAcessoProntuario>()),
            Times.Never);
    }

    // ── Busca: R11 — vazia é no-op ─────────────────────────────────────────

    [Test]
    public async Task Handle_BuscaVazia_PassaNuloAoRepositorio()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync(PacienteAtivo());
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
                       .ReturnsAsync((Prontuario)null);

        string buscaPassada = "NAO_CHAMADO";
        _queryRepo.Setup(r => r.ListarDoPaciente(
            It.IsAny<long>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>()))
            .Callback((long _, long _, int _, int _, string _, DateTime? _, DateTime? _, string b) =>
            {
                buscaPassada = b;
            })
            .ReturnsAsync(PaginaVazia());

        await _sut.Handle(Query(busca: "   ")); // só espaços → no-op

        Assert.That(buscaPassada, Is.Null, "Busca com só espaços deve ser normalizada para null");
    }

    [Test]
    public async Task Handle_BuscaNula_PassaNuloAoRepositorio()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync(PacienteAtivo());
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
                       .ReturnsAsync((Prontuario)null);

        string buscaPassada = "NAO_CHAMADO";
        _queryRepo.Setup(r => r.ListarDoPaciente(
            It.IsAny<long>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>()))
            .Callback((long _, long _, int _, int _, string _, DateTime? _, DateTime? _, string b) =>
            {
                buscaPassada = b;
            })
            .ReturnsAsync(PaginaVazia());

        await _sut.Handle(Query(busca: null));

        Assert.That(buscaPassada, Is.Null);
    }

    [Test]
    public async Task Handle_BuscaComTermo_PassaTermoTrimadoAoRepositorio()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync(PacienteAtivo());
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
                       .ReturnsAsync((Prontuario)null);

        string buscaPassada = null;
        _queryRepo.Setup(r => r.ListarDoPaciente(
            It.IsAny<long>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>()))
            .Callback((long _, long _, int _, int _, string _, DateTime? _, DateTime? _, string b) =>
            {
                buscaPassada = b;
            })
            .ReturnsAsync(PaginaVazia());

        await _sut.Handle(Query(busca: "  dipirona  "));

        Assert.That(buscaPassada, Is.EqualTo("dipirona"), "Busca deve ser trimada antes de passar ao repositório");
    }

    // ── Filtro de tipo repassado ao repositório (CA4) ─────────────────────

    [Test]
    public async Task Handle_FiltroTipoReceita_RepassaTipoAoRepositorio()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync(PacienteAtivo());
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
                       .ReturnsAsync((Prontuario)null);

        string tipoPassado = null;
        _queryRepo.Setup(r => r.ListarDoPaciente(
            It.IsAny<long>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>()))
            .Callback((long _, long _, int _, int _, string t, DateTime? _, DateTime? _, string _) =>
            {
                tipoPassado = t;
            })
            .ReturnsAsync(PaginaVazia());

        await _sut.Handle(Query(tipo: "Receita"));

        Assert.That(tipoPassado, Is.EqualTo("Receita"));
    }
}
