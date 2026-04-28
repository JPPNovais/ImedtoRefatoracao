export interface SugestaoSecaoRequest {
    secaoAlvoTitulo: string
    secoesContexto: Record<string, string>
}

export async function* sugerirSecaoProntuario(
    request: SugestaoSecaoRequest,
): AsyncGenerator<string, void, unknown> {
    const response = await fetch("/api/ia/sugestao-secao", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            secaoAlvoTitulo: request.secaoAlvoTitulo,
            secoesContexto: request.secoesContexto,
        }),
        credentials: "include",
    })

    if (!response.ok) throw new Error(`Erro ${response.status} ao chamar IA`)
    if (!response.body) throw new Error("Resposta sem corpo")

    const reader = response.body.getReader()
    const decoder = new TextDecoder()
    let buffer = ""

    try {
        while (true) {
            const { done, value } = await reader.read()
            if (done) break

            buffer += decoder.decode(value, { stream: true })
            const lines = buffer.split("\n")
            buffer = lines.pop() ?? ""

            for (const line of lines) {
                if (!line.startsWith("data: ")) continue
                const data = line.slice(6)
                if (data === "[DONE]") return

                try {
                    const parsed = JSON.parse(data)
                    if (parsed.erro) throw new Error(parsed.erro)
                    if (parsed.text) yield parsed.text
                } catch (e) {
                    if (e instanceof Error && e.message.startsWith("IA")) throw e
                }
            }
        }
    } finally {
        reader.releaseLock()
    }
}
