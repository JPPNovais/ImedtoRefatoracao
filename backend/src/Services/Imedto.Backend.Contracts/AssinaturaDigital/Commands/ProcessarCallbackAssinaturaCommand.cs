using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.AssinaturaDigital.Commands;

/// <summary>
/// Processa o callback do provedor BirdID/VIDaaS após assinatura.
/// O handler valida a assinatura HMAC do payload antes de qualquer mutação.
/// </summary>
public class ProcessarCallbackAssinaturaCommand : ICommand
{
    public long ReceitaId { get; set; }
    /// <summary>Payload JSON bruto do webhook para validação HMAC.</summary>
    public string PayloadJson { get; set; } = string.Empty;
    /// <summary>Header de assinatura do provedor (ex.: X-BirdID-Signature).</summary>
    public string HeaderAssinatura { get; set; } = string.Empty;
}
