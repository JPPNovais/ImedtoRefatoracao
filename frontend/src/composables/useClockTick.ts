/*
 * Tick global em segundos para componentes que mostram tempo decorrido
 * (timer "Em atendimento agora", etc.).
 *
 * Um único setInterval compartilhado entre todos os consumidores — evita
 * gerar timers duplicados quando vários componentes montam ao mesmo tempo.
 */
import { onMounted, onUnmounted, ref } from "vue"

const agora = ref(Date.now())
let timerId: ReturnType<typeof setInterval> | null = null
let consumidores = 0

function start() {
    if (timerId !== null) return
    timerId = setInterval(() => { agora.value = Date.now() }, 1000)
}
function stop() {
    if (timerId === null) return
    clearInterval(timerId)
    timerId = null
}

export function useClockTick() {
    onMounted(() => {
        consumidores++
        if (consumidores === 1) start()
    })
    onUnmounted(() => {
        consumidores--
        if (consumidores <= 0) {
            consumidores = 0
            stop()
        }
    })
    return { agora }
}

/**
 * Formata duração em milisegundos como "MM:SS" ou "HH:MM:SS" se >= 1h.
 */
export function formatarDuracao(ms: number): string {
    if (!Number.isFinite(ms) || ms < 0) return "00:00"
    const totalSeg = Math.floor(ms / 1000)
    const h  = Math.floor(totalSeg / 3600)
    const m  = Math.floor((totalSeg % 3600) / 60)
    const s  = totalSeg % 60
    const dois = (n: number) => String(n).padStart(2, "0")
    return h > 0 ? `${dois(h)}:${dois(m)}:${dois(s)}` : `${dois(m)}:${dois(s)}`
}
