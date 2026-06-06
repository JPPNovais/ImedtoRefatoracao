<script setup lang="ts">
import { computed, onActivated, onMounted, ref } from "vue"
import { useRouter } from "vue-router"
import { agendaService } from "@/services/agenda.service"
import type { Agendamento } from "@/types"
import { useUiStore } from "@/stores/ui"
import { usePermissoesStore } from "@/stores/permissoes"
import { norm, horaDe, toISODate } from "@/lib/format"
import AppAvatar from "@/components/ui/AppAvatar.vue"
import AppStatusPill from "@/components/ui/AppStatusPill.vue"
import SwipeableRow from "@/components/ui/SwipeableRow.vue"
import BottomSheet from "@/components/ui/BottomSheet.vue"

const router = useRouter()
const ui = useUiStore()
const permissoes = usePermissoesStore()

// RBAC: só quem pode editar agenda vê as ações de atender/faltar.
const podeEditarAgenda = computed(() => permissoes.pode("agenda.editar"))

const DOW = ["Dom", "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb"]
const selectedISO = ref(toISODate(new Date())) // data real (local, sem shift de UTC)

const week = computed(() => {
  const base = new Date(selectedISO.value + "T12:00:00")
  const monday = new Date(base)
  monday.setDate(base.getDate() - ((base.getDay() + 6) % 7))
  return Array.from({ length: 7 }, (_, i) => {
    const d = new Date(monday)
    d.setDate(monday.getDate() + i)
    const iso = toISODate(d)
    return { iso, dow: DOW[d.getDay()], n: d.getDate(), sel: iso === selectedISO.value }
  })
})

const appts = ref<Agendamento[]>([])
const carregando = ref(true)

// Stats do dia derivados da própria lista (robusto ao shape do DTO de contagem).
const stats = computed(() => ({
  agendados: appts.value.filter((a) => !["Cancelado"].includes(a.status)).length,
  atendidos: appts.value.filter((a) => a.status === "Concluido").length,
  faltas: appts.value.filter((a) => a.status === "Faltou").length,
}))

// Busca + filtro (top bar contextual)
const searchOpen = ref(false)
const query = ref("")
const filterOpen = ref(false)
const fStatus = ref("todos")
const fSala = ref("todas")
const fAlerta = ref("todos")
const filtrosAtivos = computed(() => fStatus.value !== "todos" || fSala.value !== "todas" || fAlerta.value !== "todos")
const buscaOuFiltro = computed(() => query.value.trim() !== "" || filtrosAtivos.value)

const ativos = computed(() => appts.value.filter((a) => !["Cancelado", "Faltou"].includes(a.status)))
const proximo = computed(() => ativos.value.find((a) => a.status === "Agendado" || a.status === "Confirmado") || null)
const maisTarde = computed(() => ativos.value.filter((a) => a.id !== proximo.value?.id))

const resultados = computed(() => {
  const q = query.value.trim()
  return appts.value.filter((a) => {
    const okNome = !q || norm(a.pacienteNome).includes(norm(q))
    const okStatus = fStatus.value === "todos" || a.status === fStatus.value
    const okSala = fSala.value === "todas" || a.salaNome === fSala.value
    const okAlerta = fAlerta.value === "todos" || a.temAlertaClinico
    return okNome && okStatus && okSala && okAlerta
  })
})

async function carregar() {
  carregando.value = true
  try {
    const pagina = await agendaService.listar({
      dataInicio: selectedISO.value,
      dataFim: selectedISO.value,
    })
    appts.value = pagina.itens
  } catch {
    ui.toast("Não foi possível carregar a agenda", "error")
  } finally {
    carregando.value = false
  }
}

onMounted(carregar)
onActivated(carregar)

function selecionarDia(iso: string) {
  selectedISO.value = iso
  void carregar()
}
function mudarDia(delta: number) {
  const d = new Date(selectedISO.value + "T12:00:00")
  d.setDate(d.getDate() + delta)
  selecionarDia(toISODate(d))
}
function abrir(a: Agendamento) {
  router.push(`/agenda/${a.id}`)
}
async function marcarAtendido(a: Agendamento) {
  try {
    await agendaService.concluir(a.id)
    ui.toast("Marcado como atendido")
    await carregar()
  } catch {
    ui.toast("Não foi possível concluir", "error")
  }
}
function marcarFaltou(a: Agendamento) {
  ui.openConfirm({
    title: "Marcar como faltou?",
    msg: "O paciente será registrado como ausente.",
    onConfirm: async () => {
      try {
        await agendaService.cancelar(a.id, "Faltou")
        ui.toast("Marcado como faltou")
        await carregar()
      } catch {
        ui.toast("Não foi possível registrar", "error")
      }
    },
  })
}
function aplicarFiltros() {
  filterOpen.value = false
}
function limparTudo() {
  query.value = ""
  searchOpen.value = false
  fStatus.value = "todos"
  fSala.value = "todas"
  fAlerta.value = "todos"
}

// Pull-to-refresh (gesto no topo da lista)
const ptr = ref<HTMLElement | null>(null)
const ptrState = ref<"" | "show" | "spin">("")
let startY = 0
let pulling = false
function getScroller(el: HTMLElement | null): HTMLElement | null {
  return el?.closest(".body") as HTMLElement | null
}
function onTouchStart(e: TouchEvent) {
  const sc = getScroller(ptr.value)
  if (sc && sc.scrollTop <= 0) {
    startY = e.touches[0].clientY
    pulling = true
  }
}
function onTouchMove(e: TouchEvent) {
  if (!pulling) return
  const dy = e.touches[0].clientY - startY
  ptrState.value = dy > 60 ? "show" : ""
}
async function onTouchEnd() {
  if (ptrState.value === "show") {
    ptrState.value = "spin"
    await carregar()
  }
  ptrState.value = ""
  pulling = false
}
</script>

<template>
  <section class="view" @touchstart="onTouchStart" @touchmove="onTouchMove" @touchend="onTouchEnd">
    <!-- Ferramentas contextuais no top bar -->
    <Teleport defer to="#topbar-tools">
      <button class="iconbtn" title="Buscar" @click="searchOpen = !searchOpen">
        <i class="fa-solid fa-magnifying-glass"></i>
      </button>
      <button class="iconbtn" title="Filtrar" @click="filterOpen = true">
        <i class="fa-solid fa-sliders"></i>
        <span v-if="filtrosAtivos" class="dot"></span>
      </button>
    </Teleport>

    <div ref="ptr" class="ptr" :class="ptrState"><i class="fa-solid fa-arrow-rotate-right"></i></div>

    <div v-if="searchOpen" class="psearch" style="margin-bottom: 14px">
      <i class="fa-solid fa-magnifying-glass"></i>
      <input v-model="query" type="text" placeholder="Buscar paciente na agenda…" autocomplete="off" />
    </div>

    <!-- date strip -->
    <div class="datestrip">
      <button class="arrow" @click="mudarDia(-1)"><i class="fa-solid fa-chevron-left"></i></button>
      <div class="days">
        <div
          v-for="d in week"
          :key="d.iso"
          class="dcell"
          :class="{ sel: d.sel }"
          @click="selecionarDia(d.iso)"
        >
          <span class="dow">{{ d.dow }}</span>
          <span class="dnum">{{ d.n }}</span>
          <span class="ddot"></span>
        </div>
      </div>
      <button class="arrow" @click="mudarDia(1)"><i class="fa-solid fa-chevron-right"></i></button>
    </div>

    <!-- Normal -->
    <div v-if="!buscaOuFiltro">
      <div class="stats">
        <div class="stat"><div class="n">{{ stats?.agendados ?? 0 }}</div><div class="l"><i class="fa-regular fa-calendar"></i> Agendados</div></div>
        <div class="stat ok"><div class="n">{{ stats?.atendidos ?? 0 }}</div><div class="l"><i class="fa-solid fa-circle-check"></i> Atendidos</div></div>
        <div class="stat"><div class="n">{{ stats?.faltas ?? 0 }}</div><div class="l"><i class="fa-regular fa-circle-xmark"></i> Faltas</div></div>
      </div>

      <!-- skeleton -->
      <div v-if="carregando" class="plist">
        <div v-for="i in 3" :key="i" class="skrow">
          <div class="sk sk-av"></div>
          <div style="flex: 1"><div class="sk sk-l" style="width: 60%"></div><div class="sk sk-l" style="width: 40%; margin-top: 8px"></div></div>
        </div>
      </div>

      <template v-else>
        <template v-if="proximo">
          <div class="sec-h"><div class="t">Próximo</div></div>
          <div style="margin-bottom: 22px">
            <div class="appt next-card" :class="{ alert: proximo.temAlertaClinico }" @click="abrir(proximo)">
              <div class="accent"></div>
              <AppAvatar :nome="proximo.pacienteNome" />
              <div class="who">
                <div class="ln1">
                  <span class="time">{{ horaDe(proximo.inicioPrevisto) }}</span>
                  <span class="nm">{{ proximo.pacienteNome }}</span>
                  <span v-if="proximo.temAlertaClinico" class="alert-dot"></span>
                </div>
                <div class="ln2">
                  <span class="sub">{{ proximo.tipoServico }} · {{ proximo.salaNome || "—" }}</span>
                  <AppStatusPill :status="proximo.status" />
                </div>
              </div>
            </div>
          </div>
        </template>

        <div class="sec-h"><div class="t">Mais tarde</div></div>
        <div class="stack">
          <template v-for="a in maisTarde" :key="a.id">
            <!-- Com permissão de editar: swipe revela atender/faltar -->
            <SwipeableRow
              v-if="podeEditarAgenda"
              @open="abrir(a)"
              @done="marcarAtendido(a)"
              @miss="marcarFaltou(a)"
            >
              <div class="appt" :class="{ alert: a.temAlertaClinico }">
                <div class="accent"></div>
                <AppAvatar :nome="a.pacienteNome" />
                <div class="who">
                  <div class="ln1">
                    <span class="time">{{ horaDe(a.inicioPrevisto) }}</span>
                    <span class="nm">{{ a.pacienteNome }}</span>
                    <span v-if="a.temAlertaClinico" class="alert-dot"></span>
                  </div>
                  <div class="ln2">
                    <span class="sub">{{ a.tipoServico }} · {{ a.salaNome || "—" }}</span>
                    <AppStatusPill :status="a.status" />
                  </div>
                </div>
              </div>
            </SwipeableRow>
            <!-- Sem permissão: só leitura (sem ações) -->
            <div v-else class="appt" :class="{ alert: a.temAlertaClinico }" @click="abrir(a)">
              <div class="accent"></div>
              <AppAvatar :nome="a.pacienteNome" />
              <div class="who">
                <div class="ln1">
                  <span class="time">{{ horaDe(a.inicioPrevisto) }}</span>
                  <span class="nm">{{ a.pacienteNome }}</span>
                  <span v-if="a.temAlertaClinico" class="alert-dot"></span>
                </div>
                <div class="ln2">
                  <span class="sub">{{ a.tipoServico }} · {{ a.salaNome || "—" }}</span>
                  <AppStatusPill :status="a.status" />
                </div>
              </div>
            </div>
          </template>
          <div v-if="!proximo && !maisTarde.length" class="empty">
            <i class="fa-regular fa-calendar"></i><b>Nenhuma consulta hoje 🎉</b><p>Aproveite o tempo livre.</p>
          </div>
        </div>
      </template>
    </div>

    <!-- Resultados de busca/filtro -->
    <div v-else>
      <div class="sec-h">
        <div class="t">Resultados · {{ resultados.length }}</div>
        <button class="chip" @click="limparTudo"><i class="fa-solid fa-xmark"></i> Limpar</button>
      </div>
      <div v-if="resultados.length" class="stack">
        <div v-for="a in resultados" :key="a.id" class="appt" :class="{ alert: a.temAlertaClinico }" @click="abrir(a)">
          <div class="accent"></div>
          <AppAvatar :nome="a.pacienteNome" />
          <div class="who">
            <div class="ln1"><span class="time">{{ horaDe(a.inicioPrevisto) }}</span><span class="nm">{{ a.pacienteNome }}</span><span v-if="a.temAlertaClinico" class="alert-dot"></span></div>
            <div class="ln2"><span class="sub">{{ a.tipoServico }} · {{ a.salaNome || "—" }}</span><AppStatusPill :status="a.status" /></div>
          </div>
        </div>
      </div>
      <div v-else class="empty" style="padding: 48px 24px">
        <i class="fa-regular fa-calendar"></i><b>Nada encontrado</b><p>Ajuste a busca ou os filtros.</p>
      </div>
    </div>

    <!-- Filtro sheet -->
    <BottomSheet v-model:open="filterOpen" titulo="Filtrar agenda" sub="Filtre o dia por status, sala e alerta." closable>
      <div class="f-label">Status</div>
      <div class="fav-chips">
        <button v-for="s in [{ k: 'todos', l: 'Todos' }, { k: 'Confirmado', l: 'Confirmados' }, { k: 'Agendado', l: 'Agendados' }]" :key="s.k" class="fav-chip" :class="{ on: fStatus === s.k }" @click="fStatus = s.k">{{ s.l }}</button>
      </div>
      <div class="f-label">Sala</div>
      <div class="fav-chips">
        <button v-for="s in ['todas', 'Sala 1', 'Sala 2', 'Sala 3']" :key="s" class="fav-chip" :class="{ on: fSala === s }" @click="fSala = s">{{ s === "todas" ? "Todas" : s }}</button>
      </div>
      <div class="f-label">Alerta clínico</div>
      <div class="fav-chips">
        <button class="fav-chip" :class="{ on: fAlerta === 'todos' }" @click="fAlerta = 'todos'">Todos</button>
        <button class="fav-chip" :class="{ on: fAlerta === 'alerta' }" @click="fAlerta = 'alerta'">
          <i class="fa-solid fa-triangle-exclamation" style="color: hsl(var(--error)); font-size: 11px"></i> Com alerta
        </button>
      </div>
      <button class="btn-primary-lg" style="margin-top: 8px" @click="aplicarFiltros"><i class="fa-solid fa-check"></i> Aplicar filtros</button>
    </BottomSheet>
  </section>
</template>
