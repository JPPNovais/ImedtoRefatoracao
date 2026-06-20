import { describe, it, expect, vi, afterEach } from "vitest"
import { useDebouncedRef } from "@/composables/useDebouncedRef"

describe("useDebouncedRef", () => {
  afterEach(() => {
    vi.useRealTimers()
  })

  it("retorna o valor inicial imediatamente", () => {
    const ref = useDebouncedRef("inicial")
    expect(ref.value).toBe("inicial")
  })

  it("não atualiza o valor antes do delay", () => {
    vi.useFakeTimers()
    const ref = useDebouncedRef("inicial", 350)
    ref.value = "novo"
    expect(ref.value).toBe("inicial") // ainda não disparou
    vi.clearAllTimers()
  })

  it("atualiza o valor após o delay completo", () => {
    vi.useFakeTimers()
    const ref = useDebouncedRef("inicial", 350)
    ref.value = "novo"
    vi.advanceTimersByTime(350)
    expect(ref.value).toBe("novo")
  })

  it("cancela o timer anterior quando o valor muda novamente antes do delay", () => {
    vi.useFakeTimers()
    const ref = useDebouncedRef("inicial", 350)

    ref.value = "intermediário"
    vi.advanceTimersByTime(200) // ainda não disparou
    ref.value = "final"
    vi.advanceTimersByTime(350) // agora dispara com "final"

    expect(ref.value).toBe("final")
  })

  it("não mantém valor intermediário após debounce completo com múltiplas trocas", () => {
    vi.useFakeTimers()
    const ref = useDebouncedRef("a", 350)

    ref.value = "b"
    vi.advanceTimersByTime(100)
    ref.value = "c"
    vi.advanceTimersByTime(100)
    ref.value = "d"
    vi.advanceTimersByTime(350)

    expect(ref.value).toBe("d")
  })

  it("funciona com delay padrão de 350ms quando não especificado", () => {
    vi.useFakeTimers()
    const ref = useDebouncedRef("x")
    ref.value = "y"
    vi.advanceTimersByTime(349)
    expect(ref.value).toBe("x") // ainda não
    vi.advanceTimersByTime(1)
    expect(ref.value).toBe("y") // agora sim
  })

  it("funciona com tipo número", () => {
    vi.useFakeTimers()
    const ref = useDebouncedRef(0, 100)
    ref.value = 42
    vi.advanceTimersByTime(100)
    expect(ref.value).toBe(42)
  })
})
