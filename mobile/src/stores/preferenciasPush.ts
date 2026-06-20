import { defineStore } from "pinia"
import { ref } from "vue"
import { Preferences } from "@capacitor/preferences"
import type { PreferenciasPushDto } from "@/types"

const CHAVE = "pref_push_categorias"

const DEFAULTS: PreferenciasPushDto = {
  caixa: true,
  estoque: true,
  fotos: true,
  pagamento: true,
  automacao: true,
  avisos: true,
}

/** Preferências de push por categoria — locais ao device (sem endpoint de servidor no MVP).
    Persistidas via @capacitor/preferences com fallback em localStorage para dev web. */
export const usePreferenciasPushStore = defineStore("preferenciasPush", () => {
  const prefs = ref<PreferenciasPushDto>({ ...DEFAULTS })
  const carregado = ref(false)

  async function carregar() {
    if (carregado.value) return
    try {
      const { value } = await Preferences.get({ key: CHAVE })
      if (value) {
        const parsed = JSON.parse(value) as Partial<PreferenciasPushDto>
        prefs.value = { ...DEFAULTS, ...parsed }
      }
    } catch {
      // fallback: mantém defaults
    }
    carregado.value = true
  }

  async function alternar(categoria: keyof PreferenciasPushDto) {
    prefs.value[categoria] = !prefs.value[categoria]
    await _persistir()
  }

  async function _persistir() {
    try {
      await Preferences.set({ key: CHAVE, value: JSON.stringify(prefs.value) })
    } catch {
      // fallback web: localStorage (não crítico em nativo)
      try {
        localStorage.setItem(CHAVE, JSON.stringify(prefs.value))
      } catch {}
    }
  }

  /** Verifica se uma categoria de push deve exibir banner no foreground. */
  function deveExibir(categoria: keyof PreferenciasPushDto): boolean {
    return prefs.value[categoria]
  }

  return { prefs, carregado, carregar, alternar, deveExibir }
})
