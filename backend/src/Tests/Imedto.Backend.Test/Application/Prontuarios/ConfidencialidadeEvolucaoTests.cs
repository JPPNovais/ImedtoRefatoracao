using Imedto.Backend.Application.Prontuarios.Queries;
using Imedto.Backend.Contracts.Prontuarios.Queries;
using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.SharedKernel.Tenancy;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Prontuarios;

/// <summary>
/// Testes unitários de confidencialidade da evolução — briefing 2026-06-27_001.
/// Valida que os handlers repassam os parâmetros de gating (solicitanteUsuarioId + papel)
/// para o repositório, garantindo que o predicado autor-ou-dono seja aplicado.
///
/// Os testes de predicado SQL real (WHERE + autor_usuario_id) exigem banco real e ficam
/// para o QA executar com EXPLAIN ANALYZE (CA15). Aqui cobrimos a cadeia handler→repo
/// para CA1, CA2 (Profissional), CA5 (Dono), CA7 (falha-fechada), CA8 (contagem coerente).
/// </summary>
[TestFixture]
public class ConfidencialidadeEvolucaoTests
{
    private const long EstabelecimentoId = 1;
    private const long PacienteId = 100;
    private const long ProntuarioId = 200;

    private static readonly Guid DrA = Guid.NewGuid();
    private static readonly Guid DrB = Guid.NewGuid();
    private static readonly Guid DonoId = Guid.NewGuid();

    // ─── ContarEvolucoes ────────────────────────────────────────────────────

    [Test]
    public async Task ContarEvolucoes_Profissional_RepassaPredicadoDeAutoria_AoRepositorio()
    {
        // CA8: Profissional recebe contagem filtrada — handler deve propagar
        // SolicitanteUsuarioId + TenantPapel.Profissional ao repositório.
        var repoMock = new Mock<ProntuarioQueryRepository>(new AppReadConnectionString("Host=ignored"));
        repoMock.Setup(r => r.ContarEvolucoes(PacienteId, EstabelecimentoId, DrA, TenantPapel.Profissional))
                .ReturnsAsync(3);

        var sut = new ContarEvolucoesProntuarioPacienteQueryHandlers(repoMock.Object);

        var result = await sut.Handle(new ContarEvolucoesProntuarioPacienteQuery
        {
            PacienteId = PacienteId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = DrA,
            SolicitantePapel = TenantPapel.Profissional,
        });

        Assert.That(result.Total, Is.EqualTo(3));
        repoMock.Verify(r => r.ContarEvolucoes(PacienteId, EstabelecimentoId, DrA, TenantPapel.Profissional), Times.Once,
            "O handler deve repassar o papel Profissional ao repositório para filtrar por autoria.");
    }

    [Test]
    public async Task ContarEvolucoes_Dono_RepassaPapelDono_AoRepositorio()
    {
        // CA5: Dono deve receber contagem total — o papel Dono bypassa o filtro de autoria no SQL.
        var repoMock = new Mock<ProntuarioQueryRepository>(new AppReadConnectionString("Host=ignored"));
        repoMock.Setup(r => r.ContarEvolucoes(PacienteId, EstabelecimentoId, DonoId, TenantPapel.Dono))
                .ReturnsAsync(10);

        var sut = new ContarEvolucoesProntuarioPacienteQueryHandlers(repoMock.Object);

        var result = await sut.Handle(new ContarEvolucoesProntuarioPacienteQuery
        {
            PacienteId = PacienteId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = DonoId,
            SolicitantePapel = TenantPapel.Dono,
        });

        Assert.That(result.Total, Is.EqualTo(10));
        repoMock.Verify(r => r.ContarEvolucoes(PacienteId, EstabelecimentoId, DonoId, TenantPapel.Dono), Times.Once,
            "O handler deve repassar o papel Dono ao repositório — Dono vê o total real.");
    }

    [Test]
    public async Task ContarEvolucoes_NaoConfundeIdentidadeEntreProfissionais()
    {
        // CA2/CA8: Dr. B não deve ver a contagem de Dr. A — handler deve propagar DrB, não DrA.
        var repoMock = new Mock<ProntuarioQueryRepository>(new AppReadConnectionString("Host=ignored"));
        repoMock.Setup(r => r.ContarEvolucoes(PacienteId, EstabelecimentoId, DrB, TenantPapel.Profissional))
                .ReturnsAsync(0); // Dr. B não tem evoluções neste paciente.
        repoMock.Setup(r => r.ContarEvolucoes(PacienteId, EstabelecimentoId, DrA, TenantPapel.Profissional))
                .ReturnsAsync(5); // Dr. A tem 5.

        var sut = new ContarEvolucoesProntuarioPacienteQueryHandlers(repoMock.Object);

        var resultDrB = await sut.Handle(new ContarEvolucoesProntuarioPacienteQuery
        {
            PacienteId = PacienteId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = DrB,
            SolicitantePapel = TenantPapel.Profissional,
        });

        Assert.That(resultDrB.Total, Is.EqualTo(0),
            "CA8: Dr. B sem evoluções próprias deve receber contagem 0, não o total do paciente.");

        // Verifica que o repositório foi chamado com o ID correto (DrB) e não com DrA.
        repoMock.Verify(r => r.ContarEvolucoes(PacienteId, EstabelecimentoId, DrB, TenantPapel.Profissional), Times.Once);
        repoMock.Verify(r => r.ContarEvolucoes(PacienteId, EstabelecimentoId, DrA, It.IsAny<TenantPapel>()), Times.Never,
            "O handler nunca deve chamar o repositório com o ID de um colega.");
    }

    // ─── ListarEvolucoes ─────────────────────────────────────────────────────

    [Test]
    public async Task ListarEvolucoes_Profissional_RepassaPredicadoDeAutoria_AoRepositorio()
    {
        // CA1: Dr. A vê as próprias 2 evoluções — handler deve repassar seu ID e Profissional ao repo.
        var repoMock = new Mock<ProntuarioQueryRepository>(new AppReadConnectionString("Host=ignored"));
        var pacienteRepoMock = new Mock<IPacienteRepository>();
        var prontuarioRepoMock = new Mock<IProntuarioRepository>();
        var acessoLogMock = new Mock<IProntuarioAcessoLogService>();

        var paciente = Paciente.Cadastrar(EstabelecimentoId, "Ana Silva", null, null, GeneroPaciente.NaoInformado, null, null, null, null);
        pacienteRepoMock.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                        .ReturnsAsync(paciente);

        var paginaEsperada = new PaginaEvolucoesDto { Itens = new List<EvolucaoDto>(), Total = 2, Pagina = 1, TamanhoPagina = 20 };
        repoMock.Setup(r => r.ListarEvolucoesPaginadas(
                    It.IsAny<long>(), EstabelecimentoId, 1, 20, DrA, TenantPapel.Profissional))
                .ReturnsAsync(paginaEsperada);

        var sut = new ListarEvolucoesProntuarioPacienteQueryHandlers(
            repoMock.Object, pacienteRepoMock.Object, prontuarioRepoMock.Object, acessoLogMock.Object);

        var result = await sut.Handle(new ListarEvolucoesProntuarioPacienteQuery
        {
            PacienteId = PacienteId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = DrA,
            SolicitantePapel = TenantPapel.Profissional,
            Pagina = 1,
            TamanhoPagina = 20,
        });

        Assert.That(result.Total, Is.EqualTo(2));
        repoMock.Verify(r => r.ListarEvolucoesPaginadas(
                It.IsAny<long>(), EstabelecimentoId, 1, 20, DrA, TenantPapel.Profissional), Times.Once,
            "CA1: handler deve repassar SolicitanteUsuarioId e papel ao repositório para filtrar por autoria.");
    }

    [Test]
    public async Task ListarEvolucoes_Dono_RepassaPapelDono_AoRepositorio()
    {
        // CA5: Dono vê todas as evoluções — papel Dono bypassa filtro de autoria no SQL.
        var repoMock = new Mock<ProntuarioQueryRepository>(new AppReadConnectionString("Host=ignored"));
        var pacienteRepoMock = new Mock<IPacienteRepository>();
        var prontuarioRepoMock = new Mock<IProntuarioRepository>();
        var acessoLogMock = new Mock<IProntuarioAcessoLogService>();

        var paciente = Paciente.Cadastrar(EstabelecimentoId, "Ana Silva", null, null, GeneroPaciente.NaoInformado, null, null, null, null);
        pacienteRepoMock.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                        .ReturnsAsync(paciente);

        var paginaTodas = new PaginaEvolucoesDto { Itens = new List<EvolucaoDto>(), Total = 8, Pagina = 1, TamanhoPagina = 20 };
        repoMock.Setup(r => r.ListarEvolucoesPaginadas(
                    It.IsAny<long>(), EstabelecimentoId, 1, 20, DonoId, TenantPapel.Dono))
                .ReturnsAsync(paginaTodas);

        var sut = new ListarEvolucoesProntuarioPacienteQueryHandlers(
            repoMock.Object, pacienteRepoMock.Object, prontuarioRepoMock.Object, acessoLogMock.Object);

        var result = await sut.Handle(new ListarEvolucoesProntuarioPacienteQuery
        {
            PacienteId = PacienteId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = DonoId,
            SolicitantePapel = TenantPapel.Dono,
            Pagina = 1,
            TamanhoPagina = 20,
        });

        Assert.That(result.Total, Is.EqualTo(8));
        repoMock.Verify(r => r.ListarEvolucoesPaginadas(
                It.IsAny<long>(), EstabelecimentoId, 1, 20, DonoId, TenantPapel.Dono), Times.Once,
            "CA5: Dono deve receber papel Dono no repositório — bypassa filtro de autoria.");
    }

    [Test]
    public void ListarEvolucoes_PacienteNaoEncontrado_LancaBusinessException()
    {
        // CA9/CA7: Paciente de outro tenant → "não encontrado" genérico. Nenhum conteúdo vaza.
        var repoMock = new Mock<ProntuarioQueryRepository>(new AppReadConnectionString("Host=ignored"));
        var pacienteRepoMock = new Mock<IPacienteRepository>();
        var prontuarioRepoMock = new Mock<IProntuarioRepository>();
        var acessoLogMock = new Mock<IProntuarioAcessoLogService>();

        pacienteRepoMock.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                        .ReturnsAsync((Paciente?)null);

        var sut = new ListarEvolucoesProntuarioPacienteQueryHandlers(
            repoMock.Object, pacienteRepoMock.Object, prontuarioRepoMock.Object, acessoLogMock.Object);

        var ex = Assert.ThrowsAsync<BusinessException>(() => sut.Handle(new ListarEvolucoesProntuarioPacienteQuery
        {
            PacienteId = PacienteId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = DrA,
            SolicitantePapel = TenantPapel.Profissional,
            Pagina = 1,
            TamanhoPagina = 20,
        }));

        Assert.That(ex.Message, Is.EqualTo("Paciente não encontrado."),
            "CA9: mensagem genérica — nunca revela dados do tenant alheio.");

        repoMock.Verify(r => r.ListarEvolucoesPaginadas(
                It.IsAny<long>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<Guid>(), It.IsAny<TenantPapel>()), Times.Never,
            "CA9: repositório não deve ser consultado quando paciente não pertence ao tenant.");
    }

    [Test]
    public async Task ListarEvolucoes_SemProntuarioIniciado_RetornaPaginaVazia()
    {
        // CA7/falha-fechada: paciente sem prontuário → lista vazia, sem erro, sem conteúdo de colega.
        var repoMock = new Mock<ProntuarioQueryRepository>(new AppReadConnectionString("Host=ignored"));
        var pacienteRepoMock = new Mock<IPacienteRepository>();
        var prontuarioRepoMock = new Mock<IProntuarioRepository>();
        var acessoLogMock = new Mock<IProntuarioAcessoLogService>();

        var paciente = Paciente.Cadastrar(EstabelecimentoId, "João Teste", null, null, GeneroPaciente.NaoInformado, null, null, null, null);
        pacienteRepoMock.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                        .ReturnsAsync(paciente);

        repoMock.Setup(r => r.ListarEvolucoesPaginadas(
                    It.IsAny<long>(), EstabelecimentoId, 1, 20, DrB, TenantPapel.Profissional))
                .ReturnsAsync(new PaginaEvolucoesDto { Itens = new List<EvolucaoDto>(), Total = 0, Pagina = 1, TamanhoPagina = 20 });

        var sut = new ListarEvolucoesProntuarioPacienteQueryHandlers(
            repoMock.Object, pacienteRepoMock.Object, prontuarioRepoMock.Object, acessoLogMock.Object);

        var result = await sut.Handle(new ListarEvolucoesProntuarioPacienteQuery
        {
            PacienteId = PacienteId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = DrB,
            SolicitantePapel = TenantPapel.Profissional,
            Pagina = 1,
            TamanhoPagina = 20,
        });

        Assert.That(result.Total, Is.EqualTo(0),
            "CA2: Dr. B sem evoluções próprias recebe lista vazia — não vaza total de colegas.");
        Assert.That(result.Itens, Is.Empty);
    }
}
