using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Cobrancas;

/// <summary>
/// Registro imutável de estorno de um <see cref="Pagamento"/> (INV-7).
/// Filha de <see cref="Cobranca"/> (acessa via aggregate root).
/// Nunca apaga/edita o Pagamento original — estorno é histórico append-only.
/// </summary>
public class EstornoPagamento : Entity
{
    public virtual long PagamentoId { get; protected set; }
    /// <summary>Desnormalizado para filtro por cobrança/tenant (DC2).</summary>
    public virtual long CobrancaId { get; protected set; }
    public virtual long EstabelecimentoId { get; protected set; }
    /// <summary>Valor do pagamento estornado (= pagamento.Valor, estorno total — DC3).</summary>
    public virtual decimal Valor { get; protected set; }
    public virtual string Motivo { get; protected set; } = string.Empty;
    /// <summary>FK para o Lancamento negativo criado atomicamente (INV-7).</summary>
    public virtual long? LancamentoEstornoId { get; protected set; }
    public virtual Guid EstornadoPorUsuarioId { get; protected set; }
    public virtual DateOnly DataEstorno { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }

    protected EstornoPagamento() { }

    /// <summary>
    /// Cria o registro de estorno. Chamado pelo aggregate <see cref="Cobranca.EstornarPagamento"/>.
    /// O <see cref="LancamentoEstornoId"/> é vinculado em seguida via <see cref="VincularLancamento"/>
    /// (antes do commit — padrão INV-3 da F1).
    /// </summary>
    internal static EstornoPagamento Criar(
        long pagamentoId,
        long cobrancaId,
        long estabelecimentoId,
        decimal valor,
        string motivo,
        Guid estornadoPorUsuarioId)
    {
        return new EstornoPagamento
        {
            PagamentoId = pagamentoId,
            CobrancaId = cobrancaId,
            EstabelecimentoId = estabelecimentoId,
            Valor = ArredondamentoMonetario.Arredondar(valor),
            Motivo = motivo.Trim(),
            EstornadoPorUsuarioId = estornadoPorUsuarioId,
            DataEstorno = DateOnly.FromDateTime(DateTime.UtcNow),
            CriadoEm = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Vincula o lançamento de estorno após sua persistência (padrão INV-3 / INV-7).
    /// Deve ser chamado ANTES do commit da transação.
    /// </summary>
    public void VincularLancamento(long lancamentoEstornoId)
    {
        if (lancamentoEstornoId <= 0)
            throw new InvalidOperationException("LancamentoEstornoId inválido.");
        LancamentoEstornoId = lancamentoEstornoId;
    }
}
