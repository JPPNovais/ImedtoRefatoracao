/**
 * Catálogo das 17 seções e helpers do ModeloProntuarioBuilder.
 * Fonte única de verdade — tenant e admin importam daqui.
 */
import type { SecaoModelo } from "@/services/prontuarioService"

export interface SecaoBuilderItem {
    key: string
    label: string
    tipo: "texto" | "texto_longo" | "conduta_checklist"
    info: string
}

export const SECOES_MODELO_PRONTUARIO: SecaoBuilderItem[] = [
    { key: "queixa",                   label: "Queixa principal (QP)",            tipo: "texto_longo", info: "Motivo pelo qual o paciente procurou atendimento, descrito com suas próprias palavras." },
    { key: "hda",                      label: "História da doença atual (HDA)",    tipo: "texto_longo", info: "Detalhamento cronológico dos sintomas e evolução da condição atual do paciente." },
    { key: "hpp",                      label: "História pregressa (HPP)",           tipo: "texto_longo", info: "Doenças anteriores, cirurgias, internações, alergias e medicamentos em uso contínuo." },
    { key: "h-familiar",               label: "História familiar",                  tipo: "texto_longo", info: "Doenças hereditárias ou de alta prevalência nos familiares diretos do paciente." },
    { key: "h-social",                 label: "História social e hábitos de vida",  tipo: "texto_longo", info: "Ocupação, hábitos, tabagismo, consumo de álcool, atividade física e condições socioeconômicas." },
    { key: "exame-fisico",             label: "Exame físico",                       tipo: "texto_longo", info: "Dados vitais e avaliação clínica dos sistemas do organismo." },
    { key: "exames-realizados",        label: "Exames realizados",                  tipo: "texto_longo", info: "Resultados de exames laboratoriais, de imagem e outros já realizados." },
    { key: "procedimentos-indicados",  label: "Procedimentos indicados",            tipo: "texto_longo", info: "Procedimentos, cirurgias ou intervenções recomendadas." },
    { key: "evolucao-pos-op",          label: "Evolução pós-operatória",            tipo: "texto_longo", info: "Acompanhamento e recuperação do paciente após cirurgia ou procedimento." },
    { key: "desc-cirurgica",           label: "Descrição cirúrgica",                tipo: "texto_longo", info: "Relato detalhado do ato cirúrgico realizado, incluindo técnica e intercorrências." },
    { key: "procedimento-consultorio", label: "Procedimento em consultório",        tipo: "texto_longo", info: "Procedimentos menores realizados no consultório durante o atendimento." },
    { key: "ficha-anestesica",         label: "Ficha anestésica",                   tipo: "texto_longo", info: "Avaliação pré-anestésica e execução da anestesia no procedimento." },
    { key: "equipe-cirurgica",         label: "Equipe cirúrgica",                   tipo: "texto_longo", info: "Profissionais que participaram do procedimento cirúrgico." },
    { key: "fotos-paciente",           label: "Fotos do paciente",                  tipo: "texto_longo", info: "Registro fotográfico do paciente (pré e pós-operatório)." },
    { key: "anexos",                   label: "Anexos",                             tipo: "texto_longo", info: "Documentos e arquivos complementares ao prontuário." },
    { key: "cid10",                    label: "CID-10",                             tipo: "texto",       info: "Classificação Internacional de Doenças para codificação do diagnóstico." },
    { key: "conduta",                  label: "Conduta",                            tipo: "conduta_checklist", info: "Plano terapêutico: checklist de ações com observação livre." },
]

const CHAVES_CONHECIDAS = new Set(SECOES_MODELO_PRONTUARIO.map(s => s.key))

export function parsearEstruturaJson(json: string): { conhecidas: SecaoModelo[]; customizadas: SecaoModelo[] } {
    if (!json || !json.trim()) return { conhecidas: [], customizadas: [] }
    try {
        let parsed: unknown = JSON.parse(json)
        // Retrocompat: suporta envelope { secoes: [...] } além de array direto
        if (parsed && typeof parsed === "object" && !Array.isArray(parsed) && "secoes" in (parsed as object)) {
            parsed = (parsed as { secoes: unknown }).secoes
        }
        if (!Array.isArray(parsed)) {
            console.warn("[ModeloProntuarioBuilder] estruturaJson não é array nem { secoes: [] } — iniciando vazio.")
            return { conhecidas: [], customizadas: [] }
        }
        const array = parsed as SecaoModelo[]
        const conhecidas = array.filter(s => CHAVES_CONHECIDAS.has(s.chave))
        const customizadas = array.filter(s => !CHAVES_CONHECIDAS.has(s.chave))
        return { conhecidas, customizadas }
    } catch {
        console.warn("[ModeloProntuarioBuilder] estruturaJson inválido — iniciando vazio.")
        return { conhecidas: [], customizadas: [] }
    }
}

export function gerarEstruturaJson(secoesAtivas: SecaoModelo[], customizadas: SecaoModelo[]): string {
    const todas: SecaoModelo[] = [
        ...secoesAtivas,
        ...customizadas.map((s, i) => ({ ...s, ordem: secoesAtivas.length + i })),
    ]
    return JSON.stringify(todas)
}
