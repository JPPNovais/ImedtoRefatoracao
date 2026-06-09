<script setup lang="ts">
import { ref, watch } from 'vue'
import { AppButton } from '@/components/ui'

export interface RegiaoExaminada {
  regiao_id: string
  caminho: string
  lateralidade: 'D' | 'E' | 'bilateral' | 'misto' | null
  /** Vista anatômica resolvida (anterior/posterior/circunferencial). Não vai no payload de persistência. */
  vista?: 'anterior' | 'posterior' | 'circunferencial' | null
  texto_exame: string
  achados: string
  observacoes: string
  timestamp: string
}

const props = defineProps<{
  regiao: RegiaoExaminada
  index: number
  readonly?: boolean
  open?: boolean
}>()

const emit = defineEmits<{
  remover: [index: number]
  'update:open': [value: boolean]
  /** Atualiza um campo da região via patch. O pai aplica no array via splice. */
  atualizar: [payload: { index: number; patch: Partial<RegiaoExaminada> }]
}>()

function atualizarCampo(patch: Partial<RegiaoExaminada>) {
  emit('atualizar', { index: props.index, patch })
}

const isOpen = ref(props.open !== undefined ? props.open : true)

watch(() => props.open, (val) => {
  if (val !== undefined) isOpen.value = val
})

function getLateralidadeLabel(lat: string | null): string {
  if (!lat) return ''
  if (lat === 'D') return 'Direito'
  if (lat === 'E') return 'Esquerdo'
  if (lat === 'bilateral') return 'Bilateral'
  if (lat === 'misto') return 'Vários lados'
  return ''
}

function getVistaLabel(vista: string | null | undefined): string {
  if (!vista) return ''
  if (vista === 'anterior') return 'Anterior'
  if (vista === 'posterior') return 'Posterior'
  if (vista === 'circunferencial') return 'Circunferencial'
  return ''
}
</script>

<template>
  <div class="border border-border rounded-lg">
    <!-- Cabeçalho colapsável -->
    <button
      type="button"
      class="rec-header w-full flex items-center gap-2 py-2 px-3 transition-colors rounded-t-lg text-left focus:outline-none focus-visible:ring-1 focus-visible:ring-ring"
      :class="{ 'rec-header--open': isOpen }"
      @click="isOpen = !isOpen; emit('update:open', isOpen)"
    >
      <i
        class="fa-solid fa-chevron-down text-[8px] text-muted-foreground transition-transform"
        :class="{ 'rotate-180': isOpen }"
      />
      <span class="text-[11px] font-medium text-foreground flex-1 truncate">
        {{ regiao.caminho }}
      </span>
      <span
        v-if="regiao.lateralidade"
        class="text-[9px] border border-primary/40 rounded px-1.5 py-0.5 bg-primary/[0.08] text-primary shrink-0"
      >
        {{ getLateralidadeLabel(regiao.lateralidade) }}
      </span>
      <span
        v-if="getVistaLabel(regiao.vista)"
        class="text-[9px] border border-secondary/60 rounded px-1.5 py-0.5 bg-secondary/[0.08] text-secondary-foreground shrink-0"
      >
        {{ getVistaLabel(regiao.vista) }}
      </span>
      <AppButton
        v-if="!readonly"
        variant="ghost"
        size="sm"
        class="h-5 w-5 p-0 text-muted-foreground hover:text-destructive shrink-0"
        @click.stop="emit('remover', index)"
      >
        <i class="fa-solid fa-xmark text-[10px]" />
      </AppButton>
    </button>

    <!-- Conteúdo expansível -->
    <div v-if="isOpen" class="px-3 pb-3 pt-3 space-y-2">
      <div class="space-y-1">
        <label class="field-label-compact">Exame</label>
        <textarea
          :value="regiao.texto_exame"
          class="flex w-full rounded-md border border-input bg-background px-3 py-2 text-xs shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:opacity-50 min-h-[80px] resize-y"
          placeholder="Descreva os achados do exame físico desta região..."
          :readonly="readonly"
          @input="(e) => atualizarCampo({ texto_exame: (e.target as HTMLTextAreaElement).value })"
        />
      </div>
      <div class="space-y-1">
        <label class="field-label-compact">Achados</label>
        <input
          :value="regiao.achados"
          class="flex h-8 w-full rounded-md border border-input bg-background px-3 text-xs shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:opacity-50"
          placeholder="Ex: Normal, Sem alterações..."
          :readonly="readonly"
          @input="(e) => atualizarCampo({ achados: (e.target as HTMLInputElement).value })"
        />
      </div>
      <div class="space-y-1">
        <label class="field-label-compact">Observações</label>
        <textarea
          :value="regiao.observacoes"
          class="flex w-full rounded-md border border-input bg-background px-3 py-2 text-xs shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:opacity-50 min-h-[48px] resize-y"
          placeholder="Observações adicionais..."
          :readonly="readonly"
          @input="(e) => atualizarCampo({ observacoes: (e.target as HTMLTextAreaElement).value })"
        />
      </div>
    </div>
  </div>
</template>

<style scoped>
.rec-header {
  border: none;
  background: hsl(var(--primary) / 0.06);
}
.rec-header:hover {
  background: hsl(var(--primary) / 0.10);
}
.rec-header--open {
  border-bottom: 1px solid hsl(var(--secondary) / 0.06);
}
</style>
