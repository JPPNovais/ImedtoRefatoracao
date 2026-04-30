import httpClient from "./httpClient"

export interface ExameFisicoRegiao {
    id: string
    nome: string
    nivel: 1 | 2 | 3
    lateralidade: boolean
    pai_id: string | null
    vista: 'anterior' | 'posterior' | 'ambos' | null
    template_texto: string | null
    ordem: number
    ativo: boolean
}

export interface RegiaoExaminada {
    regiao_id: string
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

export interface ExameFisicoRegistro {
    id: string
    prontuario_id: string
    estabelecimento_id: string
    profissional_id: string
    paciente_id: string
    evolucao_prontuario_id: string | null
    dados_gerais: DadosGeraisExame
    regioes_examinadas: RegiaoExaminada[]
    observacoes: string | null
    criado_em: string
    profissional_nome?: string
}

export interface RegistrarExameFisicoInput {
    realizadoEm?: string
    dadosGeraisJson: DadosGeraisExame
    observacoesGerais?: string
    regioes: Array<{
        regiaoId: string
        caminhoTexto: string
        lateralidade: 'D' | 'E' | 'bilateral' | null
        textoExame: string
        achados: string
        observacoes: string
    }>
}

export const exameFisicoService = {
    /**
     * Lista regiões anatômicas do catálogo.
     * `vista` filtra por "anterior", "posterior" ou "ambos" (sem filtro = todas).
     */
    async listarRegioes(vista?: string, ativas = true): Promise<ExameFisicoRegiao[]> {
        const { data } = await httpClient.get<ExameFisicoRegiao[]>("/catalogo/regioes-anatomicas", {
            params: { vista, ativas },
        })
        return data
    },

    /**
     * Retorna o exame físico de uma evolução específica.
     */
    async obterPorEvolucao(evolucaoId: string | number): Promise<ExameFisicoRegistro | null> {
        const { data } = await httpClient.get<ExameFisicoRegistro | null>(
            `/evolucoes/${evolucaoId}/exame-fisico`
        )
        return data
    },

    /**
     * Lista os últimos N exames físicos de um paciente (timeline).
     */
    async listarTimeline(pacienteId: string, ate = 10): Promise<ExameFisicoRegistro[]> {
        const { data } = await httpClient.get<ExameFisicoRegistro[]>(
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
