namespace Imedto.Backend.Contracts.PacienteConvenios.Queries.Results;

/// <summary>
/// DTO de carteirinha para a aba Convênios do paciente.
/// numero_carteirinha é PII — exposto apenas na tela de detalhe do paciente (com audit de leitura).
/// </summary>
public record PacienteConvenioDto(
    long Id,
    long ConvenioId,
    string ConvenioNome,
    long? PlanoId,
    string? PlanoNome,
    string NumeroCarteirinha,
    DateOnly? Validade,
    bool Ativo);

/// <summary>
/// DTO minimalista para pré-seleção no check-in (R8).
/// Inclui numero e validade para exibição informativa no modal.
/// </summary>
public record CarteirinhaCheckInDto(
    long ConvenioId,
    string ConvenioNome,
    long? PlanoId,
    string? PlanoNome,
    string NumeroCarteirinha,
    DateOnly? Validade);
