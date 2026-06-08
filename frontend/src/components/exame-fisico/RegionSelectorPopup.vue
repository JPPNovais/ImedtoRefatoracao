<script setup lang="ts">
import { ref, computed, reactive } from 'vue'
import { AppModal } from '@/components/ui'
import { AppButton } from '@/components/ui'
import { AppPillToggle } from '@/components/ui'

export interface ExameFisicoRegiao {
  id: string
  nome: string
  nivel: 1 | 2 | 3
  lateralidade: boolean
  pai_id: string | null
  vista: 'anterior' | 'posterior' | 'ambos' | null
  template_texto: string | null
  filhos?: ExameFisicoRegiao[]
}

export interface MembroRegioes {
  tipo: 'superior' | 'inferior'
  dirBase: ExameFisicoRegiao | null
  esquBase: ExameFisicoRegiao | null
}

type LadoMembro = 'D' | 'E' | 'bilateral'

const OPCOES_LADO = [
  { valor: 'D' as LadoMembro,        label: 'Direito'  },
  { valor: 'E' as LadoMembro,        label: 'Esquerdo' },
  { valor: 'bilateral' as LadoMembro, label: 'Ambos'   },
]

const props = defineProps<{
  aberto: boolean
  regiaoClicada: ExameFisicoRegiao | null
  regioes: ExameFisicoRegiao[]
  regioesJaSelecionadas: string[]
  getFilhos: (regiaoId: string) => ExameFisicoRegiao[]
  membroRegioes?: MembroRegioes | null
}>()

const emit = defineEmits<{
  'update:aberto': [value: boolean]
  confirmar: [regioes: Array<{ regiaoId: string; lateralidade: 'D' | 'E' | 'bilateral' | null }>]
}>()

// ── Estado de passo (só relevante quando membroRegioes != null) ───────────────
const passo = ref<'lado' | 'subregioes'>('lado')
const ladoEscolhido = ref<LadoMembro | null>(null)

// ── Base ativa depende do lado (R4) ──────────────────────────────────────────
const baseAtiva = computed<ExameFisicoRegiao | null>(() => {
  if (!props.membroRegioes) return props.regiaoClicada
  if (ladoEscolhido.value === 'E') return props.membroRegioes.esquBase
  // Direito e Ambos usam dirBase como base canônica (R4/R5)
  return props.membroRegioes.dirBase
})

// ── Navegação hierárquica ─────────────────────────────────────────────────────
const navegacao = ref<ExameFisicoRegiao[]>([])

// ── Estado de seleção ─────────────────────────────────────────────────────────
const idsSelecionados = ref<string[]>([])
const lateralidades = reactive<Record<string, 'D' | 'E' | 'bilateral'>>({})

const totalSelecionados = computed(() => idsSelecionados.value.length)

// Título sem "direito/esquerdo" para membros
const dialogTitle = computed(() => {
  if (!props.membroRegioes || !props.regiaoClicada) return 'Selecionar região'
  return props.regiaoClicada.nome.replace(/\s*(direito|esquerdo)\s+/i, ' ')
})

const regiaoAtual = computed(() => {
  if (navegacao.value.length > 0) {
    return navegacao.value[navegacao.value.length - 1]
  }
  return baseAtiva.value
})

const filhosAtuais = computed(() => {
  if (!regiaoAtual.value) return []
  return props.getFilhos(regiaoAtual.value.id)
})

// Breadcrumb usa baseAtiva como raiz (não regiaoClicada fixa)
const breadcrumb = computed(() => {
  const itens: ExameFisicoRegiao[] = []
  const raiz = props.membroRegioes ? baseAtiva.value : props.regiaoClicada
  if (raiz) itens.push(raiz)
  itens.push(...navegacao.value)
  return itens
})

// Label do lado escolhido para badge no passo de sub-regiões
const labelLado = computed(() => {
  if (!ladoEscolhido.value) return ''
  if (ladoEscolhido.value === 'D') return 'Direito'
  if (ladoEscolhido.value === 'E') return 'Esquerdo'
  return 'Ambos (bilateral)'
})

function jaFoiSelecionada(regiaoId: string): boolean {
  return props.regioesJaSelecionadas.includes(regiaoId)
}

function temFilhos(regiaoId: string): boolean {
  return props.getFilhos(regiaoId).length > 0
}

function estaSelecionado(regiaoId: string): boolean {
  return idsSelecionados.value.includes(regiaoId)
}

function toggleRegiao(regiao: ExameFisicoRegiao) {
  const idx = idsSelecionados.value.indexOf(regiao.id)
  if (idx >= 0) {
    idsSelecionados.value.splice(idx, 1)
    delete lateralidades[regiao.id]
  } else {
    idsSelecionados.value.push(regiao.id)
  }
}

function setLateralidade(regiaoId: string, lat: 'D' | 'E' | 'bilateral') {
  lateralidades[regiaoId] = lat
}

function navegarPara(regiao: ExameFisicoRegiao) {
  navegacao.value.push(regiao)
}

function voltarNivel() {
  if (props.membroRegioes && navegacao.value.length === 0) {
    // No raiz do passo de sub-regiões de membro: Voltar → passo de lado (R6)
    limparSelecao()
    passo.value = 'lado'
  } else {
    navegacao.value.pop()
  }
}

function voltarParaNivel(index: number) {
  navegacao.value.splice(index)
}

// Avança do passo de lado para sub-regiões ao escolher o lado (R2)
function escolherLado(lado: LadoMembro) {
  ladoEscolhido.value = lado
  limparSelecao()
  passo.value = 'subregioes'
}

// Exibe nome do filho sem sufixo "direito" quando no contexto de membro
function nomeExibido(filho: ExameFisicoRegiao): string {
  if (!props.membroRegioes) return filho.nome
  return filho.nome.replace(/\s+direito\b/gi, '').replace(/\s+D\b/, '').trim()
}

function confirmar() {
  const resultado: Array<{ regiaoId: string; lateralidade: 'D' | 'E' | 'bilateral' | null }> = []

  for (const regiaoId of idsSelecionados.value) {
    if (props.membroRegioes && ladoEscolhido.value) {
      // Fluxo de membro: lateralidade derivada do lado escolhido no passo 1 (R3/R4/R5)
      resultado.push({ regiaoId, lateralidade: ladoEscolhido.value })
    } else {
      // Fluxo não-lateral: lateralidade por sub-região (comportamento original)
      const lat = (lateralidades[regiaoId] ?? null) as 'D' | 'E' | 'bilateral' | null
      resultado.push({ regiaoId, lateralidade: lat })
    }
  }

  emit('confirmar', resultado)
  fechar()
}

function limparSelecao() {
  navegacao.value = []
  idsSelecionados.value = []
  Object.keys(lateralidades).forEach((k) => delete lateralidades[k])
}

function fechar() {
  limparSelecao()
  passo.value = 'lado'
  ladoEscolhido.value = null
  emit('update:aberto', false)
}
</script>

<template>
  <AppModal
    :aberto="aberto"
    :titulo="dialogTitle"
    largura="sm"
    @fechar="fechar"
  >
    <template #default>
      <!-- ── PASSO 1: Escolha do lado (apenas para membros) ─────────────────── -->
      <template v-if="membroRegioes && passo === 'lado'">
        <p class="text-xs text-muted-foreground mb-3">
          Selecione o lado a examinar:
        </p>
        <div class="flex justify-center">
          <AppPillToggle
            model-value=""
            :opcoes="OPCOES_LADO"
            @update:model-value="escolherLado($event as LadoMembro)"
          />
        </div>
      </template>

      <!-- ── PASSO 2: Seleção de sub-regiões ───────────────────────────────── -->
      <template v-else>
        <!-- Badge do lado escolhido (somente membros) -->
        <div
          v-if="membroRegioes && ladoEscolhido"
          class="flex items-center gap-1.5 -mt-1 mb-1"
        >
          <span class="text-[10px] text-muted-foreground">Lado:</span>
          <span class="text-[10px] border border-border rounded px-1.5 py-0.5 text-foreground font-medium">
            {{ labelLado }}
          </span>
        </div>

        <!-- Breadcrumb de navegação hierárquica (regiões não-laterais) -->
        <div
          v-if="breadcrumb.length > 0 && !membroRegioes"
          class="flex items-center gap-1 text-xs text-muted-foreground flex-wrap -mt-1"
        >
          <template v-for="(item, idx) in breadcrumb" :key="item.id">
            <button
              type="button"
              class="hover:text-foreground transition-colors border-0"
              :class="idx === breadcrumb.length - 1 ? 'font-semibold text-foreground' : ''"
              @click="idx < breadcrumb.length - 1 ? voltarParaNivel(idx) : undefined"
            >
              {{ item.nome }}
            </button>
            <i
              v-if="idx < breadcrumb.length - 1"
              class="fa-solid fa-chevron-right text-[8px] text-muted-foreground/50"
            />
          </template>
        </div>

        <div class="border-t border-border" />

        <!-- Lista de opções -->
        <div class="space-y-1 max-h-[400px] overflow-y-auto">
          <!-- Opção de selecionar a região atual (geral) -->
          <div
            v-if="regiaoAtual && !jaFoiSelecionada(regiaoAtual.id)"
            class="flex items-center gap-2 p-2 rounded-md hover:bg-muted/50 transition-colors cursor-pointer"
            @click="toggleRegiao(regiaoAtual)"
          >
            <input
              type="checkbox"
              :checked="estaSelecionado(regiaoAtual.id)"
              class="h-4 w-4 rounded border-input text-primary focus:ring-primary shrink-0 cursor-pointer"
              @click.stop
              @change="toggleRegiao(regiaoAtual)"
            />
            <span class="text-xs font-medium flex-1 select-none">
              {{ membroRegioes ? dialogTitle : regiaoAtual.nome }} (geral)
            </span>

            <!-- D/E/Bilateral por sub-região: apenas em regiões não-laterais (R3) -->
            <div
              v-if="!membroRegioes && regiaoAtual.lateralidade && estaSelecionado(regiaoAtual.id)"
              class="flex items-center gap-1"
              @click.stop
            >
              <button
                type="button"
                class="px-1.5 py-0.5 text-[9px] h-auto rounded border transition-colors"
                :class="lateralidades[regiaoAtual.id] === 'D'
                  ? 'bg-primary text-primary-foreground border-primary'
                  : 'border-input text-foreground hover:bg-muted'"
                @click="setLateralidade(regiaoAtual.id, 'D')"
              >D</button>
              <button
                type="button"
                class="px-1.5 py-0.5 text-[9px] h-auto rounded border transition-colors"
                :class="lateralidades[regiaoAtual.id] === 'E'
                  ? 'bg-primary text-primary-foreground border-primary'
                  : 'border-input text-foreground hover:bg-muted'"
                @click="setLateralidade(regiaoAtual.id, 'E')"
              >E</button>
              <button
                type="button"
                class="px-1.5 py-0.5 text-[9px] h-auto rounded border transition-colors"
                :class="lateralidades[regiaoAtual.id] === 'bilateral'
                  ? 'bg-primary text-primary-foreground border-primary'
                  : 'border-input text-foreground hover:bg-muted'"
                @click="setLateralidade(regiaoAtual.id, 'bilateral')"
              >Bilateral</button>
            </div>
          </div>

          <div v-if="filhosAtuais.length > 0" class="border-t border-border" />

          <!-- Sub-regiões -->
          <div
            v-for="filho in filhosAtuais"
            :key="filho.id"
            class="flex items-center gap-2 p-2 rounded-md transition-colors"
            :class="!jaFoiSelecionada(filho.id) ? 'hover:bg-muted/50 cursor-pointer' : ''"
            @click="!jaFoiSelecionada(filho.id) && toggleRegiao(filho)"
          >
            <!-- Checkbox se não foi selecionado antes -->
            <input
              v-if="!jaFoiSelecionada(filho.id)"
              type="checkbox"
              :checked="estaSelecionado(filho.id)"
              class="h-4 w-4 rounded border-input text-primary focus:ring-primary shrink-0 cursor-pointer"
              @click.stop
              @change="toggleRegiao(filho)"
            />
            <i
              v-else
              class="fa-solid fa-check text-[10px] text-success w-4 text-center shrink-0"
            />

            <span
              class="text-xs flex-1 select-none"
              :class="{ 'text-muted-foreground': jaFoiSelecionada(filho.id) }"
            >
              {{ nomeExibido(filho) }}
            </span>

            <!-- D/E/Bilateral: apenas em regiões não-laterais (R3) -->
            <div
              v-if="!membroRegioes && filho.lateralidade && estaSelecionado(filho.id)"
              class="flex items-center gap-1"
              @click.stop
            >
              <button
                type="button"
                class="px-1.5 py-0.5 text-[9px] h-auto rounded border transition-colors"
                :class="lateralidades[filho.id] === 'D'
                  ? 'bg-primary text-primary-foreground border-primary'
                  : 'border-input text-foreground hover:bg-muted'"
                @click="setLateralidade(filho.id, 'D')"
              >D</button>
              <button
                type="button"
                class="px-1.5 py-0.5 text-[9px] h-auto rounded border transition-colors"
                :class="lateralidades[filho.id] === 'E'
                  ? 'bg-primary text-primary-foreground border-primary'
                  : 'border-input text-foreground hover:bg-muted'"
                @click="setLateralidade(filho.id, 'E')"
              >E</button>
              <button
                type="button"
                class="px-1.5 py-0.5 text-[9px] h-auto rounded border transition-colors"
                :class="lateralidades[filho.id] === 'bilateral'
                  ? 'bg-primary text-primary-foreground border-primary'
                  : 'border-input text-foreground hover:bg-muted'"
                @click="setLateralidade(filho.id, 'bilateral')"
              >Bilateral</button>
            </div>

            <!-- Botão drill-down -->
            <button
              v-if="temFilhos(filho.id)"
              type="button"
              class="text-[10px] text-primary hover:text-primary/80 p-1 h-auto rounded border-0"
              title="Ver sub-regiões"
              @click.stop="navegarPara(filho)"
            >
              <i class="fa-solid fa-chevron-right" />
            </button>

            <span
              v-if="jaFoiSelecionada(filho.id)"
              class="text-[9px] border border-border rounded px-1.5 py-0.5 text-muted-foreground"
            >
              Selecionado
            </span>
          </div>

          <p
            v-if="filhosAtuais.length === 0 && regiaoAtual"
            class="text-xs text-muted-foreground text-center py-4"
          >
            Nenhuma sub-região disponível.
          </p>
        </div>
      </template>
    </template>

    <template #rodape>
      <div class="flex items-center gap-2 w-full">
        <!-- Passo de lado: só Cancelar -->
        <template v-if="membroRegioes && passo === 'lado'">
          <div class="flex-1" />
          <AppButton variant="ghost" size="sm" @click="fechar">
            Cancelar
          </AppButton>
        </template>

        <!-- Passo de sub-regiões de membro: Voltar + Cancelar + Confirmar -->
        <template v-else-if="membroRegioes && passo === 'subregioes'">
          <AppButton variant="ghost" size="sm" @click="voltarNivel">
            <i class="fa-solid fa-arrow-left mr-1 text-[10px]" />
            Voltar
          </AppButton>
          <div class="flex-1" />
          <AppButton variant="ghost" size="sm" @click="fechar">
            Cancelar
          </AppButton>
          <AppButton
            size="sm"
            :disabled="totalSelecionados === 0"
            @click="confirmar"
          >
            Confirmar ({{ totalSelecionados }})
          </AppButton>
        </template>

        <!-- Região não-lateral: comportamento original -->
        <template v-else>
          <AppButton
            v-if="navegacao.length > 0"
            variant="ghost"
            size="sm"
            @click="voltarNivel"
          >
            <i class="fa-solid fa-arrow-left mr-1 text-[10px]" />
            Voltar
          </AppButton>
          <div class="flex-1" />
          <AppButton variant="ghost" size="sm" @click="fechar">
            Cancelar
          </AppButton>
          <AppButton
            size="sm"
            :disabled="totalSelecionados === 0"
            @click="confirmar"
          >
            Confirmar ({{ totalSelecionados }})
          </AppButton>
        </template>
      </div>
    </template>
  </AppModal>
</template>
