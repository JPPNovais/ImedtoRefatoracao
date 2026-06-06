<script setup lang="ts">
import { ref } from "vue"
import { useUiStore } from "@/stores/ui"
import { useShare } from "@/native/useShare"

/* Fluxo reutilizável "assinar → sucesso → share" para Receita/Atestado/Exame.
   A assinatura digital (ANVISA) é assíncrona no backend; aqui orquestramos a
   animação + o estado de sucesso. O pai chama iniciar(assinarFn). */
const props = defineProps<{
  tituloSucesso: string
  resumo: string
  copySend: string
}>()
const emit = defineEmits<{ concluir: [] }>()

const ui = useUiStore()
const share = useShare()

const etapa = ref("")
const assinando = ref(false)
const sucesso = ref(false)
let urlPdf = "#"

const PASSOS = ["Validando certificado…", "Assinando documento…", "Gerando PDF…"]

async function iniciar(assinarFn: () => Promise<{ url?: string }>) {
  assinando.value = true
  for (const p of PASSOS) {
    etapa.value = p
    await new Promise((r) => setTimeout(r, 700))
  }
  try {
    const res = await assinarFn()
    urlPdf = res.url || "#"
    assinando.value = false
    sucesso.value = true
  } catch {
    assinando.value = false
    ui.toast("Falha na assinatura. Tente novamente.", "error")
  }
}

async function enviar() {
  const ok = await share.compartilhar({ title: props.tituloSucesso, text: props.copySend, url: urlPdf })
  if (ok) ui.toast("Compartilhado")
}
function verPdf() {
  ui.toast("Abrindo PDF")
}
function concluir() {
  sucesso.value = false
  emit("concluir")
}

defineExpose({ iniciar, sucesso })
</script>

<template>
  <!-- Animação de assinatura -->
  <Teleport defer to=".screen">
    <div class="dialog-scrim" :class="{ show: assinando }"></div>
    <div class="dialog" :class="{ show: assinando }">
      <div class="sign-spin"></div>
      <b class="sign-step">{{ etapa }}</b>
      <div class="sign-sub">Assinatura digital com validade legal</div>
    </div>
  </Teleport>

  <!-- Tela de sucesso (cobre o push) -->
  <div v-if="sucesso" class="push show" style="z-index: 56">
    <div class="push-head">
      <span style="width: 40px"></span>
      <div class="ph-title">Concluído</div>
      <span style="width: 40px"></span>
    </div>
    <div class="push-body">
      <div class="ok-hero">
        <div class="ok-check"><i class="fa-solid fa-circle-check"></i></div>
        <h3>{{ tituloSucesso }}</h3>
        <p>{{ resumo }}</p>
        <div class="ok-sig"><i class="fa-solid fa-shield-halved"></i> Assinada digitalmente · ANVISA</div>
      </div>
    </div>
    <div class="push-foot">
      <button class="btn-primary-lg" style="margin: 0 0 10px" @click="enviar"><i class="fa-brands fa-whatsapp"></i> {{ copySend }}</button>
      <div class="btn-row" style="margin: 0">
        <button class="btn-soft ok" style="flex: 1" @click="verPdf"><i class="fa-regular fa-file-pdf"></i> Ver PDF</button>
        <button class="btn-soft" style="flex: 1; background: var(--app-card-2); color: var(--app-text)" @click="concluir"><i class="fa-solid fa-check"></i> Concluir</button>
      </div>
    </div>
  </div>
</template>
