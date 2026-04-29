using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Assinaturas.Queries;

/// <summary>Retorna a assinatura do estabelecimento ativo na request (header X-Estabelecimento-Id).</summary>
public class ObterMinhaAssinaturaQuery : IQuery<AssinaturaDto?>
{
    public long EstabelecimentoId { get; set; }
}
