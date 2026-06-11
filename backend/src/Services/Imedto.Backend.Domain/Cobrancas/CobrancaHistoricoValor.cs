using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Cobrancas;

/// <summary>
/// Registro imutável de alteração de valor cobrado de uma cirurgia (F5/R8).
/// Filha de <see cref="Cobranca"/> (acessa via aggregate root — append-only).
/// </summary>
public class CobrancaHistoricoValor : Entity
{
    public virtual long CobrancaId { get; protected set; }
    /// <summary>Tenant redundante para defense-in-depth/filtro de leitura (espelha EstornoPagamento).</summary>
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual decimal ValorAnterior { get; protected set; }
    public virtual decimal ValorNovo { get; protected set; }
    public virtual Guid AlteradoPorUsuarioId { get; protected set; }
    public virtual DateTime AlteradoEm { get; protected set; }

    protected CobrancaHistoricoValor() { }

    /// <summary>
    /// Cria um registro de histórico. Chamado apenas por <see cref="Cobranca.SincronizarValorCobrado"/>.
    /// </summary>
    internal static CobrancaHistoricoValor Criar(
        long cobrancaId,
        long estabelecimentoId,
        decimal valorAnterior,
        decimal valorNovo,
        Guid alteradoPorUsuarioId)
    {
        return new CobrancaHistoricoValor
        {
            CobrancaId = cobrancaId,
            EstabelecimentoId = estabelecimentoId,
            ValorAnterior = ArredondamentoMonetario.Arredondar(valorAnterior),
            ValorNovo = ArredondamentoMonetario.Arredondar(valorNovo),
            AlteradoPorUsuarioId = alteradoPorUsuarioId,
            AlteradoEm = DateTime.UtcNow,
        };
    }
}
