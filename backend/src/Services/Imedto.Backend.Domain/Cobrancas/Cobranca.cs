using Imedto.Backend.Domain.Cobrancas.Events;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Cobrancas;

/// <summary>
/// Aggregate root de contas a receber do paciente.
/// Conceito-âncora: cobrado ≠ pago. Uma Cobranca é a conta a receber;
/// Pagamento(s) a quitam ao longo do tempo.
/// </summary>
public class Cobranca : Entity
{
    // ── Campos do aggregate ──────────────────────────────────────────────────
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual long PacienteId { get; protected set; }
    /// <summary>Origem aberta: F1 = "Consulta". F4/F5 = "Procedimento"/"Cirurgia".</summary>
    public virtual string Origem { get; protected set; } = string.Empty;
    public virtual long? AgendamentoId { get; protected set; }
    /// <summary>Reservado para F5 (orçamento de cirurgia).</summary>
    public virtual long? OrcamentoId { get; protected set; }
    /// <summary>
    /// Id da evolução de origem — preenchido para cobrança de procedimento (F4).
    /// Junto com o índice UNIQUE parcial garante idempotência: 1 cobrança de procedimento por evolução.
    /// </summary>
    public virtual long? EvolucaoId { get; protected set; }
    public virtual TipoAtendimento TipoAtendimento { get; protected set; }
    /// <summary>Preenchido pela F6 quando tipo=Convenio. Null para Particular.</summary>
    public virtual long? ConvenioId { get; protected set; }
    public virtual decimal ValorCobrado { get; protected set; }
    // ── Guia de autorização (F6/R10) — apenas para tipo=Convenio ─────────────
    /// <summary>Nº da guia emitida pelo convênio. Null = guia pendente.</summary>
    public virtual string? GuiaNumero { get; protected set; }
    /// <summary>Senha de autorização do convênio. Opcional.</summary>
    public virtual string? GuiaSenha { get; protected set; }
    /// <summary>Data em que a guia foi autorizada. Opcional.</summary>
    public virtual DateOnly? GuiaAutorizadaEm { get; protected set; }
    public virtual decimal Desconto { get; protected set; }
    public virtual StatusCobranca Status { get; protected set; }
    public virtual string? Descricao { get; protected set; }
    public virtual Guid CriadoPorUsuarioId { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    // Coleção de pagamentos filhos — Pagamento não tem navegação de volta ao aggregate
    // (DDD: filhos acessados via root). EF preenche.
    private readonly List<Pagamento> _pagamentos = new();
    public virtual IReadOnlyCollection<Pagamento> Pagamentos => _pagamentos.AsReadOnly();

    // Coleção de estornos filhos (INV-7). EF preenche junto com Include nos repositórios.
    private readonly List<EstornoPagamento> _estornos = new();
    public virtual IReadOnlyCollection<EstornoPagamento> Estornos => _estornos.AsReadOnly();

    // Histórico de alterações de valor (F5/R8). EF preenche quando a cobrança é carregada com Include.
    private readonly List<CobrancaHistoricoValor> _historicoValor = new();
    public virtual IReadOnlyCollection<CobrancaHistoricoValor> HistoricoValor => _historicoValor.AsReadOnly();

    protected Cobranca() { }

    // ── Factory ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Cria cobrança a partir de uma consulta no check-in (R1/INV-6).
    /// F6: aceita convenioId opcional quando tipoAtendimento=Convenio.
    /// </summary>
    public static Cobranca CriarParaConsulta(
        long estabelecimentoId,
        long pacienteId,
        long agendamentoId,
        TipoAtendimento tipoAtendimento,
        decimal valorCobrado,
        string descricao,
        Guid criadoPorUsuarioId,
        long? convenioId = null)
    {
        // INV-6: tenant + paciente obrigatórios
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (pacienteId <= 0)
            throw new BusinessException("Paciente é obrigatório.");
        if (agendamentoId <= 0)
            throw new BusinessException("Agendamento é obrigatório.");

        // R12: Convênio nasce sem valor de balcão
        if (tipoAtendimento == TipoAtendimento.Particular && valorCobrado <= 0)
            throw new BusinessException("Valor cobrado deve ser maior que zero para consultas particulares.");

        var cobranca = new Cobranca
        {
            EstabelecimentoId = estabelecimentoId,
            PacienteId = pacienteId,
            Origem = "Consulta",
            AgendamentoId = agendamentoId,
            TipoAtendimento = tipoAtendimento,
            // F6/R7: grava convenioId quando convênio; Particular nunca tem convenioId
            ConvenioId = tipoAtendimento == TipoAtendimento.Convenio ? convenioId : null,
            ValorCobrado = tipoAtendimento == TipoAtendimento.Particular
                ? ArredondamentoMonetario.Arredondar(valorCobrado)
                : 0m,
            Desconto = 0m,
            Status = StatusCobranca.Aberta,
            Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim(),
            CriadoPorUsuarioId = criadoPorUsuarioId,
            CriadoEm = DateTime.UtcNow
        };

        return cobranca;
    }

    /// <summary>
    /// Cria cobrança de procedimento ao marcar realizado (F4/R3).
    /// Tipo sempre Particular nesta fase (D2). EvolucaoId sinaliza origem e garante idempotência via índice parcial.
    /// </summary>
    public static Cobranca CriarParaProcedimento(
        long estabelecimentoId,
        long pacienteId,
        long evolucaoId,
        long? agendamentoId,
        decimal valorCobrado,
        string descricao,
        Guid criadoPorUsuarioId)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (pacienteId <= 0)
            throw new BusinessException("Paciente é obrigatório.");
        if (evolucaoId <= 0)
            throw new BusinessException("Evolução é obrigatória.");
        if (valorCobrado <= 0)
            throw new BusinessException("Valor cobrado deve ser maior que zero para procedimento realizado.");
        if (criadoPorUsuarioId == Guid.Empty)
            throw new BusinessException("Usuário criador é obrigatório.");

        return new Cobranca
        {
            EstabelecimentoId = estabelecimentoId,
            PacienteId = pacienteId,
            Origem = "Procedimento",
            AgendamentoId = agendamentoId,
            EvolucaoId = evolucaoId,
            TipoAtendimento = TipoAtendimento.Particular,
            ValorCobrado = ArredondamentoMonetario.Arredondar(valorCobrado),
            Desconto = 0m,
            Status = StatusCobranca.Aberta,
            Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim(),
            CriadoPorUsuarioId = criadoPorUsuarioId,
            CriadoEm = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Cria cobrança de cirurgia na aprovação do orçamento (F5/R5).
    /// Tipo sempre Particular (D2 herdada da F4). AgendamentoId=null (cirurgia não tem agendamento na cobrança).
    /// </summary>
    public static Cobranca CriarParaCirurgia(
        long estabelecimentoId,
        long pacienteId,
        long orcamentoId,
        decimal valorCobrado,
        string descricao,
        Guid criadoPorUsuarioId)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (pacienteId <= 0)
            throw new BusinessException("Paciente é obrigatório.");
        if (orcamentoId <= 0)
            throw new BusinessException("Orçamento é obrigatório.");
        if (valorCobrado <= 0)
            throw new BusinessException("Valor cobrado deve ser maior que zero para cirurgia.");
        if (criadoPorUsuarioId == Guid.Empty)
            throw new BusinessException("Usuário criador é obrigatório.");

        return new Cobranca
        {
            EstabelecimentoId = estabelecimentoId,
            PacienteId = pacienteId,
            Origem = "Cirurgia",
            OrcamentoId = orcamentoId,
            AgendamentoId = null,
            EvolucaoId = null,
            TipoAtendimento = TipoAtendimento.Particular,
            ValorCobrado = ArredondamentoMonetario.Arredondar(valorCobrado),
            Desconto = 0m,
            Status = StatusCobranca.Aberta,
            Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim(),
            CriadoPorUsuarioId = criadoPorUsuarioId,
            CriadoEm = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Sincroniza o valor cobrado de uma cobrança de cirurgia quando o orçamento é re-aprovado (F5/R8).
    /// - Se novoTotal == ValorCobrado atual → no-op (sem histórico).
    /// - Se diferente → valida R9, atualiza ValorCobrado, grava CobrancaHistoricoValor, recalcula status.
    /// </summary>
    public virtual void SincronizarValorCobrado(decimal novoTotal, Guid alteradoPorUsuarioId)
    {
        novoTotal = ArredondamentoMonetario.Arredondar(novoTotal);

        // No-op: valor não mudou (CA103 — re-aprovação idêntica não gera histórico).
        if (novoTotal == ValorCobrado) return;

        // R9: bloqueio de redução abaixo do pago líquido.
        var pagoLiquido = TotalPagoLiquido();
        if (novoTotal - Desconto < pagoLiquido)
            throw new BusinessException(
                $"O novo valor da cirurgia (R$ {novoTotal:N2}) é menor que o total já pago (R$ {pagoLiquido:N2}). " +
                "Estorne pagamentos antes de reduzir o valor.");

        var historico = CobrancaHistoricoValor.Criar(
            cobrancaId: Id,
            estabelecimentoId: EstabelecimentoId,
            valorAnterior: ValorCobrado,
            valorNovo: novoTotal,
            alteradoPorUsuarioId: alteradoPorUsuarioId);

        _historicoValor.Add(historico);
        ValorCobrado = novoTotal;
        RecalcularStatus();
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>Adiciona domain event após persistir (Id disponível).</summary>
    public virtual void MarcarComoCriada()
    {
        if (Id == 0)
            throw new InvalidOperationException("Cobrança ainda não foi persistida — Id é 0.");
        AddDomainEvent(new CobrancaCriadaEvent(Id, EstabelecimentoId, PacienteId, TipoAtendimento, ValorCobrado));
    }

    // ── Desconto ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Aplica (ou altera) desconto na cobrança (INV-4/INV-8).
    /// O controle de RBAC é feito pelo handler — o aggregate recebe a flag.
    /// </summary>
    public virtual void AplicarDesconto(decimal desconto, bool podeAplicarDesconto)
    {
        if (!podeAplicarDesconto && desconto > 0)
            throw new BusinessException("Sem permissão para aplicar desconto.");
        if (desconto < 0)
            throw new BusinessException("Desconto não pode ser negativo.");
        if (desconto > ValorCobrado)
            throw new BusinessException("Desconto não pode ser maior que o valor cobrado.");

        Desconto = ArredondamentoMonetario.Arredondar(desconto);
        RecalcularStatus();
        AtualizadoEm = DateTime.UtcNow;
    }

    // ── Pagamento ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Registra um pagamento (INV-1/INV-5). A taxa é derivada da config e passada pelo handler.
    /// Retorna o Pagamento criado para que o handler vincule o LancamentoId.
    /// </summary>
    public virtual Pagamento RegistrarPagamento(
        decimal valor,
        long formaPagamentoId,
        int parcelas,
        decimal juros,
        decimal taxa,
        DateOnly dataPagamento,
        Guid registradoPorUsuarioId)
    {
        // R12: Convênio sem balcão na F1
        if (TipoAtendimento == TipoAtendimento.Convenio)
            throw new BusinessException("Pagamento de balcão não disponível para cobranças de convênio.");

        if (Status == StatusCobranca.Cancelada)
            throw new BusinessException("Não é possível registrar pagamento em cobrança cancelada.");

        // INV-5
        if (valor <= 0)
            throw new BusinessException("Valor do pagamento deve ser maior que zero.");

        var saldo = SaldoDevedor();
        var valorArredondado = ArredondamentoMonetario.Arredondar(valor);

        // INV-1: não pagar além do saldo
        if (valorArredondado > saldo)
            throw new BusinessException("Valor do pagamento excede o saldo devedor.");

        var pagamento = Pagamento.Criar(
            Id,
            valor,
            formaPagamentoId,
            parcelas < 1 ? 1 : parcelas,
            juros < 0 ? 0 : juros,
            taxa,
            dataPagamento,
            registradoPorUsuarioId);

        _pagamentos.Add(pagamento);
        RecalcularStatus();
        AtualizadoEm = DateTime.UtcNow;

        return pagamento;
    }

    // ── Estorno (INV-7) ───────────────────────────────────────────────────────

    /// <summary>
    /// Estorna um pagamento por inteiro (DC3 — estorno total).
    /// Atômico com Lancamento de estorno — handler chama VincularLancamento antes do commit.
    /// Retorna o EstornoPagamento criado para que o handler vincule o LancamentoEstornoId.
    /// </summary>
    public virtual EstornoPagamento EstornarPagamento(
        long pagamentoId,
        string motivo,
        Guid estornadoPorUsuarioId)
    {
        // R12: Convênio não tem pagamento de balcão, logo não tem estorno
        if (TipoAtendimento == TipoAtendimento.Convenio)
            throw new BusinessException("Estorno não disponível para cobranças de convênio.");

        if (Status == StatusCobranca.Cancelada)
            throw new BusinessException("Não é possível estornar pagamento em cobrança cancelada.");

        // R5: motivo obrigatório
        if (string.IsNullOrWhiteSpace(motivo))
            throw new BusinessException("Motivo do estorno é obrigatório.");

        var pagamento = _pagamentos.FirstOrDefault(p => p.Id == pagamentoId)
            ?? throw new BusinessException("Não encontrado.");

        // R8: 1 estorno por pagamento (DC3)
        if (_estornos.Any(e => e.PagamentoId == pagamentoId))
            throw new BusinessException("Este pagamento já foi estornado.");

        var estorno = EstornoPagamento.Criar(
            pagamentoId: pagamento.Id,
            cobrancaId: Id,
            estabelecimentoId: EstabelecimentoId,
            valor: pagamento.Valor,
            motivo: motivo,
            estornadoPorUsuarioId: estornadoPorUsuarioId);

        _estornos.Add(estorno);
        RecalcularStatus();
        AtualizadoEm = DateTime.UtcNow;

        return estorno;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>Valor total líquido (cobrado − desconto).</summary>
    public decimal TotalLiquido()
        => ArredondamentoMonetario.Arredondar(ValorCobrado - Desconto);

    /// <summary>Soma bruta dos pagamentos registrados.</summary>
    public decimal TotalPago()
        => _pagamentos.Aggregate(0m, (acc, p) => acc + p.Valor);

    /// <summary>Soma dos estornos registrados.</summary>
    public decimal TotalEstornado()
        => _estornos.Aggregate(0m, (acc, e) => acc + e.Valor);

    /// <summary>Total pago líquido de estornos (R3/DC7).</summary>
    public decimal TotalPagoLiquido()
        => ArredondamentoMonetario.Arredondar(TotalPago() - TotalEstornado());

    /// <summary>Saldo devedor restante (considerando estornos — R3).</summary>
    public decimal SaldoDevedor()
        => ArredondamentoMonetario.Arredondar(TotalLiquido() - TotalPagoLiquido());

    /// <summary>INV-2: status derivado da soma líquida (pago − estornado). Nunca setado à mão (salvo Cancelada).</summary>
    private void RecalcularStatus()
    {
        if (Status == StatusCobranca.Cancelada) return;
        var totalLiquido = TotalPagoLiquido();
        var liquido = TotalLiquido();

        Status = totalLiquido == 0
            ? StatusCobranca.Aberta
            : totalLiquido < liquido
                ? StatusCobranca.ParcialmentePaga
                : StatusCobranca.Paga;
    }

    // ── Guia (F6/R10) ────────────────────────────────────────────────────────

    /// <summary>
    /// Registra ou edita os dados de guia/autorização da cobrança convênio (F6/R10).
    /// Rejeita cobrança Particular (guia só faz sentido em convênio).
    /// Estado derivado: preenchida se GuiaNumero presente.
    /// </summary>
    public virtual void RegistrarGuia(string guiaNumero, string? guiaSenha, DateOnly? guiaAutorizadaEm)
    {
        // R10: guia só em convênio
        if (TipoAtendimento != TipoAtendimento.Convenio)
            throw new BusinessException("Guia de autorização disponível apenas para cobranças de convênio.");

        if (string.IsNullOrWhiteSpace(guiaNumero))
            throw new BusinessException("Número da guia é obrigatório.");

        GuiaNumero = guiaNumero.Trim();
        GuiaSenha = string.IsNullOrWhiteSpace(guiaSenha) ? null : guiaSenha.Trim();
        GuiaAutorizadaEm = guiaAutorizadaEm;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Cancelar()
    {
        if (Status == StatusCobranca.Cancelada)
            throw new BusinessException("Cobrança já está cancelada.");
        Status = StatusCobranca.Cancelada;
        AtualizadoEm = DateTime.UtcNow;
    }
}
