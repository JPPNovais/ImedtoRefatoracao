using Imedto.Backend.Domain.Cobrancas;
using Imedto.Backend.Domain.Financeiro.Events;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Financeiro;

public class Lancamento : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual TipoLancamento Tipo { get; protected set; }
    public virtual string Descricao { get; protected set; } = string.Empty;
    public virtual decimal Valor { get; protected set; }
    public virtual DateOnly DataVencimento { get; protected set; }
    public virtual DateOnly? DataPagamento { get; protected set; }
    public virtual StatusLancamento Status { get; protected set; }
    public virtual string Categoria { get; protected set; } = string.Empty;
    public virtual long? OrcamentoId { get; protected set; }
    /// <summary>F1 — vínculo com cobrança (INV-3). Nullable — lançamentos manuais não têm cobrança.</summary>
    public virtual long? CobrancaId { get; protected set; }
    /// <summary>F1 — vínculo com pagamento (INV-3). Nullable — lançamentos manuais não têm pagamento.</summary>
    public virtual long? PagamentoId { get; protected set; }
    public virtual Guid CriadoPorUsuarioId { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    protected Lancamento() { }

    /// <summary>
    /// Cria lançamento vinculado a cobrança (INV-3 — Cobranças F1).
    /// PagamentoId é vinculado após persistência via <see cref="VincularPagamento"/>.
    /// Nasce com Status=Pendente; o caller chama Pagar() logo após.
    /// </summary>
    public static Lancamento CriarParaPagamento(
        long estabelecimentoId,
        string descricao,
        decimal valor,
        DateOnly dataVencimento,
        string categoria,
        Guid criadoPorUsuarioId,
        long cobrancaId)
    {
        var l = Criar(estabelecimentoId, TipoLancamento.Receita, descricao, valor, dataVencimento, categoria, criadoPorUsuarioId);
        l.CobrancaId = cobrancaId;
        return l;
    }

    /// <summary>
    /// Vincula o pagamento ao lançamento após o Pagamento ter sido persistido (INV-3).
    /// Deve ser chamado ANTES do commit da transação.
    /// </summary>
    public void VincularPagamento(long pagamentoId)
    {
        if (pagamentoId <= 0) throw new InvalidOperationException("PagamentoId inválido.");
        PagamentoId = pagamentoId;
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Cria lançamento de estorno (INV-7 — DC2).
    /// Valor negativo + categoria "Estorno: Pagamento" — sem coluna/flag nova.
    /// CobrancaId e PagamentoId (do pagamento estornado) são desnormalizados para rastreabilidade.
    /// Nasce já como Pago (estorno ocorre no ato do registro, não é conta a pagar).
    /// </summary>
    public static Lancamento CriarParaEstorno(
        long estabelecimentoId,
        decimal valorEstornado,
        DateOnly dataEstorno,
        Guid criadoPorUsuarioId,
        long cobrancaId,
        long pagamentoId)
    {
        // Valor negativo representa saída de receita (abate no fluxo de caixa).
        var l = new Lancamento
        {
            EstabelecimentoId = estabelecimentoId,
            Tipo = TipoLancamento.Receita,
            Descricao = "Estorno de pagamento",
            Valor = -ArredondamentoMonetario.Arredondar(valorEstornado),
            DataVencimento = dataEstorno,
            Status = StatusLancamento.Pago,
            DataPagamento = dataEstorno,
            Categoria = CategoriaEstorno,
            CobrancaId = cobrancaId,
            PagamentoId = pagamentoId,
            CriadoPorUsuarioId = criadoPorUsuarioId,
            CriadoEm = DateTime.UtcNow
        };
        return l;
    }

    /// <summary>Categoria de catálogo usada por lançamentos de estorno (DC2).</summary>
    public const string CategoriaEstorno = "Estorno: Pagamento";

    public static Lancamento Criar(
        long estabelecimentoId,
        TipoLancamento tipo,
        string descricao,
        decimal valor,
        DateOnly dataVencimento,
        string categoria,
        Guid criadoPorUsuarioId,
        long? orcamentoId = null)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            throw new BusinessException("Descrição é obrigatória.");
        if (valor <= 0)
            throw new BusinessException("Valor deve ser maior que zero.");
        if (string.IsNullOrWhiteSpace(categoria))
            throw new BusinessException("Categoria é obrigatória.");

        var lancamento = new Lancamento
        {
            EstabelecimentoId = estabelecimentoId,
            Tipo = tipo,
            Descricao = descricao.Trim(),
            Valor = valor,
            DataVencimento = dataVencimento,
            Status = StatusLancamento.Pendente,
            Categoria = categoria.Trim(),
            OrcamentoId = orcamentoId,
            CriadoPorUsuarioId = criadoPorUsuarioId,
            CriadoEm = DateTime.UtcNow
        };

        lancamento.AddDomainEvent(new LancamentoCriadoEvent(0, estabelecimentoId, tipo, valor));
        return lancamento;
    }

    public void Atualizar(string descricao, decimal valor, DateOnly dataVencimento, string categoria)
    {
        if (Status == StatusLancamento.Cancelado)
            throw new BusinessException("Lançamento cancelado não pode ser editado.");
        if (Status == StatusLancamento.Pago)
            throw new BusinessException("Lançamento já pago não pode ser editado.");
        if (string.IsNullOrWhiteSpace(descricao))
            throw new BusinessException("Descrição é obrigatória.");
        if (valor <= 0)
            throw new BusinessException("Valor deve ser maior que zero.");

        Descricao = descricao.Trim();
        Valor = valor;
        DataVencimento = dataVencimento;
        Categoria = categoria.Trim();
        AtualizadoEm = DateTime.UtcNow;
    }

    public void Pagar(DateOnly? dataPagamento = null)
    {
        if (Status != StatusLancamento.Pendente)
            throw new BusinessException("Apenas lançamentos pendentes podem ser baixados como pagos.");

        DataPagamento = dataPagamento ?? DateOnly.FromDateTime(DateTime.UtcNow);
        Status = StatusLancamento.Pago;
        AtualizadoEm = DateTime.UtcNow;
        AddDomainEvent(new LancamentoPagoEvent(Id, EstabelecimentoId, Tipo, Valor, DataPagamento.Value));
    }

    public void Cancelar()
    {
        if (Status == StatusLancamento.Cancelado)
            throw new BusinessException("Lançamento já está cancelado.");
        if (Status == StatusLancamento.Pago)
            throw new BusinessException("Lançamento já pago não pode ser cancelado.");

        Status = StatusLancamento.Cancelado;
        AtualizadoEm = DateTime.UtcNow;
    }
}
