<script setup lang="ts">
import { onMounted, ref } from "vue"
import { useRoute, useRouter } from "vue-router"
import { prontuarioService } from "@/services/prontuario.service"
import { pacienteService } from "@/services/paciente.service"
import type { Evolucao, Paciente, AnexoDto, ApiError } from "@/types"
import { useUiStore } from "@/stores/ui"
import { useTenantStore } from "@/stores/tenant"
import { useVoice } from "@/native/useVoice"
import { useCamera } from "@/native/useCamera"
import { useDownload } from "@/native/useDownload"
import { localDb } from "@/lib/db"
import { iniciais, idade, dataCurta, renderConteudoEvolucao } from "@/lib/format"
import BottomSheet from "@/components/ui/BottomSheet.vue"

const route = useRoute()
const router = useRouter()
const ui = useUiStore()
const tenant = useTenantStore()
const voice = useVoice()
const camera = useCamera()
const download = useDownload()

const pacienteId = Number(route.params.id)
const paciente = ref<Paciente | null>(null)
const evolucoes = ref<Evolucao[]>([])
const carregando = ref(true)
// Verdadeiro quando o backend já tem prontuário para este paciente
const prontuarioIniciado = ref(false)

// Sheet de nova evolução
const sheetOpen = ref(false)
const modelo = ref("Retorno")
const texto = ref("")
const anexos = ref<{ dataUrl: string; done: boolean; blob?: Blob; nome?: string }[]>([])
const salvando = ref(false)

// Sheet de detalhe de evolução (item 1)
const evolucaoDetalhe = ref<Evolucao | null>(null)
const detalheAnexos = ref<AnexoDto[]>([])
const detalheCarregandoAnexos = ref(false)
const detalheOpen = ref(false)

// Action sheet do "..." (item 2)
const menuOpen = ref(false)
const exportandoPdf = ref(false)

const MODELOS = ["Retorno", "Consulta", "Avaliação", "Pós-operatório", "Evolução livre"]

onMounted(async () => {
  try {
    paciente.value = await pacienteService.obter(pacienteId)
    const pront = await prontuarioService.obter(pacienteId)
    prontuarioIniciado.value = pront.prontuario !== null
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
  const item = { dataUrl: foto.dataUrl, done: false, blob: foto.blob, nome: `anexo-${Date.now()}.jpg` }
  anexos.value.push(item)
  // Upload só acontece após salvar a evolução (item 4 — vincula ao evolucaoId)
  item.done = true
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
    // Se o prontuário ainda não foi iniciado, inicia antes de registrar a evolução.
    if (!prontuarioIniciado.value) {
      await prontuarioService.iniciarProntuario(pacienteId)
      prontuarioIniciado.value = true
    }
    const res = await prontuarioService.registrarEvolucao(pacienteId, {
      conteudoJson: JSON.stringify({ resumo: texto.value.trim(), modelo: modelo.value }),
    })
    // Upload dos anexos com o evolucaoId retornado (item 4)
    const evolucaoId = res.evolucaoId
    await Promise.allSettled(
      anexos.value
        .filter((a) => a.blob)
        .map((a) =>
          prontuarioService.uploadAnexo(pacienteId, a.blob!, a.nome ?? `anexo-${Date.now()}.jpg`, evolucaoId),
        ),
    )
    // Recarrega do backend (item 3 — substitui o insert fake)
    const pront = await prontuarioService.obter(pacienteId)
    prontuarioIniciado.value = pront.prontuario !== null
    evolucoes.value = pront.evolucoes
    ui.toast("Evolução salva")
    fecharSheet()
  } catch (err) {
    // Erros com status (ApiError: 422 BusinessException, 4xx, 5xx) NÃO são offline
    // e NÃO geram rascunho — exibem a mensagem do backend via toast.
    if (err && typeof err === "object" && "status" in err) {
      ui.toast((err as ApiError).mensagem, "error")
      return
    }
    // Falha real de rede (fetch rejeitou sem status) → salva rascunho local.
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

// ── Detalhe de evolução (item 1) ────────────────────────────────────────────

async function abrirDetalhe(e: Evolucao) {
  evolucaoDetalhe.value = e
  detalheOpen.value = true
  detalheAnexos.value = []
  if (e.qtdAnexos) {
    detalheCarregandoAnexos.value = true
    try {
      const pagina = await prontuarioService.listarAnexos(pacienteId, { evolucaoId: e.id })
      detalheAnexos.value = pagina.itens
    } catch {
      // silencioso; a lista ficará vazia
    } finally {
      detalheCarregandoAnexos.value = false
    }
  }
}

async function abrirAnexo(anexoId: number) {
  try {
    const urlDto = await prontuarioService.obterUrlAnexo(pacienteId, anexoId)
    window.open(urlDto.url, "_blank", "noopener,noreferrer")
  } catch {
    ui.toast("Não foi possível abrir o anexo", "error")
  }
}

// ── Exportar PDF (item 2) ────────────────────────────────────────────────────

async function exportarPdf() {
  menuOpen.value = false
  exportandoPdf.value = true
  try {
    // Backend audita LGPD (exportação) dentro do handler — não chamamos registrar-exportacao separado
    await download.baixarPdf(`/paciente/${pacienteId}/prontuario/pdf`, `prontuario-${pacienteId}.pdf`)
  } catch {
    ui.toast("Não foi possível exportar o PDF", "error")
  } finally {
    exportandoPdf.value = false
  }
}

function renderConteudo(e: Evolucao): Array<{ chave: string; valor: string }> {
  return renderConteudoEvolucao(e.conteudo as Record<string, unknown>)
}
</script>

<template>
  <div class="push show">
    <div class="push-head">
      <button class="iconbtn" @click="router.back()"><i class="fa-solid fa-arrow-left"></i></button>
      <div class="ph-title">Prontuário</div>
      <button class="iconbtn" :disabled="exportandoPdf" @click="menuOpen = true">
        <i v-if="exportandoPdf" class="fa-solid fa-spinner fa-spin"></i>
        <i v-else class="fa-solid fa-ellipsis"></i>
      </button>
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
      <div v-if="carregando">
        <div class="skeleton" style="height: 80px; border-radius: 12px; margin-bottom: 10px;"></div>
        <div class="skeleton" style="height: 80px; border-radius: 12px;"></div>
      </div>
      <div v-else-if="evolucoes.length" class="timeline">
        <div
          v-for="e in evolucoes"
          :key="e.id"
          class="tl-item"
          role="button"
          tabindex="0"
          @click="abrirDetalhe(e)"
          @keydown.enter="abrirDetalhe(e)"
        >
          <div class="tl-card">
            <div class="tl-head"><b>{{ e.modeloNome || "Evolução" }}</b><span class="dt">{{ dataCurta(e.criadaEm) }}</span></div>
            <div class="tl-by">{{ e.autorNome }}</div>
            <p v-if="resumo(e)" class="tl-snip">{{ resumo(e) }}</p>
            <div v-if="e.qtdAnexos" class="att"><i class="fa-solid fa-paperclip"></i> {{ e.qtdAnexos }} {{ e.qtdAnexos! > 1 ? "anexos" : "anexo" }}</div>
            <div class="tl-ver"><i class="fa-solid fa-chevron-right"></i></div>
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

    <!-- Menu "..." do header (item 2) -->
    <BottomSheet v-model:open="menuOpen" titulo="Prontuário">
      <button class="act-row" style="width: 100%; text-align: left;" @click="exportarPdf">
        <div class="ic ic-violet"><i class="fa-solid fa-file-pdf"></i></div>
        <div class="tx"><b>Exportar PDF</b><span>Histórico completo do prontuário</span></div>
        <i class="fa-solid fa-chevron-right chev"></i>
      </button>
    </BottomSheet>

    <!-- Detalhe da evolução (item 1) -->
    <BottomSheet v-model:open="detalheOpen" titulo="Evolução" tall>
      <template v-if="evolucaoDetalhe">
        <div class="f-label">{{ evolucaoDetalhe.modeloNome || "Evolução" }}</div>
        <div class="evo-det-meta">
          <span>{{ evolucaoDetalhe.autorNome }}</span>
          <span class="dt">{{ dataCurta(evolucaoDetalhe.criadaEm) }}</span>
        </div>

        <div v-for="par in renderConteudo(evolucaoDetalhe)" :key="par.chave" class="evo-det-row">
          <div class="evo-det-label">{{ par.chave }}</div>
          <div class="evo-det-val">{{ par.valor }}</div>
        </div>

        <!-- Anexos -->
        <div v-if="evolucaoDetalhe.qtdAnexos" class="f-label" style="margin-top: 14px;">Anexos</div>
        <div v-if="detalheCarregandoAnexos" class="skeleton" style="height: 48px; border-radius: 8px;"></div>
        <div v-else-if="detalheAnexos.length" class="anexo-list">
          <button
            v-for="a in detalheAnexos"
            :key="a.id"
            class="anexo-row"
            @click="abrirAnexo(a.id)"
          >
            <i class="fa-solid fa-paperclip"></i>
            <span>{{ a.nomeOriginal }}</span>
            <i class="fa-solid fa-arrow-up-right-from-square"></i>
          </button>
        </div>
      </template>
    </BottomSheet>

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

<style scoped>
.tl-item {
  cursor: pointer;
  position: relative;
}
.tl-item:active .tl-card {
  opacity: 0.75;
}
.tl-ver {
  position: absolute;
  right: 14px;
  top: 50%;
  transform: translateY(-50%);
  color: var(--app-text-faint);
  font-size: var(--fs-xs);
}
.evo-det-meta {
  display: flex;
  gap: 10px;
  font-size: var(--fs-sm);
  color: var(--app-text-dim);
  margin-bottom: 12px;
}
.evo-det-row {
  margin-bottom: 10px;
}
.evo-det-label {
  font-size: var(--fs-xs);
  font-weight: var(--fw-semibold);
  color: var(--app-text-faint);
  text-transform: capitalize;
  margin-bottom: 2px;
}
.evo-det-val {
  font-size: var(--fs-sm);
  color: var(--app-text);
  white-space: pre-wrap;
}
.anexo-list {
  display: flex;
  flex-direction: column;
  gap: 6px;
}
.anexo-row {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 12px;
  background: var(--app-card);
  border: 1px solid var(--app-border);
  border-radius: var(--radius-xl);
  font-family: var(--font-sans);
  font-size: var(--fs-sm);
  font-weight: var(--fw-semibold);
  color: var(--brand);
  cursor: pointer;
  min-height: 44px;
  text-align: left;
}
.anexo-row span {
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
