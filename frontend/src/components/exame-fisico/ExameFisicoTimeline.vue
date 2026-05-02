<script setup lang="ts">
import { AppButton } from '@/components/ui'
import type { ExameFisicoResumoDto } from '@/services/exameFisicoService'

defineProps<{
  exames: ExameFisicoResumoDto[]
  isLoading: boolean
}>()

const emit = defineEmits<{
  visualizar: [exame: ExameFisicoResumoDto]
  duplicar: [exame: ExameFisicoResumoDto]
}>()

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('pt-BR', {
    day: '2-digit', month: '2-digit', year: 'numeric',
  })
}

function formatTime(dateStr: string): string {
  return new Date(dateStr).toLocaleTimeString('pt-BR', {
    hour: '2-digit', minute: '2-digit',
  })
}

const SEVERIDADE_LABEL: Record<string, string> = {
  LeveAlteracao: 'leve alteração',
  Alterado: 'alterado',
  Critico: 'crítico',
  Normal: 'normal',
}

function getRegioesResumo(exame: ExameFisicoResumoDto): string {
  if (exame.totalRegioes === 0) return 'Sem regiões específicas'
  const label = exame.severidadeMaxima ? ` — ${SEVERIDADE_LABEL[exame.severidadeMaxima] ?? exame.severidadeMaxima}` : ''
  return `${exame.totalRegioes} ${exame.totalRegioes === 1 ? 'região' : 'regiões'}${label}`
}
</script>

<template>
  <div class="space-y-2">
    <h4 class="text-xs font-semibold text-foreground flex items-center gap-2">
      <i class="fa-solid fa-clock-rotate-left text-[10px] text-muted-foreground" />
      Histórico
      <span
        v-if="exames.length > 0"
        class="text-[9px] border border-border rounded px-1.5 py-0.5 text-muted-foreground ml-auto"
      >
        {{ exames.length }}
      </span>
    </h4>

    <!-- Loading -->
    <div v-if="isLoading" class="space-y-2">
      <div v-for="i in 3" :key="i" class="h-20 w-full rounded-lg bg-muted animate-pulse" />
    </div>

    <!-- Lista vazia -->
    <p
      v-else-if="exames.length === 0"
      class="text-[11px] text-muted-foreground text-center py-6"
    >
      Nenhum exame físico registrado.
    </p>

    <!-- Timeline -->
    <div v-else class="space-y-2 max-h-[calc(100vh-300px)] overflow-y-auto pr-1">
      <div
        v-for="exame in exames"
        :key="exame.id"
        class="border border-border rounded-lg border-l-2 border-l-primary/30 hover:border-l-primary/60 transition-colors p-3 space-y-2 bg-card"
      >
        <div class="flex items-start justify-between gap-2">
          <div class="space-y-0.5">
            <p class="text-[11px] font-semibold text-foreground">
              {{ formatDate(exame.realizadoEm) }}
              <span class="font-normal text-muted-foreground">
                {{ formatTime(exame.realizadoEm) }}
              </span>
            </p>
            <p class="text-[10px] text-muted-foreground">
              {{ exame.realizadoPorNome }}
            </p>
          </div>
          <span
            v-if="exame.evolucaoId"
            class="text-[8px] border border-border rounded px-1.5 py-0.5 text-muted-foreground shrink-0"
          >
            Vinculado
          </span>
        </div>

        <p class="text-[10px] text-muted-foreground/80 line-clamp-2">
          {{ getRegioesResumo(exame) }}
        </p>

        <div class="flex items-center gap-1">
          <AppButton
            variant="ghost"
            size="sm"
            class="h-6 text-[10px] px-2"
            @click="emit('visualizar', exame)"
          >
            <i class="fa-solid fa-eye mr-1 text-[8px]" />
            Ver
          </AppButton>
          <AppButton
            variant="ghost"
            size="sm"
            class="h-6 text-[10px] px-2"
            @click="emit('duplicar', exame)"
          >
            <i class="fa-solid fa-copy mr-1 text-[8px]" />
            Duplicar
          </AppButton>
        </div>
      </div>
    </div>
  </div>
</template>
