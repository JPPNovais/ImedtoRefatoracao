using Imedto.Backend.Application.Prontuarios.Queries;
using Imedto.Backend.Contracts.Prontuarios.Queries;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.Infrastructure.Storage;
using Imedto.Backend.SharedKernel.Tenancy;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Prontuarios;

/// <summary>
/// Testes de regressão para ObterUrlsAnexosQueryHandler (batch de URLs assinadas).
/// Garante multi-tenant, IDOR e audit LGPD no caminho batch.
/// </summary>
[TestFixture]
public class ObterUrlsAnexosQueryHandlerTests
{
    private Mock<ProntuarioAnexoQueryRepository> _repo;
    private Mock<IAnexoStorageService> _storage;
    private Mock<IProntuarioAcessoLogService> _acessoLog;
    private ObterUrlsAnexosQueryHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long PacienteId = 100;
    private const long AnexoId1 = 10;
    private const long AnexoId2 = 20;
    private const long AnexoIdOutroTenant = 999;
    private const long ProntuarioId1 = 200;
    private const long ProntuarioId2 = 201;
    private readonly Guid _solicitante = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<ProntuarioAnexoQueryRepository>(
            new AppReadConnectionString("Host=ignored"));
        _storage = new Mock<IAnexoStorageService>();
        _acessoLog = new Mock<IProntuarioAcessoLogService>();

        _sut = new ObterUrlsAnexosQueryHandler(
            _repo.Object, _storage.Object, _acessoLog.Object,
            Options.Create(new StorageOptions { TtlSignedUrlMinutos = 5 }));
    }

    [Test]
    public async Task Handle_AnexosValidos_RetornaUrlsERegistraAuditPorProntuario()
    {
        var refs = new (long, long, string, string, string)[]
        {
            (AnexoId1, ProntuarioId1, "s3://a/img1.jpg", "img1.jpg", "image/jpeg"),
            (AnexoId2, ProntuarioId2, "s3://a/img2.pdf", "img2.pdf", "application/pdf"),
        };

        _repo.Setup(r => r.ObterReferenciasAnexos(
                It.Is<IReadOnlyList<long>>(l => l.Contains(AnexoId1) && l.Contains(AnexoId2)),
                PacienteId, EstabelecimentoId, It.IsAny<Guid>(), It.IsAny<TenantPapel>()))
            .ReturnsAsync(refs);

        _storage.Setup(s => s.GerarUrlAssinadaLeituraAsync("s3://a/img1.jpg", 300))
            .ReturnsAsync("https://cdn/img1.jpg?sig=1");
        _storage.Setup(s => s.GerarUrlAssinadaLeituraAsync("s3://a/img2.pdf", 300))
            .ReturnsAsync("https://cdn/img2.pdf?sig=2");

        var resultado = (await _sut.Handle(new ObterUrlsAnexosQuery
        {
            AnexoIds = new[] { AnexoId1, AnexoId2 },
            PacienteId = PacienteId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = _solicitante,
            SolicitantePapel = TenantPapel.Profissional,
        })).ToList();

        Assert.That(resultado, Has.Count.EqualTo(2));
        Assert.That(resultado.Select(r => r.Id), Is.EquivalentTo(new[] { AnexoId1, AnexoId2 }));
        Assert.That(resultado.First(r => r.Id == AnexoId1).Url, Is.EqualTo("https://cdn/img1.jpg?sig=1"));

        // Audit: um registro por prontuário distinto acessado.
        _acessoLog.Verify(a => a.RegistrarAsync(ProntuarioId1, _solicitante, EstabelecimentoId, TipoAcessoProntuario.Leitura), Times.Once);
        _acessoLog.Verify(a => a.RegistrarAsync(ProntuarioId2, _solicitante, EstabelecimentoId, TipoAcessoProntuario.Leitura), Times.Once);
    }

    [Test]
    public async Task Handle_IdsInvalidosOuDeOutroTenant_SaoIgnoradosSemLeak()
    {
        // Repositório retorna vazio quando os ids não pertencem ao paciente/tenant.
        _repo.Setup(r => r.ObterReferenciasAnexos(
                It.IsAny<IReadOnlyList<long>>(), PacienteId, EstabelecimentoId,
                It.IsAny<Guid>(), It.IsAny<TenantPapel>()))
            .ReturnsAsync(Enumerable.Empty<(long, long, string, string, string)>());

        var resultado = await _sut.Handle(new ObterUrlsAnexosQuery
        {
            AnexoIds = new[] { AnexoIdOutroTenant },
            PacienteId = PacienteId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = _solicitante,
            SolicitantePapel = TenantPapel.Profissional,
        });

        Assert.That(resultado, Is.Empty, "Ids de outro tenant retornam lista vazia, sem 404 nem 403.");
        _storage.Verify(s => s.GerarUrlAssinadaLeituraAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        _acessoLog.Verify(a => a.RegistrarAsync(It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<TipoAcessoProntuario>()), Times.Never);
    }

    [Test]
    public async Task Handle_MixValidoEInvalido_RetornaSomentOsValidos()
    {
        // Repositório filtra: retorna só o válido; o de outro tenant não aparece.
        var refs = new (long, long, string, string, string)[]
        {
            (AnexoId1, ProntuarioId1, "s3://a/img1.jpg", "img1.jpg", "image/jpeg"),
        };

        _repo.Setup(r => r.ObterReferenciasAnexos(
                It.IsAny<IReadOnlyList<long>>(), PacienteId, EstabelecimentoId,
                It.IsAny<Guid>(), It.IsAny<TenantPapel>()))
            .ReturnsAsync(refs);

        _storage.Setup(s => s.GerarUrlAssinadaLeituraAsync("s3://a/img1.jpg", 300))
            .ReturnsAsync("https://cdn/img1.jpg?sig=ok");

        var resultado = (await _sut.Handle(new ObterUrlsAnexosQuery
        {
            AnexoIds = new[] { AnexoId1, AnexoIdOutroTenant },
            PacienteId = PacienteId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = _solicitante,
            SolicitantePapel = TenantPapel.Profissional,
        })).ToList();

        Assert.That(resultado, Has.Count.EqualTo(1));
        Assert.That(resultado[0].Id, Is.EqualTo(AnexoId1));
    }

    [Test]
    public async Task Handle_ListaVazia_RetornaImediatamenteSemChamarRepositorio()
    {
        var resultado = await _sut.Handle(new ObterUrlsAnexosQuery
        {
            AnexoIds = Array.Empty<long>(),
            PacienteId = PacienteId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = _solicitante
        });

        Assert.That(resultado, Is.Empty);
        _repo.Verify(r => r.ObterReferenciasAnexos(It.IsAny<IReadOnlyList<long>>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<TenantPapel>()), Times.Never);
    }
}
