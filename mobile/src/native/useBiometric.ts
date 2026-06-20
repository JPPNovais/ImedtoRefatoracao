import { Capacitor } from "@capacitor/core"
import { Preferences } from "@capacitor/preferences"
import { NativeBiometric } from "capacitor-native-biometric"

const CHAVE_BIO = "pref_biometria"

/** Lê a preferência do usuário (default: ativado). */
async function habilitadaPeloUsuario(): Promise<boolean> {
  try {
    const { value } = await Preferences.get({ key: CHAVE_BIO })
    return value === null ? true : value === "true"
  } catch {
    // Fallback para localStorage (web)
    try {
      const v = localStorage.getItem(CHAVE_BIO)
      return v === null ? true : v === "true"
    } catch {
      return true
    }
  }
}

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

  /** Pede confirmação de identidade.
   *  Retorna false imediatamente se o usuário desativou a biometria nas preferências.
   *  Na web (dev) resolve true para fluxo demonstrável.
   *  No nativo: se a biometria não estiver disponível/cadastrada no aparelho, permite
   *  seguir mesmo assim (o acesso é auditado no backend via /dados-sensiveis — a
   *  autorização real é do lado do servidor, não do sensor). Se disponível, exige. */
  async function confirmar(motivo: string): Promise<boolean> {
    const prefAtiva = await habilitadaPeloUsuario()
    if (!prefAtiva) return false
    if (!Capacitor.isNativePlatform()) return true
    const bioDisp = await disponivel()
    // Biometria não cadastrada/disponível no aparelho → libera (backend audita).
    if (!bioDisp) return true
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

  return { disponivel, confirmar, habilitadaPeloUsuario }
}
