/**
 * Catálogo das 17 seções e helpers do ModeloProntuarioBuilder.
 * Fonte única de verdade — tenant e admin importam daqui.
 */
import type { SecaoModelo } from "@/services/prontuarioService"

// ─── Dados de exemplo para prévia de seção ────────────────────────────────────
//
// Padrão "prévia de seção": ModeloProntuarioBuilder.vue oferece um botão de olho
// por seção que abre AppModal renderizando SecaoProntuario.vue em readOnly com
// estes dados de exemplo estáticos (R3/R4 do briefing 2026-06-26_001).
//
// Seções de texto (string): cid10 e fallback texto_longo.
// Seções estruturadas (objeto): shape exato que cada componente filho espera.
// R-EXFIS: ExameFisico traz regioes já materializadas para não depender do
// catálogo do estabelecimento — o componente não pode quebrar sem catálogo.

export const EXEMPLOS_SECAO_MODELO: Record<string, unknown> = {
    // ── Texto livre / texto_longo ──────────────────────────────────────────────
    queixa: "Paciente refere dor abdominal em região epigástrica há 3 dias, de intensidade 6/10, contínua, sem irradiação, associada a náuseas e hiporexia.",
    hda: "Dor iniciou de forma insidiosa há 72 horas, sem fator de melhora ou piora aparente. Paciente nega febre, vômitos ou alteração do hábito intestinal. Fez uso de analgésico comum sem melhora significativa.",
    "procedimento-consultorio": "Realizada curetagem de ceratose seborreica em região dorsal com bisturi elétrico, sob anestesia local com lidocaína 2%. Hemostasia adequada. Peça enviada para anatomopatológico.",
    "ficha-anestesica": "Paciente em bom estado geral, orientada, corada, hidratada, anictérica, acianótica. Mallampati I. Via aérea sem alterações. ASA II por hipertensão arterial controlada.",
    "equipe-cirurgica": "Cirurgião: Dr. Carlos Andrade. 1º auxiliar: Dra. Fernanda Lima. Anestesiologista: Dr. Rodrigo Souza. Instrumentadora: Enf. Mariana Costa.",
    "fotos-paciente": "Fotografia pré-operatória de frente e perfil realizadas. Imagens armazenadas no prontuário digital.",
    anexos: "Laudo de ultrassonografia abdominal total anexado. Resultado de hemograma completo e PCR em anexo.",
    cid10: "K92.1 — Melenas",

    // ── HPP — História pregressa ───────────────────────────────────────────────
    hpp: {
        alergiasTem: true,
        alergias: [
            { nome: "Dipirona", observacao: "Urticária generalizada após uso oral" },
            { nome: "Penicilina", observacao: "Anafilaxia — uso hospitalar proibido" },
        ],
        medicacoesTem: true,
        medicacoes: [
            { nome: "Losartana", dose: "50 mg", frequencia: "1x/dia", motivo: "Hipertensão arterial", observacoes: "Em uso há 5 anos, tolerada bem" },
            { nome: "Metformina", dose: "850 mg", frequencia: "2x/dia", motivo: "Diabetes tipo 2", observacoes: "Controle glicêmico adequado" },
        ],
        cirurgiasTem: true,
        cirurgias: [
            { nome: "Apendicectomia", ano: "2015", observacao: "Laparoscópica, sem intercorrências" },
        ],
        doencasTem: true,
        doencas: [
            { nome: "Hipertensão arterial sistêmica", observacao: "Controlada com losartana" },
            { nome: "Diabetes mellitus tipo 2", observacao: "HbA1c: 7,2% — última consulta" },
        ],
        observacoes: "Paciente nega outras condições crônicas relevantes.",
    },

    // ── H-familiar — História familiar ────────────────────────────────────────
    "h-familiar": {
        paiDoencas: "Sim",
        paiDescricao: "Diabetes mellitus tipo 2, hipertensão arterial e infarto agudo do miocárdio aos 62 anos.",
        maeDoencas: "Sim",
        maeDescricao: "Câncer de mama diagnosticado aos 55 anos — tratada com quimioterapia, em remissão.",
        parentes: [
            { parentesco: "Irmão", doencas: "Dislipidemia, hipertensão", comentario: "Em uso de estatina" },
            { parentesco: "Avó materna", doencas: "Glaucoma", comentario: "Cirurgia ocular aos 70 anos" },
        ],
        observacao: "Histórico familiar relevante para doenças cardiovasculares e oncológicas.",
    },

    // ── H-social — História social ────────────────────────────────────────────
    "h-social": {
        estadoCivil: "Casado(a)",
        filhosTem: true,
        filhosQuantos: "2",
        filhosIdades: "8 e 12 anos",
        filhosObs: "Crianças saudáveis",
        tabagismoTem: false,
        tabagismoStatus: "Não fuma / nunca fumou",
        etilismoTem: true,
        etilismoStatus: "Social",
        etilismoObs: "Faz uso ocasional nos fins de semana",
        drogasTem: false,
        atividadeFisicaTem: true,
        atividadeFisicaNivel: "Moderado",
        atividadeFisicaObs: "Caminhada 3x/semana, 40 minutos",
        alimentacao: "Equilibrada",
        alimentacaoObs: "Refere seguir dieta para diabéticos orientada por nutricionista",
        sonoQualidade: "Bom (7-8h)",
        sonoObs: "Sem queixas de insônia ou apneia",
    },

    // ── Exame físico (R-EXFIS: regioes materializadas, sem catálogo) ──────────
    "exame-fisico": {
        paSistolica: "130",
        paDiastolica: "85",
        fc: "78",
        fr: "16",
        temperatura: "36,7",
        spo2: "98",
        glicemia: "112",
        peso: "72",
        altura: "168",
        estadoGeral: "Bom",
        consciencia: "Orientado(a)",
        estadoNutricional: "Eutrófico(a)",
        coloracao: "Corado(a)",
        hidratacao: "Hidratado(a)",
        cianose: "Acianótico(a)",
        ictericia: "Anictérico(a)",
        observacoesExame: "Abdome plano, flácido, com dor à palpação profunda em epigástrio. Ruídos hidroaéreos presentes. Sem visceromegalias.",
        // Regiões já materializadas — não depende do catálogo do estabelecimento
        regioes: [
            {
                regiao_id: "abdome-anterior",
                caminho: "Abdome / Anterior",
                lateralidade: null,
                vista: "anterior" as const,
                texto_exame: "Dor à palpação profunda em epigástrio.",
                achados: "Epigastralgia à palpação profunda, sem defesa muscular.",
                observacoes: "Ruídos hidroaéreos presentes e normoativos.",
                timestamp: "2026-01-15T10:30:00.000Z",
            },
        ],
    },

    // ── Exames realizados ──────────────────────────────────────────────────────
    "exames-realizados": {
        itens: [
            { tipo: "Laboratorial", material: "Sangue", nome: "Hemograma completo", comentario: "Hemoglobina 13,2 g/dL. Leucócitos 9.800/mm³. Plaquetas normais." },
            { tipo: "Laboratorial", material: "Sangue", nome: "PCR (proteína C-reativa)", comentario: "PCR: 18 mg/L (elevado). Sugere processo inflamatório." },
            { tipo: "Imagem", material: "", nome: "Ultrassonografia abdominal total", comentario: "Fígado com esteatose grau I. Vesícula biliar sem cálculos. Sem líquido livre." },
        ],
        observacoes: "Exames colhidos em 15/01/2026. Aguardando resultado de lipase e amilase.",
    },

    // ── Procedimentos indicados ────────────────────────────────────────────────
    "procedimentos-indicados": {
        procedimentos: [
            { descricao: "Colecistectomia videolaparoscópica", observacao: "Indicada por colelitíase sintomática — cirurgia eletiva, aguarda preparo" },
            { descricao: "Endoscopia digestiva alta", observacao: "Para avaliação de gastrite e rastreamento de H. pylori" },
        ],
        observacoes: "Procedimentos discutidos com a paciente. Consentimento obtido.",
    },

    // ── Evolução pós-operatória ────────────────────────────────────────────────
    "evolucao-pos-op": {
        evolucaoPaciente: "boa",
        evolucaoComentario: "Paciente em bom estado geral, sem queixas álgicas significativas. Aceitando dieta oral.",
        seguindoOrientacoes: "sim",
        orientacoesComentario: "Segue todas as orientações pós-operatórias. Ferida operatória limpa e seca.",
        dataCirurgia: "2026-01-10",
        dpo: "5",
        destino: "Alta",
        dieta: "Livre",
        observacao: "Alta hospitalar prevista para amanhã. Retorno ambulatorial em 14 dias para retirada de pontos.",
    },

    // ── Descrição cirúrgica ────────────────────────────────────────────────────
    "desc-cirurgica": {
        cirurgiao: "Dr. Carlos Andrade",
        data: "2026-01-10",
        diaSemana: "Sexta-feira",
        cirurgiasRealizadas: "Colecistectomia videolaparoscópica",
        anestesista: "Dr. Rodrigo Souza",
        auxiliar: "Dra. Fernanda Lima",
        instrumentador: "Enf. Mariana Costa",
        outrosMembros: [],
        cirurgiaInicio: "08:30",
        cirurgiaFim: "09:45",
        profilaxia: {
            enoxaparina: true,
            meiaCompressiva: true,
            botaPneumatica: false,
            deambulacaoPrecoce: true,
            antitrombOutroAtivo: false,
            antitrombOutro: "",
            cefazolina: true,
            gentamicina: false,
            antibioOutroAtivo: false,
            antibioOutro: "",
        },
        intercorrencia: "sem",
        intercorrenciaDescricao: "",
        tecnicaOperatoria: "Pneumoperitônio criado com agulha de Veress. Quatro portais inseridos. Colecistectomia anterógrada com clipagem do ducto cístico e artéria cística. Peça retirada em endobag. Hemostasia revisada. Fechamento por planos.",
        observacoes: "Evolução intraoperatória sem intercorrências. Paciente encaminhada à RPA em bom estado geral.",
    },

    // ── Conduta checklist ──────────────────────────────────────────────────────
    conduta: {
        acoesMarcadas: ["CriarReceita", "AgendarRetorno"],
        observacao: "Prescrito omeprazol 20 mg 1x/dia em jejum por 30 dias. Retorno em 4 semanas para reavaliação.",
    },
}

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
