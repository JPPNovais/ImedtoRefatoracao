import httpClient from "./httpClient"

// ─── Tipos do catálogo (GET /api/catalogo/regioes-anatomicas) ──────────────

/**
 * Formato retornado pelo backend. Mapeamos para ExameFisicoRegiao (snake_case)
 * para manter compatibilidade com BodyMap.vue e RegionSelectorPopup.vue.
 */
interface RegiaoCatalogoDto {
    id: number
    codigo: string
    nome: string
    paiCodigo: string | null
    nivel: number
    vista: string | null
    templateTexto: string | null
    svgCoordsJson: string | null
    ordem: number
    lateralidade: boolean
    ativo: boolean
}

/**
 * Modelo local do catálogo usado pelos componentes do mapa corporal.
 * Usamos `codigo` como `id` (string) para facilitar lookup por código semântico.
 */
export interface ExameFisicoRegiao {
    id: string          // = RegiaoCatalogoDto.codigo
    nome: string
    nivel: 1 | 2 | 3
    lateralidade: boolean
    pai_id: string | null   // = paiCodigo do pai
    vista: 'anterior' | 'posterior' | 'ambos' | null
    template_texto: string | null
    ordem: number
    ativo: boolean
}

// ─── Modelo local do formulário (estado interno do ExameFisicoTab) ─────────

export interface RegiaoExaminada {
    regiao_id: string       // = codigo da região do catálogo
    caminho: string
    lateralidade: 'D' | 'E' | 'bilateral' | null
    texto_exame: string
    achados: string
    observacoes: string
    timestamp: string
}

export interface DadosGeraisExame {
    sinais_vitais: {
        pa_sistolica: string
        pa_diastolica: string
        fc: string
        fr: string
        temp: string
        spo2: string
        glicemia: string
    }
    antropometria: {
        peso: string
        altura: string
        imc: string
        imc_classificacao: string
    }
    ectoscopia: {
        estado_geral: string
        consciencia: string
        estado_nutricional: string
        coloracao: string
        hidratacao: string
        cianose: string
        ictericia: string
        temperatura_estado: string
        batimentos_cardiacos: string
        respiracao: string
        descricao: string
    }
}

// ─── DTOs do backend (camelCase via System.Text.Json) ─────────────────────

export interface RegiaoExameFisicoDto {
    id: number
    regiaoCodigo: string
    regiaoPaiCodigo: string | null
    lateralidade: string | null     // "Esquerda" | "Direita" | "Bilateral" | null
    achados: string | null
    severidade: string | null       // "Normal" | "LeveAlteracao" | "Alterado" | "Critico"
    ordem: number
}

export interface ExameFisicoDto {
    id: number
    evolucaoId: number
    prontuarioId: number
    pacienteId: number
    realizadoEm: string
    realizadoPorNome: string | null
    dadosGeraisJson: string | null  // string JSON serializado — front faz JSON.parse
    observacoesGerais: string | null
    criadoEm: string
    atualizadoEm: string | null
    regioes: RegiaoExameFisicoDto[]
}

/** Resumo usado na timeline — sem regiões detalhadas */
export interface ExameFisicoResumoDto {
    id: number
    evolucaoId: number
    realizadoEm: string
    realizadoPorNome: string | null
    totalRegioes: number
    temDadosGerais: boolean
    severidadeMaxima: string | null
}

// ─── Input para POST/PUT ───────────────────────────────────────────────────

export interface RegistrarExameFisicoInput {
    realizadoEm?: string
    dadosGeraisJson?: string        // string JSON serializada (não o objeto)
    observacoesGerais?: string
    regioes: Array<{
        codigo: string
        paiCodigo?: string | null
        lateralidade?: string | null    // "Esquerda" | "Direita" | "Bilateral" | null
        achados?: string
        severidade?: string             // "Normal" | "LeveAlteracao" | "Alterado" | "Critico"
        ordem: number
    }>
}

// ─── Helpers de mapeamento ─────────────────────────────────────────────────

function mapCatalogoParaLocal(dto: RegiaoCatalogoDto): ExameFisicoRegiao {
    return {
        id: dto.codigo,
        nome: dto.nome,
        nivel: Math.min(Math.max(dto.nivel, 1), 3) as 1 | 2 | 3,
        lateralidade: dto.lateralidade,
        pai_id: dto.paiCodigo,
        vista: (dto.vista as ExameFisicoRegiao['vista']) ?? null,
        template_texto: dto.templateTexto,
        ordem: dto.ordem,
        ativo: dto.ativo,
    }
}

// ─── Service ──────────────────────────────────────────────────────────────

export const exameFisicoService = {
    /**
     * Lista regiões anatômicas do catálogo mapeadas para o modelo local
     * (compatível com BodyMap.vue). `vista` filtra por "anterior", "posterior"
     * ou "ambos" (sem filtro = todas).
     */
    async listarRegioes(vista?: string, ativas = true): Promise<ExameFisicoRegiao[]> {
        const { data } = await httpClient.get<RegiaoCatalogoDto[]>("/catalogo/regioes-anatomicas", {
            params: { vista, ativas },
        })
        return data.map(mapCatalogoParaLocal)
    },

    /**
     * Retorna o exame físico completo de uma evolução específica.
     * `dadosGeraisJson` é uma string — o caller faz JSON.parse se necessário.
     */
    async obterPorEvolucao(evolucaoId: string | number): Promise<ExameFisicoDto | null> {
        const { data } = await httpClient.get<ExameFisicoDto | null>(
            `/evolucoes/${evolucaoId}/exame-fisico`
        )
        return data
    },

    /**
     * Retorna o exame físico completo por ID.
     * Usado para duplicar a partir do resumo da timeline.
     */
    async obterPorId(exameFisicoId: string | number): Promise<ExameFisicoDto | null> {
        const { data } = await httpClient.get<ExameFisicoDto | null>(
            `/exame-fisico/${exameFisicoId}`
        )
        return data
    },

    /**
     * Lista os últimos N exames físicos de um paciente (timeline resumida).
     * Não inclui regiões detalhadas — use `obterPorId` para duplicar.
     */
    async listarTimeline(pacienteId: string, ate = 10): Promise<ExameFisicoResumoDto[]> {
        const { data } = await httpClient.get<ExameFisicoResumoDto[]>(
            `/pacientes/${pacienteId}/exames-fisicos/timeline`,
            { params: { ate } }
        )
        return data
    },

    /**
     * Registra um novo exame físico para uma evolução.
     */
    async registrar(evolucaoId: string | number, input: RegistrarExameFisicoInput): Promise<{ exameFisicoId: number }> {
        const { data } = await httpClient.post<{ exameFisicoId: number }>(
            `/evolucoes/${evolucaoId}/exame-fisico`,
            input
        )
        return data
    },
}
