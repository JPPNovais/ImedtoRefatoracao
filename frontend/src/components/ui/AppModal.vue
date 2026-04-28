<script setup lang="ts">
import { computed } from "vue"
import {
    Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter,
} from "@imedto/ui"

const props = defineProps<{
    aberto:           boolean
    titulo?:          string
    largura?:         "sm" | "md" | "lg"
    acimaDeDrawer?:   boolean
    semPaddingCorpo?: boolean
}>()

const emit = defineEmits<{ fechar: [] }>()

function handleOpen(open: boolean) {
    if (!open) emit("fechar")
}

const contentClass = computed(() => {
    const cls: string[] = ["flex flex-col gap-0 p-0"]
    switch (props.largura) {
        case "sm": cls.push("max-w-[420px]"); break
        case "lg": cls.push("max-w-[720px]"); break
        default:   cls.push("max-w-[560px]")
    }
    if (props.acimaDeDrawer) cls.push("z-[600]")
    return cls.join(" ")
})

const bodyClass = computed(() =>
    props.semPaddingCorpo ? "flex-1 overflow-y-auto" : "flex-1 overflow-y-auto px-6 py-5 flex flex-col gap-4"
)
</script>

<template>
    <Dialog :open="aberto" @update:open="handleOpen">
        <DialogContent :class="contentClass">
            <DialogHeader class="px-6 py-4 border-b border-border flex-shrink-0">
                <slot name="titulo">
                    <DialogTitle>{{ titulo }}</DialogTitle>
                </slot>
            </DialogHeader>

            <div :class="bodyClass">
                <slot />
            </div>

            <DialogFooter v-if="$slots.rodape" class="px-6 py-4 border-t border-border justify-end flex-shrink-0">
                <slot name="rodape" />
            </DialogFooter>
        </DialogContent>
    </Dialog>
</template>
