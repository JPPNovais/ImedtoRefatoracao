<script setup lang="ts">
import { onMounted, ref, watch } from "vue"
import { useRoute, useRouter } from "vue-router"
import { exameService } from "@/services/documentos.service"
import { catalogoService } from "@/services/catalogo.service"
import { pacienteService } from "@/services/paciente.service"
import { useUiStore } from "@/stores/ui"
import { iniciais } from "@/lib/format"
import { useDebouncedRef } from "@/composables/useDebouncedRef"
import AppEmptyState from "@/components/ui/AppEmptyState.vue"
import AssinaturaFlow from "@/components/AssinaturaFlow.vue"
import type { ExameCatalogoDto } from "@/types"

const route = useRoute()
const router = useRouter()
const ui = useUiStore()

const pacienteIdRaw = Number(route.query.pacienteId)
if (!pacienteIdRaw || pacienteIdRaw <= 0) {
  ui.toast("Paciente não identificado", "error")
  router.back()
}
const pacienteId = pacienteIdRaw
const pacienteNome = ref("Paciente")
const busca = useDebouncedRef("", 350)
const examesDisponiveis = ref<ExameCatalogoDto[]>([])
const examesCarregando = ref(false)
const selecionados = ref<string[]>([])
const indicacao = ref("")
const flow = ref<InstanceType<typeof AssinaturaFlow> | null>(null)

onMounted(async () => {
  const [p] = await Promise.all([
    pacienteService.obter(pacienteId).catch(() => null),
    carregarExames(""),
  ])
  if (p) pacienteNome.value = p.nomeCompleto
})

async function carregarExames(termo: string) {
  examesCarregando.value = true
  try {
    examesDisponiveis.value = await catalogoService.buscarExames(termo || undefined)
  } catch {
    ui.toast("Não foi possível carregar os exames", "error")
  } finally {
    examesCarregando.value = false
  }
}

watch(busca, (termo) => carregarExames(termo))

function toggle(nome: string) {
  const i = selecionados.value.indexOf(nome)
  if (i >= 0) selecionados.value.splice(i, 1)
  else selecionados.value.push(nome)
}

async function assinar() {
  if (!selecionados.value.length) return ui.toast("Selecione ao menos um exame", "error")
  await flow.value?.iniciar(async () => {
    await exameService.emitir(pacienteId, {
      tipo: "Laboratorial",
      exames: selecionados.value,
      indicacaoClinica: indicacao.value,
    })
    return {}
  })
}
</script>

<template>
  <div class="push show">
    <div class="push-head">
      <button class="iconbtn" @click="router.back()"><i class="fa-solid fa-arrow-left"></i></button>
      <div class="ph-title">Pedido de exame</div>
      <span style="width: 40px"></span>
    </div>

    <div class="push-body">
      <button class="rc-patient">
        <span class="av">{{ iniciais(pacienteNome) }}</span>
        <span class="rx"><b>{{ pacienteNome }}</b><span>Paciente</span></span>
      </button>

      <div class="f-label">Exames <span style="color: var(--brand)">{{ selecionados.length ? `· ${selecionados.length}` : "" }}</span></div>
      <div class="psearch ex-search">
        <i class="fa-solid fa-magnifying-glass"></i>
        <input v-model="busca" type="text" placeholder="Buscar exame…" autocomplete="off" />
      </div>

      <div v-if="examesCarregando" class="exame-loading">
        <i class="fa-solid fa-spinner fa-spin"></i> Buscando…
      </div>
      <AppEmptyState
        v-else-if="examesDisponiveis.length === 0"
        icon="fa-flask"
        titulo="Nenhum exame encontrado"
        texto="Tente outro termo de busca"
      />
      <div v-else class="fav-chips">
        <button
          v-for="e in examesDisponiveis"
          :key="e.id"
          class="fav-chip"
          :class="{ on: selecionados.includes(e.nome) }"
          @click="toggle(e.nome)"
        >
          <i v-if="selecionados.includes(e.nome)" class="fa-solid fa-check"></i> {{ e.nome }}
        </button>
      </div>

      <div class="f-label">Indicação clínica</div>
      <textarea v-model="indicacao" class="doc-ta" placeholder="Hipótese diagnóstica / motivo do pedido…"></textarea>
    </div>

    <div class="push-foot">
      <button class="btn-primary-lg" style="margin: 0" @click="assinar"><i class="fa-solid fa-signature"></i> Assinar e gerar</button>
    </div>

    <AssinaturaFlow
      ref="flow"
      titulo-sucesso="Pedido de exame assinado"
      :resumo="`${pacienteNome} · ${selecionados.length} exame(s)`"
      copy-send="Enviar pedido de exame"
      @concluir="router.back()"
    />
  </div>
</template>

<style scoped>
.exame-loading {
  padding: var(--space-4) var(--space-3);
  color: var(--app-text-dim);
  text-align: center;
}
</style>
