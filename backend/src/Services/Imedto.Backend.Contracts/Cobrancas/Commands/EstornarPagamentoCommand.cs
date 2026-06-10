using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Cobrancas.Commands;

/// <summary>
/// Estorna um pagamento de uma cobrança (INV-7).
/// Gera EstornoPagamento + Lancamento negativo na mesma transação (atômico).
/// </summary>
public class EstornarPagamentoCommand : ICommand
{
    public long CobrancaId { get; init; }
    public long PagamentoId { get; init; }
    public long EstabelecimentoId { get; init; }
    public Guid UsuarioId { get; init; }
    public string Motivo { get; init; } = string.Empty;
}
