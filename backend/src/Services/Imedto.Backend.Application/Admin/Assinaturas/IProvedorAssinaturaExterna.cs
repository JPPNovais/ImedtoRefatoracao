namespace Imedto.Backend.Application.Admin.Assinaturas;

// ============================================================
// PORTA PARA INTEGRAÇÃO FUTURA — NÃO IMPLEMENTADA NESTA ENTREGA
// ============================================================
// Esta interface é o contrato de integração com o gateway de pagamento externo (R10 do briefing
// 2026-06-11_003). Nenhuma implementação concreta existe agora — zero adapter, zero SDK,
// zero registro de DI obrigatório.
//
// Quando o gateway for implementado, o adapter concreto vive na Infraestrutura
// (espelhando BirdIdAssinaturaProvider, ResendEmailProvider, S3StorageProvider, etc.)
// e é registrado via Container.cs com a chave de configuração do provider.
//
// As colunas dormentes (origem, referencia_externa, status_cobranca) em imedto_assinaturas
// foram desenhadas para receber os dados que esses métodos retornarão.
// ============================================================

/// <summary>
/// Resultado da criação de cobrança no gateway externo.
/// </summary>
public record ResultadoCobrancaExterna(
    bool Sucesso,
    string? ReferenciaExterna,
    string StatusCobranca,
    string? MensagemErro = null);

/// <summary>
/// Status de cobrança retornado pela consulta ao gateway.
/// </summary>
public record StatusCobrancaExterna(
    string ReferenciaExterna,
    string Status,
    bool Sucesso,
    string? MensagemErro = null);

/// <summary>
/// Resultado da confirmação de pagamento via webhook do gateway.
/// </summary>
public record ConfirmacaoPagamento(
    bool Sucesso,
    string? ReferenciaExterna,
    DateTimeOffset? PagamentoEm = null,
    string? MensagemErro = null);

/// <summary>
/// Porta de integração com o gateway de pagamento externo (contrato futuro — sem implementação).
///
/// Nenhuma regra de assinatura referencia um provider concreto.
/// A implementação concreta (adapter) vive na Infraestrutura quando existir.
/// </summary>
public interface IProvedorAssinaturaExterna
{
    /// <summary>
    /// Cria uma cobrança recorrente no gateway para o estabelecimento/plano.
    /// Retorna a referencia_externa e o status_cobranca inicial para gravar em imedto_assinaturas.
    /// </summary>
    Task<ResultadoCobrancaExterna> CriarCobrancaAsync(
        long estabelecimentoId,
        Guid planoId,
        CancellationToken ct = default);

    /// <summary>
    /// Consulta o status atual de uma cobrança pelo seu id externo no gateway.
    /// </summary>
    Task<StatusCobrancaExterna> ConsultarStatusCobrancaAsync(
        string referenciaExterna,
        CancellationToken ct = default);

    /// <summary>
    /// Processa o callback/webhook de confirmação de pagamento enviado pelo gateway.
    /// O payloadAssinado deve ser validado por HMAC/assinatura antes de mutar estado.
    /// </summary>
    Task<ConfirmacaoPagamento> ConfirmarPagamentoPorWebhookAsync(
        string payloadAssinado,
        CancellationToken ct = default);
}
