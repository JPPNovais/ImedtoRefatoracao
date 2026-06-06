import { customRef } from "vue"

/** Debounce de busca (espelha useDebouncedRef do web). Performance: busca só
    dispara após o usuário parar de digitar. */
export function useDebouncedRef<T>(value: T, delay = 350) {
  let timeout: ReturnType<typeof setTimeout>
  return customRef<T>((track, trigger) => ({
    get() {
      track()
      return value
    },
    set(newValue) {
      clearTimeout(timeout)
      timeout = setTimeout(() => {
        value = newValue
        trigger()
      }, delay)
    },
  }))
}
