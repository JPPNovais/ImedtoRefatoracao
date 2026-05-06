/**
 * Catálogo de tags clínicas/operacionais aplicáveis a pacientes.
 *
 * O backend armazena apenas a chave (string curta). O front-end mapeia para
 * label + ícone + cor para badges visuais e filtros. Tags fora do catálogo
 * caem em um estilo neutro (sem cor) — front nunca "quebra" se o backend
 * tiver uma tag desconhecida.
 */

export interface TagPaciente {
    chave: string
    label: string
    icone: string
    cor: string
}

export const PACIENTE_TAGS: TagPaciente[] = [
    { chave: "vip",        label: "VIP",                icone: "fa-star",                 cor: "hsl(45 96% 50%)" },
    { chave: "gestante",   label: "Gestante",           icone: "fa-baby",                 cor: "hsl(340 60% 55%)" },
    { chave: "alergia",    label: "Alergia grave",      icone: "fa-triangle-exclamation", cor: "hsl(0 84% 60%)" },
    { chave: "cronico",    label: "Crônico",            icone: "fa-heart-pulse",          cor: "hsl(280 50% 50%)" },
    { chave: "idoso",      label: "Idoso",              icone: "fa-person-cane",          cor: "hsl(220 50% 50%)" },
    { chave: "novo",       label: "Novo paciente",      icone: "fa-seedling",             cor: "hsl(160 79% 39%)" },
    { chave: "recorrente", label: "Recorrente",         icone: "fa-rotate",               cor: "hsl(199 89% 48%)" },
    { chave: "inativo",    label: "Inativo",            icone: "fa-circle-pause",         cor: "hsl(0 0% 50%)" },
]

const TAG_BY_CHAVE: Record<string, TagPaciente> = Object.fromEntries(
    PACIENTE_TAGS.map(t => [t.chave, t]),
)

/**
 * Resolve uma tag pela chave. Para chaves desconhecidas, retorna um objeto
 * "fallback" com a própria chave como label e estilo neutro — o front nunca
 * deve quebrar caso o backend tenha tags fora do catálogo.
 */
export function resolverTag(chave: string): TagPaciente {
    return TAG_BY_CHAVE[chave] ?? {
        chave,
        label: chave,
        icone: "fa-tag",
        cor: "hsl(0 0% 45%)",
    }
}
