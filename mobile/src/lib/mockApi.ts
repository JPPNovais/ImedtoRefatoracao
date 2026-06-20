/* ─────────────────────────────────────────────────────────────
   Mock API — APENAS dev/preview (ativado por VITE_USE_MOCKS=true).
   Permite navegar o app inteiro sem subir o backend. Os dados são
   os mesmos da prototipação. Em produção isto NUNCA roda: o app
   fala com a API real. É um atalho de desenvolvimento, não um
   espelho de regra de negócio (regra mora no backend).
   ───────────────────────────────────────────────────────────── */
import type {
  Agendamento,
  BootstrapMe,
  Estabelecimento,
  Notificacao,
  Orcamento,
  Paciente,
  PaginaPacientes,
  ProntuarioCompleto,
} from "@/types"

export const USE_MOCKS = import.meta.env.VITE_USE_MOCKS === "true"

const estabelecimentos: Estabelecimento[] = [
  {
    id: 1,
    nomeFantasia: "Clínica Vida",
    papelDoUsuario: "Dono",
    permissoes: [],
    permissoesExtras: [],
  },
  {
    id: 2,
    nomeFantasia: "Hospital Norte",
    papelDoUsuario: "Profissional",
    permissoes: ["agenda", "pacientes", "prontuario", "prescricao"],
    permissoesExtras: [],
  },
  {
    id: 3,
    nomeFantasia: "Consultório Aliança",
    papelDoUsuario: "Profissional",
    permissoes: ["agenda", "pacientes", "prontuario", "prescricao", "orcamento.ver"],
    permissoesExtras: [],
  },
]

const bootstrap: BootstrapMe = {
  usuario: {
    id: "u-marina",
    email: "marina.castro@imedto.com",
    nomeCompleto: "Dra. Marina Castro",
    telefone: null,
    status: "Ativo",
    onboardingCompleto: true,
    ultimoEstabelecimentoId: 1,
  },
  profissional: {
    usuarioId: "u-marina",
    conselho: "CRM",
    uf: "SP",
    numeroRegistro: "123456",
    especialidade: "Clínica geral",
  },
  estabelecimentos,
}

interface MockAppt extends Agendamento {
  age: number
}
const today = "2026-06-05"
const appts: MockAppt[] = [
  { id: 1, pacienteId: 1, pacienteNome: "Maria Silva", profissionalUsuarioId: "u-marina", inicioPrevisto: `${today}T09:30:00`, fimPrevisto: `${today}T10:00:00`, tipoServico: "Retorno", observacoes: "Paciente pós-operatório. Avaliar cicatrização e queixa de dor leve.", status: "Agendado", salaNome: "Sala 2", temAlertaClinico: true, age: 34 },
  { id: 2, pacienteId: 2, pacienteNome: "João Souza", profissionalUsuarioId: "u-marina", inicioPrevisto: `${today}T10:00:00`, fimPrevisto: `${today}T10:30:00`, tipoServico: "Consulta", observacoes: "Acompanhamento de hipertensão arterial.", status: "Confirmado", salaNome: "Sala 1", temAlertaClinico: false, age: 51 },
  { id: 3, pacienteId: 3, pacienteNome: "Ana Lima", profissionalUsuarioId: "u-marina", inicioPrevisto: `${today}T10:30:00`, fimPrevisto: `${today}T11:00:00`, tipoServico: "Avaliação", status: "Agendado", salaNome: "Sala 2", temAlertaClinico: false, age: 29 },
  { id: 4, pacienteId: 5, pacienteNome: "Carlos Mendes", profissionalUsuarioId: "u-marina", inicioPrevisto: `${today}T11:15:00`, fimPrevisto: `${today}T11:45:00`, tipoServico: "Retorno", status: "Agendado", salaNome: "Sala 1", temAlertaClinico: false, age: 45 },
  { id: 5, pacienteId: 4, pacienteNome: "Beatriz Rocha", profissionalUsuarioId: "u-marina", inicioPrevisto: `${today}T14:00:00`, fimPrevisto: `${today}T14:30:00`, tipoServico: "Consulta", status: "Agendado", salaNome: "Sala 3", temAlertaClinico: true, age: 38 },
]

const pacientes: Record<number, Paciente> = {
  1: { id: 1, nomeCompleto: "Maria Silva", cpf: "312.456.789-00", dataNascimento: "1992-03-12", genero: "F", telefone: "(11) 99812-3477", email: null, observacoes: null, tags: [], alertas: ["Alergia grave a penicilina"] },
  2: { id: 2, nomeCompleto: "João Souza", cpf: "487.221.220-13", dataNascimento: "1975-01-08", genero: "M", telefone: "(11) 98461-2290", email: null, observacoes: null, tags: [], alertas: [] },
  3: { id: 3, nomeCompleto: "Ana Lima", cpf: "201.330.115-44", dataNascimento: "1997-07-21", genero: "F", telefone: "(11) 99770-1180", email: null, observacoes: null, tags: [], alertas: [] },
  4: { id: 4, nomeCompleto: "Beatriz Rocha", cpf: "350.118.992-05", dataNascimento: "1988-11-02", genero: "F", telefone: "(11) 99123-4567", email: null, observacoes: null, tags: [], alertas: ["Anticoagulante de uso contínuo"] },
  5: { id: 5, nomeCompleto: "Carlos Mendes", cpf: "129.884.330-77", dataNascimento: "1981-05-19", genero: "M", telefone: "(11) 98800-2211", email: null, observacoes: null, tags: [], alertas: [] },
}

const pacienteLista: PaginaPacientes = {
  total: 5,
  pagina: 1,
  tamanhoPagina: 20,
  itens: [
    { id: 1, nomeCompleto: "Maria Silva", dataNascimento: "1992-03-12", genero: "F", ultimaVisita: "2026-06-02", qtdAlertas: 1 },
    { id: 2, nomeCompleto: "João Souza", dataNascimento: "1975-01-08", genero: "M", ultimaVisita: "2026-05-28", qtdAlertas: 0 },
    { id: 3, nomeCompleto: "Ana Lima", dataNascimento: "1997-07-21", genero: "F", ultimaVisita: "2026-05-30", qtdAlertas: 0 },
    { id: 4, nomeCompleto: "Beatriz Rocha", dataNascimento: "1988-11-02", genero: "F", ultimaVisita: "2026-05-20", qtdAlertas: 1 },
    { id: 5, nomeCompleto: "Carlos Mendes", dataNascimento: "1981-05-19", genero: "M", ultimaVisita: "2026-05-12", qtdAlertas: 0 },
  ],
}

const prontuarios: Record<number, ProntuarioCompleto> = {
  1: {
    prontuario: { id: 11, pacienteId: 1, modeloNome: "Retorno" },
    evolucoes: [
      { id: 101, prontuarioId: 11, autorNome: "Dr. Você · Clínica Vida", modeloNome: "Retorno pós-operatório", conteudo: { resumo: "Cicatrização adequada, sem sinais flogísticos. Mantida orientação de repouso e retorno em 14 dias." }, criadaEm: "2026-06-02T10:00:00", qtdAnexos: 2 },
      { id: 102, prontuarioId: 11, autorNome: "Dr. Você · Clínica Vida", modeloNome: "Consulta inicial", conteudo: { resumo: "Paciente encaminhada para procedimento. Solicitados exames pré-operatórios." }, criadaEm: "2026-05-18T09:00:00" },
    ],
  },
  2: {
    prontuario: { id: 12, pacienteId: 2, modeloNome: "Consulta" },
    evolucoes: [
      { id: 121, prontuarioId: 12, autorNome: "Dr. Você · Clínica Vida", modeloNome: "Acompanhamento de hipertensão", conteudo: { resumo: "PA 130/85 mmHg. Ajuste de dose do anti-hipertensivo. Controle em 30 dias." }, criadaEm: "2026-05-28T11:00:00" },
    ],
  },
}

const notificacoes: Notificacao[] = [
  { id: 1, titulo: "Novo agendamento", mensagem: "Maria Silva · 10:30 hoje", categoria: "NovoAgendamento", linkAcao: "/agenda", lida: false, criadaEm: new Date(Date.now() - 5 * 60000).toISOString() },
  { id: 2, titulo: "Consulta cancelada", mensagem: "João Souza · 14:00", categoria: "Cancelamento", linkAcao: "/agenda", lida: false, criadaEm: new Date(Date.now() - 3600000).toISOString() },
  { id: 3, titulo: "Receita assinada", mensagem: "Pronta para envio ao paciente", categoria: "Receita", linkAcao: null, lida: false, criadaEm: new Date(Date.now() - 2 * 3600000).toISOString() },
  { id: 4, titulo: "Lembrete de agenda", mensagem: "3 consultas amanhã", categoria: "Lembrete", linkAcao: "/agenda", lida: true, criadaEm: new Date(Date.now() - 26 * 3600000).toISOString() },
  { id: 5, titulo: "Presença confirmada", mensagem: "Ana Lima · 30/05", categoria: "Confirmacao", linkAcao: "/agenda", lida: true, criadaEm: new Date(Date.now() - 28 * 3600000).toISOString() },
  { id: 6, titulo: "Vínculo aceito", mensagem: "Hospital Norte · Médico", categoria: "Vinculo", linkAcao: "/mais", lida: true, criadaEm: new Date(Date.now() - 8 * 86400000).toISOString() },
]

const orcamentos: Record<number, Orcamento> = {
  1042: {
    id: 1042,
    numero: "#1042",
    pacienteId: 2,
    pacienteNome: "João Souza",
    titulo: "Implante unitário + coroa",
    status: "Aguardando aprovação",
    total: 3400,
    itens: [
      { descricao: "Implante de titânio", valor: 1800 },
      { descricao: "Coroa de porcelana", valor: 1200 },
      { descricao: "Cirurgia e anestesia", valor: 400 },
    ],
  },
}

function delay<T>(data: T, ms = 280): Promise<T> {
  return new Promise((r) => setTimeout(() => r(data), ms))
}

/** Roteia método+path para um dado mock. Retorna {status,data} igual ao http real. */
export async function mockRoute(
  method: string,
  path: string,
  params?: Record<string, unknown>,
): Promise<{ status: number; data: unknown } | null> {
  const p = path.split("?")[0]

  if (p === "/auth/bootstrap") return { status: 200, data: await delay(bootstrap) }
  if (p === "/auth/login" || p === "/auth/logout" || p === "/auth/refresh") return { status: 200, data: {} }
  if (p === "/estabelecimento") return { status: 200, data: await delay(estabelecimentos) }

  if (p === "/agendamentos" && method === "GET") {
    const itens = appts.map((a) => ({ ...a }))
    return { status: 200, data: await delay({ itens, total: itens.length, pagina: 1, tamanhoPagina: 20 }) }
  }
  if (p === "/agendamentos/contagem-por-dia") {
    return { status: 200, data: await delay([{ data: today, agendados: 12, atendidos: 4, faltas: 0 }]) }
  }
  const agId = p.match(/^\/agendamentos\/(\d+)$/)
  if (agId && method === "GET") {
    const a = appts.find((x) => x.id === Number(agId[1]))
    return { status: a ? 200 : 404, data: a ? await delay(a) : null }
  }
  if (/^\/agendamentos\/\d+\/(confirmar|concluir|cancelar|checkin)$/.test(p)) return { status: 204, data: null }
  if (p === "/agendamentos" && method === "POST") return { status: 201, data: { agendamentoId: 999 } }
  if (p === "/agendamentos/disponibilidade" && method === "GET") {
    const dataInicio = String(params?.dataInicio || today)
    const dataFim = String(params?.dataFim || today)
    // Gera dias do intervalo solicitado
    const dias: unknown[] = []
    const cur = new Date(dataInicio + "T12:00:00Z")
    const fim = new Date(dataFim + "T12:00:00Z")
    const DIAS_SEMANA = ["DOM", "SEG", "TER", "QUA", "QUI", "SEX", "SAB"]
    // Slots ocupados por agendamentos existentes (só para a dataInicio = today do mock)
    const ocupados = new Set(appts.map((a) => a.inicioPrevisto.substring(11, 16)))
    while (cur <= fim) {
      const iso = cur.toISOString().substring(0, 10)
      const dow = cur.getUTCDay() // 0=DOM, 6=SAB
      const diaSemana = DIAS_SEMANA[dow]
      if (dow === 0 || dow === 6) {
        // Fim de semana: fechado
        dias.push({ data: iso, diaSemana, status: "fechado", slots: [] })
      } else {
        const slots: unknown[] = []
        // Horários de atendimento: 08:00–12:00 e 14:00–17:00, de 30 em 30 min
        const ranges = [
          [8, 0], [8, 30], [9, 0], [9, 30], [10, 0], [10, 30], [11, 0], [11, 30],
          [14, 0], [14, 30], [15, 0], [15, 30], [16, 0], [16, 30],
        ]
        for (const [h, m] of ranges) {
          const hora = `${String(h).padStart(2, "0")}:${String(m).padStart(2, "0")}`
          const esteOcupado = iso === today && ocupados.has(hora)
          // Simula 11:15 como bloqueado para demonstrar o motivo "bloqueado"
          const bloqueado = hora === "11:30"
          slots.push({
            hora,
            disponivel: !esteOcupado && !bloqueado,
            motivo: esteOcupado ? "agendado" : bloqueado ? "bloqueado" : null,
            pacienteNome: esteOcupado ? (appts.find((a) => a.inicioPrevisto.substring(11, 16) === hora)?.pacienteNome ?? null) : null,
          })
        }
        dias.push({ data: iso, diaSemana, status: "disponivel", slots })
      }
      cur.setUTCDate(cur.getUTCDate() + 1)
    }
    return { status: 200, data: await delay({ profissionalUsuarioId: String(params?.profissionalUsuarioId || "u-marina"), dias }) }
  }

  if (p === "/paciente" && method === "GET") {
    const busca = String(params?.busca || "").toLowerCase()
    const itens = busca
      ? pacienteLista.itens.filter((x) => x.nomeCompleto.toLowerCase().includes(busca))
      : pacienteLista.itens
    return { status: 200, data: await delay({ ...pacienteLista, itens, total: itens.length }) }
  }
  if (p === "/paciente" && method === "POST") {
    // Mock: registra o novo paciente na lista em memória para busca-rápida retornar
    const body = params as Record<string, unknown>
    const novoId = 900 + pacienteLista.itens.length
    const nomeCompleto = String(body?.nomeCompleto || "Novo Paciente")
    pacienteLista.itens.push({ id: novoId, nomeCompleto, qtdAlertas: 0 })
    pacientes[novoId] = { id: novoId, nomeCompleto, tags: [], alertas: [] }
    return { status: 201, data: null }
  }
  if (p === "/paciente/busca-rapida") {
    const q = String(params?.q || "").toLowerCase()
    const todos = pacienteLista.itens.map((x) => ({ id: x.id, nomeCompleto: x.nomeCompleto }))
    const itens = q ? todos.filter((x) => x.nomeCompleto.toLowerCase().includes(q)) : todos
    const limite = Number(params?.limite || 10)
    return { status: 200, data: await delay(itens.slice(0, limite)) }
  }
  const pacId = p.match(/^\/paciente\/(\d+)$/)
  if (pacId && method === "GET") {
    const pac = pacientes[Number(pacId[1])]
    return { status: pac ? 200 : 404, data: pac ? await delay(pac) : null }
  }
  const pacBasicos = p.match(/^\/paciente\/(\d+)\/dados-basicos$/)
  if (pacBasicos && method === "PATCH") {
    // Mock: atualização parcial — só altera o nome se enviado (demais campos preservados)
    const id = Number(pacBasicos[1])
    const body = params as Record<string, unknown>
    if (pacientes[id] && body?.nomeCompleto) {
      const nome = String(body.nomeCompleto)
      pacientes[id] = { ...pacientes[id], nomeCompleto: nome }
      const item = pacienteLista.itens.find((x) => x.id === id)
      if (item) item.nomeCompleto = nome
    }
    return { status: 204, data: null }
  }
  if (pacId && method === "PUT") {
    // Mock: atualiza nome se enviado
    const id = Number(pacId[1])
    const body = params as Record<string, unknown>
    if (pacientes[id] && body?.nomeCompleto) {
      const nome = String(body.nomeCompleto)
      pacientes[id] = { ...pacientes[id], nomeCompleto: nome }
      const item = pacienteLista.itens.find((x) => x.id === id)
      if (item) item.nomeCompleto = nome
    }
    return { status: 204, data: null }
  }
  const prontId = p.match(/^\/paciente\/(\d+)\/prontuario$/)
  if (prontId && method === "GET") {
    const pr = prontuarios[Number(prontId[1])] || { prontuario: null, evolucoes: [] }
    return { status: 200, data: await delay(pr) }
  }
  if (/^\/paciente\/\d+\/prontuario\/evolucoes$/.test(p) && method === "POST") {
    return { status: 201, data: { evolucaoId: Math.floor(Math.random() * 1e6) } }
  }

  if (p === "/notificacoes") return { status: 200, data: await delay({ itens: notificacoes, total: notificacoes.length, pagina: 1, tamanho: 50 }) }
  if (p === "/notificacoes/contador-nao-lidas") return { status: 200, data: { total: notificacoes.filter((n) => !n.lida).length } }
  if (/^\/notificacoes\/(\d+\/marcar-lida|marcar-todas-lidas)$/.test(p)) return { status: 204, data: null }

  if (p === "/receitas" && method === "POST") return { status: 201, data: { receitaId: 555 } }
  if (/^\/receitas\/\d+\/assinar$/.test(p)) return { status: 202, data: { status: "AssinaturaPendente" } }
  if (/^\/receitas\/\d+\/status-assinatura$/.test(p)) return { status: 200, data: { status: "AssinadaIcp", pdfAssinadoUrl: "#" } }
  if (/^\/pacientes\/\d+\/atestados$/.test(p) && method === "POST") return { status: 201, data: { atestadoId: 777 } }
  if (/^\/pacientes\/\d+\/pedidos-exame$/.test(p) && method === "POST") return { status: 201, data: { pedidoExameId: 888 } }

  if (p === "/catalogo/cid" && method === "GET") {
    const busca = String(params?.busca || "").toLowerCase()
    const todos = [
      { codigo: "J06.9", descricao: "Infecção aguda das vias aéreas superiores", categoria: "J" },
      { codigo: "M54.5", descricao: "Dor lombar baixa", categoria: "M" },
      { codigo: "A09", descricao: "Diarreia e gastroenterite de origem infecciosa", categoria: "A" },
      { codigo: "R51", descricao: "Cefaleia", categoria: "R" },
      { codigo: "J11", descricao: "Influenza (gripe)", categoria: "J" },
      { codigo: "K29.7", descricao: "Gastrite não especificada", categoria: "K" },
      { codigo: "I10", descricao: "Hipertensão essencial (primária)", categoria: "I" },
      { codigo: "E11", descricao: "Diabetes mellitus tipo 2", categoria: "E" },
      { codigo: "F32", descricao: "Episódio depressivo", categoria: "F" },
      { codigo: "J45", descricao: "Asma", categoria: "J" },
    ]
    const itens = busca ? todos.filter((c) => (c.codigo + " " + c.descricao).toLowerCase().includes(busca)) : todos
    const limite = Number(params?.limite || 20)
    return { status: 200, data: await delay(itens.slice(0, limite)) }
  }
  if (p === "/catalogo/exames" && method === "GET") {
    const busca = String(params?.busca || "").toLowerCase()
    const todos = [
      { id: 1, nome: "Hemograma completo", tipo: "Laboratorial" },
      { id: 2, nome: "Glicemia de jejum", tipo: "Laboratorial" },
      { id: 3, nome: "Colesterol total e frações", tipo: "Laboratorial" },
      { id: 4, nome: "TSH", tipo: "Laboratorial" },
      { id: 5, nome: "Ureia e creatinina", tipo: "Laboratorial" },
      { id: 6, nome: "Urina tipo 1", tipo: "Laboratorial" },
      { id: 7, nome: "Raio-X de tórax", tipo: "Imagem" },
      { id: 8, nome: "Ultrassom abdominal", tipo: "Imagem" },
      { id: 9, nome: "Eletrocardiograma", tipo: "Cardiológico" },
      { id: 10, nome: "Vitamina D", tipo: "Laboratorial" },
      { id: 11, nome: "Ferritina", tipo: "Laboratorial" },
      { id: 12, nome: "PCR (Proteína C-reativa)", tipo: "Laboratorial" },
      { id: 13, nome: "Triglicerídeos", tipo: "Laboratorial" },
      { id: 14, nome: "Ressonância magnética", tipo: "Imagem" },
      { id: 15, nome: "Tomografia computadorizada", tipo: "Imagem" },
    ]
    const itens = busca ? todos.filter((e) => e.nome.toLowerCase().includes(busca)) : todos
    const limite = Number(params?.limite || 30)
    return { status: 200, data: await delay(itens.slice(0, limite)) }
  }

  if (p === "/orcamentos" && method === "GET") return { status: 200, data: await delay(Object.values(orcamentos)) }
  const orcId = p.match(/^\/orcamentos\/(\d+)$/)
  if (orcId && method === "GET") {
    const o = orcamentos[Number(orcId[1])]
    return { status: o ? 200 : 404, data: o ? await delay(o) : null }
  }
  if (/^\/orcamentos\/\d+\/(aprovar|recusar)$/.test(p)) return { status: 204, data: null }

  return null
}
