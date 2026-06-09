<script setup lang="ts">
/**
 * AppPopover — componente genérico do design system.
 *
 * Abre um painel flutuante ancorado ao gatilho via Teleport para <body>,
 * posicionado com clamp de viewport para não estourar bordas. Fecha por
 * Esc ou clique fora. O foco retorna ao gatilho ao fechar.
 *
 * Slots:
 *   - #gatilho   — elemento que dispara o popover (recebe `{ abrir, fechar, toggle, aberto }`).
 *   - #conteudo  — corpo do popover (renderizado dentro do painel flutuante).
 *
 * Uso mínimo:
 *   <AppPopover>
 *     <template #gatilho="{ toggle }">
 *       <button @click="toggle">Abrir</button>
 *     </template>
 *     <template #conteudo>
 *       <p>Conteúdo aqui</p>
 *     </template>
 *   </AppPopover>
 */
import { ref, onMounted, onBeforeUnmount, nextTick } from "vue"

const props = withDefaults(defineProps<{
    /**
     * Posição preferida em relação ao gatilho. Se não couber, inverte
     * automaticamente (ex.: bottom → top).
     */
    posicao?: "bottom-start" | "bottom-end" | "top-start" | "top-end"
    /** Deslocamento vertical entre gatilho e painel (px). */
    offset?: number
}>(), {
    posicao: "bottom-start",
    offset: 6,
})

const aberto = ref(false)
const gatilhoRef = ref<HTMLElement | null>(null)
const painelRef  = ref<HTMLElement | null>(null)

// Posição calculada do painel (aplicada via style)
const estilo = ref<{ top: string; left: string }>({ top: "0px", left: "0px" })

function calcularPosicao() {
    if (!gatilhoRef.value || !painelRef.value) return

    // O host tem display:contents (sem caixa própria) → seu próprio
    // getBoundingClientRect retorna zeros. Medir o 1º elemento filho real,
    // que é o gatilho de fato renderizado pelo slot.
    const alvo = gatilhoRef.value.firstElementChild ?? gatilhoRef.value
    const rect  = alvo.getBoundingClientRect()
    const painel = painelRef.value.getBoundingClientRect()
    const vw = window.innerWidth
    const vh = window.innerHeight
    const margem = 8 // px de segurança na borda da viewport

    const prefBaixo = props.posicao.startsWith("bottom")
    const prefInicio = props.posicao.endsWith("start")

    // Calcular posição vertical
    let top: number
    const espacoBaixo = vh - rect.bottom
    const espacoCima  = rect.top

    if (prefBaixo) {
        top = espacoBaixo >= painel.height + props.offset
            ? rect.bottom + props.offset
            : rect.top - painel.height - props.offset
    } else {
        top = espacoCima >= painel.height + props.offset
            ? rect.top - painel.height - props.offset
            : rect.bottom + props.offset
    }

    // Calcular posição horizontal
    let left: number
    if (prefInicio) {
        left = rect.left
    } else {
        left = rect.right - painel.width
    }

    // Clamp na viewport
    left = Math.max(margem, Math.min(left, vw - painel.width - margem))
    top  = Math.max(margem, Math.min(top,  vh - painel.height - margem))

    estilo.value = { top: `${top}px`, left: `${left}px` }
}

async function abrir() {
    if (aberto.value) return
    aberto.value = true
    await nextTick()
    calcularPosicao()
}

async function fechar() {
    if (!aberto.value) return
    aberto.value = false
    // O span wrapper tem display:contents e não é focável diretamente.
    // Busca o primeiro elemento focável dentro dele para devolver o foco.
    await nextTick()
    const focavel = gatilhoRef.value?.querySelector<HTMLElement>(
        'button, [tabindex="0"], a[href]',
    )
    focavel?.focus()
}

function toggle() {
    if (aberto.value) fechar()
    else abrir()
}

// Fechar ao clicar fora
function aoClicarFora(e: MouseEvent) {
    if (!aberto.value) return
    const alvo = e.target as Node
    if (painelRef.value?.contains(alvo)) return
    if (gatilhoRef.value?.contains(alvo)) return
    fechar()
}

// Fechar com Esc
function aoTeclar(e: KeyboardEvent) {
    if (e.key === "Escape" && aberto.value) {
        e.stopPropagation()
        fechar()
    }
}

onMounted(() => {
    document.addEventListener("mousedown", aoClicarFora)
    document.addEventListener("keydown", aoTeclar)
})

onBeforeUnmount(() => {
    document.removeEventListener("mousedown", aoClicarFora)
    document.removeEventListener("keydown", aoTeclar)
})

defineExpose({ abrir, fechar, aberto })
</script>

<template>
    <span ref="gatilhoRef" class="app-popover-host">
        <slot name="gatilho" :abrir="abrir" :fechar="fechar" :toggle="toggle" :aberto="aberto" />
    </span>

    <Teleport to="body">
        <div
            v-if="aberto"
            ref="painelRef"
            class="app-popover-painel"
            :style="estilo"
            role="dialog"
            aria-modal="false"
        >
            <slot name="conteudo" :fechar="fechar" />
        </div>
    </Teleport>
</template>

<style scoped>
.app-popover-host {
    /* Wrapper inline — não altera layout do gatilho */
    display: contents;
}

.app-popover-painel {
    position: fixed;
    z-index: 900;
    background: white;
    border: 1px solid hsl(var(--secondary) / 0.12);
    border-radius: 12px;
    box-shadow: 0 8px 24px rgb(0 0 0 / 0.12), 0 2px 6px rgb(0 0 0 / 0.06);
    min-width: 220px;
    max-width: 340px;
}
</style>
