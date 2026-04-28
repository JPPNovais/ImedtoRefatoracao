/**
 * Catálogo de permissões granulares — portado do legado
 * (Imedto/src/constants/permissions.ts).
 *
 * Cada modelo de permissão guarda um array com as keys aqui definidas. Telas
 * do app consultam essa lista para decidir se mostram itens de menu / liberam
 * ações.
 */
export type PermissionId =
    | "home"
    | "agenda"
    | "minhas_consultas"
    | "pacientes"
    | "prontuario"
    | "modelos_prontuario"
    | "perfil_profissional"
    | "profissionais"
    | "permissoes"
    | "estoque"
    | "orcamentos"
    | "financeiro"
    | "config_estabelecimento"
    | "relatorios"
    | "assistente_clinico"
    | "automacao"

export interface PermissionDef {
    id: PermissionId
    label: string
}

export const PERMISSIONS: PermissionDef[] = [
    { id: "home",                   label: "Painel inicial" },
    { id: "agenda",                 label: "Agendamentos" },
    { id: "minhas_consultas",       label: "Minhas consultas" },
    { id: "pacientes",              label: "Pacientes" },
    { id: "prontuario",             label: "Prontuário do paciente" },
    { id: "modelos_prontuario",     label: "Modelos de prontuário" },
    { id: "perfil_profissional",    label: "Meu cadastro profissional" },
    { id: "profissionais",          label: "Gestão de profissionais" },
    { id: "permissoes",             label: "Gestão de permissões" },
    { id: "estoque",                label: "Estoque" },
    { id: "orcamentos",             label: "Orçamentos cirúrgicos" },
    { id: "financeiro",             label: "Financeiro" },
    { id: "config_estabelecimento", label: "Configurações do estabelecimento" },
    { id: "relatorios",             label: "Relatórios gerenciais" },
    { id: "assistente_clinico",     label: "Assistente clínico (IA)" },
    { id: "automacao",              label: "Automação inteligente" },
]

export function permissionLabel(id: string): string {
    return PERMISSIONS.find(p => p.id === id)?.label ?? id
}
