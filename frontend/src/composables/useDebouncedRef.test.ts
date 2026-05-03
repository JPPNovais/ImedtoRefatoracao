import { describe, it, expect, beforeEach, afterEach, vi } from "vitest"
import { ref, effectScope, nextTick } from "vue"
import { useDebouncedRef } from "./useDebouncedRef"

describe("useDebouncedRef", () => {
    beforeEach(() => {
        vi.useFakeTimers()
    })
    afterEach(() => {
        vi.useRealTimers()
        vi.restoreAllMocks()
    })

    it("inicializa com o valor atual do source", () => {
        const source = ref("inicial")
        const debounced = useDebouncedRef(source)
        expect(debounced.value).toBe("inicial")
    })

    it("nao atualiza imediatamente apos source mudar", async () => {
        const source = ref("a")
        const debounced = useDebouncedRef(source, 300)
        source.value = "b"
        await nextTick()
        expect(debounced.value).toBe("a")
    })

    it("atualiza apos o delay", async () => {
        const source = ref("a")
        const debounced = useDebouncedRef(source, 300)
        source.value = "b"
        await nextTick() // watcher dispara, agenda setTimeout
        vi.advanceTimersByTime(300)
        expect(debounced.value).toBe("b")
    })

    it("zera o timer a cada nova mudanca (so emite ultima)", async () => {
        const source = ref("a")
        const debounced = useDebouncedRef(source, 300)
        source.value = "b"
        await nextTick()
        vi.advanceTimersByTime(150)
        source.value = "c"
        await nextTick()
        vi.advanceTimersByTime(150)
        expect(debounced.value).toBe("a")
        vi.advanceTimersByTime(150)
        expect(debounced.value).toBe("c")
    })

    it("limpa timer quando o scope eh destruido", async () => {
        const source = ref("a")
        const scope = effectScope()
        let debounced: ReturnType<typeof useDebouncedRef<string>> | null = null
        scope.run(() => {
            debounced = useDebouncedRef(source, 300)
        })
        source.value = "b"
        await nextTick()
        scope.stop()
        vi.advanceTimersByTime(500)
        expect(debounced!.value).toBe("a")
    })

    it("respeita delay customizado", async () => {
        const source = ref(0)
        const debounced = useDebouncedRef(source, 1000)
        source.value = 1
        await nextTick()
        vi.advanceTimersByTime(500)
        expect(debounced.value).toBe(0)
        vi.advanceTimersByTime(500)
        expect(debounced.value).toBe(1)
    })
})
