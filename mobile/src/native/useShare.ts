import { Capacitor } from "@capacitor/core"
import { Share } from "@capacitor/share"

/** Share sheet nativo — enviar link/PDF de receita, atestado, exame e confirmação. */
export function useShare() {
  async function compartilhar(opts: { title: string; text: string; url?: string }): Promise<boolean> {
    if (Capacitor.isNativePlatform()) {
      try {
        await Share.share({ title: opts.title, text: opts.text, url: opts.url, dialogTitle: opts.title })
        return true
      } catch {
        return false
      }
    }
    // Web (dev): Web Share API, com fallback de copiar para a área de transferência.
    const nav = navigator as Navigator & { share?: (d: ShareData) => Promise<void> }
    if (nav.share) {
      try {
        await nav.share({ title: opts.title, text: opts.text, url: opts.url })
        return true
      } catch {
        return false
      }
    }
    try {
      await navigator.clipboard.writeText(opts.url || opts.text)
      return true
    } catch {
      return false
    }
  }

  return { compartilhar }
}
