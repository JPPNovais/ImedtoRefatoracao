namespace Imedto.Backend.Domain.Orcamentos;

/// <summary>
/// Status de um orçamento. Alinhado ao legado:
/// <c>Rascunho</c> (estado inicial pré-envio) → <c>Enviado</c> (apresentado ao paciente)
/// → <c>Aprovado</c> | <c>Recusado</c> | <c>Cancelado</c> (terminais).
/// <c>Expirado</c> é definido pela validade vencida.
/// </summary>
public enum OrcamentoStatus
{
    Rascunho,
    Enviado,
    Aprovado,
    Recusado,
    Cancelado,
    Expirado
}
