/**
 * ViaCEP — busca de endereço pelo CEP (API pública dos Correios via ViaCEP).
 *
 * Usa fetch direto (não httpClient) porque é uma API pública, externa,
 * que não tem nada a ver com o BFF do Imedto e não precisa de cookies/CORS.
 */

export interface ViaCepResultado {
    cep: string
    logradouro: string
    bairro: string
    complemento: string
    localidade: string  // cidade
    uf: string
    erro?: boolean
}

export interface EnderecoEncontrado {
    cep: string
    logradouro: string
    bairro: string
    complemento: string
    cidade: string
    uf: string
}

/**
 * Busca o endereço pelo CEP. Retorna null se não encontrado ou se houver
 * erro de rede — chamador trata como "não preencheu, deixa o usuário digitar".
 */
export async function buscarPorCep(cep: string): Promise<EnderecoEncontrado | null> {
    const digitos = cep.replace(/\D/g, "")
    if (digitos.length !== 8) return null

    try {
        const r = await fetch(`https://viacep.com.br/ws/${digitos}/json/`)
        if (!r.ok) return null
        const data = (await r.json()) as ViaCepResultado
        if (data.erro) return null
        return {
            cep: data.cep,
            logradouro: data.logradouro,
            bairro: data.bairro,
            complemento: data.complemento,
            cidade: data.localidade,
            uf: data.uf,
        }
    } catch {
        return null
    }
}
