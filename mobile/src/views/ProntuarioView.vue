<script setup lang="ts">
import { onMounted, ref } from "vue"
import { useRoute, useRouter } from "vue-router"
import { prontuarioService } from "@/services/prontuario.service"
import { pacienteService } from "@/services/paciente.service"
import type { Evolucao, Paciente } from "@/types"
import { useUiStore } from "@/stores/ui"
import { useTenantStore } from "@/stores/tenant"
import { useVoice } from "@/native/useVoice"
import { useCamera } from "@/native/useCamera"
import { localDb } from "@/lib/db"
import { iniciais, idade, dataCurta } from "@/lib/format"
import BottomSheet from "@/components/ui/BottomSheet.vue"

const route = useRoute()
const router = useRouter()
const ui = useUiStore()
const tenant = useTenantStore()
const voice = useVoice()
const camera = useCamera()

const pacienteId = Number(route.params.id)
const paciente = ref<Paciente | null>(null)
const evolucoes = ref<Evolucao[]>([])
const carregando = ref(true)

const sheetOpen = ref(false)
const modelo = ref("Retorno")
const texto = ref("")
const anexos = ref<{ dataUrl: string; done: boolean }[]>([])
const salvando = ref(false)

const MODELOS = ["Retorno", "Consulta", "Avaliação", "Pós-operatório", "Evolução livre"]

onMounted(async () => {
  try {
    paciente.value = await pacienteService.obter(pacienteId)
    const pront = await prontuarioService.obter(pacienteId)
    evolucoes.value = pront.evolucoes
  } catch {
    ui.toast("Não foi possível abrir o prontuário", "error")
  } finally {
    carregando.value = false
  }
  if (route.query.nova === "1") setTimeout(() => (sheetOpen.value = true), 320)
})

function toggleVoz() {
  if (voice.ouvindo.value) voice.parar()
  else voice.iniciar((t) => (texto.value = (texto.value ? texto.value + " " : "") + t))
}

async function anexar(source: "foto" | "galeria") {
  const foto = await camera.capturar(source)
  if (!foto) return
  const item = { dataUrl: foto.dataUrl, done: false }
  anexos.value.push(item)
  try {
    await prontuarioService.uploadAnexo(pacienteId, foto.blob, `anexo-${Date.now()}.jpg`)
    item.done = true
  } catch {
    item.done = true // mock/offline: marca como local
  }
}
function removerAnexo(i: number) {
  anexos.value.splice(i, 1)
}

async function salvar() {
  if (!texto.value.trim()) {
    ui.toast("Escreva a evolução antes de salvar", "error")
    return
  }
  salvando.value = true
  try {
    await prontuarioService.registrarEvolucao(pacienteId, {
      conteudoJson: { resumo: texto.value.trim(), modelo: modelo.value },
    })
    evolucoes.value.unshift({
      id: Date.now(),
      prontuarioId: 0,
      autorNome: "Dr. Você",
      modeloNome: modelo.value,
      conteudo: { resumo: texto.value.trim() },
      criadaEm: new Date().toISOString(),
      qtdAnexos: anexos.value.length,
    })
    ui.toast("Evolução salva")
    fecharSheet()
  } catch {
    // Offline: salva rascunho local com sync ao voltar (§7).
    if (tenant.estabelecimentoAtivoId) {
      await localDb.draftSave({
        id: `draft-${Date.now()}`,
        pacienteId,
        estabelecimentoId: tenant.estabelecimentoAtivoId,
        modelo: modelo.value,
        texto: texto.value.trim(),
        anexos: anexos.value.length,
        criadoEm: Date.now(),
      })
      ui.toast("Sem conexão — salvo como rascunho local")
      fecharSheet()
    } else {
      ui.toast("Não foi possível salvar", "error")
    }
  } finally {
    salvando.value = false
  }
}

function fecharSheet() {
  sheetOpen.value = false
  texto.value = ""
  anexos.value = []
  modelo.value = "Retorno"
  voice.parar()
}
function resumo(e: Evolucao): string {
  const c = e.conteudo as Record<string, unknown>
  return (c.resumo as string) || (c.texto as string) || ""
}
</script>

<template>
  <div class="push show">
    <div class="push-head">
      <button class="iconbtn" @click="router.back()"><i class="fa-solid fa-arrow-left"></i></button>
      <div class="ph-title">Prontuário</div>
      <button class="iconbtn"><i class="fa-solid fa-ellipsis"></i></button>
    </div>

    <div class="push-body">
      <div v-if="paciente" class="pr-ctx">
        <div class="av">{{ iniciais(paciente.nomeCompleto) }}</div>
        <div>
          <b>{{ paciente.nomeCompleto }}</b>
          <span>{{ idade(paciente.dataNascimento) ?? "—" }} anos · {{ paciente.genero || "—" }}</span>
        </div>
      </div>

      <div class="f-label">Linha do tempo</div>
      <div v-if="evolucoes.length" class="timeline">
        <div v-for="e in evolucoes" :key="e.id" class="tl-item">
          <div class="tl-card">
            <div class="tl-head"><b>{{ e.modeloNome || "Evolução" }}</b><span class="dt">{{ dataCurta(e.criadaEm) }}</span></div>
            <div class="tl-by">{{ e.autorNome }}</div>
            <p v-if="resumo(e)" class="tl-snip">{{ resumo(e) }}</p>
            <div v-if="e.qtdAnexos" class="att"><i class="fa-solid fa-paperclip"></i> {{ e.qtdAnexos }} {{ e.qtdAnexos > 1 ? "anexos" : "anexo" }}</div>
          </div>
        </div>
      </div>
      <div v-else class="tab-empty">
        <i class="fa-regular fa-file-lines"></i>
        <p>Nenhuma evolução registrada.<br />Toque em "Nova evolução" para começar.</p>
      </div>
    </div>

    <div class="push-foot">
      <button class="btn-primary-lg" style="margin: 0" @click="sheetOpen = true"><i class="fa-solid fa-plus"></i> Nova evolução</button>
    </div>

    <!-- Nova evolução (bottom sheet alto) -->
    <BottomSheet v-model:open="sheetOpen" titulo="Nova evolução" tall @update:open="(v) => !v && voice.parar()">
      <div class="f-label">Modelo</div>
      <div class="sel-wrap">
        <select v-model="modelo" class="msel">
          <option v-for="m in MODELOS" :key="m">{{ m }}</option>
        </select>
        <i class="fa-solid fa-chevron-down"></i>
      </div>

      <div class="f-label">Evolução</div>
      <div class="evo-ta-wrap">
        <textarea v-model="texto" class="evo-ta" placeholder="Descreva a evolução do paciente…"></textarea>
        <button class="mic" :class="{ listening: voice.ouvindo.value }" title="Ditado por voz" @click="toggleVoz">
          <i class="fa-solid fa-microphone"></i>
        </button>
      </div>
      <div class="listen-hint" :class="{ on: voice.ouvindo.value }" @click="voice.parar()">
        <span class="wave"><span></span><span></span><span></span></span> Ouvindo… toque para parar
      </div>

      <div class="f-label">Anexos</div>
      <div class="att-btns">
        <button @click="anexar('foto')"><i class="fa-solid fa-camera"></i> Foto</button>
        <button @click="anexar('galeria')"><i class="fa-regular fa-images"></i> Galeria</button>
      </div>
      <div class="att-thumbs">
        <div v-for="(a, i) in anexos" :key="i" class="thumb" :class="{ done: a.done }">
          <img :src="a.dataUrl" alt="anexo" />
          <button class="rm" @click="removerAnexo(i)"><i class="fa-solid fa-xmark"></i></button>
          <div class="prog"><div class="bar" :style="{ width: a.done ? '100%' : '60%' }"></div></div>
          <div class="done-check"><i class="fa-solid fa-check"></i></div>
        </div>
      </div>

      <template #footer>
        <button class="btn-primary-lg" style="margin: 0" :disabled="salvando" @click="salvar">
          <i v-if="salvando" class="fa-solid fa-spinner fa-spin"></i>
          <i v-else class="fa-solid fa-floppy-disk"></i>
          {{ salvando ? "Salvando…" : "Salvar evolução" }}
        </button>
      </template>
    </BottomSheet>
  </div>
</template>
