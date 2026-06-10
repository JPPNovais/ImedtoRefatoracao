/**
 * Catálogo de áreas e ações granulares de permissão (formato `area.acao`).
 *
 * Este catálogo é a fonte de verdade no front e deve casar com
 * `backend/src/Services/Imedto.Backend.Domain/ModelosPermissao/CatalogoPermissoes.cs`.
 *
 * O modelo de permissão de cada profissional armazena um array de strings com
 * essas chaves. Para checar acesso, use `temArea(perms, 'agenda')` ou
 * `temAcao(perms, 'agenda', 'criar')` — ambos aceitam tanto o formato novo
 * (`agenda.criar`) quanto o legado (`agenda` = acesso total à área), garantindo
 * convivência durante a transição.
 */

export interface AreaPermissao {
    chave: string
    label: string
    icone: string
    descricao: string
    acoes: AcaoPermissao[]
}

export interface AcaoPermissao {
    chave: string
    label: string
}

export const PERMISSION_AREAS: AreaPermissao[] = [
    {
        chave: "agenda",
        label: "Agenda",
        icone: "fa-calendar-days",
        descricao: "Visualizar, criar e gerenciar agendamentos",
        acoes: [
            { chave: "ver",     label: "Ver agenda" },
            { chave: "criar",   label: "Criar agendamentos" },
            { chave: "editar",  label: "Editar e reagendar" },
            { chave: "excluir", label: "Excluir e cancelar" },
        ],
    },
    {
        chave: "prontuario",
        label: "Prontuário eletrônico",
        icone: "fa-file-medical",
        descricao: "Acesso ao histórico clínico do paciente",
        acoes: [
            { chave: "ver",     label: "Ver prontuário" },
            { chave: "editar",  label: "Editar prontuário" },
            { chave: "assinar", label: "Assinar digitalmente" },
        ],
    },
    {
        chave: "prescricao",
        label: "Prescrição digital",
        icone: "fa-prescription-bottle-medical",
        descricao: "Emitir receitas e atestados",
        acoes: [
            { chave: "criar",   label: "Emitir prescrição" },
            { chave: "assinar", label: "Assinar com certificado" },
        ],
    },
    {
        chave: "pacientes",
        label: "Pacientes",
        icone: "fa-user-injured",
        descricao: "Cadastro e gestão de pacientes",
        acoes: [
            { chave: "ver",     label: "Ver cadastro" },
            { chave: "criar",   label: "Cadastrar" },
            { chave: "editar",  label: "Editar" },
            { chave: "excluir", label: "Excluir" },
        ],
    },
    {
        chave: "financeiro",
        label: "Financeiro",
        icone: "fa-coins",
        descricao: "Lançamentos, caixa e fluxo de caixa",
        acoes: [
            { chave: "ver",    label: "Ver financeiro" },
            { chave: "lancar", label: "Lançar entradas/saídas" },
            { chave: "fechar", label: "Fechar caixa" },
        ],
    },
    {
        chave: "financeiro_paciente",
        label: "Cobrança do paciente",
        icone: "fa-hand-holding-dollar",
        descricao: "Ver e registrar pagamentos de consultas e atendimentos",
        acoes: [
            { chave: "ver",       label: "Ver cobranças" },
            { chave: "registrar", label: "Registrar pagamentos" },
        ],
    },
    {
        chave: "orcamento",
        label: "Orçamento",
        icone: "fa-file-invoice-dollar",
        descricao: "Criação, edição, aprovação e configuração de orçamentos",
        acoes: [
            { chave: "ver",        label: "Ver orçamentos" },
            { chave: "criar",      label: "Criar orçamento" },
            { chave: "editar",     label: "Editar orçamento" },
            { chave: "aprovar",    label: "Aprovar / recusar / converter em cirurgia" },
            { chave: "configurar", label: "Configurar catálogos (procedimentos, produtos, equipe, local, pagamento)" },
        ],
    },
    {
        chave: "convenios",
        label: "Convênios e faturamento",
        icone: "fa-shield-halved",
        descricao: "Glosas, faturas e configuração de convênios",
        acoes: [
            { chave: "ver",        label: "Ver convênios" },
            { chave: "gerenciar",  label: "Gerenciar e faturar" },
        ],
    },
    {
        chave: "estoque",
        label: "Estoque / produtos",
        icone: "fa-boxes-stacked",
        descricao: "Controle de produtos e materiais",
        acoes: [
            { chave: "ver",        label: "Ver estoque" },
            { chave: "gerenciar",  label: "Movimentar estoque" },
        ],
    },
    {
        chave: "relatorios",
        label: "Relatórios e dashboards",
        icone: "fa-chart-pie",
        descricao: "Indicadores e relatórios gerenciais",
        acoes: [
            { chave: "ver",       label: "Ver relatórios" },
            { chave: "exportar",  label: "Exportar dados" },
        ],
    },
    {
        chave: "configuracoes",
        label: "Configurações da clínica",
        icone: "fa-gear",
        descricao: "Preferências, horários e dados da clínica",
        acoes: [
            { chave: "gerenciar", label: "Editar configurações" },
        ],
    },
    {
        chave: "equipe",
        label: "Gerenciar equipe",
        icone: "fa-users-gear",
        descricao: "Convidar, remover e definir permissões",
        acoes: [
            { chave: "ver",         label: "Ver equipe" },
            { chave: "convidar",    label: "Convidar profissionais" },
            { chave: "permissoes",  label: "Alterar permissões" },
            { chave: "remover",     label: "Remover da clínica" },
        ],
    },
]

/** Lista plana de todas as permissões granulares (formato `area.acao`). */
export const TODAS_PERMISSOES: string[] =
    PERMISSION_AREAS.flatMap(a => a.acoes.map(ac => `${a.chave}.${ac.chave}`))

/**
 * Indica se a lista de permissões concede acesso à área (em qualquer ação).
 * Aceita tanto chave legada (`"agenda"`) quanto granular (`"agenda.ver"`).
 */
export function temArea(perms: string[] | null | undefined, area: string): boolean {
    if (!perms || !perms.length || !area) return false
    const prefixo = area + "."
    return perms.some(p => p === area || p.startsWith(prefixo))
}

/**
 * Indica se a lista de permissões concede a ação granular informada.
 * Para chaves legadas (sem ponto), considera que ter a área concede todas as ações.
 */
export function temAcao(perms: string[] | null | undefined, area: string, acao: string): boolean {
    if (!perms || !perms.length || !area || !acao) return false
    const chaveCompleta = `${area}.${acao}`
    return perms.some(p => p === chaveCompleta || p === area)
}

/** Conta quantas ações da área estão concedidas (legado conta como todas concedidas). */
export function contarAcoesGranjadas(perms: string[] | null | undefined, area: AreaPermissao): number {
    if (!perms || !perms.length) return 0
    if (perms.includes(area.chave)) return area.acoes.length
    return area.acoes.filter(ac => perms.includes(`${area.chave}.${ac.chave}`)).length
}

// ── Compatibilidade com o catálogo antigo (uma chave por tela) ────────────
// Mantido para callers legados que ainda chamam permissionLabel(string).
// Novas telas devem usar o catálogo PERMISSION_AREAS acima.

const LABELS_LEGADOS: Record<string, string> = {
    home: "Painel inicial",
    minhas_consultas: "Minhas consultas",
    perfil_profissional: "Meu cadastro profissional",
    profissionais: "Gestão de profissionais",
    permissoes: "Gestão de permissões",
    modelos_prontuario: "Modelos de prontuário",
    config_estabelecimento: "Configurações do estabelecimento",
    assistente_clinico: "Assistente clínico (IA)",
    automacao: "Automação inteligente",
}

/**
 * Resolve um label humano para uma chave de permissão. Aceita tanto o formato
 * granular (`"agenda.ver"` → "Ver agenda"), quanto o legado por área (`"agenda"`
 * → "Agenda") e chaves legadas extras (home, minhas_consultas, etc.).
 */
export function permissionLabel(chave: string): string {
    if (!chave) return ""
    // Granular: "area.acao"
    if (chave.includes(".")) {
        const [areaKey, acaoKey] = chave.split(".", 2)
        const area = PERMISSION_AREAS.find(a => a.chave === areaKey)
        const acao = area?.acoes.find(ac => ac.chave === acaoKey)
        if (area && acao) return `${area.label} · ${acao.label}`
        return chave
    }
    // Legacy: chave de área ou tela.
    const area = PERMISSION_AREAS.find(a => a.chave === chave)
    if (area) return area.label
    return LABELS_LEGADOS[chave] ?? chave
}


