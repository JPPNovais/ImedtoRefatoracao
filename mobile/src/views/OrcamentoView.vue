<script setup lang="ts">
import { computed, onMounted, ref } from "vue"
import { useRoute, useRouter } from "vue-router"
import { orcamentoService } from "@/services/orcamento.service"
import type { Orcamento } from "@/types"
import { usePermissoesStore } from "@/stores/permissoes"
import { useUiStore } from "@/stores/ui"
import { iniciais, moeda } from "@/lib/format"
import AppStatusPill from "@/components/ui/AppStatusPill.vue"

const route = useRoute()
const router = useRouter()
const permissoes = usePermissoesStore()
const ui = useUiStore()

const id = Number(route.params.id)
const orc = ref<Orcamento | null>(null)
const carregando = ref(true)

// G2 — degradação por permissão: sem orcamento.aprovar, vira só leitura.
const podeAprovar = computed(() => permissoes.pode("orcamento.aprovar"))
const decidivel = computed(() => orc.value?.status?.toLowerCase().includes("aguardando"))

onMounted(async () => {
  try {
    orc.value = await orcamentoService.obter(id)
  } catch {
    ui.toast("Orçamento não encontrado", "error")
    router.back()
  } finally {
    carregando.value = false
  }
})

async function aprovar() {
  if (!orc.value) return
  try {
    await orcamentoService.aprovar(orc.value.id)
    orc.value.status = "Aprovado"
    ui.toast("Orçamento aprovado")
  } catch {
    ui.toast("Não foi possível aprovar", "error")
  }
}
function recusar() {
  ui.openConfirm({
    title: "Recusar orçamento?",
    msg: "O orçamento será marcado como recusado. Você pode emitir um novo depois.",
    confirmLabel: "Recusar",
    onConfirm: async () => {
      if (!orc.value) return
      try {
        await orcamentoService.recusar(orc.value.id)
        orc.value.status = "Recusado"
        ui.toast("Orçamento recusado")
      } catch {
        ui.toast("Não foi possível recusar", "error")
      }
    },
  })
}
</script>

<template>
  <div class="push show">
    <div class="push-head">
      <button class="iconbtn" @click="router.back()"><i class="fa-solid fa-arrow-left"></i></button>
      <div class="ph-title">Orçamento {{ orc?.numero || "" }}</div>
      <span style="width: 40px"></span>
    </div>

    <div v-if="orc" class="push-body">
      <AppStatusPill :status="orc.status" style="margin-bottom: 16px; display: inline-flex" />

      <button class="rc-patient">
        <span class="av">{{ iniciais(orc.pacienteNome) }}</span>
        <span class="rx"><b>{{ orc.pacienteNome }}</b><span>Paciente</span></span>
      </button>

      <div class="f-label">Procedimento</div>
      <div class="orc-proc">{{ orc.titulo || "—" }}</div>

      <div class="f-label">Itens</div>
      <div class="orc-items">
        <div v-for="(it, i) in orc.itens" :key="i" class="orc-line">
          <span class="ln-t">{{ it.descricao }}</span>
          <span class="ln-v">{{ moeda(it.valor) }}</span>
        </div>
      </div>
      <div class="orc-total"><span class="tt">Total</span><span class="tv">{{ moeda(orc.total) }}</span></div>

      <!-- G2: sem permissão → some o CTA, mostra nota de leitura -->
      <div v-if="!podeAprovar" class="orc-readonly" style="margin-top: 8px">
        <i class="fa-solid fa-lock"></i> Somente leitura · sem permissão para aprovar
      </div>
    </div>

    <div v-if="orc && podeAprovar && decidivel" class="push-foot">
      <div class="btn-row" style="margin: 0">
        <button class="btn-soft danger" style="flex: 1" @click="recusar"><i class="fa-solid fa-xmark"></i> Recusar</button>
        <button class="btn-primary-lg" style="flex: 1; margin: 0" @click="aprovar"><i class="fa-solid fa-check"></i> Aprovar</button>
      </div>
    </div>
  </div>
</template>
