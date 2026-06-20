<script setup lang="ts">
import { ref, computed, onMounted, onActivated } from "vue"
import { useRouter } from "vue-router"
import { useAuthStore } from "@/stores/auth"
import { usePermissoesStore } from "@/stores/permissoes"
import { useUiStore } from "@/stores/ui"
import { dashboardService } from "@/services/dashboard.service"
import { financeiroService } from "@/services/financeiro.service"
import AppEmptyState from "@/components/ui/AppEmptyState.vue"
import { moeda, horaDe, iniciais } from "@/lib/format"
import type { DashboardDto, CaixaDiarioDto } from "@/types"

const router = useRouter()
const auth = useAuthStore()
const permissoes = usePermissoesStore()
const ui = useUiStore()

const carregando = ref(true)
const erro = ref(false)
const dashboard = ref<DashboardDto | null>(null)
const caixa = ref<CaixaDiarioDto | null>(null)

const verFinanceiro = computed(() => permissoes.pode("financeiro.ver"))

// Saudação por horário
const saudacao = computed(() => {
  const h = new Date().getHours()
  if (h < 12) return "Bom dia,"
  if (h < 18) return "Boa tarde,"
  return "Boa noite,"
})

// Nome amigável (profissional > usuário)
const nomeExibicao = computed(() => {
  const nomeCompleto = auth.usuario?.nomeCompleto ?? null
  if (!nomeCompleto) return "—"
  // Título + primeiro nome
  const partes = nomeCompleto.trim().split(/\s+/)
  const titulos = ["dr.", "dra.", "dr", "dra"]
  if (partes.length >= 2 && titulos.includes(partes[0].toLowerCase())) {
    return `${partes[0]} ${partes[1]}`
  }
  return partes[0]
})

// Próximo agendamento
const proximo = computed(() => dashboard.value?.proximosAgendamentos?.[0] ?? null)

const horaProximo = computed(() => {
  if (!proximo.value) return "—"
  return horaDe(proximo.value.inicioPrevisto)
})

// Minutos até o próximo (aproximado)
const minutosParaProximo = computed(() => {
  if (!proximo.value) return null
  const diff = new Date(proximo.value.inicioPrevisto).getTime() - Date.now()
  const min = Math.round(diff / 60000)
  if (min <= 0) return "agora"
  if (min < 60) return `em ${min} min`
  const h = Math.floor(min / 60)
  return `em ${h}h`
})

// Stats do dia — derivados do DashboardDto
// Backend não expõe atendidos/faltas no dashboard; usa AgendamentosHoje como total
// Atendidos e faltas só existem na agenda; aqui exibimos o que o dashboard tem.
const statsHoje = computed(() => {
  const d = dashboard.value
  if (!d) return null
  return {
    agendados: d.agendamentosHoje,
    // Dashboard não tem contagem de atendidos/faltas separada; exibimos como 0 (os próximos
    // agentes que implementam a agenda podem alimentar um store compartilhado).
    atendidos: 0,
    faltas: 0,
  }
})

// Itens de atenção (derivados do DashboardDto)
interface ItemAtencao {
  icon: string
  cls: string
  titulo: string
  subtitulo: string
  rotulo: string
  acao: () => void
}

const itensAtencao = computed<ItemAtencao[]>(() => {
  const d = dashboard.value
  if (!d) return []
  const items: ItemAtencao[] = []

  if (d.itensAbaixoMinimo > 0 && permissoes.pode("estoque")) {
    const n = d.itensAbaixoMinimo
    const sub = d.itensAbaixoMinimoLista.slice(0, 2).map((i) => i.nome).join(" · ") || "Verificar estoque"
    items.push({
      icon: "fa-boxes-stacked",
      cls: "ic-amber",
      titulo: `${n} ${n > 1 ? "itens" : "item"} em baixo estoque`,
      subtitulo: sub,
      rotulo: "Repor",
      acao: () => router.push("/mais"), // próximos agentes adicionam rota de estoque
    })
  }

  if (d.orcamentosPendentes > 0 && permissoes.pode("orcamento.ver")) {
    const n = d.orcamentosPendentes
    items.push({
      icon: "fa-file-invoice-dollar",
      cls: "ic-amber",
      titulo: `${n} orçamento${n > 1 ? "s" : ""} aguardando`,
      subtitulo: "Toque para ver",
      rotulo: "Ver",
      acao: () => router.push("/mais"),
    })
  }

  if (d.lancamentosVencidos > 0 && verFinanceiro.value) {
    const n = d.lancamentosVencidos
    items.push({
      icon: "fa-triangle-exclamation",
      cls: "ic-amber",
      titulo: `${n} lançamento${n > 1 ? "s" : ""} vencido${n > 1 ? "s" : ""}`,
      subtitulo: `A receber: ${moeda(d.vencidosAReceber)}`,
      rotulo: "Ver",
      acao: () => router.push("/mais"),
    })
  }

  return items
})

async function carregar() {
  carregando.value = true
  erro.value = false
  try {
    const [dash, cx] = await Promise.all([
      dashboardService.obter(),
      verFinanceiro.value ? financeiroService.obterCaixa() : Promise.resolve(null),
    ])
    dashboard.value = dash
    caixa.value = cx
  } catch {
    erro.value = true
    ui.toast("Erro ao carregar o painel", "error")
  } finally {
    carregando.value = false
  }
}

function irParaCaixa() {
  if (!verFinanceiro.value) return
  router.push("/caixa")
}

function irParaAgendamento(id: number) {
  router.push(`/agenda/${id}`)
}

function irParaAgenda() {
  router.push("/agenda")
}

onMounted(carregar)
// Recarrega ao voltar para a aba (KeepAlive)
onActivated(carregar)

const iniciaisProximo = computed(() =>
  proximo.value ? iniciais(proximo.value.pacienteNome) : ""
)
</script>

<template>
  <div class="view view-inicio">
    <!-- Saudação -->
    <div class="home-hero">
      <div class="hh-greet">{{ saudacao }}</div>
      <div class="hh-name">{{ nomeExibicao }}</div>
    </div>

    <!-- Skeleton de carregamento -->
    <template v-if="carregando">
      <div class="skeleton" style="height: 96px; margin-bottom: 8px; border-radius: 16px;"></div>
      <div class="stats">
        <div class="skeleton" style="height: 68px; border-radius: 12px;"></div>
        <div class="skeleton" style="height: 68px; border-radius: 12px;"></div>
        <div class="skeleton" style="height: 68px; border-radius: 12px;"></div>
      </div>
      <div class="skeleton" style="height: 80px; border-radius: 12px; margin-bottom: 20px;"></div>
      <div class="skeleton" style="height: 120px; border-radius: 12px;"></div>
    </template>

    <!-- Erro com retry -->
    <template v-else-if="erro">
      <AppEmptyState
        icon="fa-circle-exclamation"
        titulo="Não foi possível carregar"
        texto="Verifique a conexão e tente novamente."
      />
      <div style="text-align: center; margin-top: 12px;">
        <button class="btn-retry" @click="carregar">
          <i class="fa-solid fa-rotate-right"></i> Tentar novamente
        </button>
      </div>
    </template>

    <!-- Conteúdo -->
    <template v-else-if="dashboard">
      <!-- Card de Recebimentos (só com financeiro.ver) -->
      <button v-if="verFinanceiro" class="cash-card" @click="irParaCaixa">
        <div class="cc-top">
          <span class="cc-lbl"><i class="fa-regular fa-money-bill-1"></i> Recebido hoje</span>
          <i class="fa-solid fa-chevron-right"></i>
        </div>
        <div class="cc-val">{{ moeda(caixa?.totalDia ?? 0) }}</div>
        <div class="cc-foot">
          <span>
            {{
              caixa && caixa.resumoPorForma.length > 0
                ? `${caixa.resumoPorForma.reduce((s, r) => s + (r.total > 0 ? 1 : 0), 0)} recebimento${caixa.resumoPorForma.length !== 1 ? "s" : ""}`
                : "Nenhum recebimento"
            }}
          </span>
          <span v-if="dashboard.vencidosAReceber > 0" class="cc-pending">
            {{ moeda(dashboard.vencidosAReceber) }} a receber
          </span>
        </div>
      </button>

      <!-- Stats do dia -->
      <div class="stats">
        <div class="stat ok">
          <div class="n">{{ statsHoje?.atendidos ?? 0 }}</div>
          <div class="l"><i class="fa-solid fa-circle-check"></i> Atendidos</div>
        </div>
        <!-- Stat "Na recepção" — clicável, vai pra agenda -->
        <button class="stat tap info" style="border: 1px solid var(--app-border); text-align: left; font: inherit;" @click="irParaAgenda">
          <div class="n">{{ dashboard.agendamentosHoje }}</div>
          <div class="l"><i class="fa-solid fa-hourglass-half"></i> Na recepção</div>
        </button>
        <div class="stat">
          <div class="n">{{ statsHoje?.faltas ?? 0 }}</div>
          <div class="l"><i class="fa-regular fa-circle-xmark"></i> Faltas</div>
        </div>
      </div>

      <!-- Próximo agendamento -->
      <div class="sec-h">
        <div class="t">Próximo</div>
        <div class="chip">
          <i class="fa-regular fa-clock"></i>
          {{ proximo ? (minutosParaProximo ?? horaProximo) : "dia concluído" }}
        </div>
      </div>

      <div v-if="proximo" style="margin-bottom: 22px;">
        <div
          class="appt next-card"
          role="button"
          tabindex="0"
          style="cursor: pointer;"
          @click="irParaAgendamento(proximo.id)"
          @keydown.enter="irParaAgendamento(proximo.id)"
        >
          <div class="accent"></div>
          <div class="av">{{ iniciaisProximo }}</div>
          <div class="who">
            <div class="ln1">
              <span class="time">{{ horaProximo }}</span>
              <span class="nm">{{ proximo.pacienteNome }}</span>
            </div>
            <div class="ln2">
              <span class="sub">{{ proximo.tipoServico }}</span>
            </div>
          </div>
        </div>
      </div>
      <div v-else style="margin-bottom: 22px; padding: 16px; text-align: center; color: var(--app-text-faint); font-size: var(--fs-sm);">
        Nenhum agendamento hoje
      </div>

      <!-- Precisa de atenção -->
      <div class="sec-h">
        <div class="t">Precisa de atenção</div>
      </div>

      <div v-if="itensAtencao.length > 0" class="home-list">
        <div
          v-for="item in itensAtencao"
          :key="item.titulo"
          class="home-row"
          role="button"
          tabindex="0"
          @click="item.acao"
          @keydown.enter="item.acao"
        >
          <div class="hi" :class="item.cls">
            <i class="fa-solid" :class="item.icon"></i>
          </div>
          <div class="hx">
            <b>{{ item.titulo }}</b>
            <span>{{ item.subtitulo }}</span>
          </div>
          <div class="hr">{{ item.rotulo }} ›</div>
        </div>
      </div>
      <div v-else class="home-list">
        <div style="padding: 26px 16px; text-align: center; color: var(--app-text-faint);">
          <i class="fa-regular fa-circle-check" style="font-size: var(--fs-xl); margin-bottom: 8px; display: block;"></i>
          <span style="font-size: var(--fs-sm); font-weight: var(--fw-semibold);">Tudo em dia</span>
        </div>
      </div>
    </template>

    <!-- Vazio (dashboard nulo sem erro) -->
    <template v-else>
      <AppEmptyState icon="fa-chart-simple" titulo="Nenhum dado disponível" texto="Sem informações para exibir no momento." />
    </template>
  </div>
</template>

<style scoped>
.view-inicio {
  padding-bottom: 120px;
}
.btn-retry {
  background: var(--brand-soft);
  color: var(--brand);
  border: 0;
  border-radius: var(--radius-full);
  padding: 10px 20px;
  font: inherit;
  font-weight: var(--fw-bold);
  cursor: pointer;
  min-height: 44px;
}
.btn-retry:active {
  opacity: 0.8;
}
</style>
