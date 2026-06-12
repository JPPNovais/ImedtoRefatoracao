namespace Imedto.Backend.Domain.Termos;

/// <summary>
/// Tipo de assinatura do termo emitido.
///   <see cref="PdfAnexado"/>: emissor sobe documento físico assinado (foto JPG/PNG convertida
///   para PDF, ou PDF direto). Único tipo válido para termos novos (briefing 2026-06-12_002).
///   <see cref="AceiteLink"/>: legado — link público (fluxo removido). Mantido apenas para
///   leitura de histórico; não utilizável em emissão nova.
/// </summary>
public enum AssinaturaTipo
{
    PdfAnexado,

    /// <summary>
    /// LEGADO — aceite por link público foi removido (briefing 2026-06-12_002).
    /// Manter o valor no enum para compatibilidade de leitura do histórico materializado.
    /// Não usar em emissão nova.
    /// </summary>
    AceiteLink,
}
