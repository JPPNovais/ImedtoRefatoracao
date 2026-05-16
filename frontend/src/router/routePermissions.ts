/**
 * Catálogo único de permissões necessárias por rota nomeada.
 *
 * Fonte de verdade compartilhada entre:
 *  - O `beforeEach` do router (bloqueia navegação e redireciona para Home).
 *  - O sidebar/layout (esconde itens de menu que o usuário não pode acessar).
 *
 * Manter a regra em um único lugar evita o anti-padrão atual de "menu esconde
 * o item mas a URL direta abre a tela e estoura 422 lá dentro". Com este mapa,
 * as duas pontas usam exatamente a mesma checagem.
 *
 * Convenções:
 *  - `acao`: chave "area.acao" do catálogo backend (CatalogoPermissoes / PERMISSION_AREAS).
 *    Dono passa em qualquer ação automaticamente (ver `permissoesStore.pode`).
 *  - `extras`: lista de permissões finas (PermissoesExtras). Usuário precisa de
 *    PELO MENOS UMA. Dono passa em qualquer extra.
 *  - `somenteDono`: gate adicional para telas exclusivas do proprietário do
 *    estabelecimento (ex: configurações de IA, modelos de prontuário, equipe).
 *  - Quando há mais de um critério, basta atender QUALQUER UM deles (OR), igual
 *    ao que já era feito no AppLayout para o item "Equipe".
 *
 * Rotas que NÃO aparecem aqui são livres para qualquer usuário autenticado com
 * tenant ativo — Home, Pacientes (lista paginada já gateada por endpoint),
 * MeusConvites, MinhaConta, Notificacoes, MinhaContaLgpd, MinhaAssinatura etc.
 */

export interface RegraRota {
    acao?: string
    extras?: string[]
    somenteDono?: boolean
}

/**
 * Mapeamento route.name → regra. Use `routeRequer(name)` para consultar.
 *
 * Cada entrada espelha o gate efetivamente exigido pelo backend (ver
 * controllers com `[RequiresAcao(...)]` / `[RequiresPermissaoExtra(...)]`).
 * Quando a tela combina várias áreas (ex: Equipe = listar profissionais +
 * gerir permissões), aceitamos qualquer uma — defense-in-depth: o backend
 * continua filtrando por endpoint.
 */
export const ROTAS_RESTRITAS: Record<string, RegraRota> = {
    // Agenda e atendimentos
    Agenda:           { acao: "agenda.ver" },
    MinhasConsultas:  { acao: "agenda.ver" },

    // Pacientes
    Pacientes:        { acao: "pacientes.ver" },
    PacienteDetalhe:  { acao: "pacientes.ver" },
    Prontuario:       { acao: "prontuario.ver" },

    // Equipe e permissões — tela unificada (qualquer entrada de gestão libera).
    Equipe:           { acao: "equipe.ver", extras: ["gerir_profissionais", "gerir_permissoes"] },

    // Financeiro
    Financeiro:               { acao: "financeiro.ver" },
    CategoriasFinanceiras:    { acao: "financeiro.ver" },
    FormasPagamento:          { acao: "financeiro.ver" },

    // Orçamentos
    Orcamentos:         { acao: "orcamento.ver" },
    OrcamentoDetalhe:   { acao: "orcamento.ver" },
    OrcamentoForm:      { acao: "orcamento.ver" },
    OrcamentoSettings:  { acao: "orcamento.ver" },

    // Estoque
    Inventario:           { acao: "estoque.ver" },
    InventarioCadastros:  { acao: "estoque.ver" },

    // Relatórios
    Relatorios:         { acao: "relatorios.ver" },

    // Configurações do estabelecimento e IA — `config_estabelecimento` libera o
    // não-dono; sem essa extra, é Dono-only (controllers atrás exigem a mesma extra).
    Estabelecimento:    { extras: ["config_estabelecimento"] },
    IaSettings:         { extras: ["config_estabelecimento"] },
    ModelosProntuario:  { extras: ["modelos_prontuario"] },

    // Automação inteligente — permissão extra dedicada.
    Automacoes:         { extras: ["automacao_config"] },

    // Cirurgias — vinculadas a prontuário.
    CirurgiaDetalhe:    { acao: "prontuario.ver" },
}

/** Indica se a rota é restrita (precisa de checagem extra além de auth+tenant). */
export function rotaRestrita(nome: string | null | undefined): boolean {
    if (!nome) return false
    return Object.prototype.hasOwnProperty.call(ROTAS_RESTRITAS, nome)
}

/** Devolve a regra da rota nomeada, ou null se a rota não exige gate adicional. */
export function regraDaRota(nome: string | null | undefined): RegraRota | null {
    if (!nome) return null
    return ROTAS_RESTRITAS[nome] ?? null
}

/**
 * Avalia se um usuário (representado pelos helpers do `permissoesStore`) pode
 * acessar a rota. Recebe funções para evitar acoplamento direto ao store —
 * facilita testes unitários sem montar Pinia.
 *
 * Retorna true quando QUALQUER critério é satisfeito. Dono passa sempre (os
 * helpers `pode`/`podeExtra` já tratam isso).
 */
export function podeAcessarRota(
    nome: string | null | undefined,
    helpers: {
        ehDono: boolean
        pode: (chave: string) => boolean
        podeExtra: (chave: string) => boolean
    },
): boolean {
    const regra = regraDaRota(nome)
    if (!regra) return true

    if (helpers.ehDono) return true
    if (regra.somenteDono) return false

    if (regra.acao && helpers.pode(regra.acao)) return true
    if (regra.extras && regra.extras.some(e => helpers.podeExtra(e))) return true

    // Quando a regra só especifica `somenteDono` (já tratado acima) e nada
    // mais, qualquer não-Dono é bloqueado. Quando especifica `acao`/`extras`
    // e nenhum bateu, idem.
    return false
}
