/**
 * Catálogo de variáveis disponíveis no editor de modelos de termo.
 *
 * Fonte de verdade: `TermoResolverDeVariaveis.VariaveisDisponiveis` no backend.
 * Sincronizado manualmente — se a lista lá mudar, atualizar aqui.
 *
 * O backend resolve as variáveis server-side no momento da emissão; aqui é só
 * UX (inserção de chip no editor + preview com dados fake).
 */

export type GrupoVariavel = "paciente" | "estabelecimento" | "profissional" | "data"

export interface TermoVariavel {
    /** Chave canônica usada no HTML do modelo: `{{paciente.nome}}`. */
    chave: string
    /** Rótulo amigável exibido na sidebar. */
    rotulo: string
    grupo: GrupoVariavel
    /** Valor fake usado pelo preview (não é o que o backend resolve em produção). */
    exemplo: string
}

export const TERMO_VARIAVEIS: TermoVariavel[] = [
    // Paciente
    { chave: "{{paciente.nome}}",                  rotulo: "Nome do paciente",          grupo: "paciente",       exemplo: "João da Silva" },
    { chave: "{{paciente.cpf}}",                   rotulo: "CPF",                       grupo: "paciente",       exemplo: "123.456.789-00" },
    { chave: "{{paciente.documento_internacional}}", rotulo: "Documento internacional", grupo: "paciente",       exemplo: "AB123456" },
    { chave: "{{paciente.data_nascimento}}",       rotulo: "Data de nascimento",        grupo: "paciente",       exemplo: "15/03/1985" },
    { chave: "{{paciente.idade}}",                 rotulo: "Idade",                     grupo: "paciente",       exemplo: "40 anos" },
    { chave: "{{paciente.telefone}}",              rotulo: "Telefone",                  grupo: "paciente",       exemplo: "(11) 99999-8888" },
    { chave: "{{paciente.email}}",                 rotulo: "E-mail",                    grupo: "paciente",       exemplo: "joao@exemplo.com" },
    { chave: "{{paciente.endereco}}",              rotulo: "Endereço",                  grupo: "paciente",       exemplo: "Rua das Flores, 123 - Centro" },
    { chave: "{{paciente.genero}}",                rotulo: "Gênero",                    grupo: "paciente",       exemplo: "Masculino" },

    // Estabelecimento
    { chave: "{{estabelecimento.nome}}",           rotulo: "Nome fantasia",             grupo: "estabelecimento", exemplo: "Clínica Exemplo" },
    { chave: "{{estabelecimento.razao_social}}",   rotulo: "Razão social",              grupo: "estabelecimento", exemplo: "Clínica Exemplo Ltda" },
    { chave: "{{estabelecimento.cnpj}}",           rotulo: "CNPJ",                      grupo: "estabelecimento", exemplo: "12.345.678/0001-90" },
    { chave: "{{estabelecimento.endereco}}",       rotulo: "Endereço",                  grupo: "estabelecimento", exemplo: "Av. Paulista, 1000 - São Paulo/SP" },
    { chave: "{{estabelecimento.telefone}}",       rotulo: "Telefone",                  grupo: "estabelecimento", exemplo: "(11) 3333-4444" },

    // Profissional
    { chave: "{{profissional.nome}}",              rotulo: "Nome do emissor",           grupo: "profissional",   exemplo: "Dra. Maria Souza" },
    { chave: "{{profissional.conselho_completo}}", rotulo: "Conselho-UF Nº",            grupo: "profissional",   exemplo: "CRM-SP 123456" },
    { chave: "{{profissional.especialidade}}",     rotulo: "Especialidade",             grupo: "profissional",   exemplo: "Cardiologia" },

    // Data / Cidade
    { chave: "{{data_atual}}",                     rotulo: "Data atual por extenso",    grupo: "data",           exemplo: "19 de maio de 2026" },
    { chave: "{{data_atual_curta}}",               rotulo: "Data atual (curta)",        grupo: "data",           exemplo: "19/05/2026" },
    { chave: "{{cidade_atual}}",                   rotulo: "Cidade do estabelecimento", grupo: "data",           exemplo: "São Paulo" },
]

export const GRUPOS_VARIAVEL: { chave: GrupoVariavel; rotulo: string; icone: string }[] = [
    { chave: "paciente",        rotulo: "Paciente",        icone: "fa-solid fa-user-injured" },
    { chave: "estabelecimento", rotulo: "Estabelecimento", icone: "fa-solid fa-hospital" },
    { chave: "profissional",    rotulo: "Profissional",    icone: "fa-solid fa-user-doctor" },
    { chave: "data",            rotulo: "Data / Cidade",   icone: "fa-solid fa-calendar-day" },
]

export interface CategoriaTermoMeta {
    chave: "lgpd" | "cirurgico" | "imagem" | "financeiro" | "telemedicina" | "geral"
    label: string
    cor: "default" | "success" | "warning" | "error" | "info" | "muted"
}

/**
 * Mapeia categoria → label e cor de badge.
 * As cores são mapeadas para variantes do AppBadge (que renderiza CountBadge do design system).
 */
export const CATEGORIAS_TERMO: CategoriaTermoMeta[] = [
    { chave: "lgpd",         label: "LGPD",          cor: "info"    },
    { chave: "cirurgico",    label: "Cirúrgico",     cor: "error"   },
    { chave: "imagem",       label: "Uso de imagem", cor: "default" },
    { chave: "financeiro",   label: "Financeiro",    cor: "success" },
    { chave: "telemedicina", label: "Telemedicina",  cor: "warning" },
    { chave: "geral",        label: "Geral",         cor: "muted"   },
]

/**
 * Substitui as variáveis `{{...}}` no HTML pelo `exemplo` correspondente
 * — usado apenas no preview do editor. Em produção quem faz a substituição
 * é o backend (server-side, com dados reais minimizados).
 */
export function resolverVariaveisFake(html: string): string {
    if (!html) return ""
    return html.replace(/\{\{\s*([a-z_.]+)\s*\}\}/gi, (match, chave) => {
        const v = TERMO_VARIAVEIS.find(x => x.chave === `{{${chave}}}`)
        return v ? v.exemplo : match
    })
}
