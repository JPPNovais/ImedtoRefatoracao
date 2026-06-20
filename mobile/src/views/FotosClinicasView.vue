<script setup lang="ts">
import { computed, onMounted, ref } from "vue"
import { useRoute, useRouter } from "vue-router"
import { prontuarioService } from "@/services/prontuario.service"
import { pacienteService } from "@/services/paciente.service"
import { useCamera } from "@/native/useCamera"
import { usePermissoesStore } from "@/stores/permissoes"
import { useUiStore } from "@/stores/ui"
import BottomSheet from "@/components/ui/BottomSheet.vue"
import AppEmptyState from "@/components/ui/AppEmptyState.vue"
import { dataCurta, iniciais, idade } from "@/lib/format"
import type { AnexoDto, AnexoUrlDto, Paciente } from "@/types"

const route = useRoute()
const router = useRouter()
const ui = useUiStore()
const permissoes = usePermissoesStore()
const camera = useCamera()

// RBAC: fotos clínicas são restritas a Profissional/Dono (prontuario.ver)
const podeFotos = computed(() => permissoes.pode("prontuario.ver"))

const pacienteId = Number(route.params.id)

const paciente = ref<Paciente | null>(null)
const anexos = ref<AnexoDto[]>([])
const carregando = ref(true)
const uploadando = ref(false)

// --- Sheet de captura ---
const sheetCaptura = ref(false)
const regiao = ref("")
const marcador = ref<"Antes" | "Depois" | "Evolução">("Depois")

// --- Sheet de visualização ---
const sheetView = ref(false)
const fotoView = ref<{ foto: AnexoUrlDto; marcador: string | null; regiao: string | null; criadoEm: string } | null>(null)
const carregandoUrl = ref(false)

// --- Sheet de comparação ---
const sheetCompare = ref(false)
const fotosCompare = ref<{ url: AnexoUrlDto; marcador: string | null; regiao: string | null }[]>([])
const carregandoCompare = ref(false)

// Fotos (somente imagens — filtra por mimeType)
const fotos = computed(() =>
  anexos.value.filter((a) => a.mimeType.startsWith("image/")),
)

// Botão comparar: visível quando há ≥2 fotos
const podeComparar = computed(() => fotos.value.length >= 2)

onMounted(async () => {
  if (!podeFotos.value) {
    ui.toast("Sem permissão para ver fotos clínicas", "error")
    router.back()
    return
  }
  try {
    const [pac, lista] = await Promise.all([
      pacienteService.obter(pacienteId),
      prontuarioService.listarAnexos(pacienteId),
    ])
    paciente.value = pac
    anexos.value = lista
  } catch {
    ui.toast("Não foi possível carregar as fotos", "error")
    router.back()
  } finally {
    carregando.value = false
  }
})

function abrirCaptura() {
  regiao.value = ""
  marcador.value = "Depois"
  sheetCaptura.value = true
}

async function capturarEEnviar() {
  const foto = await camera.capturar("foto")
  if (!foto) return

  sheetCaptura.value = false

  uploadando.value = true
  try {
    const regiaoFinal = regiao.value.trim() || "Região clínica"
    const ts = Date.now()
    const nome = `foto-clinica_${ts}.jpg`
    await prontuarioService.uploadFotoClinica(
      pacienteId,
      foto.blob,
      nome,
      regiaoFinal,
      marcador.value,
    )
    // Recarrega a lista após upload bem-sucedido
    anexos.value = await prontuarioService.listarAnexos(pacienteId)
    ui.toast("Foto registrada com sucesso")
  } catch {
    ui.toast("Não foi possível salvar a foto. Tente novamente.", "error")
  } finally {
    uploadando.value = false
  }
}

async function abrirFoto(foto: AnexoDto) {
  carregandoUrl.value = true
  sheetView.value = true
  fotoView.value = null
  try {
    const urlDto = await prontuarioService.obterUrlAnexo(pacienteId, foto.id)
    fotoView.value = {
      foto: urlDto,
      marcador: foto.marcador ?? null,
      regiao: foto.regiaoAnatomica ?? null,
      criadoEm: foto.criadoEm,
    }
  } catch {
    ui.toast("Não foi possível carregar a foto", "error")
    sheetView.value = false
  } finally {
    carregandoUrl.value = false
  }
}

async function abrirComparacao() {
  // Agrupa por região, pega as 2 mais recentes da mesma região (ou as 2 primeiras)
  const lista = fotos.value
  if (lista.length < 2) return

  carregandoCompare.value = true
  sheetCompare.value = true
  fotosCompare.value = []

  try {
    // Tenta encontrar par Antes/Depois da mesma região
    let par: AnexoDto[] = []
    const regioes = [...new Set(lista.map((f) => f.regiaoAnatomica).filter(Boolean))] as string[]
    for (const r of regioes) {
      const grupo = lista.filter((f) => f.regiaoAnatomica === r)
      if (grupo.length >= 2) { par = grupo.slice(0, 2); break }
    }
    if (par.length < 2) par = lista.slice(0, 2)

    const urls = await Promise.all(par.map((f) => prontuarioService.obterUrlAnexo(pacienteId, f.id)))
    fotosCompare.value = par.map((f, i) => ({
      url: urls[i],
      marcador: f.marcador ?? null,
      regiao: f.regiaoAnatomica ?? null,
    }))
  } catch {
    ui.toast("Não foi possível carregar as fotos para comparação", "error")
    sheetCompare.value = false
  } finally {
    carregandoCompare.value = false
  }
}

function labelData(iso: string): string {
  return dataCurta(iso)
}
</script>

<template>
  <div class="push show">
    <div class="push-head">
      <button class="iconbtn" @click="router.back()">
        <i class="fa-solid fa-arrow-left"></i>
      </button>
      <div class="ph-title">Fotos clínicas</div>
      <button
        class="iconbtn"
        :style="{ visibility: podeComparar ? 'visible' : 'hidden' }"
        title="Comparar antes / depois"
        @click="abrirComparacao"
      >
        <i class="fa-solid fa-images"></i>
      </button>
    </div>

    <div class="push-body">
      <!-- Contexto do paciente -->
      <div v-if="paciente" class="pr-ctx">
        <div class="av">{{ iniciais(paciente.nomeCompleto) }}</div>
        <div>
          <b>{{ paciente.nomeCompleto }}</b>
          <span>{{ idade(paciente.dataNascimento) ?? "—" }} anos · {{ paciente.genero || "—" }}</span>
        </div>
      </div>
      <div v-else-if="carregando" class="pr-ctx">
        <div class="skeleton" style="width: 44px; height: 44px; border-radius: 50%; flex: none"></div>
        <div style="flex: 1">
          <div class="skeleton" style="height: 14px; width: 60%; margin-bottom: 6px; border-radius: 6px"></div>
          <div class="skeleton" style="height: 11px; width: 40%; border-radius: 6px"></div>
        </div>
      </div>

      <div class="f-label">Registros</div>

      <!-- Loading: grade skeleton -->
      <div v-if="carregando" class="foto-grid">
        <div v-for="n in 4" :key="n" class="skeleton foto-tile-sk"></div>
      </div>

      <!-- Vazio -->
      <div v-else-if="fotos.length === 0" style="padding: 10px 0">
        <AppEmptyState
          icon="fa-images"
          titulo="Nenhuma foto clínica"
          texto="Toque em &quot;Capturar foto&quot; para registrar a primeira imagem."
        />
      </div>

      <!-- Grade de fotos -->
      <div v-else class="foto-grid">
        <button
          v-for="foto in fotos"
          :key="foto.id"
          class="foto-tile"
          @click="abrirFoto(foto)"
        >
          <i class="fa-regular fa-image ph"></i>
          <span v-if="foto.marcador" class="tagp">{{ foto.marcador }}</span>
          <div class="ov">
            <b>{{ foto.regiaoAnatomica || foto.nomeOriginal }}</b>
            <span>{{ labelData(foto.criadoEm) }}</span>
          </div>
        </button>
      </div>

      <div class="audit-foot" style="margin-top: 14px">
        <i class="fa-solid fa-lock"></i> Imagens protegidas · acesso auditado
      </div>
    </div>

    <!-- Botão capturar (fixo no rodapé) -->
    <div class="push-foot">
      <button
        class="btn-primary-lg"
        style="margin: 0"
        :disabled="uploadando"
        @click="abrirCaptura"
      >
        <i class="fa-solid fa-camera"></i>
        {{ uploadando ? "Enviando…" : "Capturar foto" }}
      </button>
    </div>
  </div>

  <!-- Sheet de captura / câmera -->
  <BottomSheet v-model:open="sheetCaptura" titulo="Capturar foto" :closable="true">
    <!-- Viewfinder simulado (web: abre seletor ao tocar shutter) -->
    <div class="cam-view">
      <div class="cam-region">{{ regiao.trim() || "Enquadre a região clínica" }}</div>
      <div class="frame"></div>
      <i class="fa-regular fa-image cam-ico"></i>
    </div>

    <div class="f-label">Região / descrição</div>
    <input
      v-model="regiao"
      class="cam-input"
      type="text"
      placeholder="Ex.: incisão dorsal, face, arcada superior…"
      autocomplete="off"
    />

    <div class="f-label">Marcador</div>
    <div class="fav-chips" style="margin-bottom: 18px">
      <button
        class="fav-chip"
        :class="{ on: marcador === 'Antes' }"
        @click="marcador = 'Antes'"
      >Antes</button>
      <button
        class="fav-chip"
        :class="{ on: marcador === 'Depois' }"
        @click="marcador = 'Depois'"
      >Depois</button>
      <button
        class="fav-chip"
        :class="{ on: marcador === 'Evolução' }"
        @click="marcador = 'Evolução'"
      >Evolução</button>
    </div>

    <button class="cam-shutter" @click="capturarEEnviar"></button>
    <div style="text-align: center; font-size: var(--fs-xs); color: var(--app-text-faint); font-weight: var(--fw-bold); margin-top: 10px">
      Toque para capturar
    </div>
  </BottomSheet>

  <!-- Sheet de visualização -->
  <BottomSheet v-model:open="sheetView" :titulo="fotoView?.regiao || 'Registro'" :closable="true">
    <div v-if="carregandoUrl" style="padding: 30px 0; text-align: center">
      <div class="skeleton" style="height: 240px; border-radius: 14px"></div>
    </div>
    <div v-else-if="fotoView">
      <div class="foto-tile" style="aspect-ratio: 3 / 4; cursor: default; width: 100%; margin-bottom: 10px">
        <img :src="fotoView.foto.url" :alt="fotoView.regiao || 'Foto clínica'" class="foto-img" />
        <span v-if="fotoView.marcador" class="tagp">{{ fotoView.marcador }}</span>
        <div class="ov">
          <b>{{ fotoView.regiao || fotoView.foto.nomeOriginal }}</b>
          <span>{{ dataCurta(fotoView.criadoEm) }}</span>
        </div>
      </div>
    </div>
  </BottomSheet>

  <!-- Sheet de comparação antes/depois -->
  <BottomSheet v-model:open="sheetCompare" titulo="Comparar antes / depois" :closable="true">
    <div v-if="carregandoCompare" class="foto-compare">
      <div v-for="n in 2" :key="n" class="skeleton foto-tile-sk"></div>
    </div>
    <div v-else-if="fotosCompare.length >= 2">
      <div class="foto-compare">
        <div
          v-for="(item, i) in fotosCompare"
          :key="i"
          class="foto-tile"
          style="aspect-ratio: 3 / 4; cursor: default"
        >
          <img :src="item.url.url" :alt="item.regiao || 'Foto'" class="foto-img" />
          <span class="tagp">{{ item.marcador || (i === 0 ? 'Antes' : 'Depois') }}</span>
          <div class="ov">
            <b>{{ item.regiao || item.url.nomeOriginal }}</b>
            <span>{{ dataCurta(item.url.expiraEm) }}</span>
          </div>
        </div>
      </div>
      <div class="audit-foot" style="margin-top: 12px">
        <i class="fa-solid fa-arrows-left-right"></i>
        Comparação antes / depois · {{ fotosCompare[0]?.regiao || "mesma região" }}
      </div>
    </div>
  </BottomSheet>
</template>
