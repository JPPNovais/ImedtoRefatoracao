<script setup lang="ts">
import { onMounted, ref, watch } from "vue"
import { useRoute, useRouter } from "vue-router"
import { atestadoService } from "@/services/documentos.service"
import { catalogoService } from "@/services/catalogo.service"
import { pacienteService } from "@/services/paciente.service"
import { useUiStore } from "@/stores/ui"
import { iniciais, toISODate } from "@/lib/format"
import { useDebouncedRef } from "@/composables/useDebouncedRef"
import BottomSheet from "@/components/ui/BottomSheet.vue"
import AppSearchInput from "@/components/ui/AppSearchInput.vue"
import AppEmptyState from "@/components/ui/AppEmptyState.vue"
import AssinaturaFlow from "@/components/AssinaturaFlow.vue"
import type { Cid10Dto } from "@/types"

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
const dias = ref(1)
const data = ref(toISODate(new Date()))
const cid = ref<Cid10Dto | null>(null)
const obs = ref("")

const cidSheet = ref(false)
const buscaCid = useDebouncedRef("", 350)
const cidResultados = ref<Cid10Dto[]>([])
const cidCarregando = ref(false)
const cidErro = ref(false)
const flow = ref<InstanceType<typeof AssinaturaFlow> | null>(null)

onMounted(async () => {
  const p = await pacienteService.obter(pacienteId).catch(() => null)
  if (p) pacienteNome.value = p.nomeCompleto
})

async function carregarCids(termo: string) {
  cidCarregando.value = true
  cidErro.value = false
  try {
    cidResultados.value = await catalogoService.buscarCid(termo || undefined)
  } catch {
    cidErro.value = true
    ui.toast("Não foi possível carregar os CIDs", "error")
  } finally {
    cidCarregando.value = false
  }
}

// Ao abrir o sheet carrega os mais comuns; ao digitar rebusca com debounce
watch(cidSheet, (aberto) => {
  if (aberto) carregarCids(buscaCid.value)
})
watch(buscaCid, (termo) => {
  if (cidSheet.value) carregarCids(termo)
})

async function assinar() {
  await flow.value?.iniciar(async () => {
    await atestadoService.emitir(pacienteId, {
      tipo: "Afastamento",
      diasAfastamento: dias.value,
      cid10: cid.value?.codigo,
      conteudo: obs.value || `Atestado de ${dias.value} dia(s) de afastamento.`,
    })
    return {}
  })
}
</script>

<template>
  <div class="push show">
    <div class="push-head">
      <button class="iconbtn" aria-label="Voltar" @click="router.back()"><i class="fa-solid fa-arrow-left"></i></button>
      <div class="ph-title">Atestado</div>
      <span style="width: 40px"></span>
    </div>

    <div class="push-body">
      <button class="rc-patient">
        <span class="av">{{ iniciais(pacienteNome) }}</span>
        <span class="rx"><b>{{ pacienteNome }}</b><span>Paciente</span></span>
      </button>

      <div class="f-label">Dias de afastamento</div>
      <div class="stepper">
        <button :disabled="dias <= 1" @click="dias = Math.max(1, dias - 1)"><i class="fa-solid fa-minus"></i></button>
        <div class="val"><span>{{ dias }}</span> <small>dias</small></div>
        <button @click="dias++"><i class="fa-solid fa-plus"></i></button>
      </div>

      <div class="f-label">A partir de</div>
      <div class="tap-field">
        <i class="fa-regular fa-calendar lead"></i>
        <input v-model="data" type="date" style="border: 0; background: transparent; font: inherit; color: var(--app-text); flex: 1; outline: none" />
      </div>

      <div class="f-label">CID-10 (opcional)</div>
      <button class="tap-field" :class="{ placeholder: !cid }" @click="cidSheet = true">
        <i class="fa-solid fa-hashtag lead"></i>
        <span>{{ cid ? `${cid.codigo} · ${cid.descricao}` : "Selecionar CID-10" }}</span>
        <i class="fa-solid fa-chevron-right chev"></i>
      </button>

      <div class="f-label">Observações (opcional)</div>
      <textarea v-model="obs" class="doc-ta" placeholder="Ex.: necessita de repouso domiciliar…"></textarea>
    </div>

    <div class="push-foot">
      <button class="btn-primary-lg" style="margin: 0" @click="assinar"><i class="fa-solid fa-signature"></i> Assinar e gerar</button>
    </div>

    <BottomSheet v-model:open="cidSheet" titulo="CID-10" tall>
      <AppSearchInput v-model="buscaCid" placeholder="Buscar código ou doença…" />

      <div v-if="cidCarregando" class="cid-loading">
        <i class="fa-solid fa-spinner fa-spin"></i> Buscando…
      </div>
      <AppEmptyState
        v-else-if="!cidErro && cidResultados.length === 0"
        icon="fa-file-medical"
        titulo="Nenhum CID encontrado"
        texto="Tente outro termo de busca"
      />
      <template v-else>
        <div
          v-for="c in cidResultados"
          :key="c.codigo"
          class="med-row"
          @click="cid = c; cidSheet = false"
        >
          <div class="mi"><i class="fa-solid fa-hashtag"></i></div>
          <b>{{ c.codigo }} · {{ c.descricao }}</b>
          <i class="fa-solid fa-plus add-i"></i>
        </div>
      </template>
    </BottomSheet>

    <AssinaturaFlow
      ref="flow"
      titulo-sucesso="Atestado assinado"
      :resumo="`${pacienteNome} · ${dias} dia(s)${cid ? ' · ' + cid.codigo : ''}`"
      copy-send="Enviar atestado"
      @concluir="router.back()"
    />
  </div>
</template>

<style scoped>
.cid-loading {
  padding: var(--space-4) var(--space-3);
  color: var(--app-text-dim);
  text-align: center;
}
</style>
