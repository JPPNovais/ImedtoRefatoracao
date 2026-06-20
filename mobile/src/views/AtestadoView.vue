<script setup lang="ts">
import { computed, onMounted, ref } from "vue"
import { useRoute, useRouter } from "vue-router"
import { atestadoService } from "@/services/documentos.service"
import { pacienteService } from "@/services/paciente.service"
import { iniciais } from "@/lib/format"
import BottomSheet from "@/components/ui/BottomSheet.vue"
import AppSearchInput from "@/components/ui/AppSearchInput.vue"
import AssinaturaFlow from "@/components/AssinaturaFlow.vue"

const route = useRoute()
const router = useRouter()

const CIDS = [
  { cod: "J06.9", desc: "Infecção aguda das vias aéreas superiores" },
  { cod: "M54.5", desc: "Dor lombar baixa" },
  { cod: "A09", desc: "Diarreia e gastroenterite de origem infecciosa" },
  { cod: "R51", desc: "Cefaleia" },
  { cod: "J11", desc: "Influenza (gripe)" },
  { cod: "K29.7", desc: "Gastrite não especificada" },
]

const pacienteId = Number(route.query.pacienteId || 1)
const pacienteNome = ref("Paciente")
const dias = ref(2)
const data = ref(new Date().toISOString().slice(0, 10))
const cid = ref<{ cod: string; desc: string } | null>(null)
const obs = ref("")

const cidSheet = ref(false)
const buscaCid = ref("")
const flow = ref<InstanceType<typeof AssinaturaFlow> | null>(null)

const cidsFiltrados = computed(() =>
  CIDS.filter((c) => (c.cod + " " + c.desc).toLowerCase().includes(buscaCid.value.toLowerCase())),
)

onMounted(async () => {
  const p = await pacienteService.obter(pacienteId).catch(() => null)
  if (p) pacienteNome.value = p.nomeCompleto
})

async function assinar() {
  await flow.value?.iniciar(async () => {
    await atestadoService.emitir(pacienteId, {
      tipo: "Afastamento",
      diasAfastamento: dias.value,
      cid10: cid.value?.cod,
      conteudo: obs.value || `Atestado de ${dias.value} dia(s) de afastamento.`,
    })
    return {}
  })
}
</script>

<template>
  <div class="push show">
    <div class="push-head">
      <button class="iconbtn" @click="router.back()"><i class="fa-solid fa-arrow-left"></i></button>
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
        <span>{{ cid ? `${cid.cod} · ${cid.desc}` : "Selecionar CID-10" }}</span>
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
      <div v-for="c in cidsFiltrados" :key="c.cod" class="med-row" @click="cid = c; cidSheet = false">
        <div class="mi"><i class="fa-solid fa-hashtag"></i></div>
        <b>{{ c.cod }} · {{ c.desc }}</b>
        <i class="fa-solid fa-plus add-i"></i>
      </div>
    </BottomSheet>

    <AssinaturaFlow
      ref="flow"
      titulo-sucesso="Atestado assinado"
      :resumo="`${pacienteNome} · ${dias} dia(s)${cid ? ' · ' + cid.cod : ''}`"
      copy-send="Enviar atestado"
      @concluir="router.back()"
    />
  </div>
</template>
