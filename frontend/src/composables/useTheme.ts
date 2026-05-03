/**
 * useTheme — preferência de tema (claro/escuro/auto) global e persistida.
 *
 * Persistência: localStorage com a chave `imedto-theme` (sincronizada com o
 * inline script de `index.html`, que aplica o tema antes do primeiro render
 * para evitar FOUC).
 *
 * Modo "auto" segue `prefers-color-scheme` do sistema operacional e reage
 * dinamicamente quando o usuário troca o tema do SO.
 */
import { ref, computed, watch, onBeforeUnmount } from "vue"

export type Theme = "light" | "dark" | "auto"

const STORAGE_KEY = "imedto-theme"

function lerInicial(): Theme {
    if (typeof localStorage === "undefined") return "auto"
    const v = localStorage.getItem(STORAGE_KEY)
    return v === "light" || v === "dark" || v === "auto" ? v : "auto"
}

function preferenciaSistema(): "light" | "dark" {
    if (typeof window === "undefined" || !window.matchMedia) return "light"
    return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light"
}

// Estado global compartilhado — uma única fonte de verdade pra app inteira.
const tema = ref<Theme>(lerInicial())

function aplicarNoDocumento(t: Theme) {
    if (typeof document === "undefined") return
    const efetivo = t === "auto" ? preferenciaSistema() : t
    document.documentElement.classList.toggle("dark", efetivo === "dark")
    document.documentElement.dataset.theme = efetivo
}

// Aplica imediatamente (caso o inline script de index.html não tenha rodado).
aplicarNoDocumento(tema.value)

// Sincroniza com o sistema quando estiver em "auto".
let mql: MediaQueryList | null = null
let mqlListener: ((e: MediaQueryListEvent) => void) | null = null

function ouvirSistema() {
    if (typeof window === "undefined" || !window.matchMedia) return
    pararDeOuvirSistema()
    mql = window.matchMedia("(prefers-color-scheme: dark)")
    mqlListener = () => { if (tema.value === "auto") aplicarNoDocumento("auto") }
    mql.addEventListener("change", mqlListener)
}

function pararDeOuvirSistema() {
    if (mql && mqlListener) {
        mql.removeEventListener("change", mqlListener)
        mql = null
        mqlListener = null
    }
}

watch(tema, (t) => {
    aplicarNoDocumento(t)
    try { localStorage.setItem(STORAGE_KEY, t) } catch { /* noop — modo privado */ }
    if (t === "auto") ouvirSistema()
    else pararDeOuvirSistema()
}, { immediate: true })

export function useTheme() {
    onBeforeUnmount(() => { /* noop — listener é global, não desfaz por componente */ })

    const temaEfetivo = computed<"light" | "dark">(() =>
        tema.value === "auto" ? preferenciaSistema() : tema.value,
    )

    function definirTema(t: Theme) {
        tema.value = t
    }

    return {
        tema,           // Theme — pode ser "auto"
        temaEfetivo,    // "light" | "dark" — resolvido
        definirTema,
    }
}
