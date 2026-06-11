using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Cobrancas;

/// <summary>
/// Entidade filha de <see cref="Cobranca"/>. Imutável por design — nunca recebe update/delete
/// de valor (preparado para estorno com histórico na F2/INV-7).
/// </summary>
public class Pagamento : Entity
{
    public virtual long CobrancaId { get; protected set; }
    public virtual decimal Valor { get; protected set; }
    public virtual long FormaPagamentoId { get; protected set; }
    public virtual int Parcelas { get; protected set; }
    public virtual decimal Juros { get; protected set; }
    /// <summary>Taxa derivada da ConfigTaxaFormaPagamento no ato do pagamento (informativa).</summary>
    public virtual decimal Taxa { get; protected set; }
    public virtual DateOnly DataPagamento { get; protected set; }
    public virtual Guid RegistradoPorUsuarioId { get; protected set; }
    /// <summary>FK para o Lancamento gerado atomicamente (INV-3). Nulo até VincularLancamento() ser chamado antes do commit.</summary>
    public virtual long? LancamentoId { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    /// <summary>
    /// Timestamp da primeira emissão de recibo (F8/CA128). Null = recibo nunca emitido.
    /// Idempotente: reemissões não sobrescrevem o timestamp original.
    /// </summary>
    public virtual DateTime? ReciboEmitidoEm { get; protected set; }

    // Para uso do EF Core e do Cobranca.RegistrarPagamento.
    protected Pagamento() { }

    public static Pagamento Criar(
        long cobrancaId,
        decimal valor,
        long formaPagamentoId,
        int parcelas,
        decimal juros,
        decimal taxa,
        DateOnly dataPagamento,
        Guid registradoPorUsuarioId)
    {
        return new Pagamento
        {
            CobrancaId = cobrancaId,
            Valor = ArredondamentoMonetario.Arredondar(valor),
            FormaPagamentoId = formaPagamentoId,
            Parcelas = parcelas,
            Juros = ArredondamentoMonetario.Arredondar(juros),
            Taxa = ArredondamentoMonetario.Arredondar(taxa),
            DataPagamento = dataPagamento,
            RegistradoPorUsuarioId = registradoPorUsuarioId,
            CriadoEm = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Define o LancamentoId após a persistência atômica (INV-3).
    /// Deve ser chamado pelo handler ANTES do commit da transação.
    /// </summary>
    public void VincularLancamento(long lancamentoId)
    {
        if (lancamentoId <= 0)
            throw new InvalidOperationException("LancamentoId inválido.");
        LancamentoId = lancamentoId;
    }

    /// <summary>
    /// Valida que o pagamento pode gerar recibo e grava a flag na 1ª emissão (F8/CA120/CA128).
    /// - Pagamento estornado → lança <see cref="BusinessException"/> (CA120).
    /// - Idempotente: reemissões não sobrescrevem <see cref="ReciboEmitidoEm"/> (CA128).
    /// O caller (handler) persiste o aggregate após a chamada.
    /// </summary>
    /// <param name="estornos">Lista de estornos já carregados do aggregate pai.</param>
    public void RegistrarEmissaoRecibo(IEnumerable<EstornoPagamento> estornos)
    {
        // CA120: pagamento estornado bloqueia
        if (estornos.Any(e => e.PagamentoId == Id))
            throw new BusinessException("Pagamento estornado não pode gerar recibo.");

        // CA128: grava apenas na 1ª emissão
        if (ReciboEmitidoEm is null)
            ReciboEmitidoEm = DateTime.UtcNow;
    }
}
