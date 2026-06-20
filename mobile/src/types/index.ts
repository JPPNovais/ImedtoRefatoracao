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
  | "CheckIn"
  | "EmAtendimento"
  | "Concluido"
  | "Cancelado"
  | "Faltou"
  | "Expirado"

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
  /** No mobile: sempre mascarado (obter() usa ?contato=mascarado). Completo só via obterDadosSensiveis(). */
  cpf?: string | null
  dataNascimento?: string | null
  genero?: string | null
  /** No mobile: sempre mascarado (obter() usa ?contato=mascarado). Completo só via obterDadosSensiveis(). */
  telefone?: string | null
  email?: string | null
  observacoes?: string | null
  tags: string[]
  /** Conteúdo dos alertas clínicos — só no detalhe, cujo acesso é auditado. */
  alertas: string[]
}

/**
 * Resultado de GET /api/paciente/{id}/dados-sensiveis — PII completa auditada (LGPD).
 * Só disponível após biometria confirmada e chamada explícita de obterDadosSensiveis().
 */
export interface DadosSensiveisPaciente {
  cpf?: string | null
  telefone?: string | null
}

/**
 * Payload mínimo para criar ou atualizar um paciente pelo app mobile.
 * Campos opcionais: só enviar o que o usuário preencheu (LGPD: minimização).
 * nomeCompleto é obrigatório — o backend retorna 422 se ausente.
 */
export interface PacientePayloadRapido {
  nomeCompleto: string
  telefone?: string
  email?: string
  dataNascimento?: string // ISO "YYYY-MM-DD"
  cpf?: string
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

/** Favorito real do backend — GET /api/receitas/favoritos (Item 13). */
export interface MedicamentoFavoritoBackend {
  id: number
  medicamento: string
  posologia?: string | null
  viaAdministracao?: string | null
  usoCount: number
  ultimoUso?: string | null
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

// ─── Dashboard ──────────────────────────────────────────────────────────────

export interface ProximoAgendamentoDto {
  id: number
  pacienteNome: string
  profissionalNome: string
  inicioPrevisto: string // ISO
  tipoServico: string
  status: string
}

export interface ItemAbaixoMinimoDto {
  id: number
  nome: string
  quantidadeAtual: number
  quantidadeMinima: number
  unidadeMedida: string
}

export interface DashboardDto {
  totalPacientesAtivos: number
  agendamentosHoje: number
  agendamentosSemana: number
  receitasMes: number
  despesasMes: number
  saldoMes: number
  itensAbaixoMinimo: number
  orcamentosPendentes: number
  lancamentosVencidos: number
  vencidosAReceber: number
  vencidosAPagar: number
  proximosAgendamentos: ProximoAgendamentoDto[]
  itensAbaixoMinimoLista: ItemAbaixoMinimoDto[]
}

// ─── Financeiro / Caixa ─────────────────────────────────────────────────────

export interface ResumoCaixaFormaPagamentoDto {
  formaPagamento: string
  total: number
}

export interface CaixaDiarioDto {
  id: number
  data: string
  status: string // "Aberto" | "Fechado"
  totalDia: number
  totalEstornos: number
  resumoPorForma: ResumoCaixaFormaPagamentoDto[]
}

/** Erro de negócio normalizado (422 BusinessException) ou de rede. */
export interface ApiError {
  status: number
  tipo?: string
  mensagem: string
}

// ─── Financeiro / Extrato ──────────────────────────────────────────────────
export interface LancamentoExtratoDto {
  id: number
  descricao: string
  pacienteNome?: string | null
  categoria: string
  formaPagamento?: string | null
  valor: number
  status: string
  dataPagamento?: string | null
  dataVencimento: string // "yyyy-MM-dd"
  tipo: string // "Receita" | "Despesa"
}

export interface PaginaLancamentosExtratoDto {
  itens: LancamentoExtratoDto[]
  total: number
  pagina: number
  tamanhoPagina: number
}

// ─── Financeiro / Formas de pagamento ─────────────────────────────────────
export interface FormaPagamentoDto {
  id: number
  nome: string
  ativa: boolean
  padrao: boolean
}

// ─── Cobranças ─────────────────────────────────────────────────────────────
export interface CobrancaDetalheDto {
  id: number
  agendamentoId?: number | null
  pacienteId: number
  pacienteNome: string
  valorCobrado: number
  desconto: number
  totalPago: number
  status: string // "Aberto" | "Pago" | "Parcial" | "Estornado"
}

export interface ValorSugeridoCheckInDto {
  valorSugerido?: number | null
  profissionalNome?: string | null
}

// ─── Agendamento (estendido) ────────────────────────────────────────────────
export interface AgendamentoDetalhe extends Agendamento {
  checkInEm?: string | null // ISO — presença confirmada
}

// ─── Inventário / Estoque ───────────────────────────────────────────────────
export interface ItemInventarioDto {
  id: number
  estabelecimentoId: number
  codigo: string
  nome: string
  categoria: string
  categoriaId?: number | null
  categoriaCor?: string | null
  categoriaIcone?: string | null
  fabricanteId?: number | null
  fabricanteNome?: string | null
  fornecedorPadraoId?: number | null
  fornecedorPadraoNome?: string | null
  localPadraoId?: number | null
  localPadraoNome?: string | null
  unidadeMedida: string
  quantidadeAtual: number
  quantidadeMinima: number
  custoMedio: number
  custoUnitario?: number | null
  estoqueAbaixoMinimo: boolean
  ativo: boolean
  criadoEm: string
  atualizadoEm?: string | null
}

export interface PaginaItensInventarioDto {
  itens: ItemInventarioDto[]
  total: number
  pagina: number
  tamanhoPagina: number
}

export interface RegistrarMovimentacaoDto {
  itemInventarioId: number
  tipo: string // "Entrada" | "Saída" | "Ajuste"
  quantidade: number
  custoUnitario: number
  observacao?: string | null
}

// ─── Automação ──────────────────────────────────────────────────────────────
export interface ConfiguracaoAutomacaoDto {
  lembretesHabilitados: boolean
  lembretesWhatsappHabilitados: boolean
  horasAntecedenciaLembrete: number
  expiracaoOrcamentosHabilitada: boolean
  emailRemetente?: string | null
}

// ─── Push — preferências por categoria (local ao device) ───────────────────
export interface PreferenciasPushDto {
  caixa: boolean
  estoque: boolean
  fotos: boolean
  pagamento: boolean
  automacao: boolean
  avisos: boolean
}

// ─── Prontuário / Anexos (fotos clínicas) ──────────────────────────────────

/** Resposta paginada de GET /api/paciente/{id}/prontuario/anexos (Item 19). */
export interface PaginaAnexosDto {
  itens: AnexoDto[]
  total: number
  pagina: number
  tamanhoPagina: number
}

export interface AnexoDto {
  id: number
  evolucaoId?: number | null
  nomeOriginal: string
  mimeType: string
  tamanhoBytes: number
  criadoEm: string // ISO
  autorNome: string
  /** Metadados de foto clínica — null para docs antigos. */
  regiaoAnatomica?: string | null
  marcador?: string | null
}

export interface AnexoUrlDto {
  id: number
  nomeOriginal: string
  mimeType: string
  url: string
  expiraEm: string // ISO
}

// ─── Catálogo clínico (global, referência) ──────────────────────────────────

export interface Cid10Dto {
  codigo: string
  descricao: string
  categoria?: string | null
}

export interface ExameCatalogoDto {
  id: number
  nome: string
  tipo: string
}
