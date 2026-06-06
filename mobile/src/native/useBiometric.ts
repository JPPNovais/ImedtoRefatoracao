import { Capacitor } from "@capacitor/core"
import { NativeBiometric } from "capacitor-native-biometric"

/** Biometria (FaceID/digital) — login rápido e revelação de PII sensível (LGPD). */
export function useBiometric() {
  async function disponivel(): Promise<boolean> {
    if (!Capacitor.isNativePlatform()) return false
    try {
      const r = await NativeBiometric.isAvailable()
      return r.isAvailable
    } catch {
      return false
    }
  }

  /** Pede confirmação de identidade. Na web (dev) resolve true para fluxo demonstrável. */
  async function confirmar(motivo: string): Promise<boolean> {
    if (!Capacitor.isNativePlatform()) return true
    try {
      await NativeBiometric.verifyIdentity({
        reason: motivo,
        title: "Imedto",
        subtitle: "Confirme sua identidade",
      })
      return true
    } catch {
      return false
    }
  }

  return { disponivel, confirmar }
}
