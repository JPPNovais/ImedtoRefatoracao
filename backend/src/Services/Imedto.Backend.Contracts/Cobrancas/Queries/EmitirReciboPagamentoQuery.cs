using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Cobrancas.Queries;

/// <summary>
/// Query que gera o PDF do recibo de um pagamento quitado (F8/CA118).
/// Retorna os bytes do PDF para stream direto na response.
/// </summary>
public class EmitirReciboPagamentoQuery : IQuery<byte[]>
{
    public long PagamentoId { get; init; }
    public long EstabelecimentoId { get; init; }
    public Guid UsuarioId { get; init; }
}
