using Imedto.Backend.Application.Orcamentos.Events;
using Imedto.Backend.Domain.Cobrancas;
using Imedto.Backend.Domain.Orcamentos;
using Imedto.Backend.Domain.Orcamentos.Events;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.Test.Helpers;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Orcamentos;

/// <summary>
/// Testes F5: OrcamentoAprovadoEventHandler — criação de cobrança de cirurgia
/// na aprovação do orçamento (CA101–CA108, CA110–CA111, CA116).
/// </summary>
[TestFixture]
public class OrcamentoAprovadoEventHandlerTests
{
    private Mock<ICobrancaRepository> _cobrancaRepo;
    private Mock<IOrcamentoRepository> _orcamentoRepo;
    private OrcamentoAprovadoEventHandler _sut;

    private const long EstabId = 1L;
    private const long EstabOutroId = 99L;
    private const long PacienteId = 10L;
    private const long OrcamentoId = 50L;
    private static readonly Guid UsuarioId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static OrcamentoAprovadoEvent CriarEvento(long orcamentoId = OrcamentoId, decimal total = 1500m)
        => new(orcamentoId, EstabId, PacienteId, total);

    private static Orcamento CriarOrcamento(long id = OrcamentoId)
    {
        // Cria via reflection: usa fábrica real com cirurgia mínima (aggregate exige ao menos 1 item).
        var validade = DateOnly.FromDateTime(DateTime.Today.AddDays(30));
        var cirurgia = new Orcamento.CirurgiaPayload(null, "Rinoplastia", 1, 60, 1500m);
        var orc = Orcamento.Criar(EstabId, PacienteId, validade, null, UsuarioId, null, cirurgias: [cirurgia]);
        orc.SimularIdBanco(id);
        return orc;
    }

    private static Cobranca CriarCobrancaCirurgia(decimal valor = 1500m)
    {
        var c = Cobranca.CriarParaCirurgia(EstabId, PacienteId, OrcamentoId, valor, "Cirurgia — orçamento #50", UsuarioId);
        c.SimularIdBanco(200L);
        return c;
    }

    [SetUp]
    public void SetUp()
    {
        _cobrancaRepo = new Mock<ICobrancaRepository>();
        _orcamentoRepo = new Mock<IOrcamentoRepository>();
        _sut = new OrcamentoAprovadoEventHandler(_cobrancaRepo.Object, _orcamentoRepo.Object);
    }

    // ── CA101: primeira aprovação cria cobrança com origem=Cirurgia ──────────

    [Test]
    public async Task Handle_PrimeiraAprovacao_CriaCobrancaCirurgia()
    {
        var orc = CriarOrcamento();
        _orcamentoRepo.Setup(r => r.ObterPorIdCompletoOuNulo(OrcamentoId, EstabId)).ReturnsAsync(orc);
        _cobrancaRepo.Setup(r => r.ObterPorOrcamentoOuNulo(OrcamentoId, EstabId)).ReturnsAsync((Cobranca?)null);

        Cobranca? cobrancaSalva = null;
        _cobrancaRepo.Setup(r => r.Salvar(It.IsAny<Cobranca>()))
            .Callback<Cobranca>(c => cobrancaSalva = c)
            .Returns(Task.CompletedTask);

        await _sut.Handle(CriarEvento(total: 1500m));

        Assert.That(cobrancaSalva, Is.Not.Null);
        Assert.That(cobrancaSalva!.Origem, Is.EqualTo("Cirurgia"));
        Assert.That(cobrancaSalva.OrcamentoId, Is.EqualTo(OrcamentoId));
        Assert.That(cobrancaSalva.ValorCobrado, Is.EqualTo(1500m));
        Assert.That(cobrancaSalva.PacienteId, Is.EqualTo(PacienteId));
        Assert.That(cobrancaSalva.EstabelecimentoId, Is.EqualTo(EstabId));
        Assert.That(cobrancaSalva.TipoAtendimento, Is.EqualTo(TipoAtendimento.Particular));
        Assert.That(cobrancaSalva.Status, Is.EqualTo(StatusCobranca.Aberta));
    }

    // ── CA102: atomicidade — falha reverte aprovação ─────────────────────────

    [Test]
    public void Handle_OrcamentoNaoEncontrado_Lanca()
    {
        _orcamentoRepo.Setup(r => r.ObterPorIdCompletoOuNulo(OrcamentoId, EstabId)).ReturnsAsync((Orcamento?)null);

        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(CriarEvento()));
    }

    // ── CA103: idempotência — re-aprovação com mesmo total é no-op ───────────

    [Test]
    public async Task Handle_CobrancaExistenteMesmoTotal_NaoSalva()
    {
        var orc = CriarOrcamento();
        _orcamentoRepo.Setup(r => r.ObterPorIdCompletoOuNulo(OrcamentoId, EstabId)).ReturnsAsync(orc);
        _cobrancaRepo.Setup(r => r.ObterPorOrcamentoOuNulo(OrcamentoId, EstabId))
            .ReturnsAsync(CriarCobrancaCirurgia(1500m));

        await _sut.Handle(CriarEvento(total: 1500m));

        // Não deve chamar Salvar (no-op idempotente).
        _cobrancaRepo.Verify(r => r.Salvar(It.IsAny<Cobranca>()), Times.Never);
    }

    // ── CA105: re-aprovação com total diferente sincroniza valor + grava histórico ──

    [Test]
    public async Task Handle_CobrancaExistenteTotalDiferente_SincronizaValorEGravaHistorico()
    {
        var orc = CriarOrcamento();
        _orcamentoRepo.Setup(r => r.ObterPorIdCompletoOuNulo(OrcamentoId, EstabId)).ReturnsAsync(orc);
        var cobrancaExistente = CriarCobrancaCirurgia(1500m);
        _cobrancaRepo.Setup(r => r.ObterPorOrcamentoOuNulo(OrcamentoId, EstabId))
            .ReturnsAsync(cobrancaExistente);

        Cobranca? cobrancaSalva = null;
        _cobrancaRepo.Setup(r => r.Salvar(It.IsAny<Cobranca>()))
            .Callback<Cobranca>(c => cobrancaSalva = c)
            .Returns(Task.CompletedTask);

        await _sut.Handle(CriarEvento(total: 2000m));

        Assert.That(cobrancaSalva, Is.Not.Null);
        Assert.That(cobrancaSalva!.ValorCobrado, Is.EqualTo(2000m));
        Assert.That(cobrancaSalva.HistoricoValor.Count, Is.EqualTo(1));
        var hist = cobrancaSalva.HistoricoValor.First();
        Assert.That(hist.ValorAnterior, Is.EqualTo(1500m));
        Assert.That(hist.ValorNovo, Is.EqualTo(2000m));
    }

    // ── CA107: redução abaixo do pago → BusinessException 422 (R9) ──────────

    [Test]
    public void Handle_ReducaoAbaixoDoPago_LancaBusinessException()
    {
        var orc = CriarOrcamento();
        _orcamentoRepo.Setup(r => r.ObterPorIdCompletoOuNulo(OrcamentoId, EstabId)).ReturnsAsync(orc);

        // Cobrança de 1500, com pagamento de 1200 injetado via reflexão.
        // Tentativa de reduzir para 1000 < 1200 (pago líquido) → BusinessException R9.
        var cobrancaExistente = CriarCobrancaCirurgia(1500m);
        var pagamento = Pagamento.Criar(200L, 1200m, 1L, 1, 0m, 0m, DateOnly.FromDateTime(DateTime.Today), UsuarioId);
        typeof(Cobranca)
            .GetField("_pagamentos", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(cobrancaExistente, new System.Collections.Generic.List<Pagamento> { pagamento });

        _cobrancaRepo.Setup(r => r.ObterPorOrcamentoOuNulo(OrcamentoId, EstabId))
            .ReturnsAsync(cobrancaExistente);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(CriarEvento(total: 1000m)));
        Assert.That(ex!.Message, Does.Contain("menor que o total já pago"));
    }

    // ── CA110/CA111: multi-tenant — evento de outro estabelecimento não encontra orçamento ─

    [Test]
    public void Handle_OrcamentoDeOutroEstabelecimento_Lanca()
    {
        // Repositório retorna null quando o tenant não bate (falha-fechada).
        _orcamentoRepo.Setup(r => r.ObterPorIdCompletoOuNulo(OrcamentoId, EstabId)).ReturnsAsync((Orcamento?)null);

        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(CriarEvento()));
    }

    [Test]
    public async Task Handle_CobrancaExisteEmOutroEstab_NaoInterfere()
    {
        // Mesmo que haja cobrança no EstabOutroId, não é retornada pelo repo (multi-tenant).
        var orc = CriarOrcamento();
        _orcamentoRepo.Setup(r => r.ObterPorIdCompletoOuNulo(OrcamentoId, EstabId)).ReturnsAsync(orc);
        // Repo retorna null pois a cobrança pertence a outro tenant.
        _cobrancaRepo.Setup(r => r.ObterPorOrcamentoOuNulo(OrcamentoId, EstabId)).ReturnsAsync((Cobranca?)null);

        Cobranca? cobrancaSalva = null;
        _cobrancaRepo.Setup(r => r.Salvar(It.IsAny<Cobranca>()))
            .Callback<Cobranca>(c => cobrancaSalva = c)
            .Returns(Task.CompletedTask);

        await _sut.Handle(CriarEvento(total: 1500m));

        // Deve criar nova cobrança para EstabId (isolamento de tenant).
        Assert.That(cobrancaSalva, Is.Not.Null);
        Assert.That(cobrancaSalva!.EstabelecimentoId, Is.EqualTo(EstabId));
    }

    // ── CA116: guard de cirurgias — orçamentos sem cirurgias não geram cobrança ─

    [Test]
    public async Task Handle_OrcamentoSemCirurgias_RetornaSemSalvarNada()
    {
        // Orçamento criado só com itens (sem cirurgias) — aggregate válido sem a collection.
        var validade = DateOnly.FromDateTime(DateTime.Today.AddDays(30));
        var item = new Orcamento.ItemPayload("Consulta pós-operatória", 1, 200m, 0m);
        var orcSemCirurgia = Orcamento.Criar(EstabId, PacienteId, validade, null, UsuarioId, null, itens: [item]);
        orcSemCirurgia.SimularIdBanco(OrcamentoId);

        _orcamentoRepo.Setup(r => r.ObterPorIdCompletoOuNulo(OrcamentoId, EstabId)).ReturnsAsync(orcSemCirurgia);

        await _sut.Handle(CriarEvento(total: 200m));

        // Não deve consultar cobrança existente nem salvar nada.
        _cobrancaRepo.Verify(r => r.ObterPorOrcamentoOuNulo(It.IsAny<long>(), It.IsAny<long>()), Times.Never);
        _cobrancaRepo.Verify(r => r.Salvar(It.IsAny<Cobranca>()), Times.Never);
    }

    [Test]
    public async Task Handle_OrcamentoMistoItensECirurgias_CriaCobranca()
    {
        // Orçamento com itens simples E cirurgias → deve criar cobrança normalmente.
        var validade = DateOnly.FromDateTime(DateTime.Today.AddDays(30));
        var item = new Orcamento.ItemPayload("Taxa de sala", 1, 500m, 0m);
        var cirurgia = new Orcamento.CirurgiaPayload(null, "Rinoplastia", 1, 60, 1500m);
        var orcMisto = Orcamento.Criar(EstabId, PacienteId, validade, null, UsuarioId, null, itens: [item], cirurgias: [cirurgia]);
        orcMisto.SimularIdBanco(OrcamentoId);

        _orcamentoRepo.Setup(r => r.ObterPorIdCompletoOuNulo(OrcamentoId, EstabId)).ReturnsAsync(orcMisto);
        _cobrancaRepo.Setup(r => r.ObterPorOrcamentoOuNulo(OrcamentoId, EstabId)).ReturnsAsync((Cobranca?)null);

        Cobranca? cobrancaSalva = null;
        _cobrancaRepo.Setup(r => r.Salvar(It.IsAny<Cobranca>()))
            .Callback<Cobranca>(c => cobrancaSalva = c)
            .Returns(Task.CompletedTask);

        await _sut.Handle(CriarEvento(total: 2000m));

        Assert.That(cobrancaSalva, Is.Not.Null);
        Assert.That(cobrancaSalva!.Origem, Is.EqualTo("Cirurgia"));
        Assert.That(cobrancaSalva.ValorCobrado, Is.EqualTo(2000m));
    }
}
