namespace Imedto.Backend.Contracts.Prontuarios.Queries.Results;

/// <summary>
/// DTO do painel persistente de pendências. Minimização LGPD (R4/CA71):
/// apenas tipo da ação, vínculos por id e status — sem conteúdo clínico ou PII.
/// </summary>
public record PendenciaAbertaDto(
    long Id,
    long EvolucaoId,
    string Acao,       // valor de AcaoPendencia como string
    string Status,     // "Pendente"
    DateTime CriadoEm
);
