<script setup lang="ts">
import { computed } from "vue"
import { useMediaQuery } from "@vueuse/core"
import { Sheet, SheetContent, SheetHeader, SheetTitle } from "@imedto/ui"

const props = withDefaults(defineProps<{
    aberto:  boolean
    titulo?: string
    largura?: number
}>(), {
    largura: 500,
})

const emit = defineEmits<{ fechar: [] }>()

const isMobile = useMediaQuery("(max-width: 768px)")

const sheetStyle = computed(() => {
    if (isMobile.value) return {}
    return { "--drawer-width": `${props.largura}px` } as Record<string, string>
})

function handleOpen(open: boolean) {
    if (!open) emit("fechar")
}
</script>

<template>
    <Sheet :open="aberto" @update:open="handleOpen">
        <SheetContent
            side="right"
            :style="sheetStyle"
            :class="[
                isMobile ? 'w-full sm:max-w-full' : 'sm:max-w-[var(--drawer-width)]',
                'flex flex-col gap-0 p-0 max-w-[96vw]',
            ]"
        >
            <SheetHeader class="px-6 py-4 border-b border-border flex-shrink-0">
                <slot name="titulo">
                    <SheetTitle>{{ titulo }}</SheetTitle>
                </slot>
            </SheetHeader>

            <div class="flex-1 overflow-y-auto px-6 py-6 flex flex-col gap-5">
                <slot />
            </div>

            <div v-if="$slots.rodape" class="relative flex justify-end gap-3 px-6 py-4 border-t border-border flex-shrink-0">
                <div class="absolute -top-6 left-0 right-0 h-6 bg-gradient-to-t from-background to-transparent pointer-events-none" />
                <slot name="rodape" />
            </div>
        </SheetContent>
    </Sheet>
</template>
