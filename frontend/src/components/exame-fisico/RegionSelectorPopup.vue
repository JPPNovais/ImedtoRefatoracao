<script setup lang="ts">
import { ref, computed, reactive } from 'vue'
import { AppModal } from '@/components/ui'
import { AppButton } from '@/components/ui'

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

// Navegação hierárquica
const navegacao = ref<ExameFisicoRegiao[]>([])

// Estado de seleção
const idsSelecionados = ref<string[]>([])
const lateralidades = reactive<Record<string, 'D' | 'E' | 'bilateral'>>({})

const totalSelecionados = computed(() => idsSelecionados.value.length)

const dialogTitle = computed(() => {
  if (!props.membroRegioes || !props.regiaoClicada) return 'Selecionar região'
  return props.regiaoClicada.nome.replace(/\s*(direito|esquerdo)\s+/i, ' ')
})

const regiaoAtual = computed(() => {
  if (navegacao.value.length > 0) {
    return navegacao.value[navegacao.value.length - 1]
  }
  return props.regiaoClicada
})

const filhosAtuais = computed(() => {
  if (!regiaoAtual.value) return []
  return props.getFilhos(regiaoAtual.value.id)
})

const breadcrumb = computed(() => {
  const itens: ExameFisicoRegiao[] = []
  if (props.regiaoClicada) itens.push(props.regiaoClicada)
  itens.push(...navegacao.value)
  return itens
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
  navegacao.value.pop()
}

function voltarParaNivel(index: number) {
  navegacao.value.splice(index)
}

function confirmar() {
  const resultado: Array<{ regiaoId: string; lateralidade: 'D' | 'E' | 'bilateral' | null }> = []

  for (const regiaoId of idsSelecionados.value) {
    const lat = (lateralidades[regiaoId] ?? null) as 'D' | 'E' | 'bilateral' | null

    if (props.membroRegioes && props.membroRegioes.esquBase) {
      const dirChild = props.regioes.find((r) => r.id === regiaoId)
      const esquChild = dirChild
        ? props.getFilhos(props.membroRegioes.esquBase.id).find((r) => r.nome === dirChild.nome)
        : undefined

      if (lat === 'bilateral' && esquChild) {
        resultado.push({ regiaoId, lateralidade: 'D' })
        resultado.push({ regiaoId: esquChild.id, lateralidade: 'E' })
        continue
      }

      if (lat === 'E' && esquChild) {
        resultado.push({ regiaoId: esquChild.id, lateralidade: 'E' })
        continue
      }
    }

    resultado.push({ regiaoId, lateralidade: lat })
  }

  emit('confirmar', resultado)
  fechar()
}

function fechar() {
  navegacao.value = []
  idsSelecionados.value = []
  Object.keys(lateralidades).forEach((k) => delete lateralidades[k])
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
      <!-- Breadcrumb de navegação hierárquica -->
      <div
        v-if="breadcrumb.length > 0 && !membroRegioes"
        class="flex items-center gap-1 text-xs text-muted-foreground flex-wrap -mt-1"
      >
        <template v-for="(item, idx) in breadcrumb" :key="item.id">
          <button
            type="button"
            class="hover:text-foreground transition-colors"
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

          <!-- D/E/Bilateral quando região tem lateralidade -->
          <div
            v-if="regiaoAtual.lateralidade && estaSelecionado(regiaoAtual.id)"
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
            {{ filho.nome }}
          </span>

          <!-- Lateralidade -->
          <div
            v-if="filho.lateralidade && estaSelecionado(filho.id)"
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
            class="text-[10px] text-primary hover:text-primary/80 p-1 h-auto rounded"
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

    <template #rodape>
      <div class="flex items-center gap-2 w-full">
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
      </div>
    </template>
  </AppModal>
</template>
