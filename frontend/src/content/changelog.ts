// Conteúdo estático do changelog público — editado via commit, sem API.
// TagChangelog é um conjunto fechado: tag fora do tipo gera erro de compilação (CA6).

export type TagChangelog = "novidade" | "melhoria" | "correção"

export interface EntradaChangelog {
    data: string        // ISO "YYYY-MM-DD" — usado para ordenar por data desc
    titulo: string      // linguagem leiga para clínicas (sem jargão técnico)
    descricao: string   // idem
    tag: TagChangelog
}

export const CHANGELOG: EntradaChangelog[] = [
    {
        data: "2026-06-10",
        titulo: "Novidades e Status do sistema agora públicos",
        descricao:
            "Adicionamos duas novas páginas públicas: esta de Novidades — para você acompanhar o que mudou no Imedto — e a de Status do sistema, que mostra se a plataforma está funcionando normalmente.",
        tag: "novidade",
    },
    {
        data: "2026-06-09",
        titulo: "Aba de documentos do paciente",
        descricao:
            "No prontuário de cada paciente, uma nova aba reúne em um só lugar todas as receitas, atestados e pedidos de exame já emitidos — sem precisar navegar entre seções diferentes.",
        tag: "novidade",
    },
    {
        data: "2026-06-09",
        titulo: "Impressão de receita com cabeçalho institucional",
        descricao:
            "Receitas geradas pelo sistema agora saem com logotipo, endereço e dados do estabelecimento no topo, prontas para entregar ao paciente com mais profissionalismo.",
        tag: "melhoria",
    },
    {
        data: "2026-06-08",
        titulo: "Nova tela de configurações do estabelecimento",
        descricao:
            "Reorganizamos a área de configurações: agora tudo (dados do estabelecimento, plano, modelos de prontuário, termos, automações e IA) fica em um único painel lateral, mais fácil de navegar.",
        tag: "melhoria",
    },
    {
        data: "2026-06-04",
        titulo: "Controle de visibilidade dos profissionais",
        descricao:
            "É possível definir quais profissionais da equipe aparecem para cada papel — por exemplo, exibir somente os médicos na agenda sem mostrar toda a equipe administrativa.",
        tag: "novidade",
    },
]
