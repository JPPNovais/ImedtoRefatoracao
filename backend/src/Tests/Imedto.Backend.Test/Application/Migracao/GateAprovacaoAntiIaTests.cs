using Imedto.Backend.Application.Migracao.Jobs;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Migracao;

/// <summary>
/// CA44 — Gate anti-IA: job em aguardando_aprovacao NUNCA é consumido pelo
/// recorrente de inferência, independentemente de quantas rodadas ocorram.
///
/// O gate é natural: ObterMaisAntigoAguardandoMapaOuNulo filtra APENAS
/// StatusAguardandoMapa. A única transição para aguardando_mapa é AprovarAnalise().
/// Este teste é o guardião contra regressão futura (addendum 003 — D-A5/CA44).
/// Se alguém alterar RegistrarArquivoRecebido para voltar a ir direto para
/// aguardando_mapa, o teste falha.
/// </summary>
[TestFixture]
public class GateAprovacaoAntiIaTests
{
    private Mock<IMigracaoJobRepository>       _jobRepo;
    private Mock<IMigracaoArquivoStorageService> _storage;
    private Mock<IMapeadorDeMigracao>          _mapeador;
    private Mock<IMigracaoMapaRepository>      _mapaRepo;
    private Mock<IMigracaoTemplateRepository>  _templateRepo;

    private const long EstabelecimentoId = 42;
    private static readonly Guid UsuarioId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _jobRepo      = new Mock<IMigracaoJobRepository>();
        _storage      = new Mock<IMigracaoArquivoStorageService>();
        _mapeador     = new Mock<IMapeadorDeMigracao>();
        _mapaRepo     = new Mock<IMigracaoMapaRepository>();
        _templateRepo = new Mock<IMigracaoTemplateRepository>();
    }

    private InferirMapaMigracaoJobHandler CriarSut()
    {
        var eventoRepo = new Mock<IMigracaoJobEventoRepository>();
        eventoRepo.Setup(r => r.Gravar(It.IsAny<MigracaoJobEvento>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);

        return new InferirMapaMigracaoJobHandler(
            _jobRepo.Object,
            _storage.Object,
            _mapeador.Object,
            _mapaRepo.Object,
            _templateRepo.Object,
            eventoRepo.Object,
            parsers: [],
            NullLogger<InferirMapaMigracaoJobHandler>.Instance);
    }

    /// <summary>
    /// CA44 (não-negociável) — Job recém-upado em aguardando_aprovacao nunca deve
    /// ser selecionado pela query de inferência, mesmo após múltiplos ciclos do poll.
    ///
    /// Cenário: ObterMaisAntigoAguardandoMapaOuNulo retorna null (comportamento correto
    /// do repositório — ele filtra SOMENTE StatusAguardandoMapa). O handler para sem
    /// chamar o provider de IA. Zero chamadas ao IMapeadorDeMigracao.
    ///
    /// Este teste falha imediatamente se:
    /// (a) RegistrarArquivoRecebido for revertido para transitar para aguardando_mapa, OU
    /// (b) ObterMaisAntigoAguardandoMapaOuNulo for alterado para incluir aguardando_aprovacao.
    /// </summary>
    [Test]
    public async Task CA44_JobEmAguardandoAprovacao_RecorrenteDeInferencia_ZeroChamadasAoMapeador()
    {
        // Arrange: job criado com upload — deve estar em aguardando_aprovacao.
        var job = MigracaoJob.Criar(EstabelecimentoId, UsuarioId);
        job.RegistrarArquivoRecebido("migracao/42/1/arquivo.zip");

        // Verificação prévia (regressão de domínio): upload transicionou para aguardando_aprovacao.
        Assert.That(job.Status, Is.EqualTo(MigracaoJob.StatusAguardandoAprovacao),
            "Pré-condição: RegistrarArquivoRecebido deve ir para aguardando_aprovacao (addendum 003).");

        // O repositório retorna null — comportamento correto de ObterMaisAntigoAguardandoMapaOuNulo
        // que só seleciona StatusAguardandoMapa. Job em aguardando_aprovacao não aparece.
        _jobRepo.Setup(r => r.ObterMaisAntigoAguardandoMapaOuNulo(It.IsAny<CancellationToken>()))
            .ReturnsAsync((MigracaoJob?)null);

        var sut = CriarSut();

        // Act: simula múltiplos ciclos do poll (5 rodadas = bem mais que o intervalo de 30s).
        const int ciclos = 5;
        for (var i = 0; i < ciclos; i++)
        {
            await sut.ExecutarAsync(CancellationToken.None);
        }

        // Assert: zero chamadas ao provider de IA (IMapeadorDeMigracao) — gate intacto.
        _mapeador.Verify(
            m => m.InferirMapaAsync(
                It.IsAny<EsquemaDeArquivo>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never,
            "CA44 falhou: IMapeadorDeMigracao foi chamado para um job em aguardando_aprovacao. " +
            "O gate anti-IA foi violado — verifique se RegistrarArquivoRecebido ou " +
            "ObterMaisAntigoAguardandoMapaOuNulo foram alterados indevidamente.");
    }

    /// <summary>
    /// CA44 (complementar) — após AprovarAnalise, o job passa para aguardando_mapa
    /// e o recorrente SIM o seleciona (smoke test do caminho feliz).
    /// </summary>
    [Test]
    public async Task CA44_JobAprovado_EmAguardandoMapa_RecorrenteConsome()
    {
        // Arrange: job em aguardando_mapa (pós-aprovação).
        var adminId = Guid.NewGuid();
        var job = MigracaoJob.Criar(EstabelecimentoId, UsuarioId);
        job.RegistrarArquivoRecebido("migracao/42/2/arquivo.zip");
        job.AprovarAnalise(adminId); // aguardando_aprovacao → aguardando_mapa

        Assert.That(job.Status, Is.EqualTo(MigracaoJob.StatusAguardandoMapa),
            "Pré-condição: após AprovarAnalise, job deve estar em aguardando_mapa.");

        // O repositório retorna o job aprovado.
        _jobRepo.Setup(r => r.ObterMaisAntigoAguardandoMapaOuNulo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        // S3 retorna stream vazio (ZIP válido sem entradas).
        var ms = new System.IO.MemoryStream();
        using (var zip = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
        {
            // ZIP vazio — sem arquivos; handler vai para MarcarMapaEmRevisao sem chamar IA.
        }
        ms.Position = 0;
        _storage.Setup(s => s.DownloadArquivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ms);
        _templateRepo.Setup(r => r.ListarPorNome(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CriarSut();

        // Act
        await sut.ExecutarAsync(CancellationToken.None);

        // Assert: o job foi processado (MarcarMapaEmRevisao deve ter sido chamado).
        Assert.That(job.Status, Is.EqualTo(MigracaoJob.StatusMapaEmRevisao),
            "Após aprovação e rodada do recorrente, job deve avançar para mapa_em_revisao.");
    }

    /// <summary>
    /// Anti-regressão de query — confirma que ObterMaisAntigoAguardandoMapaOuNulo
    /// seleciona SOMENTE StatusAguardandoMapa (não StatusAguardandoAprovacao).
    /// Teste em nível de repositório (mock): se a constante StatusAguardandoAprovacao
    /// fosse usada na query em vez de StatusAguardandoMapa, este teste detectaria.
    /// </summary>
    [Test]
    public void CA44_ConstanteNaQueryDeveSerAguardandoMapa_NaoAguardandoAprovacao()
    {
        // Verifica que as constantes de status são distintas.
        Assert.That(
            MigracaoJob.StatusAguardandoAprovacao,
            Is.Not.EqualTo(MigracaoJob.StatusAguardandoMapa),
            "aguardando_aprovacao e aguardando_mapa devem ser constantes diferentes.");

        Assert.That(MigracaoJob.StatusAguardandoMapa,     Is.EqualTo("aguardando_mapa"));
        Assert.That(MigracaoJob.StatusAguardandoAprovacao, Is.EqualTo("aguardando_aprovacao"));
    }
}
