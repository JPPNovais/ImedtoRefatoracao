/* ─────────────────────────────────────────────────────────────
   Tipos de domínio — espelham os DTOs do backend Imedto (.NET CQRS).
   Mantidos minimalistas (LGPD: só o que as telas do app consomem).
   ───────────────────────────────────────────────────────────── */

export type Papel = "Dono" | "Profissional" | "Recepcionista"

export interface Usuario {
  id: string
  email: string
  nomeCompleto: string | null
  telefone: string | null
  status: "Pendente" | "Ativo" | "Inativo"
  onboardingCompleto: boolean
  ultimoEstabelecimentoId?: number | null
}

export interface ProfissionalPerfil {
  usuarioId: string
  conselho?: string | null
  uf?: string | null
  numeroRegistro?: string | null
  especialidade?: string | null
  fotoUrl?: string | null
}

/** EstabelecimentoDto — inclui papel + permissões do vínculo (multi-tenant). */
export interface Estabelecimento {
  id: number
  nomeFantasia: string
  fotoUrl?: string | null
  papelDoUsuario: Papel
  permissoes: string[] // "area.acao"
  permissoesExtras: string[]
}

export interface BootstrapMe {
  usuario: Usuario
  profissional?: ProfissionalPerfil | null
  estabelecimentos: Estabelecimento[]
}

export type StatusAgendamento =
  | "Agendado"
  | "Confirmado"
  | "EmAtendimento"
  | "Concluido"
  | "Cancelado"
  | "Faltou"

export interface Agendamento {
  id: number
  pacienteId: number
  pacienteNome: string
  profissionalUsuarioId: string
  profissionalNome?: string
  inicioPrevisto: string // ISO
  fimPrevisto: string // ISO
  tipoServico: string
  observacoes?: string | null
  status: StatusAgendamento
  salaId?: number | null
  salaNome?: string | null
  /** Marcador de alerta clínico — só a flag/contagem chega à listagem (LGPD). */
  temAlertaClinico?: boolean
}

export interface PaginaAgendamentos {
  itens: Agendamento[]
  total: number
  pagina: number
  tamanhoPagina: number
}

export interface ContagemPorDia {
  data: string // yyyy-MM-dd
  agendados: number
  atendidos: number
  faltas: number
}

export interface PacienteListaItem {
  id: number
  nomeCompleto: string
  dataNascimento?: string | null
  genero?: string | null
  ultimaVisita?: string | null
  /** LGPD: só a contagem de alertas vai pra lista, nunca o texto. */
  qtdAlertas: number
}

export interface PaginaPacientes {
  itens: PacienteListaItem[]
  total: number
  pagina: number
  tamanhoPagina: number
}

export interface Paciente {
  id: number
  nomeCompleto: string
  cpf?: string | null
  dataNascimento?: string | null
  genero?: string | null
  telefone?: string | null
  email?: string | null
  observacoes?: string | null
  tags: string[]
  /** Conteúdo dos alertas clínicos — só no detalhe, cujo acesso é auditado. */
  alertas: string[]
}

export interface Evolucao {
  id: number
  prontuarioId: number
  autorNome: string
  modeloNome?: string
  /** conteúdo estruturado (JSON do modelo) */
  conteudo: Record<string, unknown>
  criadaEm: string
  qtdAnexos?: number
}

export interface Prontuario {
  id: number
  pacienteId: number
  modeloDeProntuarioId?: number
  modeloNome?: string
}

export interface ProntuarioCompleto {
  prontuario: Prontuario | null
  evolucoes: Evolucao[]
}

export type CategoriaNotificacao =
  | "NovoAgendamento"
  | "Cancelamento"
  | "Lembrete"
  | "Receita"
  | "Confirmacao"
  | "Vinculo"

export interface Notificacao {
  id: number
  estabelecimentoId?: number | null
  titulo: string
  mensagem: string
  categoria: CategoriaNotificacao | string
  linkAcao?: string | null
  lida: boolean
  criadaEm: string
  lidaEm?: string | null
}

export interface PaginaNotificacoes {
  itens: Notificacao[]
  total: number
  pagina: number
  tamanho: number
}

export type TipoReceita = "Simples" | "Controlada" | "Antimicrobiano"

export interface ItemReceita {
  medicamento: string
  posologia: string
  quantidade?: string | null
  via?: string | null
}

export interface MedicamentoFavorito {
  id?: number
  medicamento: string
  posologia: string
}

export interface OrcamentoLinha {
  descricao: string
  valor: number
}

export interface Orcamento {
  id: number
  numero: string
  pacienteId: number
  pacienteNome: string
  titulo?: string | null
  status: string
  total: number
  itens: OrcamentoLinha[]
}

/** Erro de negócio normalizado (422 BusinessException) ou de rede. */
export interface ApiError {
  status: number
  tipo?: string
  mensagem: string
}
