using Imedto.Backend.Application.Prontuarios.Queries;
using Imedto.Backend.Contracts.Prontuarios.Queries;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.Infrastructure.Storage;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Prontuarios;

/// <summary>
/// Bug #2 (LGPD/IDOR): garante que o handler de ObterUrlAnexo nunca devolva URL
/// de anexo pertencente a um paciente diferente do informado na rota. Antes da
/// correção, qualquer membro autenticado conseguia baixar anexo de outro
/// paciente do mesmo tenant trocando apenas o <c>anexoId</c> na URL.
/// </summary>
[TestFixture]
public class ObterUrlAnexoQueryHandlersTests
{
    private Mock<ProntuarioAnexoQueryRepository> _repo;
    private Mock<IAnexoStorageService> _storage;
    private Mock<IProntuarioAcessoLogService> _acessoLog;
    private ObterUrlAnexoQueryHandlers _sut;

    private const long EstabelecimentoId = 1;
    private const long PacienteUrl = 100;
    private const long PacienteOutro = 999;
    private const long AnexoId = 555;
    private const long ProntuarioId = 200;
    private readonly Guid _solicitanteId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<ProntuarioAnexoQueryRepository>(
            new AppReadConnectionString("Host=ignored"));
        _storage = new Mock<IAnexoStorageService>();
        _acessoLog = new Mock<IProntuarioAcessoLogService>();

        _sut = new ObterUrlAnexoQueryHandlers(
            _repo.Object, _storage.Object, _acessoLog.Object,
            Options.Create(new StorageOptions { TtlSignedUrlMinutos = 5 }));
    }

    [Test]
    public async Task Handle_AnexoPertenceAoPaciente_GeraUrlAssinadaERegistraAudit()
    {
        _repo.Setup(r => r.ObterReferenciaAnexo(AnexoId, PacienteUrl, EstabelecimentoId))
            .ReturnsAsync((ProntuarioId, "s3://anexos/abc", "exame.pdf", "application/pdf"));

        _storage.Setup(s => s.GerarUrlAssinadaLeituraAsync("s3://anexos/abc", 300))
            .ReturnsAsync("https://signed.example/exame.pdf?sig=xyz");

        var dto = await _sut.Handle(new ObterUrlAnexoQuery
        {
            AnexoId = AnexoId,
            PacienteId = PacienteUrl,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = _solicitanteId
        });

        Assert.That(dto.Url, Is.EqualTo("https://signed.example/exame.pdf?sig=xyz"));
        Assert.That(dto.NomeOriginal, Is.EqualTo("exame.pdf"));
        Assert.That(dto.Id, Is.EqualTo(AnexoId));
        _acessoLog.Verify(a => a.RegistrarAsync(
                ProntuarioId, _solicitanteId, EstabelecimentoId, TipoAcessoProntuario.Leitura),
            Times.Once);
    }

    [Test]
    public void Handle_AnexoDeOutroPacienteDoMesmoTenant_LancaBusinessExceptionGenerica()
    {
        // Repositorio devolve null quando o par (paciente, anexo) nao bate —
        // exatamente o caminho que fecha o IDOR. Sem o filtro de paciente no
        // SQL, esse cenario teria devolvido o anexo do outro paciente.
        _repo.Setup(r => r.ObterReferenciaAnexo(AnexoId, PacienteOutro, EstabelecimentoId))
            .ReturnsAsync(((long, string, string, string)?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new ObterUrlAnexoQuery
        {
            AnexoId = AnexoId,
            PacienteId = PacienteOutro,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = _solicitanteId
        }));
        Assert.That(ex.Message, Is.EqualTo("Anexo não encontrado."),
            "Mensagem generica — defense-in-depth nao vaza existencia.");

        _storage.Verify(s => s.GerarUrlAssinadaLeituraAsync(It.IsAny<string>(), It.IsAny<int>()),
            Times.Never, "Nunca deve gerar URL quando o anexo nao pertence ao paciente.");
        _acessoLog.Verify(a => a.RegistrarAsync(
                It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<TipoAcessoProntuario>()),
            Times.Never, "Audit so deve registrar quando o acesso de fato ocorre.");
    }

    [Test]
    public void Handle_AnexoDeOutroTenant_LancaBusinessExceptionGenerica()
    {
        const long outroTenant = 2;
        _repo.Setup(r => r.ObterReferenciaAnexo(AnexoId, PacienteUrl, outroTenant))
            .ReturnsAsync(((long, string, string, string)?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new ObterUrlAnexoQuery
        {
            AnexoId = AnexoId,
            PacienteId = PacienteUrl,
            EstabelecimentoId = outroTenant,
            SolicitanteUsuarioId = _solicitanteId
        }));
        Assert.That(ex.Message, Is.EqualTo("Anexo não encontrado."));
    }
}
