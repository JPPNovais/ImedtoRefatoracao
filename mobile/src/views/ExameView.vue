<script setup lang="ts">
import { computed, onMounted, ref } from "vue"
import { useRoute, useRouter } from "vue-router"
import { exameService } from "@/services/documentos.service"
import { pacienteService } from "@/services/paciente.service"
import { useUiStore } from "@/stores/ui"
import { iniciais } from "@/lib/format"
import AssinaturaFlow from "@/components/AssinaturaFlow.vue"

const route = useRoute()
const router = useRouter()
const ui = useUiStore()

const EXAMES = ["Hemograma completo", "Glicemia de jejum", "Colesterol total e frações", "TSH", "Ureia e creatinina", "Urina tipo 1", "Raio-X de tórax", "Ultrassom abdominal", "Eletrocardiograma", "Vitamina D"]

const pacienteIdRaw = Number(route.query.pacienteId)
if (!pacienteIdRaw || pacienteIdRaw <= 0) {
  ui.toast("Paciente não identificado", "error")
  router.back()
}
const pacienteId = pacienteIdRaw
const pacienteNome = ref("Paciente")
const busca = ref("")
const selecionados = ref<string[]>([])
const indicacao = ref("")
const flow = ref<InstanceType<typeof AssinaturaFlow> | null>(null)

const filtrados = computed(() => EXAMES.filter((e) => e.toLowerCase().includes(busca.value.toLowerCase())))

onMounted(async () => {
  const p = await pacienteService.obter(pacienteId).catch(() => null)
  if (p) pacienteNome.value = p.nomeCompleto
})

function toggle(e: string) {
  const i = selecionados.value.indexOf(e)
  if (i >= 0) selecionados.value.splice(i, 1)
  else selecionados.value.push(e)
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
      <div class="fav-chips">
        <button
          v-for="e in filtrados"
          :key="e"
          class="fav-chip"
          :class="{ on: selecionados.includes(e) }"
          @click="toggle(e)"
        >
          <i v-if="selecionados.includes(e)" class="fa-solid fa-check"></i> {{ e }}
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
