namespace Imedto.Backend.Domain.Termos;

/// <summary>
/// Estados de um termo emitido para um paciente. Persistido como string.
///
/// Transição válida para termos novos (briefing 2026-06-12_002):
///   Pendente → Assinado → Revogado
///
/// Estados legados (somente leitura — compatibilidade com histórico já materializado):
///   <see cref="Recusado"/>: paciente recusou pelo link público (fluxo removido).
///   <see cref="Expirado"/>: link público venceu ou AceiteLink Pendente migrado pela migration de transição.
/// </summary>
public enum StatusTermoEmitido
{
    Pendente,
    Assinado,

    /// <summary>LEGADO — paciente recusou pelo link público (removido). Somente leitura.</summary>
    Recusado,

    Revogado,

    /// <summary>LEGADO — link público expirou ou AceiteLink migrado. Somente leitura para histórico.</summary>
    Expirado,
}
