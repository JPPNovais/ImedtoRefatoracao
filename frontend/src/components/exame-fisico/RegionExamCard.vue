<script setup lang="ts">
import { ref, watch } from 'vue'
import { AppButton } from '@/components/ui'

export interface RegiaoExaminada {
  regiao_id: string
  caminho: string
  lateralidade: 'D' | 'E' | 'bilateral' | null
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
}>()

const isOpen = ref(props.open !== undefined ? props.open : true)

watch(() => props.open, (val) => {
  if (val !== undefined) isOpen.value = val
})

function getLateralidadeLabel(lat: string | null): string {
  if (!lat) return ''
  if (lat === 'D') return 'Direito'
  if (lat === 'E') return 'Esquerdo'
  if (lat === 'bilateral') return 'Bilateral'
  return ''
}
</script>

<template>
  <div class="border border-border rounded-lg border-l-2 border-l-primary/40">
    <!-- Cabeçalho colapsável -->
    <button
      type="button"
      class="w-full flex items-center gap-2 py-2 px-3 hover:bg-muted/30 transition-colors rounded-t-lg text-left"
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
        class="text-[9px] border border-border rounded px-1.5 py-0.5 text-muted-foreground shrink-0"
      >
        {{ getLateralidadeLabel(regiao.lateralidade) }}
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
    <div v-if="isOpen" class="px-3 pb-3 space-y-2">
      <div class="space-y-1">
        <label class="field-label-compact">Exame</label>
        <textarea
          v-model="regiao.texto_exame"
          class="flex w-full rounded-md border border-input bg-background px-3 py-2 text-xs shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:opacity-50 min-h-[80px] resize-y"
          placeholder="Descreva os achados do exame físico desta região..."
          :readonly="readonly"
        />
      </div>
      <div class="space-y-1">
        <label class="field-label-compact">Achados</label>
        <input
          v-model="regiao.achados"
          class="flex h-8 w-full rounded-md border border-input bg-background px-3 text-xs shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:opacity-50"
          placeholder="Ex: Normal, Sem alterações..."
          :readonly="readonly"
        />
      </div>
      <div class="space-y-1">
        <label class="field-label-compact">Observações</label>
        <textarea
          v-model="regiao.observacoes"
          class="flex w-full rounded-md border border-input bg-background px-3 py-2 text-xs shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:opacity-50 min-h-[48px] resize-y"
          placeholder="Observações adicionais..."
          :readonly="readonly"
        />
      </div>
    </div>
  </div>
</template>
