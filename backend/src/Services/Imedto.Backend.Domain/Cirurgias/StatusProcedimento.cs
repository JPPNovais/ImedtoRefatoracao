namespace Imedto.Backend.Domain.Cirurgias;

/// <summary>
/// Estados do ciclo de vida de um procedimento cirúrgico.
/// Transições válidas:
///  - Planejado → Confirmado | Realizado | Cancelado
///  - Confirmado → Realizado | Cancelado
///  - Realizado → (terminal)
///  - Cancelado → (terminal)
/// </summary>
public enum StatusProcedimento
{
    Planejado,
    Confirmado,
    Realizado,
    Cancelado
}
