<script setup lang="ts">
import { computed, onMounted, ref } from "vue"
import { useRoute, useRouter } from "vue-router"
import { pacienteService } from "@/services/paciente.service"
import { prontuarioService } from "@/services/prontuario.service"
import { orcamentoService } from "@/services/orcamento.service"
import type { Evolucao, Orcamento, Paciente } from "@/types"
import { useUiStore } from "@/stores/ui"
import { usePermissoesStore } from "@/stores/permissoes"
import { useBiometric } from "@/native/useBiometric"
import { iniciais, idade, dataCurta } from "@/lib/format"

const route = useRoute()
const router = useRouter()
const ui = useUiStore()
const permissoes = usePermissoesStore()
const biometric = useBiometric()

// RBAC: ações da ficha respeitam o vínculo (G2).
const podeProntuario = computed(() => permissoes.pode("prontuario.ver"))
const podePrescrever = computed(() => permissoes.pode("prescricao"))
const podeOrcamento = computed(() => permissoes.pode("orcamento.ver"))

const id = Number(route.params.id)
const paciente = ref<Paciente | null>(null)
const evolucoes = ref<Evolucao[]>([])
const orcamentos = ref<Orcamento[]>([])
const carregando = ref(true)
const tab = ref<"hist" | "pront" | "docs" | "orc">("hist")
const piiRevelado = ref(false)

const temAlerta = computed(() => (paciente.value?.alertas.length ?? 0) > 0)

function mascarar(valor: string | null | undefined, tipo: "tel" | "cpf"): string {
  if (!valor) return tipo === "tel" ? "(••) •••••-••••" : "•••.•••.•••-••"
  if (piiRevelado.value) return valor
  return tipo === "tel" ? "(••) •••••-" + valor.slice(-4) : "•••.•••." + valor.slice(-6)
}

onMounted(async () => {
  // Abrir a ficha dispara o log de acesso no backend (PacienteAcessoLog).
  try {
    paciente.value = await pacienteService.obter(id)
    const pront = await prontuarioService.obter(id).catch(() => null)
    evolucoes.value = pront?.evolucoes ?? []
    orcamentos.value = await orcamentoService.listarPorPaciente(id).catch(() => [])
  } catch {
    ui.toast("Não foi possível abrir a ficha", "error")
    router.back()
  } finally {
    carregando.value = false
  }
})

async function revelarPii() {
  if (piiRevelado.value) return
  const ok = await biometric.confirmar("Revelar dados sensíveis do paciente")
  if (ok) {
    piiRevelado.value = true
    ui.toast("Dados revelados — acesso registrado")
  }
}

function abrirOrcamento(o: Orcamento) {
  router.push(`/orcamento/${o.id}`)
}
function acao(tipo: "evolucao" | "receita" | "atestado" | "exame") {
  if (!paciente.value) return
  if (tipo === "evolucao") router.push({ path: `/paciente/${id}/prontuario`, query: { nova: "1" } })
  else router.push({ path: `/${tipo}`, query: { pacienteId: id } })
}
</script>

<template>
  <div class="push show">
    <div class="push-head">
      <button class="iconbtn" @click="router.back()"><i class="fa-solid fa-arrow-left"></i></button>
      <div class="ph-title">{{ paciente?.nomeCompleto || "Ficha" }}</div>
      <button class="iconbtn"><i class="fa-solid fa-ellipsis"></i></button>
    </div>

    <div v-if="paciente" class="push-body">
      <div class="ficha-hero">
        <div class="av-xl">{{ iniciais(paciente.nomeCompleto) }}</div>
        <div class="fn">{{ paciente.nomeCompleto }}</div>
        <div class="fmeta">
          {{ idade(paciente.dataNascimento) ?? "—" }} anos · {{ paciente.genero || "—" }}
        </div>
        <div class="pii-row">
          <button class="pii" :class="{ revealed: piiRevelado }" @click="revelarPii">
            <i class="fa-solid fa-phone"></i><span>{{ mascarar(paciente.telefone, "tel") }}</span>
          </button>
          <button class="pii" :class="{ revealed: piiRevelado }" @click="revelarPii">
            <i class="fa-solid fa-id-card"></i><span>CPF {{ mascarar(paciente.cpf, "cpf") }}</span>
          </button>
        </div>
        <div class="reveal-hint" @click="revelarPii">
          <i v-if="!piiRevelado" class="fa-solid fa-fingerprint"></i>
          <i v-else class="fa-solid fa-circle-check" style="color: hsl(var(--success))"></i>
          {{ piiRevelado ? "Dados revelados — acesso registrado" : "Toque para revelar dados sensíveis" }}
        </div>
      </div>

      <!-- Alerta clínico (conteúdo só aqui; acesso auditado) -->
      <template v-if="temAlerta">
        <div class="alert-banner">
          <div class="ic"><i class="fa-solid fa-triangle-exclamation"></i></div>
          <div>
            <span class="lbl">Alerta clínico</span>
            <b>{{ paciente.alertas.join(" · ") }}</b>
          </div>
        </div>
        <div class="audit-note"><i class="fa-solid fa-eye"></i> Este acesso é auditado</div>
      </template>

      <div class="ftabs">
        <button class="ftab" :class="{ on: tab === 'hist' }" @click="tab = 'hist'">Histórico</button>
        <button v-if="podeProntuario" class="ftab" :class="{ on: tab === 'pront' }" @click="tab = 'pront'">Prontuário</button>
        <button class="ftab" :class="{ on: tab === 'docs' }" @click="tab = 'docs'">Documentos</button>
        <button v-if="podeOrcamento" class="ftab" :class="{ on: tab === 'orc' }" @click="tab = 'orc'">Orçamentos</button>
      </div>

      <div v-show="tab === 'hist'" class="fpanel on">
        <div class="f-label">Últimas evoluções</div>
        <template v-if="evolucoes.length">
          <div v-for="e in evolucoes" :key="e.id" class="evo-card">
            <div class="eh"><b>{{ e.modeloNome || "Evolução" }}</b><span class="dt">{{ dataCurta(e.criadaEm) }}</span></div>
            <div class="who2">{{ e.autorNome }}</div>
            <div v-if="e.qtdAnexos" class="att"><i class="fa-solid fa-paperclip"></i> {{ e.qtdAnexos }} {{ e.qtdAnexos > 1 ? "anexos" : "anexo" }}</div>
          </div>
        </template>
        <div v-else class="tab-empty"><i class="fa-regular fa-folder-open"></i><p>Sem evoluções registradas.</p></div>
      </div>

      <div v-show="tab === 'pront'" class="fpanel on">
        <div v-if="evolucoes.length">
          <div v-for="e in evolucoes" :key="e.id" class="evo-card">
            <div class="eh"><b>{{ e.modeloNome || "Evolução" }}</b><span class="dt">{{ dataCurta(e.criadaEm) }}</span></div>
            <div class="who2">{{ e.autorNome }}</div>
          </div>
        </div>
        <div v-else class="tab-empty"><i class="fa-regular fa-file-lines"></i><p>Prontuário ainda vazio.</p></div>
        <button v-if="podeProntuario" class="btn-outline" style="margin-top: 6px" @click="router.push(`/paciente/${id}/prontuario`)">
          <i class="fa-solid fa-file-waveform"></i> Abrir prontuário completo
        </button>
      </div>

      <div v-show="tab === 'docs'" class="fpanel on">
        <div class="tab-empty"><i class="fa-regular fa-file"></i><p>Nenhum documento emitido.</p></div>
      </div>

      <div v-show="tab === 'orc'" class="fpanel on">
        <div v-if="orcamentos.length" class="plist">
          <div v-for="o in orcamentos" :key="o.id" class="doc-row" style="cursor: pointer" @click="abrirOrcamento(o)">
            <div class="di" style="background: hsl(var(--warning) / 0.15); color: hsl(35 88% 38%)"><i class="fa-solid fa-file-invoice-dollar"></i></div>
            <div class="dx"><b>Orçamento {{ o.numero }}</b><span>{{ o.status }}</span></div>
            <i class="fa-solid fa-chevron-right" style="color: var(--app-text-faint); font-size: 13px"></i>
          </div>
        </div>
        <div v-else class="tab-empty"><i class="fa-regular fa-file"></i><p>Nenhum orçamento.</p></div>
      </div>

      <template v-if="podeProntuario || podePrescrever">
        <div class="f-label" style="margin-top: 18px">Ações</div>
        <div class="fact-grid">
          <button v-if="podeProntuario" class="fact" @click="acao('evolucao')"><span class="fi ic-violet"><i class="fa-solid fa-plus"></i></span> Evolução</button>
          <button v-if="podePrescrever" class="fact" @click="acao('receita')"><span class="fi ic-violet"><i class="fa-solid fa-prescription"></i></span> Receita</button>
          <button v-if="podePrescrever" class="fact" @click="acao('atestado')"><span class="fi ic-green"><i class="fa-solid fa-file-medical"></i></span> Atestado</button>
          <button v-if="podePrescrever" class="fact" @click="acao('exame')"><span class="fi ic-blue"><i class="fa-solid fa-flask"></i></span> Exame</button>
        </div>
      </template>
      <div class="audit-foot"><i class="fa-solid fa-shield-halved"></i> Este acesso foi registrado em seu nome</div>
    </div>
  </div>
</template>
