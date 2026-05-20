namespace Imedto.Backend.Domain.Termos;

/// <summary>
/// Tipo de assinatura escolhido na emissão.
///   <see cref="PdfAnexado"/>: emissor sobe um PDF assinado fisicamente (offline).
///   <see cref="AceiteLink"/>: gera link público; paciente aceita/recusa via web (Fase 4).
/// </summary>
public enum AssinaturaTipo
{
    PdfAnexado,
    AceiteLink,
}
