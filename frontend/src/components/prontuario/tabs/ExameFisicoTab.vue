<script setup lang="ts">
import { ref, reactive, computed, onMounted, watch } from 'vue'
import { AppButton } from '@/components/ui'

import BodyMap from '@/components/exame-fisico/BodyMap.vue'
import RegionSelectorPopup from '@/components/exame-fisico/RegionSelectorPopup.vue'
import RegionExamCard from '@/components/exame-fisico/RegionExamCard.vue'
import DadosGeraisForm from '@/components/exame-fisico/DadosGeraisForm.vue'
import ExameFisicoTimeline from '@/components/exame-fisico/ExameFisicoTimeline.vue'
import type { ExameFisicoRegiao } from '@/components/exame-fisico/BodyMap.vue'
import type { RegiaoExaminada } from '@/components/exame-fisico/RegionExamCard.vue'

import {
    exameFisicoService,
    type ExameFisicoResumoDto,
    type DadosGeraisExame,
} from '@/services/exameFisicoService'

const props = defineProps<{
    pacienteId: string | null
    pacienteSexo?: string | null
    evolucaoId?: string | null
    salvando?: boolean
}>()

const emit = defineEmits<{
    salvar: []
}>()

// ─── Estado ────────────────────────────────────────────────────────────────
const regioes = ref<ExameFisicoRegiao[]>([])
const historicoExames = ref<ExameFisicoResumoDto[]>([])
const isLoadingRegioes = ref(false)
const isLoadingHistorico = ref(false)
const isSaving = ref(false)

// Popup de seleção de sub-regiões
const selectorAberto = ref(false)
const regiaoClicada = ref<ExameFisicoRegiao | null>(null)
const membroRegioes = ref<{ tipo: 'superior' | 'inferior'; dirBase: ExameFisicoRegiao | null; esquBase: ExameFisicoRegiao | null } | null>(null)

// Estado colapsável dos cards de regiões
const cardOpenStates = ref<boolean[]>([])

// Formulário de novo exame
const exameAtual = reactive<{
    dados_gerais: DadosGeraisExame
    regioes_examinadas: RegiaoExaminada[]
    observacoes: string
}>({
    dados_gerais: criarDadosGeraisVazios(),
    regioes_examinadas: [],
    observacoes: '',
})

function criarDadosGeraisVazios(): DadosGeraisExame {
    return {
        sinais_vitais: { pa_sistolica: '', pa_diastolica: '', fc: '', fr: '', temp: '', spo2: '', glicemia: '' },
        antropometria: { peso: '', altura: '', imc: '', imc_classificacao: '' },
        ectoscopia: {
            estado_geral: '', consciencia: '', estado_nutricional: '', coloracao: '',
            hidratacao: '', cianose: '', ictericia: '', temperatura_estado: '',
            batimentos_cardiacos: '', respiracao: '', descricao: '',
        },
    }
}

// ─── Regiões ───────────────────────────────────────────────────────────────
const regioesNivel1 = computed(() => regioes.value.filter((r) => r.nivel === 1))

function getFilhos(regiaoId: string): ExameFisicoRegiao[] {
    return regioes.value
        .filter((r) => r.pai_id === regiaoId)
        .sort((a, b) => (a as ExameFisicoRegiao & { ordem?: number }).ordem ?? 0 - ((b as ExameFisicoRegiao & { ordem?: number }).ordem ?? 0))
}

function getCaminho(regiaoId: string): string {
    const partes: string[] = []
    let atual: ExameFisicoRegiao | undefined = regioes.value.find((r) => r.id === regiaoId)
    while (atual) {
        partes.unshift(atual.nome)
        atual = atual.pai_id ? regioes.value.find((r) => r.id === atual!.pai_id) : undefined
    }
    return partes.join(' > ')
}

function getTemplate(regiaoId: string): string {
    const regiao = regioes.value.find((r) => r.id === regiaoId)
    return regiao?.template_texto || 'Inspeção: ___. Palpação: ___. Achados: ___.'
}

function getAncestorNivel1Id(regiaoId: string): string | null {
    let atual = regioes.value.find((r) => r.id === regiaoId)
    while (atual) {
        if (atual.nivel === 1) return atual.id
        atual = atual.pai_id ? regioes.value.find((r) => r.id === atual!.pai_id) : undefined
    }
    return null
}

// IDs examinados (para highlight no mapa)
const regioesJaSelecionadas = computed(() => {
    const ids = new Set<string>()
    for (const r of exameAtual.regioes_examinadas) {
        ids.add(r.regiao_id)
        const nivel1Id = getAncestorNivel1Id(r.regiao_id)
        if (nivel1Id) ids.add(nivel1Id)
    }
    return Array.from(ids)
})

// ─── Load ──────────────────────────────────────────────────────────────────
async function loadRegioes() {
    isLoadingRegioes.value = true
    try {
        regioes.value = await exameFisicoService.listarRegioes(undefined, true)
    } catch {
        // silencioso — sem regiões o mapa continua funcionando
    } finally {
        isLoadingRegioes.value = false
    }
}

async function loadHistorico() {
    if (!props.pacienteId) return
    isLoadingHistorico.value = true
    try {
        historicoExames.value = await exameFisicoService.listarTimeline(props.pacienteId, 20)
    } catch {
        // silencioso
    } finally {
        isLoadingHistorico.value = false
    }
}

onMounted(() => {
    Promise.all([loadRegioes(), loadHistorico()])
})

// ─── Cards colapsáveis ─────────────────────────────────────────────────────
watch(() => exameAtual.regioes_examinadas.length, (len) => {
    while (cardOpenStates.value.length < len) cardOpenStates.value.push(true)
    cardOpenStates.value = cardOpenStates.value.slice(0, len)
}, { immediate: true })

function expandAll() {
    cardOpenStates.value = exameAtual.regioes_examinadas.map(() => true)
}
function collapseAll() {
    cardOpenStates.value = exameAtual.regioes_examinadas.map(() => false)
}

// ─── Handlers de mapa ──────────────────────────────────────────────────────
const MEMBRO_RE = /^Membro (superior|inferior) (?:direito|esquerdo) \((anterior|posterior)\)$/i

function onRegiaoClicada(regiao: ExameFisicoRegiao) {
    const m = MEMBRO_RE.exec(regiao.nome)
    if (m) {
        const tipo = m[1] as 'superior' | 'inferior'
        const vista = m[2]
        const base = `Membro ${tipo}`
        const dirBase = regioesNivel1.value.find((r) => r.nome === `${base} direito (${vista})`) ?? null
        const esquBase = regioesNivel1.value.find((r) => r.nome === `${base} esquerdo (${vista})`) ?? null
        membroRegioes.value = { tipo, dirBase, esquBase }
        regiaoClicada.value = dirBase
    } else {
        membroRegioes.value = null
        regiaoClicada.value = regiao
    }
    selectorAberto.value = true
}

function onConfirmarRegioes(
    selecoes: Array<{ regiaoId: string; lateralidade: 'D' | 'E' | 'bilateral' | null }>,
) {
    for (const sel of selecoes) {
        const jaSelecionada = exameAtual.regioes_examinadas.some(
            (r) => r.regiao_id === sel.regiaoId && r.lateralidade === sel.lateralidade,
        )
        if (jaSelecionada) continue

        exameAtual.regioes_examinadas.push({
            regiao_id: sel.regiaoId,
            caminho: getCaminho(sel.regiaoId),
            lateralidade: sel.lateralidade,
            texto_exame: getTemplate(sel.regiaoId),
            achados: '',
            observacoes: '',
            timestamp: new Date().toISOString(),
        })
    }
}

function removerRegiao(index: number) {
    exameAtual.regioes_examinadas.splice(index, 1)
}

// Mapeia lateralidade do backend ("Esquerda"/"Direita"/"Bilateral") para o modelo local
const LATERALIDADE_BACKEND_PARA_LOCAL: Record<string, 'D' | 'E' | 'bilateral'> = {
    Direita: 'D',
    Esquerda: 'E',
    Bilateral: 'bilateral',
}

// Mapeia lateralidade do modelo local para o backend
const LATERALIDADE_LOCAL_PARA_BACKEND: Record<string, string> = {
    D: 'Direita',
    E: 'Esquerda',
    bilateral: 'Bilateral',
}

// ─── Ações ─────────────────────────────────────────────────────────────────
async function onDuplicarExame(exame: ExameFisicoResumoDto) {
    try {
        const exameCompleto = await exameFisicoService.obterPorId(exame.id)
        if (!exameCompleto) return

        // dadosGeraisJson é string serializada no backend
        if (exameCompleto.dadosGeraisJson) {
            Object.assign(exameAtual.dados_gerais, JSON.parse(exameCompleto.dadosGeraisJson))
        }

        // Converte regiões do backend (RegiaoExameFisicoDto) para o modelo local (RegiaoExaminada)
        const regioesConvertidas = exameCompleto.regioes.map((r, idx) => ({
            regiao_id: r.regiaoCodigo,
            caminho: getCaminho(r.regiaoCodigo),
            lateralidade: r.lateralidade
                ? (LATERALIDADE_BACKEND_PARA_LOCAL[r.lateralidade] ?? null)
                : null,
            texto_exame: getTemplate(r.regiaoCodigo),
            achados: r.achados ?? '',
            observacoes: '',
            timestamp: new Date(idx).toISOString(),  // marcador de ordem
        }))
        exameAtual.regioes_examinadas.splice(0, exameAtual.regioes_examinadas.length, ...regioesConvertidas)

        exameAtual.observacoes = exameCompleto.observacoesGerais ?? ''
    } catch {
        // erro tratado pelo interceptor do httpClient
    }
}

function resetForm() {
    Object.assign(exameAtual.dados_gerais, criarDadosGeraisVazios())
    exameAtual.regioes_examinadas.splice(0)
    exameAtual.observacoes = ''
}

async function handleSalvar() {
    if (!props.evolucaoId) {
        emit('salvar')
        return
    }
    isSaving.value = true
    try {
        // dadosGeraisJson deve ser string serializada, não o objeto
        const dadosGeraisStr = JSON.stringify(exameAtual.dados_gerais)

        await exameFisicoService.registrar(props.evolucaoId, {
            dadosGeraisJson: dadosGeraisStr,
            observacoesGerais: exameAtual.observacoes || undefined,
            regioes: exameAtual.regioes_examinadas.map((r, idx) => {
                // Busca paiCodigo no catálogo para enviar ao backend
                const regiaoNocat = regioes.value.find((cat) => cat.id === r.regiao_id)
                return {
                    codigo: r.regiao_id,
                    paiCodigo: regiaoNocat?.pai_id ?? null,
                    lateralidade: r.lateralidade
                        ? (LATERALIDADE_LOCAL_PARA_BACKEND[r.lateralidade] ?? null)
                        : null,
                    achados: r.achados || undefined,
                    severidade: 'Normal',
                    ordem: idx,
                }
            }),
        })
        resetForm()
        await loadHistorico()
        emit('salvar')
    } catch {
        // erro tratado pelo interceptor do httpClient
    } finally {
        isSaving.value = false
    }
}

function scrollTo(id: string) {
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' })
}
</script>

<template>
  <div class="grid grid-cols-1 lg:grid-cols-[260px_1fr] gap-4">
    <!-- Coluna lateral -->
    <div class="lg:sticky lg:top-4 lg:self-start space-y-3">
      <!-- Navegação rápida -->
      <div class="border border-border rounded-lg overflow-hidden bg-card">
        <div class="bg-muted/50 px-3 py-2 text-[10px] font-semibold text-muted-foreground uppercase tracking-wider">
          Navegação
        </div>
        <div class="divide-y divide-border">
          <button
            type="button"
            class="w-full flex items-center gap-2 px-3 py-2 text-[11px] text-muted-foreground hover:text-foreground hover:bg-muted/30 transition-colors text-left"
            @click="scrollTo('exame-topo')"
          >
            <i class="fa-solid fa-arrow-up text-[9px] text-primary/60 w-3" />
            Voltar ao topo
          </button>
          <button
            type="button"
            class="w-full flex items-center gap-2 px-3 py-2 text-[11px] text-muted-foreground hover:text-foreground hover:bg-muted/30 transition-colors text-left"
            @click="scrollTo('mapa-corporal')"
          >
            <i class="fa-solid fa-person text-[9px] text-primary/60 w-3" />
            Mapa corporal
          </button>
          <button
            v-if="exameAtual.regioes_examinadas.length > 0"
            type="button"
            class="w-full flex items-center gap-2 px-3 py-2 text-[11px] text-muted-foreground hover:text-foreground hover:bg-muted/30 transition-colors text-left"
            @click="scrollTo('regioes-examinadas')"
          >
            <i class="fa-solid fa-list-check text-[9px] text-primary/60 w-3" />
            Regiões examinadas
            <span class="ml-auto text-[9px] border border-border rounded px-1.5 py-0.5">
              {{ exameAtual.regioes_examinadas.length }}
            </span>
          </button>
        </div>
      </div>

      <!-- Timeline do histórico -->
      <ExameFisicoTimeline
        :exames="historicoExames"
        :is-loading="isLoadingHistorico"
        @duplicar="onDuplicarExame"
      />
    </div>

    <!-- Coluna principal -->
    <div class="space-y-4">
      <!-- Header -->
      <div id="exame-topo" class="flex items-center justify-between scroll-mt-4">
        <h3 class="text-sm font-semibold text-foreground flex items-center gap-2">
          <i class="fa-solid fa-stethoscope text-primary text-[11px]" />
          Novo exame físico
        </h3>
        <AppButton
          size="sm"
          :loading="isSaving || salvando"
          :disabled="isSaving || salvando"
          @click="handleSalvar"
        >
          {{ isSaving || salvando ? 'Salvando...' : 'Salvar exame' }}
        </AppButton>
      </div>

      <!-- Dados gerais (sinais vitais + antropometria + ectoscopia) -->
      <DadosGeraisForm :dados-gerais="exameAtual.dados_gerais" />

      <!-- Mapa corporal -->
      <div id="mapa-corporal" class="border border-border rounded-lg overflow-hidden bg-card scroll-mt-4">
        <div class="px-4 py-3 border-b border-border">
          <h4 class="text-xs font-semibold flex items-center gap-2">
            <i class="fa-solid fa-person text-[10px] text-primary" />
            Mapa corporal
            <span
              v-if="isLoadingRegioes"
              class="text-[10px] font-normal text-muted-foreground"
            >
              Carregando regiões...
            </span>
            <span v-else class="text-[10px] font-normal text-muted-foreground">
              Clique em uma região para examinar
            </span>
          </h4>
        </div>
        <div class="p-4">
          <BodyMap
            :regioes="regioesNivel1"
            :regioes-examinadas="regioesJaSelecionadas"
            :sexo="pacienteSexo"
            @regiao-clicada="onRegiaoClicada"
          />
        </div>
      </div>

      <!-- Regiões selecionadas -->
      <div
        v-if="exameAtual.regioes_examinadas.length > 0"
        id="regioes-examinadas"
        class="space-y-2 scroll-mt-4"
      >
        <div class="flex items-center justify-between">
          <h4 class="text-xs font-semibold text-foreground flex items-center gap-2">
            <span class="h-1.5 w-1.5 rounded-full bg-primary" />
            Regiões examinadas ({{ exameAtual.regioes_examinadas.length }})
          </h4>
          <div class="flex items-center gap-1">
            <button
              type="button"
              class="text-[10px] text-muted-foreground hover:text-foreground px-2 py-0.5 rounded transition-colors"
              @click="expandAll"
            >
              Expandir todos
            </button>
            <span class="text-muted-foreground/40 text-[10px]">|</span>
            <button
              type="button"
              class="text-[10px] text-muted-foreground hover:text-foreground px-2 py-0.5 rounded transition-colors"
              @click="collapseAll"
            >
              Recolher todos
            </button>
          </div>
        </div>

        <RegionExamCard
          v-for="(regiao, idx) in exameAtual.regioes_examinadas"
          :key="idx"
          :regiao="regiao"
          :index="idx"
          :open="cardOpenStates[idx] !== false"
          @remover="removerRegiao"
          @update:open="cardOpenStates[idx] = $event"
        />
      </div>

      <!-- Observações gerais -->
      <div class="border border-border rounded-lg overflow-hidden bg-card">
        <div class="p-4 space-y-1">
          <label class="field-label-compact">Observações gerais</label>
          <textarea
            v-model="exameAtual.observacoes"
            class="flex w-full rounded-md border border-input bg-background px-3 py-2 text-xs shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring min-h-[60px] resize-y"
            placeholder="Observações gerais do exame físico..."
          />
        </div>
      </div>

      <!-- Ações -->
      <div class="flex justify-end gap-3">
        <AppButton
          variant="ghost"
          size="sm"
          :disabled="isSaving"
          @click="resetForm"
        >
          Cancelar
        </AppButton>
        <AppButton
          size="sm"
          :loading="isSaving || salvando"
          :disabled="isSaving || salvando"
          @click="handleSalvar"
        >
          {{ isSaving || salvando ? 'Salvando...' : 'Salvar exame físico' }}
        </AppButton>
      </div>
    </div>
  </div>

  <!-- Popup de seleção de sub-regiões -->
  <RegionSelectorPopup
    v-model:aberto="selectorAberto"
    :regiao-clicada="regiaoClicada"
    :regioes="regioes"
    :regioes-ja-selecionadas="regioesJaSelecionadas"
    :get-filhos="getFilhos"
    :membro-regioes="membroRegioes"
    @confirmar="onConfirmarRegioes"
  />
</template>
