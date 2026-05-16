/**
 * Helper compartilhado de cabeçalho institucional, marca d'água e rodapé
 * dos PDFs gerados no frontend (Prontuário e Relatórios).
 *
 * Tudo lazy: este módulo só é importado pelos composables `useProntuarioPdf`
 * e `useRelatorioPdf` — que por sua vez são chamados via `await import(...)`
 * em runtime. Resultado: o chunk do jsPDF + autotable + Nunito + estes
 * helpers fica fora do bundle inicial do app.
 *
 * Layout segue o mock de PrintPreview (sheet-head / sheet-foot / marca d'água
 * "IMEDTO" diagonal). Cores e tokens estão consolidados em `PDF_THEME` para
 * que QA / design possa ajustar tudo num só lugar.
 */
import type { jsPDF as JsPdfType } from "jspdf"
import { estabelecimentoService, type Estabelecimento } from "@/services/estabelecimentoService"
import {
    NUNITO_REGULAR_B64,
    NUNITO_SEMIBOLD_B64,
    NUNITO_BOLD_B64,
} from "@/assets/fonts/nunito-base64"

// ─── Tokens visuais ──────────────────────────────────────────────────────────

export const PDF_THEME = {
    /** Cor "ink" (azul-marinho institucional). */
    ink: [26, 36, 64] as [number, number, number],          // #1a2440
    /** Variante para títulos h1/h2/h3 (hsl 218 60% 28% ≈ #1d3557). */
    inkTitle: [29, 53, 87] as [number, number, number],
    /** Texto secundário (slate-600). */
    secondary: [71, 85, 105] as [number, number, number],   // #475569
    /** Labels mute (slate-400/500). */
    mute: [100, 116, 139] as [number, number, number],      // #64748b
    muteLight: [148, 163, 184] as [number, number, number], // #94a3b8
    /** Linhas e bordas. */
    border: [226, 232, 240] as [number, number, number],    // #e2e8f0
    borderStrong: [203, 213, 225] as [number, number, number], // #cbd5e1
    /** Fundo do card de paciente. */
    cardBg: [248, 250, 252] as [number, number, number],    // #f8fafc
    /** Cor para variante de receita controlada. */
    danger: [220, 38, 38] as [number, number, number],      // #dc2626
} as const

/** Margens da folha A4 (mm). */
export const PDF_MARGIN = { top: 14, side: 18, bottom: 22 } as const

/** Família da fonte registrada no doc. */
export const NUNITO_FAMILY = "Nunito"

// ─── Cache em sessão ─────────────────────────────────────────────────────────

/** Logo do estabelecimento já convertida para data URL (uma por sessão). */
let logoCache: { id: number; dataUrl: string | null } | null = null
/** Dados do estabelecimento ativo (uma por sessão). */
let estabelecimentoCache: { id: number; dados: Estabelecimento } | null = null

/**
 * Limpa caches — útil em testes ou ao trocar de tenant.
 * @internal exposto só para testes; produção normalmente não precisa chamar.
 */
export function _resetPdfHeaderCacheParaTestes() {
    logoCache = null
    estabelecimentoCache = null
}

// ─── Carregamento de fontes + estabelecimento ─────────────────────────────────

/**
 * Registra as 3 variantes da Nunito no doc jsPDF. Idempotente — chamar uma
 * vez por documento. Se algo falhar, cai silenciosamente em helvetica (default
 * do jsPDF).
 */
export function registrarFontesNunito(doc: JsPdfType) {
    try {
        doc.addFileToVFS("Nunito-Regular.ttf", NUNITO_REGULAR_B64)
        doc.addFont("Nunito-Regular.ttf", NUNITO_FAMILY, "normal")
        doc.addFileToVFS("Nunito-SemiBold.ttf", NUNITO_SEMIBOLD_B64)
        doc.addFont("Nunito-SemiBold.ttf", NUNITO_FAMILY, "semibold")
        doc.addFileToVFS("Nunito-Bold.ttf", NUNITO_BOLD_B64)
        doc.addFont("Nunito-Bold.ttf", NUNITO_FAMILY, "bold")
        doc.setFont(NUNITO_FAMILY, "normal")
    } catch {
        // Mantém helvetica.
    }
}

/**
 * Carrega os dados do estabelecimento ativo (a partir do tenant id da sessão).
 * Cacheado em memória. Devolve `null` se o usuário não tem estabelecimento
 * ativo ou se a chamada falhar — o caller exibe o PDF sem dados de contato.
 */
export async function carregarEstabelecimentoAtivo(tenantId: number | null): Promise<Estabelecimento | null> {
    if (tenantId == null) return null
    if (estabelecimentoCache && estabelecimentoCache.id === tenantId) return estabelecimentoCache.dados
    try {
        const lista = await estabelecimentoService.listarMeus()
        const dados = lista.find(e => e.id === tenantId) ?? null
        if (dados) estabelecimentoCache = { id: tenantId, dados }
        return dados
    } catch {
        return null
    }
}

/**
 * Baixa a foto/logo do estabelecimento e converte para data URL. Cacheado
 * por id. Devolve `null` se não houver fotoUrl ou se o fetch/conversão falhar.
 */
export async function carregarLogoComoDataUrl(est: Estabelecimento | null): Promise<string | null> {
    if (!est) return null
    if (logoCache && logoCache.id === est.id) return logoCache.dataUrl
    if (!est.fotoUrl) {
        logoCache = { id: est.id, dataUrl: null }
        return null
    }
    try {
        const resp = await fetch(est.fotoUrl, { credentials: "omit" })
        if (!resp.ok) throw new Error(`HTTP ${resp.status}`)
        const blob = await resp.blob()
        const dataUrl = await blobParaDataUrl(blob)
        logoCache = { id: est.id, dataUrl }
        return dataUrl
    } catch {
        logoCache = { id: est.id, dataUrl: null }
        return null
    }
}

function blobParaDataUrl(blob: Blob): Promise<string> {
    return new Promise((resolve, reject) => {
        const r = new FileReader()
        r.onloadend = () => resolve(r.result as string)
        r.onerror = () => reject(r.error)
        r.readAsDataURL(blob)
    })
}

// ─── Formatadores ────────────────────────────────────────────────────────────

export function formatarCnpj(cnpj: string | null | undefined): string | null {
    if (!cnpj) return null
    const d = cnpj.replace(/\D/g, "")
    if (d.length !== 14) return cnpj
    return `${d.slice(0, 2)}.${d.slice(2, 5)}.${d.slice(5, 8)}/${d.slice(8, 12)}-${d.slice(12)}`
}

export function formatarTelefone(tel: string | null | undefined): string | null {
    if (!tel) return null
    const d = tel.replace(/\D/g, "")
    if (d.length === 11) return `(${d.slice(0, 2)}) ${d.slice(2, 7)}-${d.slice(7)}`
    if (d.length === 10) return `(${d.slice(0, 2)}) ${d.slice(2, 6)}-${d.slice(6)}`
    return tel
}

export function formatarCpf(cpf: string | null | undefined): string | null {
    if (!cpf) return null
    const d = cpf.replace(/\D/g, "")
    if (d.length !== 11) return cpf
    return `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6, 9)}-${d.slice(9)}`
}

export function iniciais(nome: string | null | undefined): string {
    if (!nome) return "?"
    const partes = nome.trim().split(/\s+/).filter(Boolean)
    if (partes.length === 0) return "?"
    if (partes.length === 1) return partes[0]!.slice(0, 2).toUpperCase()
    return (partes[0]![0]! + partes[partes.length - 1]![0]!).toUpperCase()
}

// ─── Opções de desenho ───────────────────────────────────────────────────────

export interface CabecalhoOpcoes {
    /** Título do documento em UPPERCASE (ex.: "PRONTUÁRIO MÉDICO"). */
    docTitle: string
    /** Subtítulo discreto (ex.: "Atendimento de 12/05/2026"). */
    docSubtitle?: string
    /** Sobrescreve a cor do ink (vermelho para receita controlada). */
    accentOverride?: [number, number, number]
}

export interface RodapeOpcoes {
    /** Bloco esquerdo é assinatura? Se sim, fornecer nome+CRM. */
    assinatura?: {
        nome: string
        cargo?: string | null
        /** Texto cinza pequeno embaixo. Default "Assine manualmente no espaço acima". */
        aviso?: string
        /** Selo verde "Assinado digitalmente" — só quando true. */
        assinadoDigitalmente?: boolean
    }
    /** Aviso à direita (rodapé do relatório usa "Documento de gestão"). */
    avisoDireita?: string
    /** Texto livre adicional logo abaixo da meta (ex.: motivo de cancelamento). */
    nota?: string
}

// ─── Watermark ───────────────────────────────────────────────────────────────

export interface WatermarkOpcoes {
    /** Texto do watermark. Default "IMEDTO". */
    texto?: string
    /** Cor RGB. Default ink com opacidade baixa via setGState. */
    cor?: [number, number, number]
    /** Opacidade. Default 0.025. Variantes (Rascunho/Cancelada) usam 0.12. */
    opacidade?: number
    /** Tamanho da fonte. Default 140 para sutil, 96 para variantes. */
    fontSize?: number
}

/**
 * Desenha o watermark diagonal no centro da página atual. Chamar
 * APÓS adicionar nova página, ou ao final de cada página (loop sobre
 * `doc.getNumberOfPages()`).
 */
export function desenharWatermark(doc: JsPdfType, opcoes: WatermarkOpcoes = {}) {
    const texto = opcoes.texto ?? "IMEDTO"
    const cor = opcoes.cor ?? PDF_THEME.ink
    const opacidade = opcoes.opacidade ?? 0.025
    const fontSize = opcoes.fontSize ?? 140

    const w = doc.internal.pageSize.getWidth()
    const h = doc.internal.pageSize.getHeight()

    doc.saveGraphicsState()
    // GState pode não estar disponível em todos os builds — falha silenciosa
    try {
        const GState = (doc as any).GState
        if (GState) doc.setGState(new GState({ opacity: opacidade }))
    } catch { /* ignora */ }
    doc.setFont(NUNITO_FAMILY, "bold")
    doc.setFontSize(fontSize)
    doc.setTextColor(cor[0], cor[1], cor[2])
    doc.text(texto, w / 2, h / 2, { angle: -25, align: "center", baseline: "middle" })
    doc.restoreGraphicsState()
}

// ─── Desenho do cabeçalho institucional ──────────────────────────────────────

/**
 * Desenha o cabeçalho institucional no topo da página atual.
 * Retorna o `y` (mm) onde o conteúdo do corpo deve começar.
 */
export function desenharCabecalho(
    doc: JsPdfType,
    est: Estabelecimento | null,
    logoDataUrl: string | null,
    opt: CabecalhoOpcoes,
): number {
    const w = doc.internal.pageSize.getWidth()
    const left = PDF_MARGIN.side
    const right = w - PDF_MARGIN.side
    const titleColor = opt.accentOverride ?? PDF_THEME.inkTitle

    // ── Linha 1: marca à esquerda + contato à direita ───────────────────────
    const logoSize = 12 // mm
    const yTopo = PDF_MARGIN.top

    // Logo (imagem ou placeholder com iniciais)
    if (logoDataUrl) {
        try {
            doc.addImage(logoDataUrl, "PNG", left, yTopo, logoSize, logoSize, undefined, "FAST")
        } catch {
            desenharPlaceholderLogo(doc, est, left, yTopo, logoSize, titleColor)
        }
    } else {
        desenharPlaceholderLogo(doc, est, left, yTopo, logoSize, titleColor)
    }

    // Nome do estabelecimento
    const xNome = left + logoSize + 4
    doc.setFont(NUNITO_FAMILY, "bold")
    doc.setFontSize(15)
    doc.setTextColor(titleColor[0], titleColor[1], titleColor[2])
    const nome = est?.nomeFantasia ?? "Estabelecimento"
    doc.text(nome, xNome, yTopo + 6)

    // ── Bloco de contato à direita ─────────────────────────────────────────
    doc.setFont(NUNITO_FAMILY, "normal")
    doc.setFontSize(8)
    doc.setTextColor(PDF_THEME.secondary[0], PDF_THEME.secondary[1], PDF_THEME.secondary[2])
    const linhasContato: string[] = []
    if (est?.endereco) linhasContato.push(est.endereco)
    const tel = formatarTelefone(est?.telefone)
    if (tel) linhasContato.push(tel)
    const cnpj = formatarCnpj(est?.cnpj)
    if (cnpj) linhasContato.push(`CNPJ ${cnpj}`)
    let yContato = yTopo + 3
    for (const linha of linhasContato) {
        // Quebra automática para textos longos
        const partes = doc.splitTextToSize(linha, 80)
        for (const p of partes) {
            doc.text(p, right, yContato, { align: "right" })
            yContato += 3.5
        }
    }

    // ── Linha divisória fina antes do título do doc ─────────────────────────
    const yPosTopo = Math.max(yTopo + logoSize + 2, yContato + 1)
    doc.setDrawColor(PDF_THEME.border[0], PDF_THEME.border[1], PDF_THEME.border[2])
    doc.setLineWidth(0.2)
    doc.line(left, yPosTopo, right, yPosTopo)

    // ── Título do documento + subtítulo ─────────────────────────────────────
    const yTitulo = yPosTopo + 5
    doc.setFont(NUNITO_FAMILY, "bold")
    doc.setFontSize(11)
    doc.setTextColor(titleColor[0], titleColor[1], titleColor[2])
    doc.text(opt.docTitle.toUpperCase(), left, yTitulo, { charSpace: 0.4 })

    if (opt.docSubtitle) {
        doc.setFont(NUNITO_FAMILY, "normal")
        doc.setFontSize(8.5)
        doc.setTextColor(PDF_THEME.mute[0], PDF_THEME.mute[1], PDF_THEME.mute[2])
        doc.text(opt.docSubtitle, right, yTitulo, { align: "right" })
    }

    // ── Linha dupla 0.6mm separando cabeçalho do corpo ──────────────────────
    const yLinhaDupla = yTitulo + 2
    doc.setDrawColor(titleColor[0], titleColor[1], titleColor[2])
    doc.setLineWidth(0.35)
    doc.line(left, yLinhaDupla, right, yLinhaDupla)
    doc.line(left, yLinhaDupla + 0.8, right, yLinhaDupla + 0.8)

    return yLinhaDupla + 5
}

function desenharPlaceholderLogo(
    doc: JsPdfType,
    est: Estabelecimento | null,
    x: number,
    y: number,
    size: number,
    cor: [number, number, number],
) {
    doc.setFillColor(cor[0], cor[1], cor[2])
    doc.circle(x + size / 2, y + size / 2, size / 2, "F")
    doc.setFont(NUNITO_FAMILY, "bold")
    doc.setFontSize(7)
    doc.setTextColor(255, 255, 255)
    doc.text(iniciais(est?.nomeFantasia), x + size / 2, y + size / 2 + 0.6, {
        align: "center",
        baseline: "middle",
    })
}

// ─── Desenho do rodapé ───────────────────────────────────────────────────────

/**
 * Desenha o rodapé na página atual. Não é alinhado pelo topo: o caller
 * informa onde fica o footer (usualmente próximo ao bottom).
 */
export function desenharRodape(
    doc: JsPdfType,
    opt: RodapeOpcoes,
    paginaAtual: number,
    totalPaginas: number,
) {
    const w = doc.internal.pageSize.getWidth()
    const h = doc.internal.pageSize.getHeight()
    const left = PDF_MARGIN.side
    const right = w - PDF_MARGIN.side
    const yRodape = h - PDF_MARGIN.bottom + 4

    if (opt.assinatura) {
        // Bloco esquerdo: assinatura
        const xCentro = left + (right - left) / 4
        doc.setDrawColor(PDF_THEME.ink[0], PDF_THEME.ink[1], PDF_THEME.ink[2])
        doc.setLineWidth(0.5)
        const larguraLinha = (right - left) * 0.4
        doc.line(xCentro - larguraLinha / 2, yRodape, xCentro + larguraLinha / 2, yRodape)

        doc.setFont(NUNITO_FAMILY, "bold")
        doc.setFontSize(9)
        doc.setTextColor(PDF_THEME.ink[0], PDF_THEME.ink[1], PDF_THEME.ink[2])
        doc.text(opt.assinatura.nome, xCentro, yRodape + 4, { align: "center" })

        if (opt.assinatura.cargo) {
            doc.setFont(NUNITO_FAMILY, "normal")
            doc.setFontSize(8)
            doc.setTextColor(PDF_THEME.secondary[0], PDF_THEME.secondary[1], PDF_THEME.secondary[2])
            doc.text(opt.assinatura.cargo, xCentro, yRodape + 7.5, { align: "center" })
        }

        const aviso = opt.assinatura.aviso ?? "Assine manualmente no espaço acima"
        if (opt.assinatura.assinadoDigitalmente) {
            doc.setFont(NUNITO_FAMILY, "normal")
            doc.setFontSize(7)
            doc.setTextColor(22, 163, 74) // #16a34a
            doc.text("Assinado digitalmente · ICP-Brasil", xCentro, yRodape + 11, { align: "center" })
        } else {
            doc.setFont(NUNITO_FAMILY, "normal")
            doc.setFontSize(7)
            doc.setTextColor(PDF_THEME.muteLight[0], PDF_THEME.muteLight[1], PDF_THEME.muteLight[2])
            doc.text(aviso, xCentro, yRodape + 11, { align: "center" })
        }
    } else if (opt.avisoDireita) {
        // Bloco esquerdo vazio — vira "aviso" do relatório
        doc.setFont(NUNITO_FAMILY, "normal")
        doc.setFontSize(8)
        doc.setTextColor(PDF_THEME.muteLight[0], PDF_THEME.muteLight[1], PDF_THEME.muteLight[2])
        doc.text(opt.avisoDireita, left, yRodape + 4)
    }

    // Bloco direito: metadados
    doc.setFont(NUNITO_FAMILY, "normal")
    doc.setFontSize(7)
    doc.setTextColor(PDF_THEME.muteLight[0], PDF_THEME.muteLight[1], PDF_THEME.muteLight[2])
    const dataEmissao = new Date().toLocaleString("pt-BR", {
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit",
    })
    doc.text(`Emitido em ${dataEmissao}`, right, yRodape + 4, { align: "right" })
    doc.text(`Página ${paginaAtual} de ${totalPaginas}`, right, yRodape + 7.5, { align: "right" })
    if (opt.nota) {
        doc.text(opt.nota, right, yRodape + 11, { align: "right" })
    }
}

// ─── Acabamento do documento ─────────────────────────────────────────────────

/**
 * Pinta o watermark e o rodapé em TODAS as páginas do doc.
 * Chamar imediatamente antes do save().
 */
export function finalizarPaginas(
    doc: JsPdfType,
    rodapeOpt: RodapeOpcoes,
    watermarkOpt: WatermarkOpcoes = {},
) {
    const total = doc.getNumberOfPages()
    for (let p = 1; p <= total; p++) {
        doc.setPage(p)
        desenharWatermark(doc, watermarkOpt)
        desenharRodape(doc, rodapeOpt, p, total)
    }
}

// ─── Bloco de paciente (compartilhado entre prontuário e receita) ────────────

export interface PacienteParaPdf {
    nomeCompleto: string
    cpf?: string | null
    dataNascimento?: string | null
    genero?: string | null
    telefone?: string | null
}

/**
 * Desenha o card cinza do paciente. Devolve o `y` final (logo abaixo do card).
 */
export function desenharBlocoPaciente(
    doc: JsPdfType,
    paciente: PacienteParaPdf,
    y: number,
): number {
    const w = doc.internal.pageSize.getWidth()
    const left = PDF_MARGIN.side
    const right = w - PDF_MARGIN.side
    const largura = right - left

    const cellH = 12
    const padding = 3.5
    const altura = cellH + padding * 2 + 5 // 2 linhas + paddings

    // Fundo
    doc.setFillColor(PDF_THEME.cardBg[0], PDF_THEME.cardBg[1], PDF_THEME.cardBg[2])
    doc.setDrawColor(PDF_THEME.border[0], PDF_THEME.border[1], PDF_THEME.border[2])
    doc.setLineWidth(0.2)
    doc.roundedRect(left, y, largura, altura, 1.5, 1.5, "FD")

    // Layout em 4 colunas
    const colW = largura / 4
    const yLabel = y + padding + 1
    const yValor = yLabel + 3

    // Linha 1: Nome (largo) · Idade · Sexo · Telefone
    desenharCelula(doc, "Paciente", paciente.nomeCompleto, left + padding, yLabel, colW * 2 - padding * 1.5, yValor, true)

    const idade = calcularIdade(paciente.dataNascimento)
    desenharCelula(doc, "Idade", idade ? `${idade} anos` : "—", left + colW * 2 + padding, yLabel, colW - padding * 1.5, yValor)

    const sexo = formatarGenero(paciente.genero)
    desenharCelula(doc, "Sexo", sexo, left + colW * 3 + padding, yLabel, colW - padding * 1.5, yValor)

    // Linha divisória tracejada
    const yMid = y + padding + cellH / 2 + 1
    doc.setLineDashPattern([0.6, 0.6], 0)
    doc.setDrawColor(PDF_THEME.border[0], PDF_THEME.border[1], PDF_THEME.border[2])
    doc.line(left + padding, yMid, right - padding, yMid)
    doc.setLineDashPattern([], 0)

    // Linha 2: CPF · Nascimento · Telefone (3 colunas)
    const y2Label = yMid + 2
    const y2Valor = y2Label + 3
    desenharCelula(doc, "CPF", formatarCpf(paciente.cpf) ?? "—", left + padding, y2Label, colW - padding, y2Valor)
    const nasc = formatarData(paciente.dataNascimento)
    desenharCelula(doc, "Nascimento", nasc, left + colW + padding, y2Label, colW - padding, y2Valor)
    desenharCelula(doc, "Telefone", formatarTelefone(paciente.telefone) ?? "—", left + colW * 2 + padding, y2Label, colW - padding, y2Valor)

    return y + altura + 4
}

function desenharCelula(
    doc: JsPdfType,
    label: string,
    valor: string,
    x: number,
    yLabel: number,
    largura: number,
    yValor: number,
    valorEmDestaque = false,
) {
    doc.setFont(NUNITO_FAMILY, "semibold")
    doc.setFontSize(6.5)
    doc.setTextColor(PDF_THEME.muteLight[0], PDF_THEME.muteLight[1], PDF_THEME.muteLight[2])
    doc.text(label.toUpperCase(), x, yLabel, { charSpace: 0.3 })

    doc.setFont(NUNITO_FAMILY, valorEmDestaque ? "bold" : "normal")
    doc.setFontSize(9)
    doc.setTextColor(PDF_THEME.ink[0], PDF_THEME.ink[1], PDF_THEME.ink[2])
    const linha = doc.splitTextToSize(valor, largura)
    doc.text(linha[0] ?? "", x, yValor)
}

function calcularIdade(nascimento: string | null | undefined): number | null {
    if (!nascimento) return null
    const d = new Date(nascimento)
    if (Number.isNaN(d.getTime())) return null
    const agora = new Date()
    let anos = agora.getFullYear() - d.getFullYear()
    const m = agora.getMonth() - d.getMonth()
    if (m < 0 || (m === 0 && agora.getDate() < d.getDate())) anos -= 1
    return anos
}

function formatarData(d: string | null | undefined): string {
    if (!d) return "—"
    const data = new Date(d)
    if (Number.isNaN(data.getTime())) return "—"
    return data.toLocaleDateString("pt-BR")
}

function formatarGenero(g: string | null | undefined): string {
    if (!g) return "—"
    const norm = g.trim().toUpperCase()
    if (norm === "F" || norm === "FEMININO") return "Feminino"
    if (norm === "M" || norm === "MASCULINO") return "Masculino"
    return g
}
