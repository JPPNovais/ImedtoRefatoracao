import { defineStore } from "pinia"
import { ref } from "vue"
import { Preferences } from "@capacitor/preferences"

export type ThemeMode = "light" | "dark" | "auto"

const THEME_KEY = "imedto.theme-mode"

/** Estado de UI transversal: tema, toast, offline, push banner em foreground. */
export const useUiStore = defineStore("ui", () => {
  const themeMode = ref<ThemeMode>("light")
  const isDark = ref(false)

  const toastMsg = ref("")
  const toastKind = ref<"success" | "error">("success")
  const toastVisible = ref(false)
  let toastT: ReturnType<typeof setTimeout> | undefined

  const offline = ref(false)
  const lastSyncLabel = ref<string>("")

  const pushBanner = ref<{ titulo: string; corpo: string; link?: string } | null>(null)
  let pushT: ReturnType<typeof setTimeout> | undefined

  // Sheet ativo (action sheet do FAB, switcher de estabelecimento, etc.)
  const activeSheet = ref<string | null>(null)
  function openSheet(name: string) {
    activeSheet.value = name
  }
  function closeSheet() {
    activeSheet.value = null
  }

  // Confirm dialog (ações destrutivas: faltou, recusar, sair…)
  const confirmState = ref<{
    title: string
    msg: string
    confirmLabel?: string
    onConfirm: () => void
  } | null>(null)
  function openConfirm(opts: {
    title: string
    msg: string
    confirmLabel?: string
    onConfirm: () => void
  }) {
    confirmState.value = opts
  }
  function closeConfirm() {
    confirmState.value = null
  }

  function applyTheme(mode: ThemeMode) {
    themeMode.value = mode
    const dark =
      mode === "dark" ||
      (mode === "auto" && window.matchMedia("(prefers-color-scheme: dark)").matches)
    isDark.value = dark
    void Preferences.set({ key: THEME_KEY, value: mode })
  }

  async function initTheme() {
    const { value } = await Preferences.get({ key: THEME_KEY })
    applyTheme((value as ThemeMode) || "light")
    window.matchMedia("(prefers-color-scheme: dark)").addEventListener("change", () => {
      if (themeMode.value === "auto") applyTheme("auto")
    })
  }

  function toast(msg: string, kind: "success" | "error" = "success") {
    toastMsg.value = msg
    toastKind.value = kind
    toastVisible.value = true
    clearTimeout(toastT)
    toastT = setTimeout(() => (toastVisible.value = false), 2600)
  }

  function setOffline(v: boolean) {
    offline.value = v
    if (v) {
      const now = new Date()
      lastSyncLabel.value = `${String(now.getHours()).padStart(2, "0")}:${String(
        now.getMinutes(),
      ).padStart(2, "0")}`
    }
  }

  function showPush(titulo: string, corpo: string, link?: string) {
    pushBanner.value = { titulo, corpo, link }
    clearTimeout(pushT)
    pushT = setTimeout(() => (pushBanner.value = null), 5200)
  }
  function dismissPush() {
    pushBanner.value = null
    clearTimeout(pushT)
  }

  return {
    themeMode,
    isDark,
    toastMsg,
    toastKind,
    toastVisible,
    offline,
    lastSyncLabel,
    pushBanner,
    activeSheet,
    confirmState,
    applyTheme,
    initTheme,
    toast,
    setOffline,
    showPush,
    dismissPush,
    openSheet,
    closeSheet,
    openConfirm,
    closeConfirm,
  }
})
