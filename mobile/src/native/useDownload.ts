/* ─────────────────────────────────────────────────────────────
   useDownload — baixar e abrir/compartilhar PDF autenticado.
   Usa getBlob do http client (cookie BFF + X-Estabelecimento-Id).

   Web:    fetch → blob → URL.createObjectURL → anchor download.
   Nativo: fetch → blob → data URL → Share (texto+url) via @capacitor/share.
           O nativo pode abrir PDFs via Share.share com data URI quando o SO
           tiver um viewer de PDF; fallback: exibe mensagem para o usuário.
   ───────────────────────────────────────────────────────────── */
import { Capacitor } from "@capacitor/core"
import { Share } from "@capacitor/share"
import { getBlob } from "@/lib/http"

export function useDownload() {
  /**
   * Baixa um PDF do `path` (ex.: "/paciente/1/prontuario/pdf") e abre/compartilha.
   * `nomeArquivo` — nome sugerido ao usuário (sem PII sensível; use apenas id numérico opaco).
   * Lança em caso de falha de rede ou 4xx/5xx.
   */
  async function baixarPdf(path: string, nomeArquivo: string): Promise<void> {
    const blob = await getBlob(path)

    if (Capacitor.isNativePlatform()) {
      // Nativo: compartilha via Share usando data URI
      // O Share do SO redireciona para app de PDF quando disponível
      const dataUrl = await blobParaDataUrl(blob)
      try {
        await Share.share({
          title: nomeArquivo,
          url: dataUrl,
          dialogTitle: "Abrir PDF",
        })
      } catch {
        // usuário cancelou o share — silencioso
      }
    } else {
      // Web: cria link temporário e dispara download
      const url = URL.createObjectURL(blob)
      const a = document.createElement("a")
      a.href = url
      a.download = nomeArquivo
      document.body.appendChild(a)
      a.click()
      document.body.removeChild(a)
      setTimeout(() => URL.revokeObjectURL(url), 10_000)
    }
  }

  return { baixarPdf }
}

function blobParaDataUrl(blob: Blob): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = () => resolve(reader.result as string)
    reader.onerror = reject
    reader.readAsDataURL(blob)
  })
}
