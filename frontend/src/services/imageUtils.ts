/**
 * Redimensiona uma imagem para caber em um quadrado de até `maxLado` px,
 * preservando a proporção e aplicando compressão JPEG/WebP. Reduz drasticamente
 * o tamanho do arquivo antes do upload (ex.: foto de 4 MB → ~80 KB).
 *
 * Para PNG/GIF transparentes, a saída vira JPEG com fundo branco — adequado
 * para avatares e logos. GIFs animados perdem a animação (ficam em frame único).
 *
 * @param file       Arquivo escolhido pelo usuário.
 * @param maxLado    Lado máximo (px) — a imagem é encaixada num quadrado desse tamanho.
 * @param qualidade  0..1 (default 0.85 — bom equilíbrio entre qualidade e tamanho).
 * @returns          File no mesmo nome do original, mas com extensão e mime corretos.
 */
export async function redimensionarImagem(
    file: File,
    maxLado = 512,
    qualidade = 0.85,
): Promise<File> {
    const dataUrl = await lerComoDataUrl(file)
    const img = await carregarImagem(dataUrl)

    const escala = Math.min(1, maxLado / Math.max(img.width, img.height))
    const largura = Math.round(img.width * escala)
    const altura  = Math.round(img.height * escala)

    const canvas = document.createElement("canvas")
    canvas.width = largura
    canvas.height = altura

    const ctx = canvas.getContext("2d")
    if (!ctx) throw new Error("Não foi possível processar a imagem.")

    // Fundo branco para preservar transparência ao virar JPEG.
    ctx.fillStyle = "#ffffff"
    ctx.fillRect(0, 0, largura, altura)
    ctx.drawImage(img, 0, 0, largura, altura)

    const blob = await new Promise<Blob | null>(resolve =>
        canvas.toBlob(resolve, "image/jpeg", qualidade),
    )
    if (!blob) throw new Error("Falha ao comprimir a imagem.")

    const nomeBase = file.name.replace(/\.[^.]+$/, "")
    return new File([blob], `${nomeBase}.jpg`, { type: "image/jpeg" })
}

function lerComoDataUrl(file: File): Promise<string> {
    return new Promise((resolve, reject) => {
        const reader = new FileReader()
        reader.onload  = () => resolve(reader.result as string)
        reader.onerror = () => reject(reader.error ?? new Error("Erro ao ler o arquivo."))
        reader.readAsDataURL(file)
    })
}

function carregarImagem(src: string): Promise<HTMLImageElement> {
    return new Promise((resolve, reject) => {
        const img = new Image()
        img.onload  = () => resolve(img)
        img.onerror = () => reject(new Error("Não foi possível abrir a imagem."))
        img.src = src
    })
}
