<script setup lang="ts">
import { Pagination } from "@imedto/ui"

defineProps<{
    pagina:          number
    tamanho:         number
    total:           number
    tamanhos?:       number[]
    rotuloItens?:    string
    ocultarTamanhos?: boolean
}>()

const emit = defineEmits<{
    "update:pagina":  [number]
    "update:tamanho": [number]
}>()
</script>

<template>
    <Pagination
        :pagina="pagina"
        :tamanho="tamanho"
        :total="total"
        :tamanhos="tamanhos"
        :rotulo-itens="rotuloItens"
        :ocultar-tamanhos="ocultarTamanhos"
        @update:pagina="emit('update:pagina', $event)"
        @update:tamanho="emit('update:tamanho', $event)"
    />
</template>
