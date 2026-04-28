<script setup lang="ts">
import { computed } from "vue"
import { cn } from "@/utils/cn"
import type { HTMLAttributes } from "vue"

const props = withDefaults(defineProps<{
  pagina: number
  tamanho: number
  total: number
  tamanhos?: number[]
  rotuloItens?: string
  ocultarTamanhos?: boolean
  class?: HTMLAttributes["class"]
}>(), {
  tamanhos: () => [10, 20, 30],
  rotuloItens: "itens",
  ocultarTamanhos: false,
})

const emit = defineEmits<{
  "update:pagina":  [number]
  "update:tamanho": [number]
}>()

const totalPaginas = computed(() => {
  if (props.total <= 0) return 1
  return Math.max(1, Math.ceil(props.total / props.tamanho))
})

const inicio = computed(() => {
  if (props.total === 0) return 0
  return (props.pagina - 1) * props.tamanho + 1
})
const fim = computed(() => Math.min(props.pagina * props.tamanho, props.total))

const paginasMostradas = computed<(number | string)[]>(() => {
  const total = totalPaginas.value
  const atual = props.pagina
  if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1)

  const arr: (number | string)[] = [1]
  if (atual > 3) arr.push("…")
  for (let i = Math.max(2, atual - 1); i <= Math.min(total - 1, atual + 1); i++) {
    arr.push(i)
  }
  if (atual < total - 2) arr.push("…")
  arr.push(total)
  return arr
})

function irPara(p: number | string) {
  if (typeof p !== "number") return
  const destino = Math.max(1, Math.min(totalPaginas.value, p))
  if (destino !== props.pagina) emit("update:pagina", destino)
}
function primeiraPagina() { if (props.pagina !== 1)                emit("update:pagina", 1) }
function paginaAnterior() { if (props.pagina > 1)                  emit("update:pagina", props.pagina - 1) }
function proximaPagina()  { if (props.pagina < totalPaginas.value)  emit("update:pagina", props.pagina + 1) }
function ultimaPagina()   { if (props.pagina !== totalPaginas.value) emit("update:pagina", totalPaginas.value) }

function trocarTamanho(e: Event) {
  const v = +(e.target as HTMLSelectElement).value
  if (!Number.isFinite(v) || v <= 0) return
  emit("update:tamanho", v)
  if (props.pagina !== 1) emit("update:pagina", 1)
}
</script>

<template>
  <nav
    v-if="total > 0"
    :class="cn('flex flex-wrap items-center justify-between gap-3 border-t border-border pt-3', props.class)"
    aria-label="Paginação"
  >
    <!-- Info + seletor de tamanho -->
    <div class="flex flex-wrap items-center gap-4 text-sm text-muted-foreground">
      <span>{{ inicio }}–{{ fim }} de {{ total }} {{ rotuloItens }}</span>
      <label v-if="!ocultarTamanhos" class="inline-flex items-center gap-1.5">
        Itens por página
        <select
          :value="tamanho"
          class="h-8 rounded-md border border-border bg-card px-2 text-sm text-foreground focus:outline-none focus:ring-1 focus:ring-primary cursor-pointer"
          @change="trocarTamanho"
        >
          <option v-for="t in tamanhos" :key="t" :value="t">{{ t }}</option>
        </select>
      </label>
    </div>

    <!-- Botões de navegação -->
    <div class="flex items-center gap-1">
      <button
        class="inline-flex min-w-8 h-8 items-center justify-center rounded-md border border-border bg-card px-2 text-sm text-foreground transition-colors hover:bg-primary/8 hover:border-primary disabled:opacity-40 disabled:cursor-not-allowed"
        :disabled="pagina === 1"
        title="Primeira página"
        @click="primeiraPagina"
      >«</button>
      <button
        class="inline-flex min-w-8 h-8 items-center justify-center rounded-md border border-border bg-card px-2 text-sm text-foreground transition-colors hover:bg-primary/8 hover:border-primary disabled:opacity-40 disabled:cursor-not-allowed"
        :disabled="pagina === 1"
        title="Página anterior"
        @click="paginaAnterior"
      >‹</button>

      <template v-for="(p, i) in paginasMostradas" :key="i">
        <button
          v-if="typeof p === 'number'"
          class="inline-flex min-w-8 h-8 items-center justify-center rounded-md border px-2 text-sm transition-colors"
          :class="p === pagina
            ? 'bg-primary text-primary-foreground border-primary'
            : 'border-border bg-card text-foreground hover:bg-primary/8 hover:border-primary'"
          @click="irPara(p)"
        >{{ p }}</button>
        <span v-else class="px-1 text-sm text-muted-foreground">…</span>
      </template>

      <button
        class="inline-flex min-w-8 h-8 items-center justify-center rounded-md border border-border bg-card px-2 text-sm text-foreground transition-colors hover:bg-primary/8 hover:border-primary disabled:opacity-40 disabled:cursor-not-allowed"
        :disabled="pagina === totalPaginas"
        title="Próxima página"
        @click="proximaPagina"
      >›</button>
      <button
        class="inline-flex min-w-8 h-8 items-center justify-center rounded-md border border-border bg-card px-2 text-sm text-foreground transition-colors hover:bg-primary/8 hover:border-primary disabled:opacity-40 disabled:cursor-not-allowed"
        :disabled="pagina === totalPaginas"
        title="Última página"
        @click="ultimaPagina"
      >»</button>
    </div>
  </nav>
</template>
