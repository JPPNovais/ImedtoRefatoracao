import { Capacitor } from "@capacitor/core"
import { Camera, CameraResultType, CameraSource } from "@capacitor/camera"

export interface FotoCapturada {
  dataUrl: string
  blob: Blob
}

async function dataUrlToBlob(dataUrl: string): Promise<Blob> {
  const res = await fetch(dataUrl)
  return res.blob()
}

/** Câmera/galeria para anexar foto de lesão/exame/documento ao prontuário. */
export function useCamera() {
  async function capturar(source: "foto" | "galeria"): Promise<FotoCapturada | null> {
    if (!Capacitor.isNativePlatform()) {
      // Web (dev): usa o seletor de arquivo nativo do browser.
      return pickFromFileInput()
    }
    try {
      const photo = await Camera.getPhoto({
        quality: 80,
        resultType: CameraResultType.DataUrl,
        source: source === "foto" ? CameraSource.Camera : CameraSource.Photos,
        allowEditing: false,
      })
      if (!photo.dataUrl) return null
      return { dataUrl: photo.dataUrl, blob: await dataUrlToBlob(photo.dataUrl) }
    } catch {
      return null
    }
  }

  function pickFromFileInput(): Promise<FotoCapturada | null> {
    return new Promise((resolve) => {
      const input = document.createElement("input")
      input.type = "file"
      input.accept = "image/*"
      input.onchange = () => {
        const file = input.files?.[0]
        if (!file) return resolve(null)
        const reader = new FileReader()
        reader.onload = () => resolve({ dataUrl: String(reader.result), blob: file })
        reader.readAsDataURL(file)
      }
      input.click()
    })
  }

  return { capturar }
}
