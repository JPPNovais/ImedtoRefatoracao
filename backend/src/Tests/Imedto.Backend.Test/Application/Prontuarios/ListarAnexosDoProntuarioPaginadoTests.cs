using Imedto.Backend.Application.Prontuarios.Queries;
using Imedto.Backend.Contracts.Prontuarios.Queries;
using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Tenancy;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Prontuarios;

/// <summary>
/// Testes do handler de listagem paginada de anexos (Item 19).
/// Garante retrocompatibilidade (sem params = página 1/50 itens),
/// paginação correta e audit LGPD.
/// </summary>
[TestFixture]
public class ListarAnexosDoProntuarioPaginadoTests
{
    private Mock<ProntuarioAnexoQueryRepository> _repo;
    private Mock<IProntuarioRepository> _prontuarioRepo;
    private Mock<IProntuarioAcessoLogService> _acessoLog;
    private ListarAnexosDoProntuarioQueryHandlers _sut;

    private const long EstabelecimentoId = 1;
    private const long PacienteId = 10;
    private const long ProntuarioId = 20;
    private readonly Guid _solicitanteId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<ProntuarioAnexoQueryRepository>(new AppReadConnectionString("Host=ignored"));
        _prontuarioRepo = new Mock<IProntuarioRepository>();
        _acessoLog = new Mock<IProntuarioAcessoLogService>();
        _sut = new ListarAnexosDoProntuarioQueryHandlers(_repo.Object, _prontuarioRepo.Object, _acessoLog.Object);

        // Prontuário existe por padrão nos testes.
        // Usamos Prontuario.Iniciar e configuramos o mock do IProntuarioRepository
        // retornando-o; o Id será 0 (sem banco), mas basta para o handler.
        var prontuario = Prontuario.Iniciar(PacienteId, EstabelecimentoId);

        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
            .ReturnsAsync(prontuario);
    }

    private ListarAnexosDoProntuarioQuery Query(int pagina = 1, int tamanho = 50) => new()
    {
        PacienteId = PacienteId,
        EstabelecimentoId = EstabelecimentoId,
        SolicitanteUsuarioId = _solicitanteId,
        SolicitantePapel = TenantPapel.Profissional,
        Pagina = pagina,
        TamanhoPagina = tamanho
    };

    [Test]
    public async Task Handle_SemParams_RetornaPrimeiraPaginaDefault50()
    {
        var itens = new List<AnexoDto> { new() { Id = 1 }, new() { Id = 2 } };
        _repo.Setup(r => r.ListarDoProntuario(It.IsAny<long>(), null, It.IsAny<Guid>(), It.IsAny<TenantPapel>(), 1, 50))
            .ReturnsAsync((itens, 2));

        var resultado = await _sut.Handle(Query());

        Assert.That(resultado.Pagina, Is.EqualTo(1));
        Assert.That(resultado.TamanhoPagina, Is.EqualTo(50));
        Assert.That(resultado.Total, Is.EqualTo(2));
        Assert.That(resultado.Itens.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task Handle_Paginacao_PassaParametrosCorretos()
    {
        _repo.Setup(r => r.ListarDoProntuario(It.IsAny<long>(), null, It.IsAny<Guid>(), It.IsAny<TenantPapel>(), 3, 20))
            .ReturnsAsync((new List<AnexoDto>(), 100));

        var resultado = await _sut.Handle(Query(pagina: 3, tamanho: 20));

        Assert.That(resultado.Pagina, Is.EqualTo(3));
        Assert.That(resultado.TamanhoPagina, Is.EqualTo(20));
        Assert.That(resultado.Total, Is.EqualTo(100));
        _repo.Verify(r => r.ListarDoProntuario(It.IsAny<long>(), null, It.IsAny<Guid>(), It.IsAny<TenantPapel>(), 3, 20), Times.Once);
    }

    [Test]
    public async Task Handle_SemProntuario_RetornaPaginaVaziaSemAudit()
    {
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
            .ReturnsAsync((Prontuario?)null);

        var resultado = await _sut.Handle(Query());

        Assert.That(resultado.Total, Is.EqualTo(0));
        Assert.That(resultado.Itens, Is.Empty);
        _acessoLog.Verify(a => a.RegistrarAsync(
                It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<TipoAcessoProntuario>()),
            Times.Never, "Sem prontuário, não há acesso a auditar.");
    }

    [Test]
    public async Task Handle_RegistraAuditLgpd()
    {
        _repo.Setup(r => r.ListarDoProntuario(It.IsAny<long>(), null, It.IsAny<Guid>(), It.IsAny<TenantPapel>(), 1, 50))
            .ReturnsAsync((new List<AnexoDto>(), 0));

        await _sut.Handle(Query());

        _acessoLog.Verify(a => a.RegistrarAsync(
                It.IsAny<long>(), _solicitanteId, EstabelecimentoId, TipoAcessoProntuario.Leitura),
            Times.Once, "Audit registrado mesmo com lista vazia.");
    }
}
