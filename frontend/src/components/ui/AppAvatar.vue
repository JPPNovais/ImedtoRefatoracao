<script setup lang="ts">
/**
 * AppAvatar — avatar circular do design system.
 *
 * Renderiza a foto se houver `fotoUrl`, senão um placeholder com as iniciais
 * do nome em cor determinística (hash do nome). Consolida o padrão duplicado
 * em AbaProfissionais, ProfissionalDetalhesModal, MinhaContaView e seletores
 * de agenda/prontuário.
 *
 * Uso típico:
 *   <AppAvatar :nome="p.nomeCompleto" :foto-url="p.fotoUrl" tamanho="md" />
 *
 * Para upload (clicável + remover), use AppPhotoUpload em vez deste componente.
 */
import { computed } from "vue"

const props = withDefaults(defineProps<{
    /** Nome do usuário — usado para gerar iniciais + cor determinística. */
    nome?: string | null
    /** URL da foto. Se null/vazio, mostra placeholder com iniciais. */
    fotoUrl?: string | null
    /** Tamanho: sm=24px, md=40px, lg=64px, xl=96px. Default md. */
    tamanho?: "sm" | "md" | "lg" | "xl"
    /** Sobrescreve a cor de fundo do placeholder (HSL string). */
    corFundo?: string | null
    /** Mostra borda branca (útil ao sobrepor em fundos coloridos). */
    comBorda?: boolean
}>(), {
    nome: null,
    fotoUrl: null,
    tamanho: "md",
    corFundo: null,
    comBorda: false,
})

const iniciais = computed(() => {
    const base = (props.nome ?? "").trim()
    if (!base) return "?"
    const partes = base.split(/\s+/).filter(Boolean)
    if (partes.length === 1) return partes[0]!.slice(0, 2).toUpperCase()
    return (partes[0]![0]! + partes[partes.length - 1]![0]!).toUpperCase()
})

/**
 * Cor de fundo determinística pelo hash do nome — mesmo nome sempre cai
 * na mesma cor da paleta, mantendo identidade visual entre telas.
 */
const corDeterministica = computed(() => {
    if (props.corFundo) return props.corFundo
    const paleta = [
        "hsl(254 56% 38%)", "hsl(190 60% 45%)", "hsl(280 55% 50%)",
        "hsl(140 45% 45%)", "hsl(40 70% 50%)", "hsl(340 55% 55%)",
        "hsl(220 55% 50%)", "hsl(170 50% 40%)",
    ]
    const base = (props.nome ?? "?").trim()
    let hash = 0
    for (let i = 0; i < base.length; i++) hash = (hash * 31 + base.charCodeAt(i)) | 0
    const idx = Math.abs(hash) % paleta.length
    return paleta[idx]
})

const temFoto = computed(() => !!props.fotoUrl)
</script>

<template>
    <div
        class="avatar"
        :class="[`avatar--${tamanho}`, { 'avatar--borda': comBorda }]"
        :style="!temFoto ? { background: corDeterministica } : undefined"
        :title="nome ?? undefined"
    >
        <img v-if="temFoto" :src="fotoUrl!" :alt="nome ?? ''" loading="lazy" />
        <span v-else class="iniciais">{{ iniciais }}</span>
    </div>
</template>

<style scoped>
.avatar {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    border-radius: 50%;
    overflow: hidden;
    flex-shrink: 0;
    color: white;
    font-weight: 700;
    line-height: 1;
    text-transform: uppercase;
    user-select: none;
}
.avatar--borda { box-shadow: 0 0 0 2px white; }

.avatar img {
    width: 100%;
    height: 100%;
    object-fit: cover;
    display: block;
}

.iniciais { font-family: inherit; }

.avatar--sm { width: 24px; height: 24px; font-size: 10px; }
.avatar--md { width: 40px; height: 40px; font-size: 14px; }
.avatar--lg { width: 64px; height: 64px; font-size: 20px; }
.avatar--xl { width: 96px; height: 96px; font-size: 28px; }
</style>
