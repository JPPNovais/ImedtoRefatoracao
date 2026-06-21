<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue"
import { useRoute, useRouter } from "vue-router"
import { pacienteService } from "@/services/paciente.service"
import { prontuarioService } from "@/services/prontuario.service"
import { orcamentoService } from "@/services/orcamento.service"
import { receitaService, atestadoService, exameService } from "@/services/documentos.service"
import type { ReceitaResumoDto, AtestadoDto, PedidoExameDto } from "@/services/documentos.service"
import type { DadosSensiveisPaciente, Evolucao, Orcamento, Paciente, AnexoDto } from "@/types"
import { useUiStore } from "@/stores/ui"
import { usePermissoesStore } from "@/stores/permissoes"
import { useBiometric } from "@/native/useBiometric"
import { useDownload } from "@/native/useDownload"
import { iniciais, idade, dataCurta, renderConteudoEvolucao, generoLabel } from "@/lib/format"
import BottomSheet from "@/components/ui/BottomSheet.vue"
import AppEmptyState from "@/components/ui/AppEmptyState.vue"
import PacienteSeletorSheet from "@/components/ui/PacienteSeletorSheet.vue"

const menuFichaOpen = ref(false)

const route = useRoute()
const router = useRouter()
const ui = useUiStore()
const permissoes = usePermissoesStore()
const biometric = useBiometric()
const download = useDownload()

// RBAC: ações da ficha respeitam o vínculo (G2).
const podeProntuario = computed(() => permissoes.pode("prontuario.ver"))
const podePrescrever = computed(() => permissoes.pode("prescricao"))
const podeOrcamento = computed(() => permissoes.pode("orcamento.ver"))
// Fotos clínicas: restrito a Profissional/Dono via prontuario.ver (mesmo guarda do backend)
const podeFotos = computed(() => permissoes.pode("prontuario.ver"))
// Editar dados do paciente: mesmo guarda do backend (RequiresPapel Profissional/Dono = "pacientes")
const podeEditarPaciente = computed(() => permissoes.pode("pacientes"))

// Sheet de edição de dados do paciente (FIX 3)
const sheetEditarPaciente = ref(false)
const pacienteParaEditar = computed(() =>
  paciente.value ? { id: paciente.value.id, nomeCompleto: paciente.value.nomeCompleto } : null,
)

const id = Number(route.params.id)
const paciente = ref<Paciente | null>(null)
const evolucoes = ref<Evolucao[]>([])
const orcamentos = ref<Orcamento[]>([])
const carregando = ref(true)
const tab = ref<"hist" | "pront" | "docs" | "orc" | "fotos">("hist")
// piiRevelado: controla exibição após chamada auditada ao backend (nunca client-side)
const piiRevelado = ref(false)
const dadosSensiveis = ref<DadosSensiveisPaciente | null>(null)

// Detalhe de evolução (item 1 — aba Histórico)
const evolucaoDetalhe = ref<Evolucao | null>(null)
const detalheAnexos = ref<AnexoDto[]>([])
const detalheCarregandoAnexos = ref(false)
const detalheOpen = ref(false)

// Aba Documentos (item 5)
const docsCarregado = ref(false)
const docsCarregando = ref(false)
const receitas = ref<ReceitaResumoDto[]>([])
const atestados = ref<AtestadoDto[]>([])
const pedidosExame = ref<PedidoExameDto[]>([])
const docDetalhe = ref<AtestadoDto | PedidoExameDto | null>(null)
const docDetalheOpen = ref(false)
const docTipo = ref<"atestado" | "exame">("atestado")
const baixandoPdf = ref<number | null>(null)

const temAlerta = computed(() => (paciente.value?.alertas.length ?? 0) > 0)

/** Formata telefone só-dígitos para exibição: "31987654321" -> "(31) 98765-4321". */
function formatarTelefone(valor: string): string {
  const d = valor.replace(/\D/g, "")
  if (d.length === 11) return `(${d.slice(0, 2)}) ${d.slice(2, 7)}-${d.slice(7)}`
  if (d.length === 10) return `(${d.slice(0, 2)}) ${d.slice(2, 6)}-${d.slice(6)}`
  return valor
}

/** Formata CPF só-dígitos para exibição: "31245678900" -> "312.456.789-00". */
function formatarCpf(valor: string): string {
  const d = valor.replace(/\D/g, "")
  if (d.length === 11) return `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6, 9)}-${d.slice(9)}`
  return valor
}

/**
 * Exibe telefone: após reveal usa o valor completo do backend (dadosSensiveis),
 * formatado para leitura (o backend devolve só dígitos).
 * Antes do reveal: usa o valor já mascarado que vem do backend (?contato=mascarado).
 * Trata string vazia/whitespace como ausente (backend pode retornar "" para não cadastrado).
 * Após reveal, campo genuinamente ausente mostra "Não informado" (não os pontinhos).
 */
function exibirTelefone(): string {
  if (piiRevelado.value && dadosSensiveis.value) {
    const v = dadosSensiveis.value.telefone?.trim()
    return v ? formatarTelefone(v) : "Não informado"
  }
  const mascarado = paciente.value?.telefone?.trim()
  return mascarado || "(••) •••••-••••"
}

/** Exibe CPF: lógica análoga a exibirTelefone(). */
function exibirCpf(): string {
  if (piiRevelado.value && dadosSensiveis.value) {
    const v = dadosSensiveis.value.cpf?.trim()
    return v ? formatarCpf(v) : "Não informado"
  }
  const mascarado = paciente.value?.cpf?.trim()
  return mascarado || "•••.•••.•••-••"
}

onMounted(async () => {
  // Abrir a ficha dispara o log de acesso no backend (PacienteAcessoLog).
  try {
    paciente.value = await pacienteService.obter(id)
    const pront = await prontuarioService.obter(id).catch(() => null)
    evolucoes.value = pront?.evolucoes ?? []
    orcamentos.value = await orcamentoService.listarPorPaciente(id).catch(() => [])
  } catch {
    ui.toast("Não foi possível abrir a ficha", "error")
    router.back()
  } finally {
    carregando.value = false
  }
})

// Carrega documentos de forma lazy ao entrar na aba (item 5)
watch(tab, (nova) => {
  if (nova === "docs" && !docsCarregado.value) carregarDocs()
})

async function carregarDocs() {
  docsCarregando.value = true
  try {
    const [r, a, e] = await Promise.all([
      receitaService.listarReceitas(id).catch(() => ({ itens: [] as ReceitaResumoDto[] })),
      atestadoService.listarAtestados(id).catch(() => ({ itens: [] as AtestadoDto[] })),
      exameService.listarPedidosExame(id).catch(() => ({ itens: [] as PedidoExameDto[] })),
    ])
    receitas.value = r.itens
    atestados.value = a.itens
    pedidosExame.value = e.itens
    docsCarregado.value = true
  } catch {
    ui.toast("Erro ao carregar documentos", "error")
  } finally {
    docsCarregando.value = false
  }
}

const totalDocs = computed(() => receitas.value.length + atestados.value.length + pedidosExame.value.length)

async function revelarPii() {
  if (piiRevelado.value) return
  const ok = await biometric.confirmar("Revelar dados sensíveis do paciente")
  if (!ok) return
  try {
    // Chamada auditada: backend registra RevelacaoDadosSensiveis e retorna PII completa
    dadosSensiveis.value = await pacienteService.obterDadosSensiveis(id)
    piiRevelado.value = true
    ui.toast("Dados revelados — acesso registrado")
  } catch {
    ui.toast("Não foi possível revelar os dados", "error")
  }
}

function abrirOrcamento(o: Orcamento) {
  router.push(`/orcamento/${o.id}`)
}
function acao(tipo: "evolucao" | "receita" | "atestado" | "exame") {
  if (!paciente.value) return
  if (tipo === "evolucao") router.push({ path: `/paciente/${id}/prontuario`, query: { nova: "1" } })
  else router.push({ path: `/${tipo}`, query: { pacienteId: id } })
}

// ── Detalhe de evolução (item 1 — aba Histórico) ────────────────────────────

async function abrirDetalhe(e: Evolucao) {
  evolucaoDetalhe.value = e
  detalheOpen.value = true
  detalheAnexos.value = []
  if (e.qtdAnexos) {
    detalheCarregandoAnexos.value = true
    try {
      // listarAnexos retorna paginado — exibe a 1ª página (suficiente para o detalhe)
      const pagina = await prontuarioService.listarAnexos(id, { evolucaoId: e.id })
      detalheAnexos.value = pagina.itens
    } catch {
      // silencioso
    } finally {
      detalheCarregandoAnexos.value = false
    }
  }
}

async function abrirAnexo(anexoId: number) {
  try {
    const urlDto = await prontuarioService.obterUrlAnexo(id, anexoId)
    window.open(urlDto.url, "_blank", "noopener,noreferrer")
  } catch {
    ui.toast("Não foi possível abrir o anexo", "error")
  }
}

function renderConteudo(e: Evolucao): Array<{ chave: string; valor: string }> {
  return renderConteudoEvolucao(e.conteudo as Record<string, unknown>)
}

// ── Documentos — receita PDF (item 5) ───────────────────────────────────────

async function baixarReceitaPdf(receitaId: number) {
  baixandoPdf.value = receitaId
  try {
    await download.baixarPdf(`/receitas/${receitaId}/pdf`, `receita-${receitaId}.pdf`)
  } catch {
    ui.toast("Não foi possível baixar o PDF", "error")
  } finally {
    baixandoPdf.value = null
  }
}

function abrirDocDetalhe(doc: AtestadoDto | PedidoExameDto, tipo: "atestado" | "exame") {
  docDetalhe.value = doc
  docTipo.value = tipo
  docDetalheOpen.value = true
}

function etiquetaTipoReceita(tipo: string) {
  const m: Record<string, string> = { Simples: "Simples", Controlada: "Controlada", Antimicrobiano: "Antimicrobiano" }
  return m[tipo] ?? tipo
}

const baixandoProntuario = ref(false)
async function exportarProntuarioPdf() {
  menuFichaOpen.value = false
  baixandoProntuario.value = true
  try {
    await download.baixarPdf(`/paciente/${id}/prontuario/pdf`, `prontuario-${id}.pdf`)
  } catch {
    ui.toast("Não foi possível exportar o prontuário", "error")
  } finally {
    baixandoProntuario.value = false
  }
}

// ── Editar dados do paciente (FIX 3) ────────────────────────────────────────

function abrirEdicaoPaciente() {
  menuFichaOpen.value = false
  sheetEditarPaciente.value = true
}

async function aoAtualizarPaciente(p: { id: number; nomeCompleto: string }) {
  // Recarrega a ficha para refletir os novos dados (nome pode ter mudado)
  try {
    paciente.value = await pacienteService.obter(p.id)
  } catch {
    // toast já foi exibido pelo PacienteSeletorSheet; falha silenciosa aqui
  }
}
</script>

<template>
  <div class="push show">
    <div class="push-head">
      <button class="iconbtn" @click="router.back()"><i class="fa-solid fa-arrow-left"></i></button>
      <div class="ph-title">{{ paciente?.nomeCompleto || "Ficha" }}</div>
      <button v-if="podeProntuario" class="iconbtn" aria-label="Mais ações" @click="menuFichaOpen = true"><i class="fa-solid fa-ellipsis"></i></button>
      <span v-else style="width: 40px"></span>
    </div>

    <div v-if="paciente" class="push-body">
      <div class="ficha-hero">
        <div class="av-xl">{{ iniciais(paciente.nomeCompleto) }}</div>
        <div class="fn">{{ paciente.nomeCompleto }}</div>
        <div class="fmeta">
          {{ idade(paciente.dataNascimento) ?? "—" }} anos · {{ generoLabel(paciente.genero) }}
        </div>
        <div class="pii-row">
          <button class="pii" :class="{ revealed: piiRevelado }" @click="revelarPii">
            <i class="fa-solid fa-phone"></i><span>{{ exibirTelefone() }}</span>
          </button>
          <button class="pii" :class="{ revealed: piiRevelado }" @click="revelarPii">
            <i class="fa-solid fa-id-card"></i><span>CPF {{ exibirCpf() }}</span>
          </button>
        </div>
        <div class="reveal-hint" @click="revelarPii">
          <i v-if="!piiRevelado" class="fa-solid fa-fingerprint"></i>
          <i v-else class="fa-solid fa-circle-check" style="color: hsl(var(--success))"></i>
          {{ piiRevelado ? "Dados revelados — acesso registrado" : "Toque para revelar dados sensíveis" }}
        </div>
      </div>

      <!-- Alerta clínico (conteúdo só aqui; acesso auditado) -->
      <template v-if="temAlerta">
        <div class="alert-banner">
          <div class="ic"><i class="fa-solid fa-triangle-exclamation"></i></div>
          <div>
            <span class="lbl">Alerta clínico</span>
            <b>{{ paciente.alertas.join(" · ") }}</b>
          </div>
        </div>
        <div class="audit-note"><i class="fa-solid fa-eye"></i> Este acesso é auditado</div>
      </template>

      <div class="ftabs">
        <button class="ftab" :class="{ on: tab === 'hist' }" @click="tab = 'hist'">Histórico</button>
        <button v-if="podeProntuario" class="ftab" :class="{ on: tab === 'pront' }" @click="tab = 'pront'">Prontuário</button>
        <button class="ftab" :class="{ on: tab === 'docs' }" @click="tab = 'docs'">Documentos</button>
        <button v-if="podeOrcamento" class="ftab" :class="{ on: tab === 'orc' }" @click="tab = 'orc'">Orçamentos</button>
        <button v-if="podeFotos" class="ftab" :class="{ on: tab === 'fotos' }" @click="router.push(`/paciente/${id}/fotos`)">Fotos</button>
      </div>

      <!-- Aba Histórico — cards clicáveis (item 1) -->
      <div v-show="tab === 'hist'" class="fpanel on">
        <div class="f-label">Últimas evoluções</div>
        <template v-if="evolucoes.length">
          <div
            v-for="e in evolucoes"
            :key="e.id"
            class="evo-card evo-card--clicavel"
            role="button"
            tabindex="0"
            @click="abrirDetalhe(e)"
            @keydown.enter="abrirDetalhe(e)"
          >
            <div class="eh">
              <b>{{ e.modeloNome || "Evolução" }}</b>
              <span class="dt-chev">
                <span class="dt">{{ dataCurta(e.criadaEm) }}</span>
                <i class="fa-solid fa-chevron-right evo-chev"></i>
              </span>
            </div>
            <div class="who2">{{ e.autorNome }}</div>
            <div v-if="e.qtdAnexos" class="att"><i class="fa-solid fa-paperclip"></i> {{ e.qtdAnexos }} {{ e.qtdAnexos! > 1 ? "anexos" : "anexo" }}</div>
          </div>
        </template>
        <div v-else class="tab-empty"><i class="fa-regular fa-folder-open"></i><p>Sem evoluções registradas.</p></div>
      </div>

      <!-- Aba Prontuário -->
      <div v-show="tab === 'pront'" class="fpanel on">
        <div v-if="evolucoes.length">
          <div v-for="e in evolucoes" :key="e.id" class="evo-card">
            <div class="eh"><b>{{ e.modeloNome || "Evolução" }}</b><span class="dt">{{ dataCurta(e.criadaEm) }}</span></div>
            <div class="who2">{{ e.autorNome }}</div>
          </div>
        </div>
        <div v-else class="tab-empty"><i class="fa-regular fa-file-lines"></i><p>Prontuário ainda vazio.</p></div>
        <button v-if="podeProntuario" class="btn-outline" style="margin-top: 6px" @click="router.push(`/paciente/${id}/prontuario`)">
          <i class="fa-solid fa-file-waveform"></i> Abrir prontuário completo
        </button>
      </div>

      <!-- Aba Documentos (item 5) -->
      <div v-show="tab === 'docs'" class="fpanel on">
        <!-- Skeleton -->
        <template v-if="docsCarregando">
          <div class="skeleton" style="height: 56px; border-radius: 10px; margin-bottom: 8px;"></div>
          <div class="skeleton" style="height: 56px; border-radius: 10px; margin-bottom: 8px;"></div>
          <div class="skeleton" style="height: 56px; border-radius: 10px;"></div>
        </template>

        <!-- Vazio -->
        <AppEmptyState
          v-else-if="docsCarregado && totalDocs === 0"
          icon="fa-file-circle-xmark"
          titulo="Nenhum documento"
          texto="Nenhuma receita, atestado ou pedido de exame emitido para este paciente."
        />

        <template v-else>
          <!-- Receitas -->
          <template v-if="receitas.length">
            <div class="f-label">Receitas</div>
            <div v-for="r in receitas" :key="r.id" class="doc-row">
              <div class="di" style="background: hsl(var(--primary) / 0.12); color: var(--brand)">
                <i class="fa-solid fa-prescription"></i>
              </div>
              <div class="dx">
                <b>{{ etiquetaTipoReceita(r.tipo) }}</b>
                <span>{{ r.quantidadeItens }} item{{ r.quantidadeItens !== 1 ? "s" : "" }} · {{ r.emitidaEm ? dataCurta(r.emitidaEm) : "Rascunho" }}</span>
              </div>
              <button
                class="doc-pdf-btn"
                :disabled="baixandoPdf === r.id"
                @click="baixarReceitaPdf(r.id)"
              >
                <i v-if="baixandoPdf === r.id" class="fa-solid fa-spinner fa-spin"></i>
                <i v-else class="fa-regular fa-file-pdf"></i>
              </button>
            </div>
          </template>

          <!-- Atestados -->
          <template v-if="atestados.length">
            <div class="f-label" :style="receitas.length ? 'margin-top: 14px' : ''">Atestados</div>
            <div
              v-for="a in atestados"
              :key="a.id"
              class="doc-row doc-row--clicavel"
              role="button"
              tabindex="0"
              @click="abrirDocDetalhe(a, 'atestado')"
              @keydown.enter="abrirDocDetalhe(a, 'atestado')"
            >
              <div class="di" style="background: hsl(var(--success) / 0.12); color: hsl(var(--success))">
                <i class="fa-solid fa-file-medical"></i>
              </div>
              <div class="dx">
                <b>{{ a.tipo }}</b>
                <span>{{ a.profissionalNome || "—" }} · {{ dataCurta(a.criadoEm) }}</span>
              </div>
              <i class="fa-solid fa-chevron-right" style="color: var(--app-text-faint); font-size: var(--fs-xs)"></i>
            </div>
          </template>

          <!-- Pedidos de Exame -->
          <template v-if="pedidosExame.length">
            <div class="f-label" :style="(receitas.length || atestados.length) ? 'margin-top: 14px' : ''">Pedidos de exame</div>
            <div
              v-for="e in pedidosExame"
              :key="e.id"
              class="doc-row doc-row--clicavel"
              role="button"
              tabindex="0"
              @click="abrirDocDetalhe(e, 'exame')"
              @keydown.enter="abrirDocDetalhe(e, 'exame')"
            >
              <div class="di" style="background: hsl(var(--info) / 0.12); color: hsl(var(--info))">
                <i class="fa-solid fa-flask"></i>
              </div>
              <div class="dx">
                <b>{{ e.tipo }}</b>
                <span>{{ (e as PedidoExameDto).exames?.slice(0, 2).join(", ") || "—" }} · {{ dataCurta(e.criadoEm) }}</span>
              </div>
              <i class="fa-solid fa-chevron-right" style="color: var(--app-text-faint); font-size: var(--fs-xs)"></i>
            </div>
          </template>
        </template>
      </div>

      <!-- Aba Orçamentos -->
      <div v-show="tab === 'orc'" class="fpanel on">
        <div v-if="orcamentos.length" class="plist">
          <div v-for="o in orcamentos" :key="o.id" class="doc-row" style="cursor: pointer" @click="abrirOrcamento(o)">
            <div class="di" style="background: hsl(var(--warning) / 0.15); color: hsl(35 88% 38%)"><i class="fa-solid fa-file-invoice-dollar"></i></div>
            <div class="dx"><b>Orçamento {{ o.numero }}</b><span>{{ o.status }}</span></div>
            <i class="fa-solid fa-chevron-right" style="color: var(--app-text-faint); font-size: 13px"></i>
          </div>
        </div>
        <div v-else class="tab-empty"><i class="fa-regular fa-file"></i><p>Nenhum orçamento.</p></div>
      </div>

      <template v-if="podeProntuario || podePrescrever || podeFotos">
        <div class="f-label" style="margin-top: 18px">Ações</div>
        <div class="fact-grid">
          <button v-if="podeProntuario" class="fact" @click="acao('evolucao')"><span class="fi ic-violet"><i class="fa-solid fa-plus"></i></span> Evolução</button>
          <button v-if="podePrescrever" class="fact" @click="acao('receita')"><span class="fi ic-violet"><i class="fa-solid fa-prescription"></i></span> Receita</button>
          <button v-if="podePrescrever" class="fact" @click="acao('atestado')"><span class="fi ic-green"><i class="fa-solid fa-file-medical"></i></span> Atestado</button>
          <button v-if="podePrescrever" class="fact" @click="acao('exame')"><span class="fi ic-blue"><i class="fa-solid fa-flask"></i></span> Exame</button>
          <button v-if="podeFotos" class="fact" @click="router.push(`/paciente/${id}/fotos`)"><span class="fi ic-violet"><i class="fa-solid fa-camera"></i></span> Fotos</button>
        </div>
      </template>
      <div class="audit-foot"><i class="fa-solid fa-shield-halved"></i> Este acesso foi registrado em seu nome</div>
    </div>

    <!-- Detalhe de evolução (item 1) -->
    <BottomSheet v-model:open="detalheOpen" titulo="Evolução" tall>
      <template v-if="evolucaoDetalhe">
        <div class="f-label">{{ evolucaoDetalhe.modeloNome || "Evolução" }}</div>
        <div class="evo-det-meta">
          <span>{{ evolucaoDetalhe.autorNome }}</span>
          <span class="dt">{{ dataCurta(evolucaoDetalhe.criadaEm) }}</span>
        </div>
        <div v-for="par in renderConteudo(evolucaoDetalhe)" :key="par.chave" class="evo-det-row">
          <div class="evo-det-label">{{ par.chave }}</div>
          <div class="evo-det-val">{{ par.valor }}</div>
        </div>
        <div v-if="evolucaoDetalhe.qtdAnexos" class="f-label" style="margin-top: 14px;">Anexos</div>
        <div v-if="detalheCarregandoAnexos" class="skeleton" style="height: 48px; border-radius: 8px;"></div>
        <div v-else-if="detalheAnexos.length" class="anexo-list">
          <button v-for="a in detalheAnexos" :key="a.id" class="anexo-row" @click="abrirAnexo(a.id)">
            <i class="fa-solid fa-paperclip"></i>
            <span>{{ a.nomeOriginal }}</span>
            <i class="fa-solid fa-arrow-up-right-from-square"></i>
          </button>
        </div>
      </template>
    </BottomSheet>

    <!-- Menu "..." — ações da ficha -->
    <BottomSheet v-model:open="menuFichaOpen" titulo="Ações" closable>
      <div v-if="podeEditarPaciente" class="med-row" @click="abrirEdicaoPaciente">
        <div class="mi"><i class="fa-solid fa-user-pen"></i></div>
        <b>Editar dados do paciente</b>
      </div>
      <div class="med-row" @click="exportarProntuarioPdf">
        <div class="mi">
          <i v-if="baixandoProntuario" class="fa-solid fa-spinner fa-spin"></i>
          <i v-else class="fa-regular fa-file-pdf"></i>
        </div>
        <b>{{ baixandoProntuario ? "Exportando…" : "Exportar prontuário PDF" }}</b>
      </div>
    </BottomSheet>

    <!-- Sheet de edição de dados do paciente (FIX 3) -->
    <PacienteSeletorSheet
      v-if="pacienteParaEditar"
      :open="sheetEditarPaciente"
      :paciente-para-editar="pacienteParaEditar"
      @update:open="sheetEditarPaciente = $event"
      @atualizado="aoAtualizarPaciente"
    />

    <!-- Detalhe de atestado ou pedido de exame (sem PDF — item 5) -->
    <BottomSheet v-model:open="docDetalheOpen" :titulo="docTipo === 'atestado' ? 'Atestado' : 'Pedido de exame'" tall>
      <template v-if="docDetalhe">
        <template v-if="docTipo === 'atestado'">
          <div class="f-label">Tipo</div>
          <div class="doc-det-val">{{ (docDetalhe as AtestadoDto).tipo }}</div>
          <template v-if="(docDetalhe as AtestadoDto).diasAfastamento">
            <div class="f-label">Dias de afastamento</div>
            <div class="doc-det-val">{{ (docDetalhe as AtestadoDto).diasAfastamento }} dias</div>
          </template>
          <template v-if="(docDetalhe as AtestadoDto).cid10">
            <div class="f-label">CID-10</div>
            <div class="doc-det-val">{{ (docDetalhe as AtestadoDto).cid10 }}</div>
          </template>
          <div class="f-label">Conteúdo</div>
          <div class="doc-det-val doc-det-val--pre">{{ (docDetalhe as AtestadoDto).conteudo }}</div>
          <div class="f-label">Emitido por</div>
          <div class="doc-det-val">{{ (docDetalhe as AtestadoDto).profissionalNome || "—" }} · {{ dataCurta((docDetalhe as AtestadoDto).criadoEm) }}</div>
        </template>

        <template v-else>
          <div class="f-label">Tipo</div>
          <div class="doc-det-val">{{ (docDetalhe as PedidoExameDto).tipo }}</div>
          <div class="f-label">Exames</div>
          <div class="doc-det-val">{{ (docDetalhe as PedidoExameDto).exames?.join(", ") || "—" }}</div>
          <div class="f-label">Indicação clínica</div>
          <div class="doc-det-val doc-det-val--pre">{{ (docDetalhe as PedidoExameDto).indicacaoClinica }}</div>
          <template v-if="(docDetalhe as PedidoExameDto).cid10">
            <div class="f-label">CID-10</div>
            <div class="doc-det-val">{{ (docDetalhe as PedidoExameDto).cid10 }}</div>
          </template>
          <div class="f-label">Emitido por</div>
          <div class="doc-det-val">{{ (docDetalhe as PedidoExameDto).profissionalNome || "—" }} · {{ dataCurta((docDetalhe as PedidoExameDto).criadoEm) }}</div>
        </template>

        <div class="doc-det-info"><i class="fa-solid fa-circle-info"></i> PDF deste documento não disponível no app no momento.</div>
      </template>
    </BottomSheet>
  </div>
</template>

<style scoped>
.evo-card--clicavel {
  cursor: pointer;
}
.evo-card--clicavel:active {
  opacity: 0.75;
}
.dt-chev {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  flex: none;
}
.evo-chev {
  color: var(--app-text-faint);
  font-size: var(--fs-xs);
}
.doc-row--clicavel {
  cursor: pointer;
}
.doc-row--clicavel:active {
  opacity: 0.75;
}
.doc-pdf-btn {
  width: 36px;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  border: 1px solid var(--app-border);
  background: var(--app-card);
  color: var(--brand);
  cursor: pointer;
  flex: none;
}
.evo-det-meta {
  display: flex;
  gap: 10px;
  font-size: var(--fs-sm);
  color: var(--app-text-dim);
  margin-bottom: 12px;
}
.evo-det-row {
  margin-bottom: 10px;
}
.evo-det-label {
  font-size: var(--fs-xs);
  font-weight: var(--fw-semibold);
  color: var(--app-text-faint);
  text-transform: capitalize;
  margin-bottom: 2px;
}
.evo-det-val {
  font-size: var(--fs-sm);
  color: var(--app-text);
  white-space: pre-wrap;
}
.anexo-list {
  display: flex;
  flex-direction: column;
  gap: 6px;
}
.anexo-row {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 12px;
  background: var(--app-card);
  border: 1px solid var(--app-border);
  border-radius: var(--radius-xl);
  font-family: var(--font-sans);
  font-size: var(--fs-sm);
  font-weight: var(--fw-semibold);
  color: var(--brand);
  cursor: pointer;
  min-height: 44px;
  text-align: left;
}
.anexo-row span {
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.doc-det-val {
  font-size: var(--fs-sm);
  color: var(--app-text);
  margin-bottom: 12px;
}
.doc-det-val--pre {
  white-space: pre-wrap;
}
.doc-det-info {
  display: flex;
  gap: 8px;
  align-items: flex-start;
  margin-top: 16px;
  padding: 10px 12px;
  background: var(--app-card-2);
  border-radius: var(--radius-xl);
  font-size: var(--fs-xs);
  color: var(--app-text-dim);
}
</style>
