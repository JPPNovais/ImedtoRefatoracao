/**
 * Consulta a API pública ViaCEP. Retorna `null` se o CEP for inválido ou não existir.
 * Helper de UX puro — a validação de domínio (formato, conteúdo) fica no backend.
 */
export interface EnderecoViaCep {
    cep: string
    logradouro: string
    complemento: string
    bairro: string
    localidade: string
    uf: string
}

export async function buscarCep(cepBruto: string): Promise<EnderecoViaCep | null> {
    const digitos = (cepBruto ?? "").replace(/\D/g, "")
    if (digitos.length !== 8) return null

    try {
        const resp = await fetch(`https://viacep.com.br/ws/${digitos}/json/`)
        if (!resp.ok) return null
        const data = await resp.json()
        if (data.erro) return null

        return {
            cep:         data.cep         ?? "",
            logradouro:  data.logradouro  ?? "",
            complemento: data.complemento ?? "",
            bairro:      data.bairro      ?? "",
            localidade:  data.localidade  ?? "",
            uf:          data.uf          ?? "",
        }
    } catch {
        return null
    }
}
