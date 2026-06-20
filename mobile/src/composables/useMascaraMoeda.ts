/** Máscara de valor monetário BRL para inputs de texto.
 *  Uso: vincule `onValorInput` ao evento @input do campo e `:value="valorStr"`. */
export function useMascaraMoeda() {
  /** Formata uma string crua (dígitos) para o padrão "1.234,56". */
  function formatarValor(raw: string): string {
    const digits = raw.replace(/\D/g, "")
    if (!digits) return ""
    const num = parseInt(digits, 10) / 100
    return num.toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 })
  }

  /** Handler de @input para manter o cursor no fim e o valor formatado. */
  function onValorInput(e: Event, valorStr: { value: string }): void {
    const input = e.target as HTMLInputElement
    valorStr.value = formatarValor(input.value)
    const v = valorStr.value
    requestAnimationFrame(() => {
      input.value = v
      input.setSelectionRange(v.length, v.length)
    })
  }

  /** Define um valor numérico como string formatada. */
  function setValorNumerico(v: number, valorStr: { value: string }): void {
    valorStr.value = v.toLocaleString("pt-BR", { minimumFractionDigits: 2 })
  }

  /** Converte a string formatada de volta para número. */
  function valorNumerico(valorStr: string): number {
    const s = valorStr.replace(/\./g, "").replace(",", ".")
    const n = parseFloat(s)
    return isNaN(n) ? 0 : n
  }

  return { formatarValor, onValorInput, setValorNumerico, valorNumerico }
}
