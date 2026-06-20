import { Capacitor } from "@capacitor/core"
import { StatusBar, Style } from "@capacitor/status-bar"

/**
 * Configura a barra de status nativa do iOS/Android:
 *  - `overlay: false` → a barra ganha um espaço PRÓPRIO; o webview não desenha
 *    atrás dela (some o conteúdo/artefato vazando na faixa do notch/Dynamic Island).
 *  - estilo acompanha o tema (ícones escuros no claro / claros no escuro).
 * No-op no web. Tolera plugin indisponível.
 */
export async function configurarStatusBar(isDark: boolean): Promise<void> {
  if (!Capacitor.isNativePlatform()) return
  try {
    await StatusBar.setOverlaysWebView({ overlay: false })
    // Style.Dark = conteúdo escuro (tema claro); Style.Light = conteúdo claro (tema escuro).
    await StatusBar.setStyle({ style: isDark ? Style.Light : Style.Dark })
    // Cor de fundo da barra (efetivo no Android; iOS usa o fundo do webview).
    await StatusBar.setBackgroundColor({ color: isDark ? "#131019" : "#ffffff" })
  } catch {
    /* plugin indisponível — ignora */
  }
}
