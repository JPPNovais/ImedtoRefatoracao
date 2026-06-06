import { ref } from "vue"
import { Capacitor } from "@capacitor/core"
import { SpeechRecognition } from "@capacitor-community/speech-recognition"

/** Ditado por voz (STT do OS) no textarea de evolução/observações. */
export function useVoice() {
  const ouvindo = ref(false)

  async function iniciar(onResult: (texto: string) => void): Promise<void> {
    if (!Capacitor.isNativePlatform()) {
      // Web (dev): SpeechRecognition do browser, se houver.
      const W = window as unknown as {
        webkitSpeechRecognition?: new () => {
          lang: string
          onresult: (e: { results: { 0: { 0: { transcript: string } } } }) => void
          onend: () => void
          start: () => void
          stop: () => void
        }
      }
      const Rec = W.webkitSpeechRecognition
      if (!Rec) {
        ouvindo.value = false
        return
      }
      const rec = new Rec()
      rec.lang = "pt-BR"
      rec.onresult = (e) => onResult(e.results[0][0].transcript)
      rec.onend = () => (ouvindo.value = false)
      ouvindo.value = true
      rec.start()
      ;(iniciar as unknown as { _rec?: { stop: () => void } })._rec = rec
      return
    }

    const perm = await SpeechRecognition.requestPermissions()
    if (perm.speechRecognition !== "granted") return
    ouvindo.value = true
    await SpeechRecognition.start({
      language: "pt-BR",
      partialResults: false,
      popup: false,
    })
    SpeechRecognition.addListener("partialResults", (data: { matches: string[] }) => {
      if (data.matches?.length) onResult(data.matches[0])
    })
  }

  async function parar(): Promise<void> {
    ouvindo.value = false
    if (!Capacitor.isNativePlatform()) {
      ;(iniciar as unknown as { _rec?: { stop: () => void } })._rec?.stop()
      return
    }
    await SpeechRecognition.stop().catch(() => {})
  }

  return { ouvindo, iniciar, parar }
}
