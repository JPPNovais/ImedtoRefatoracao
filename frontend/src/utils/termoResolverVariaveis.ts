/**
 * Resolve as variáveis `{{...}}` de um modelo de termo no FRONT, usando dados
 * reais do paciente/estabelecimento/profissional/data.
 *
 * **Atenção**: o backend já resolve as variáveis em produção no momento da
 * emissão (`TermoResolverDeVariaveis.cs`). Este resolver existe APENAS para
 * o preview client-side no wizard "Emitir termo". Mantenha a lista, os
 * formatos e os fallbacks alinhados com o backend — divergências viram
 * "preview ≠ termo emitido", o que confunde o usuário.
 *
 * Lista de variáveis suportadas: ver `TERMO_VARIAVEIS` em
 * `@/constants/termoVariaveis`.
 */

/** Fallbacks que o backend também usa (espelho fiel do `Fallbacks` em C#). */
const FB = {
    LINHA: "___________",
    CPF: "___.___.___-__",
    DATA: "__/__/____",
    VAZIO: "",
} as const

/** Resultado da resolução: HTML pronto + log das chaves aplicadas. */
export interface ResultadoResolucao {
    htmlResolvido: string
    variaveisAplicadas: Array<{ chave: string; valor: string; fallback: boolean }>
}

/** Mínimo necessário do paciente para a resolução (campos cruzam com `Paciente`). */
export interface PacienteContextoTermo {
    nomeCompleto: string
    cpf: string | null
    documentoInternacional: string | null
    dataNascimento: string | null
    telefone: string | null
    email: string | null
    endereco: string | null
    genero: string | null
}

/** Mínimo necessário do estabelecimento. */
export interface EstabelecimentoContextoTermo {
    nomeFantasia: string | null
    razaoSocial: string | null
    cnpj: string | null
    telefone: string | null
    endereco: string | null
    cidade: string | null
}

/** Mínimo necessário do profissional emissor. */
export interface ProfissionalContextoTermo {
    nome: string | null
    conselho: string | null
    uf: string | null
    numeroRegistro: string | null
    especialidade: string | null
}

export interface ContextoResolucaoTermo {
    paciente: PacienteContextoTermo
    estabelecimento: EstabelecimentoContextoTermo | null
    profissional: ProfissionalContextoTermo | null
    /** Data usada para `data_atual*`. Default = `new Date()`. Injetável para testes. */
    dataAtual?: Date
}

const PLACEHOLDER_REGEX = /\{\{\s*([a-z_.]+)\s*\}\}/gi

export function resolverVariaveis(
    html: string,
    ctx: ContextoResolucaoTermo,
): ResultadoResolucao {
    if (!html) return { htmlResolvido: "", variaveisAplicadas: [] }
    if (!html.includes("{{")) return { htmlResolvido: html, variaveisAplicadas: [] }

    const valores = montarDicionario(ctx)
    const aplicadas = new Map<string, { valor: string; fallback: boolean }>()

    const htmlResolvido = html.replace(PLACEHOLDER_REGEX, (match, chave) => {
        const k = String(chave).trim().toLowerCase()
        const entrada = valores.get(k)
        if (!entrada) return match // chave desconhecida — não tocar
        if (!aplicadas.has(k)) aplicadas.set(k, entrada)
        return entrada.valor
    })

    return {
        htmlResolvido,
        variaveisAplicadas: Array.from(aplicadas.entries()).map(([chave, v]) => ({
            chave: `{{${chave}}}`,
            valor: v.valor,
            fallback: v.fallback,
        })),
    }
}

// ─── Dicionário variável → valor ────────────────────────────────────────────

function montarDicionario(ctx: ContextoResolucaoTermo): Map<string, { valor: string; fallback: boolean }> {
    const p = ctx.paciente
    const e = ctx.estabelecimento
    const pr = ctx.profissional
    const agora = ctx.dataAtual ?? new Date()

    const m = new Map<string, { valor: string; fallback: boolean }>()

    set(m, "paciente.nome", emptyToFallback(p.nomeCompleto, FB.LINHA))
    set(m, "paciente.cpf", formatarCpf(p.cpf))
    set(m, "paciente.documento_internacional", emptyToFallback(p.documentoInternacional, FB.VAZIO))
    set(m, "paciente.data_nascimento", formatarData(p.dataNascimento))
    set(m, "paciente.idade", calcularIdade(p.dataNascimento, agora))
    set(m, "paciente.telefone", formatarTelefone(p.telefone))
    set(m, "paciente.email", emptyToFallback(p.email, FB.VAZIO))
    set(m, "paciente.endereco", emptyToFallback(p.endereco, FB.LINHA))
    set(m, "paciente.genero", formatarGenero(p.genero))

    set(m, "estabelecimento.nome", emptyToFallback(e?.nomeFantasia ?? null, FB.LINHA))
    set(m, "estabelecimento.razao_social", emptyToFallback(e?.razaoSocial ?? null, FB.VAZIO))
    set(m, "estabelecimento.cnpj", formatarCnpj(e?.cnpj ?? null))
    set(m, "estabelecimento.endereco", emptyToFallback(e?.endereco ?? null, FB.VAZIO))
    set(m, "estabelecimento.telefone", formatarTelefone(e?.telefone ?? null))

    set(m, "profissional.nome", emptyToFallback(pr?.nome ?? null, FB.LINHA))
    set(m, "profissional.conselho_completo", formatarConselho(pr))
    set(m, "profissional.especialidade", emptyToFallback(pr?.especialidade ?? null, FB.VAZIO))

    set(m, "data_atual", { valor: formatarDataExtenso(agora), fallback: false })
    set(m, "data_atual_curta", { valor: formatarDataCurta(agora), fallback: false })
    set(m, "cidade_atual", emptyToFallback(e?.cidade ?? null, FB.LINHA))

    return m
}

function set(
    m: Map<string, { valor: string; fallback: boolean }>,
    chave: string,
    entrada: { valor: string; fallback: boolean },
) {
    m.set(chave, entrada)
}

function emptyToFallback(valor: string | null | undefined, fallback: string): { valor: string; fallback: boolean } {
    if (!valor || !valor.toString().trim()) {
        return { valor: fallback, fallback: fallback !== FB.VAZIO }
    }
    return { valor: valor.toString(), fallback: false }
}

// ─── Formatadores (mesmas regras do backend) ─────────────────────────────────

function somenteDigitos(s: string | null | undefined): string {
    if (!s) return ""
    return s.replace(/\D+/g, "")
}

// Preserva [A-Z0-9] para CNPJ alfanumérico (IN RFB 2.229/2024).
function normalizarCnpj(s: string | null | undefined): string {
    if (!s) return ""
    return s.toUpperCase().replace(/[^A-Z0-9]/g, "")
}

export function formatarCpf(cpf: string | null | undefined): { valor: string; fallback: boolean } {
    const d = somenteDigitos(cpf)
    if (d.length !== 11) return { valor: FB.CPF, fallback: true }
    return { valor: `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6, 9)}-${d.slice(9)}`, fallback: false }
}

export function formatarCnpj(cnpj: string | null | undefined): { valor: string; fallback: boolean } {
    const d = normalizarCnpj(cnpj)
    if (d.length !== 14) return { valor: FB.VAZIO, fallback: true }
    return { valor: `${d.slice(0, 2)}.${d.slice(2, 5)}.${d.slice(5, 8)}/${d.slice(8, 12)}-${d.slice(12)}`, fallback: false }
}

export function formatarTelefone(tel: string | null | undefined): { valor: string; fallback: boolean } {
    const d = somenteDigitos(tel)
    if (d.length === 11) return { valor: `(${d.slice(0, 2)}) ${d.slice(2, 7)}-${d.slice(7)}`, fallback: false }
    if (d.length === 10) return { valor: `(${d.slice(0, 2)}) ${d.slice(2, 6)}-${d.slice(6)}`, fallback: false }
    if (d.length === 0) return { valor: FB.VAZIO, fallback: true }
    // Possível formato livre — mantém original sem tachar fallback
    return { valor: tel ?? FB.VAZIO, fallback: false }
}

/**
 * Parse "agnóstico de fuso horário" para datas de nascimento. Aceita
 * `yyyy-mm-dd` (data-only) e ISO completo. Para `yyyy-mm-dd` retorna a data
 * no fuso local sem deslocamento UTC (evita o clássico "1990-05-19" virar
 * 18/05 em fusos negativos).
 */
function parseDataLocal(iso: string): Date | null {
    const m = /^(\d{4})-(\d{2})-(\d{2})($|T)/.exec(iso)
    if (m) {
        const d = new Date(Number(m[1]), Number(m[2]) - 1, Number(m[3]))
        return Number.isNaN(d.getTime()) ? null : d
    }
    const d = new Date(iso)
    return Number.isNaN(d.getTime()) ? null : d
}

export function formatarData(iso: string | null | undefined): { valor: string; fallback: boolean } {
    if (!iso) return { valor: FB.DATA, fallback: true }
    const d = parseDataLocal(iso)
    if (!d) return { valor: FB.DATA, fallback: true }
    const dia = String(d.getDate()).padStart(2, "0")
    const mes = String(d.getMonth() + 1).padStart(2, "0")
    const ano = d.getFullYear()
    return { valor: `${dia}/${mes}/${ano}`, fallback: false }
}

const MESES_PT = [
    "janeiro", "fevereiro", "março", "abril", "maio", "junho",
    "julho", "agosto", "setembro", "outubro", "novembro", "dezembro",
]

export function formatarDataExtenso(d: Date): string {
    return `${d.getDate()} de ${MESES_PT[d.getMonth()]} de ${d.getFullYear()}`
}

export function formatarDataCurta(d: Date): string {
    const dia = String(d.getDate()).padStart(2, "0")
    const mes = String(d.getMonth() + 1).padStart(2, "0")
    return `${dia}/${mes}/${d.getFullYear()}`
}

export function calcularIdade(iso: string | null | undefined, hoje: Date): { valor: string; fallback: boolean } {
    if (!iso) return { valor: FB.VAZIO, fallback: true }
    const d = parseDataLocal(iso)
    if (!d) return { valor: FB.VAZIO, fallback: true }
    let anos = hoje.getFullYear() - d.getFullYear()
    const m = hoje.getMonth() - d.getMonth()
    if (m < 0 || (m === 0 && hoje.getDate() < d.getDate())) anos--
    if (anos < 0) return { valor: FB.VAZIO, fallback: true }
    return { valor: `${anos} ${anos === 1 ? "ano" : "anos"}`, fallback: false }
}

function formatarGenero(g: string | null | undefined): { valor: string; fallback: boolean } {
    if (!g) return { valor: FB.VAZIO, fallback: true }
    const n = g.trim()
    if (n === "Feminino" || n === "Masculino" || n === "Outro") return { valor: n, fallback: false }
    return { valor: FB.VAZIO, fallback: true }
}

function formatarConselho(pr: ProfissionalContextoTermo | null | undefined): { valor: string; fallback: boolean } {
    if (!pr) return { valor: FB.VAZIO, fallback: true }
    const partes: string[] = []
    let base = ""
    if (pr.conselho) base = pr.conselho
    if (pr.uf) base = base ? `${base}-${pr.uf}` : `-${pr.uf}`
    if (base) partes.push(base)
    if (pr.numeroRegistro) partes.push(pr.numeroRegistro)
    const joined = partes.join(" ").trim()
    return joined ? { valor: joined, fallback: false } : { valor: FB.VAZIO, fallback: true }
}
