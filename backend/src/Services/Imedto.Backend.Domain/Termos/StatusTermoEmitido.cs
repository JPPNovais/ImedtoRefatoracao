namespace Imedto.Backend.Domain.Termos;

/// <summary>
/// Estados de um termo emitido para um paciente. Persistido como string.
/// Transições válidas:
///   Pendente → Assinado | Recusado | Expirado
///   Assinado → Revogado
/// </summary>
public enum StatusTermoEmitido
{
    Pendente,
    Assinado,
    Recusado,
    Revogado,
    Expirado,
}
