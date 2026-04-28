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
    public virtual Guid CriadoPorUsuarioId { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    protected Lancamento() { }

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
