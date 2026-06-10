import type { Evolucao } from "@/services/prontuarioService"

/**
 * Helpers compartilhados pelos cards de timeline de evolução
 * (ConsultasAnterioresTab no prontuário e aba Prontuário do detalhe do paciente).
 *
 * Centraliza a lógica de:
 *  - extrair um resumo textual curto da primeira seção preenchida;
 *  - contar quantas seções do modelo foram efetivamente preenchidas.
 */
export function resumoTextual(e: Evolucao): string {
    for (const s of e.modeloSnapshot) {
        const v = e.conteudo[s.chave]
        if (typeof v === "string" && v.trim()) {
            const t = v.trim().replace(/\s+/g, " ")
            return t.length > 220 ? t.slice(0, 220) + "..." : t
        }
    }
    return "Sem resumo textual disponível."
}

export function contarSecoesPreenchidas(e: Evolucao): { preenchidas: number, total: number } {
    const preenchidas = e.modeloSnapshot.filter(s => {
        const v = e.conteudo[s.chave]
        if (v === null || v === undefined) return false
        if (typeof v === "string") return v.trim().length > 0
        if (Array.isArray(v)) return (v as unknown[]).length > 0
        if (typeof v === "object") return Object.values(v as Record<string, unknown>)
            .some(x => x !== null && x !== undefined && String(x).trim() !== "")
        return true
    }).length

    return { preenchidas, total: e.modeloSnapshot.length }
}

// ────────────────────────────────────────────────────────────────────────────
// Renderização legível das seções de evolução (briefing 2026-06-09_008)
//
// `formatarSecaoLegivel(chave, valor)` é o PONTO ÚNICO de formatação consumido
// pela modal de leitura (EvolucaoDetalheDrawer) e pelo PDF (useProntuarioPdf).
// Nunca renderiza JSON cru, `true`/`false` nem chave técnica. Abordagem híbrida:
// formatadores curados para as 6 seções conhecidas + fallback genérico para o
// resto. Campos vazios são omitidos; negativas clínicas explícitas só para
// alergias/medicações/tabagismo/etilismo/drogas. Degrada com segurança em
// schema variável (campo ausente = omitido, nunca lança).
// ────────────────────────────────────────────────────────────────────────────

/** Valor como string trimada; null/undefined → "". */
function s(v: unknown): string {
    return v == null ? "" : String(v).trim()
}

/** Junta partes não-vazias (após trim) com o separador. */
function juntar(partes: (string | null | undefined)[], sep: string): string {
    return partes.map(p => (p ?? "").trim()).filter(Boolean).join(sep)
}

/** Array de objetos seguro: filtra não-objetos; não-array → []. */
function lista(v: unknown): Record<string, unknown>[] {
    return Array.isArray(v)
        ? v.filter(x => x != null && typeof x === "object") as Record<string, unknown>[]
        : []
}

/** camelCase / kebab / snake → rótulo legível ("atividadeFisicaNivel" → "Atividade fisica nivel"). */
function humanizarChave(k: string): string {
    const t = k
        .replace(/([a-z0-9])([A-Z])/g, "$1 $2")
        .replace(/[_-]+/g, " ")
        .trim()
        .toLowerCase()
    return t.charAt(0).toUpperCase() + t.slice(1)
}

// ── Itens de lista ──────────────────────────────────────────────────────────

function fmtAlergia(a: Record<string, unknown>): string {
    const nome = s(a.nome)
    if (!nome) return ""
    const obs = s(a.observacao)
    return obs ? `${nome} (${obs})` : nome
}

function fmtMedicacao(m: Record<string, unknown>): string {
    const nome = s(m.nome)
    if (!nome) return ""
    let linha = juntar([nome, s(m.dose)], " ")
    linha = juntar([linha, s(m.frequencia)], ", ")
    const motivo = s(m.motivo)
    if (motivo) linha += ` — ${motivo}`
    const obs = s(m.observacoes)
    if (obs) linha += ` (${obs})`
    return linha
}

function fmtCirurgia(c: Record<string, unknown>): string {
    const nome = s(c.nome)
    if (!nome) return ""
    const ano = s(c.ano)
    let linha = ano ? `${nome} (${ano})` : nome
    const obs = s(c.observacao)
    if (obs) linha += ` — ${obs}`
    return linha
}

function fmtDoenca(d: Record<string, unknown>): string {
    const nome = s(d.nome)
    if (!nome) return ""
    const obs = s(d.observacao)
    return obs ? `${nome} (${obs})` : nome
}

// ── Formatadores curados por seção ──────────────────────────────────────────

function fmtHpp(d: Record<string, unknown>): string {
    const linhas: string[] = []

    const alergias = lista(d.alergias).map(fmtAlergia).filter(Boolean)
    if (alergias.length) linhas.push(`Alergias: ${alergias.join("; ")}`)
    else if (d.alergiasTem === false) linhas.push("Alergias: Nega")

    const meds = lista(d.medicacoes).map(fmtMedicacao).filter(Boolean)
    if (meds.length) linhas.push(`Medicações de uso: ${meds.join("; ")}`)
    else if (d.medicacoesTem === false) linhas.push("Medicações de uso: Nega")

    const cirurgias = lista(d.cirurgias).map(fmtCirurgia).filter(Boolean)
    if (cirurgias.length) linhas.push(`Cirurgias: ${cirurgias.join("; ")}`)

    const doencas = lista(d.doencas).map(fmtDoenca).filter(Boolean)
    if (doencas.length) linhas.push(`Doenças prévias: ${doencas.join("; ")}`)

    if (s(d.observacoes)) linhas.push(`Observações: ${s(d.observacoes)}`)

    return linhas.join("\n")
}

function fmtHistoriaFamiliar(d: Record<string, unknown>): string {
    const linhas: string[] = []

    const pai = juntar([s(d.paiDoencas), s(d.paiDescricao)], " — ")
    if (pai) linhas.push(`Pai: ${pai}`)
    const mae = juntar([s(d.maeDoencas), s(d.maeDescricao)], " — ")
    if (mae) linhas.push(`Mãe: ${mae}`)

    for (const p of lista(d.parentes)) {
        const parentesco = s(p.parentesco)
        let corpo = s(p.doencas)
        const comentario = s(p.comentario)
        if (comentario) corpo = corpo ? `${corpo} — ${comentario}` : comentario
        const linha = parentesco ? juntar([`${parentesco}:`, corpo], " ") : corpo
        if (linha) linhas.push(linha)
    }

    if (s(d.observacao)) linhas.push(`Observações: ${s(d.observacao)}`)

    return linhas.join("\n")
}

/** Item social clínico: registra negativa quando `tem === false`; positivo usa detalhes. */
function fmtItemClinicoSocial(rotulo: string, tem: unknown, detalhes: string[]): string {
    const corpo = juntar(detalhes, " — ")
    if (corpo) return `${rotulo}: ${corpo}`
    if (tem === false) return `${rotulo}: Não`
    return ""
}

function fmtHistoriaSocial(d: Record<string, unknown>): string {
    const linhas: string[] = []

    if (s(d.estadoCivil)) linhas.push(`Estado civil: ${s(d.estadoCivil)}`)

    if (d.filhosTem === true) {
        const qtd = s(d.filhosQuantos)
        const idades = s(d.filhosIdades)
        const obs = s(d.filhosObs)
        let corpo = qtd
        if (idades) corpo = corpo ? `${corpo} (idades: ${idades})` : `idades: ${idades}`
        if (obs) corpo = corpo ? `${corpo} — ${obs}` : obs
        if (corpo) linhas.push(`Filhos: ${corpo}`)
    }

    const tabagismo = fmtItemClinicoSocial("Tabagismo", d.tabagismoTem, [s(d.tabagismoStatus), s(d.tabagismoObs)])
    if (tabagismo) linhas.push(tabagismo)
    const etilismo = fmtItemClinicoSocial("Etilismo", d.etilismoTem, [s(d.etilismoStatus), s(d.etilismoObs)])
    if (etilismo) linhas.push(etilismo)
    const drogas = fmtItemClinicoSocial("Drogas", d.drogasTem, [s(d.drogasObs)])
    if (drogas) linhas.push(drogas)

    if (d.atividadeFisicaTem === true) {
        const corpo = juntar([s(d.atividadeFisicaNivel), s(d.atividadeFisicaObs)], " — ")
        if (corpo) linhas.push(`Atividade física: ${corpo}`)
    }

    const alimentacao = juntar([s(d.alimentacao), s(d.alimentacaoObs)], " — ")
    if (alimentacao) linhas.push(`Alimentação: ${alimentacao}`)
    const sono = juntar([s(d.sonoQualidade), s(d.sonoObs)], " — ")
    if (sono) linhas.push(`Sono: ${sono}`)

    return linhas.join("\n")
}

/** Altura legível: ≤3 assume metros, senão centímetros. */
function fmtAltura(v: string): string {
    const n = parseFloat(v.replace(",", "."))
    if (!isFinite(n)) return v
    return n <= 3 ? `${v} m` : `${v} cm`
}

function fmtRegiao(r: Record<string, unknown>): string {
    const caminho = s(r.caminho)
    if (!caminho) return ""
    let cabeca = caminho
    const vista = s(r.vista)
    if (vista) cabeca += ` (${vista})`
    const lat = lateralidadeLegivel(r.lateralidade)
    if (lat) cabeca += `, ${lat}`
    const corpo = s(r.texto_exame) || s(r.achados)
    let linha = corpo ? `${cabeca}: ${corpo}` : cabeca
    const obs = s(r.observacoes)
    if (obs) linha += ` — ${obs}`
    return linha
}

function lateralidadeLegivel(v: unknown): string {
    switch (s(v).toLowerCase()) {
        case "d": case "direito": case "direita": return "direito"
        case "e": case "esquerdo": case "esquerda": return "esquerdo"
        case "bilateral": return "bilateral"
        case "misto": return "misto"
        default: return ""
    }
}

const CAMPOS_EXAME_TRATADOS = new Set([
    "paSistolica", "paDiastolica", "fc", "fr", "temperatura", "spo2", "glicemia",
    "peso", "altura", "imc", "regioes", "descricaoEctoscopia", "observacoesExame",
])

function fmtExameFisico(d: Record<string, unknown>): string {
    const linhas: string[] = []

    // Sinais vitais consolidados (só preenchidos, com unidades)
    const vitais: string[] = []
    const pas = s(d.paSistolica), pad = s(d.paDiastolica)
    if (pas || pad) vitais.push(`PA: ${pas || "—"}/${pad || "—"} mmHg`)
    if (s(d.fc)) vitais.push(`FC: ${s(d.fc)} bpm`)
    if (s(d.fr)) vitais.push(`FR: ${s(d.fr)} irpm`)
    if (s(d.temperatura)) vitais.push(`Temp: ${s(d.temperatura)} °C`)
    if (s(d.spo2)) vitais.push(`SpO₂: ${s(d.spo2)}%`)
    if (s(d.glicemia)) vitais.push(`Glicemia: ${s(d.glicemia)} mg/dL`)
    if (vitais.length) linhas.push(vitais.join(", "))

    // Antropometria
    const antro: string[] = []
    if (s(d.peso)) antro.push(`Peso: ${s(d.peso)} kg`)
    if (s(d.altura)) antro.push(`Altura: ${fmtAltura(s(d.altura))}`)
    if (antro.length) linhas.push(antro.join(", "))

    // Ectoscopia: descrição + selects preenchidos (genérico, sem rótulo técnico)
    if (s(d.descricaoEctoscopia)) linhas.push(`Ectoscopia: ${s(d.descricaoEctoscopia)}`)
    for (const [k, v] of Object.entries(d)) {
        if (CAMPOS_EXAME_TRATADOS.has(k)) continue
        if (typeof v === "boolean") continue
        const texto = s(v)
        if (texto) linhas.push(`${humanizarChave(k)}: ${texto}`)
    }

    // Regiões anatômicas detalhadas
    for (const r of lista(d.regioes)) {
        const linha = fmtRegiao(r)
        if (linha) linhas.push(linha)
    }

    if (s(d.observacoesExame)) linhas.push(`Observações do exame: ${s(d.observacoesExame)}`)

    return linhas.join("\n")
}

function fmtExamesRealizados(d: Record<string, unknown>): string {
    const linhas: string[] = []
    for (const e of lista(d.itens)) {
        const nome = s(e.nome)
        if (!nome) continue
        const meta = juntar([s(e.tipo), s(e.material)], ", ")
        let linha = meta ? `${nome} (${meta})` : nome
        const com = s(e.comentario)
        if (com) linha += ` — ${com}`
        linhas.push(linha)
    }
    if (s(d.observacoes)) linhas.push(`Observações: ${s(d.observacoes)}`)
    return linhas.join("\n")
}

function fmtProcedimentos(d: Record<string, unknown>): string {
    const linhas: string[] = []
    for (const p of lista(d.procedimentos)) {
        const desc = s(p.descricao)
        if (!desc) continue
        const obs = s(p.observacao)
        // Formato novo (catálogo): snapshot inclui valor → exibe "desc — R$ X,XX — obs".
        // Legado (texto-livre): sem catalogoCirurgiaId/valor → mantém "desc — obs".
        const valor = p.valor != null && p.valor !== "" ? Number(p.valor) : null
        if (valor != null && isFinite(valor)) {
            const valorStr = valor.toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 })
            linhas.push(obs ? `${desc} — R$ ${valorStr} — ${obs}` : `${desc} — R$ ${valorStr}`)
        } else {
            linhas.push(obs ? `${desc} — ${obs}` : desc)
        }
    }
    if (s(d.observacoes)) linhas.push(`Observações: ${s(d.observacoes)}`)
    return linhas.join("\n")
}

// ── Fallback genérico ───────────────────────────────────────────────────────

function formatarGenerico(v: unknown): string {
    if (v == null) return ""
    if (typeof v === "string") return v.trim()
    if (typeof v === "number") return String(v)
    if (typeof v === "boolean") return "" // nunca imprime true/false cru
    if (Array.isArray(v)) {
        return v.map(formatarGenerico).filter(Boolean).join("\n")
    }
    if (typeof v === "object") {
        const linhas: string[] = []
        for (const [k, val] of Object.entries(v as Record<string, unknown>)) {
            const texto = formatarGenerico(val)
            if (texto) linhas.push(`${humanizarChave(k)}: ${texto}`)
        }
        return linhas.join("\n")
    }
    return ""
}

const FORMATADORES_CURADOS: Record<string, (d: Record<string, unknown>) => string> = {
    "hpp": fmtHpp,
    "h-familiar": fmtHistoriaFamiliar,
    "h-social": fmtHistoriaSocial,
    "exame-fisico": fmtExameFisico,
    "exames-realizados": fmtExamesRealizados,
    "procedimentos-indicados": fmtProcedimentos,
}

/**
 * Renderiza o conteúdo de uma seção de evolução como texto legível ao usuário.
 * Ponto único compartilhado pela modal de leitura e pelo PDF. String vazia
 * significa "seção sem conteúdo legível" — o consumidor deve omitir a seção.
 */
export function formatarSecaoLegivel(chave: string, valor: unknown): string {
    const curado = FORMATADORES_CURADOS[chave]
    if (curado && valor != null && typeof valor === "object" && !Array.isArray(valor)) {
        return curado(valor as Record<string, unknown>).trim()
    }
    return formatarGenerico(valor).trim()
}
