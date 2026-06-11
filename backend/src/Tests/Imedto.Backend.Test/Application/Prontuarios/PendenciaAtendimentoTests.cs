using Imedto.Backend.Application.Prontuarios.Commands;
using Imedto.Backend.Application.Prontuarios.Events;
using Imedto.Backend.Domain.Agendamentos.Events;
using Imedto.Backend.Domain.Atestados.Events;
using Imedto.Backend.Domain.Atestados;
using Imedto.Backend.Domain.Orcamentos.Events;
using Imedto.Backend.Domain.PedidosExame.Events;
using Imedto.Backend.Domain.PedidosExame;
using Imedto.Backend.Domain.Prontuarios.Pendencias;
using Imedto.Backend.Domain.Receitas.Events;
using Imedto.Backend.Domain.Receitas;
using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Prontuarios;

/// <summary>
/// Testes de unidade para PendenciaAtendimento (domain) e handlers da F3B.
/// Cobre: criação, idempotência, falha-suave, conclusão automática (5 gatilhos), conclusão manual, multi-tenant.
/// </summary>
[TestFixture]
public class PendenciaAtendimentoTests
{
    private const long EstabId = 1;
    private const long PacienteId = 100;
    private const long EvolucaoId = 500;
    private readonly Guid _autorId = Guid.NewGuid();

    // ── Domain: PendenciaAtendimento.Criar ─────────────────────────────────────────

    [Test]
    public void Criar_DadosValidos_RetornaPendentePendente()
    {
        var p = PendenciaAtendimento.Criar(EstabId, PacienteId, EvolucaoId, null,
            AcaoPendencia.CriarReceita, _autorId);

        Assert.That(p.Status, Is.EqualTo(StatusPendencia.Pendente));
        Assert.That(p.Acao, Is.EqualTo(AcaoPendencia.CriarReceita));
        Assert.That(p.EstabelecimentoId, Is.EqualTo(EstabId));
        Assert.That(p.PacienteId, Is.EqualTo(PacienteId));
        Assert.That(p.EvolucaoId, Is.EqualTo(EvolucaoId));
        Assert.That(p.ReferenciaId, Is.Null);
        Assert.That(p.ConcluidaEm, Is.Null);
    }

    [Test]
    public void Criar_EstabelecimentoInvalido_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() =>
            PendenciaAtendimento.Criar(0, PacienteId, EvolucaoId, null, AcaoPendencia.CriarReceita, _autorId));
    }

    // ── Domain: ConcluirPorGatilho / ConcluirManualmente ───────────────────────────

    [Test]
    public void ConcluirPorGatilho_PendenciaAberta_ViraConcluidaComReferenciaId()
    {
        var p = PendenciaAtendimento.Criar(EstabId, PacienteId, EvolucaoId, null,
            AcaoPendencia.CriarReceita, _autorId);
        p.ConcluirPorGatilho(999L);

        Assert.That(p.Status, Is.EqualTo(StatusPendencia.Concluida));
        Assert.That(p.ReferenciaId, Is.EqualTo(999L));
        Assert.That(p.ConcluidaEm, Is.Not.Null);
    }

    [Test]
    public void ConcluirPorGatilho_JaConcluida_NaoSobrescreve_CA65()
    {
        var p = PendenciaAtendimento.Criar(EstabId, PacienteId, EvolucaoId, null,
            AcaoPendencia.CriarReceita, _autorId);
        p.ConcluirPorGatilho(111L);
        p.ConcluirPorGatilho(222L); // segunda chamada — não deve sobrescrever

        Assert.That(p.ReferenciaId, Is.EqualTo(111L)); // mantém a primeira
    }

    [Test]
    public void ConcluirManualmente_PendenciaAberta_ViraConcluidaSemReferencia_CA67()
    {
        var p = PendenciaAtendimento.Criar(EstabId, PacienteId, EvolucaoId, null,
            AcaoPendencia.MarcarProcedimentoRealizado, _autorId);
        p.ConcluirManualmente();

        Assert.That(p.Status, Is.EqualTo(StatusPendencia.Concluida));
        Assert.That(p.ReferenciaId, Is.Null);
        Assert.That(p.ConcluidaEm, Is.Not.Null);
    }

    // ── PendenciaExtratorEvolucao ───────────────────────────────────────────────────

    [Test]
    public async Task ExtrairECriar_AcoesMarcadas_CriaPendencias_CA59()
    {
        var repoMock = new Mock<IPendenciaAtendimentoRepository>();
        repoMock.Setup(r => r.ExistePorEvolucaoEAcao(It.IsAny<long>(), It.IsAny<AcaoPendencia>()))
                .ReturnsAsync(false);
        var sut = new PendenciaExtratorEvolucao(repoMock.Object);

        const string json = """{"conduta":{"acoesMarcadas":["CriarReceita","AgendarRetorno"],"observacao":"teste"}}""";

        await sut.ExtrairECriar(EstabId, PacienteId, EvolucaoId, null, _autorId, json);

        repoMock.Verify(r => r.Salvar(It.Is<PendenciaAtendimento>(p => p.Acao == AcaoPendencia.CriarReceita)), Times.Once);
        repoMock.Verify(r => r.Salvar(It.Is<PendenciaAtendimento>(p => p.Acao == AcaoPendencia.AgendarRetorno)), Times.Once);
        repoMock.Verify(r => r.Salvar(It.IsAny<PendenciaAtendimento>()), Times.Exactly(2));
    }

    [Test]
    public async Task ExtrairECriar_SemAcoesMarcadas_NaoCria_CA61()
    {
        var repoMock = new Mock<IPendenciaAtendimentoRepository>();
        var sut = new PendenciaExtratorEvolucao(repoMock.Object);

        const string json = """{"queixa":"dor de cabeca"}""";
        await sut.ExtrairECriar(EstabId, PacienteId, EvolucaoId, null, _autorId, json);

        repoMock.Verify(r => r.Salvar(It.IsAny<PendenciaAtendimento>()), Times.Never);
    }

    [Test]
    public async Task ExtrairECriar_JaExiste_NaoDuplica_CA62()
    {
        var repoMock = new Mock<IPendenciaAtendimentoRepository>();
        // Simula que CriarReceita já existe, AgendarRetorno não
        repoMock.Setup(r => r.ExistePorEvolucaoEAcao(EvolucaoId, AcaoPendencia.CriarReceita))
                .ReturnsAsync(true);
        repoMock.Setup(r => r.ExistePorEvolucaoEAcao(EvolucaoId, AcaoPendencia.AgendarRetorno))
                .ReturnsAsync(false);
        var sut = new PendenciaExtratorEvolucao(repoMock.Object);

        const string json = """{"conduta":{"acoesMarcadas":["CriarReceita","AgendarRetorno"]}}""";
        await sut.ExtrairECriar(EstabId, PacienteId, EvolucaoId, null, _autorId, json);

        repoMock.Verify(r => r.Salvar(It.IsAny<PendenciaAtendimento>()), Times.Once); // só AgendarRetorno
        repoMock.Verify(r => r.Salvar(It.Is<PendenciaAtendimento>(p => p.Acao == AcaoPendencia.AgendarRetorno)), Times.Once);
    }

    [Test]
    public async Task ExtrairECriar_FalhaNoRepo_NaoLancaExcecao_CA75()
    {
        // Falha-suave: exceção no repo nunca sobe para o handler da evolução.
        var repoMock = new Mock<IPendenciaAtendimentoRepository>();
        repoMock.Setup(r => r.ExistePorEvolucaoEAcao(It.IsAny<long>(), It.IsAny<AcaoPendencia>()))
                .ThrowsAsync(new Exception("erro de banco"));
        var sut = new PendenciaExtratorEvolucao(repoMock.Object);

        const string json = """{"conduta":{"acoesMarcadas":["CriarReceita"]}}""";
        Assert.DoesNotThrowAsync(() => sut.ExtrairECriar(EstabId, PacienteId, EvolucaoId, null, _autorId, json));
    }

    [Test]
    public async Task ExtrairECriar_ConteudoLegado_NaoLancaExcecao_CA73()
    {
        // ConteudoJson legado (string, sem nó "conduta" ou conduta como texto)
        var repoMock = new Mock<IPendenciaAtendimentoRepository>();
        var sut = new PendenciaExtratorEvolucao(repoMock.Object);

        const string json = """{"conduta":"encaminhar para especialista"}""";
        await sut.ExtrairECriar(EstabId, PacienteId, EvolucaoId, null, _autorId, json);

        repoMock.Verify(r => r.Salvar(It.IsAny<PendenciaAtendimento>()), Times.Never);
    }

    // ── Event handlers: conclusão automática ────────────────────────────────────────

    [Test]
    public async Task ReceitaHandler_PendenciaAberta_ConcluidaComReferenciaId_CA63()
    {
        var pendencia = PendenciaAtendimento.Criar(EstabId, PacienteId, EvolucaoId, null,
            AcaoPendencia.CriarReceita, _autorId);
        var repoMock = new Mock<IPendenciaAtendimentoRepository>();
        repoMock.Setup(r => r.ObterAbertaMaisRecentePorAcao(EstabId, PacienteId, AcaoPendencia.CriarReceita))
                .ReturnsAsync(pendencia);
        var handler = new ConcluirPendenciaAoEmitirReceitaHandler(repoMock.Object);

        await handler.Handle(new ReceitaEmitidaEvent(777L, 1, PacienteId, EstabId, Guid.NewGuid(), TipoReceita.Comum));

        Assert.That(pendencia.Status, Is.EqualTo(StatusPendencia.Concluida));
        Assert.That(pendencia.ReferenciaId, Is.EqualTo(777L));
    }

    [Test]
    public async Task ReceitaHandler_SemPendenciaAberta_NoOp_CA65()
    {
        var repoMock = new Mock<IPendenciaAtendimentoRepository>();
        repoMock.Setup(r => r.ObterAbertaMaisRecentePorAcao(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<AcaoPendencia>()))
                .ReturnsAsync((PendenciaAtendimento?)null);
        var handler = new ConcluirPendenciaAoEmitirReceitaHandler(repoMock.Object);

        Assert.DoesNotThrowAsync(() => handler.Handle(
            new ReceitaEmitidaEvent(1, 1, PacienteId, EstabId, Guid.NewGuid(), TipoReceita.Comum)));
    }

    [Test]
    public async Task AtestadoHandler_PendenciaAberta_Concluida_CA64()
    {
        var pendencia = PendenciaAtendimento.Criar(EstabId, PacienteId, EvolucaoId, null,
            AcaoPendencia.CriarAtestado, _autorId);
        var repoMock = new Mock<IPendenciaAtendimentoRepository>();
        repoMock.Setup(r => r.ObterAbertaMaisRecentePorAcao(EstabId, PacienteId, AcaoPendencia.CriarAtestado))
                .ReturnsAsync(pendencia);
        var handler = new ConcluirPendenciaAoEmitirAtestadoHandler(repoMock.Object);

        await handler.Handle(new AtestadoEmitidoEvent(888L, PacienteId, EstabId, Guid.NewGuid(), TipoAtestado.Comparecimento));

        Assert.That(pendencia.Status, Is.EqualTo(StatusPendencia.Concluida));
        Assert.That(pendencia.ReferenciaId, Is.EqualTo(888L));
    }

    [Test]
    public async Task PedidoExameHandler_PendenciaAberta_Concluida_CA64()
    {
        var pendencia = PendenciaAtendimento.Criar(EstabId, PacienteId, EvolucaoId, null,
            AcaoPendencia.PedirExame, _autorId);
        var repoMock = new Mock<IPendenciaAtendimentoRepository>();
        repoMock.Setup(r => r.ObterAbertaMaisRecentePorAcao(EstabId, PacienteId, AcaoPendencia.PedirExame))
                .ReturnsAsync(pendencia);
        var handler = new ConcluirPendenciaAoEmitirPedidoExameHandler(repoMock.Object);

        await handler.Handle(new PedidoExameEmitidoEvent(444L, PacienteId, EstabId, Guid.NewGuid(), TipoPedidoExame.Laboratorial, 3));

        Assert.That(pendencia.Status, Is.EqualTo(StatusPendencia.Concluida));
        Assert.That(pendencia.ReferenciaId, Is.EqualTo(444L));
    }

    [Test]
    public async Task OrcamentoHandler_PendenciaAberta_Concluida_CA64()
    {
        var pendencia = PendenciaAtendimento.Criar(EstabId, PacienteId, EvolucaoId, null,
            AcaoPendencia.CriarOrcamento, _autorId);
        var repoMock = new Mock<IPendenciaAtendimentoRepository>();
        repoMock.Setup(r => r.ObterAbertaMaisRecentePorAcao(EstabId, PacienteId, AcaoPendencia.CriarOrcamento))
                .ReturnsAsync(pendencia);
        var handler = new ConcluirPendenciaAoCriarOrcamentoHandler(repoMock.Object);

        await handler.Handle(new OrcamentoCriadoEvent(555L, EstabId, PacienteId, "ORC-001"));

        Assert.That(pendencia.Status, Is.EqualTo(StatusPendencia.Concluida));
        Assert.That(pendencia.ReferenciaId, Is.EqualTo(555L));
    }

    [Test]
    public async Task AgendamentoHandler_InicioPrevistFuturo_PendenciaAberta_Concluida_CA64()
    {
        var pendencia = PendenciaAtendimento.Criar(EstabId, PacienteId, EvolucaoId, null,
            AcaoPendencia.AgendarRetorno, _autorId);
        var repoMock = new Mock<IPendenciaAtendimentoRepository>();
        repoMock.Setup(r => r.ObterAbertaMaisRecentePorAcao(EstabId, PacienteId, AcaoPendencia.AgendarRetorno))
                .ReturnsAsync(pendencia);
        var handler = new ConcluirPendenciaAoCriarAgendamentoHandler(repoMock.Object);

        var futuro = DateTime.UtcNow.AddDays(7);
        await handler.Handle(new AgendamentoCriadoEvent(666L, EstabId, PacienteId, Guid.NewGuid(), futuro));

        Assert.That(pendencia.Status, Is.EqualTo(StatusPendencia.Concluida));
        Assert.That(pendencia.ReferenciaId, Is.EqualTo(666L));
    }

    [Test]
    public async Task AgendamentoHandler_InicioPrevistPassado_NaoConclui_R11()
    {
        // Agendamento no passado (mesmo dia/check-in) não deve concluir retorno pendente
        var pendencia = PendenciaAtendimento.Criar(EstabId, PacienteId, EvolucaoId, null,
            AcaoPendencia.AgendarRetorno, _autorId);
        var repoMock = new Mock<IPendenciaAtendimentoRepository>();
        var handler = new ConcluirPendenciaAoCriarAgendamentoHandler(repoMock.Object);

        var passado = DateTime.UtcNow.AddMinutes(-1);
        await handler.Handle(new AgendamentoCriadoEvent(999L, EstabId, PacienteId, Guid.NewGuid(), passado));

        Assert.That(pendencia.Status, Is.EqualTo(StatusPendencia.Pendente));
        repoMock.Verify(r => r.ObterAbertaMaisRecentePorAcao(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<AcaoPendencia>()), Times.Never);
    }

    // ── Multi-tenant: conclusão manual ─────────────────────────────────────────────

    [Test]
    public async Task ConcluirManualHandler_PendenciaOutroTenant_LancaBusinessException_CA69()
    {
        const long estabB = 99; // tenant diferente
        var repoMock = new Mock<IPendenciaAtendimentoRepository>();
        // ObterPorId retorna null (tenant filtro falha-fechada)
        repoMock.Setup(r => r.ObterPorId(It.IsAny<long>(), estabB))
                .ReturnsAsync((PendenciaAtendimento?)null);
        var handler = new ConcluirPendenciaManualCommandHandler(repoMock.Object);

        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            handler.Handle(new ConcluirPendenciaManualCommand { PendenciaId = 1, EstabelecimentoId = estabB }));

        Assert.That(ex.Message, Is.EqualTo("Pendência não encontrada.")); // mensagem genérica (LGPD)
    }

    [Test]
    public async Task ConcluirManualHandler_PendenciaEncontrada_ConcluidaSemReferencia_CA67()
    {
        var pendencia = PendenciaAtendimento.Criar(EstabId, PacienteId, EvolucaoId, null,
            AcaoPendencia.MarcarProcedimentoRealizado, _autorId);
        var repoMock = new Mock<IPendenciaAtendimentoRepository>();
        repoMock.Setup(r => r.ObterPorId(1L, EstabId))
                .ReturnsAsync(pendencia);
        var handler = new ConcluirPendenciaManualCommandHandler(repoMock.Object);

        await handler.Handle(new ConcluirPendenciaManualCommand { PendenciaId = 1, EstabelecimentoId = EstabId });

        Assert.That(pendencia.Status, Is.EqualTo(StatusPendencia.Concluida));
        Assert.That(pendencia.ReferenciaId, Is.Null);
    }
}
