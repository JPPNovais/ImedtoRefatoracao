namespace Imedto.Backend.Domain.Receitas;

/// <summary>
/// Tipo da notificação para receitas <see cref="TipoReceita.Controlada"/>
/// segundo a Portaria 344/98 (SVS/MS).
/// <list type="bullet">
///   <item><see cref="A"/> — Notificação A (amarela): entorpecentes (Listas A1, A2, A3).</item>
///   <item><see cref="B"/> — Notificação B (azul): psicotrópicos (Listas B1, B2).</item>
///   <item><see cref="C"/> — Notificação C (branca): outras substâncias sob controle especial.</item>
///   <item><see cref="Especial"/> — receituário de controle especial (ex.: anabolizantes / Lista C5).</item>
/// </list>
/// Só faz sentido quando <see cref="Receita.Tipo"/> == <see cref="TipoReceita.Controlada"/>.
/// </summary>
public enum TipoNotificacao
{
    A,
    B,
    C,
    Especial
}
