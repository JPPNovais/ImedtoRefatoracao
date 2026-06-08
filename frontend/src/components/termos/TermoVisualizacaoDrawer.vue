<script setup lang="ts">
import { computed } from "vue"
import { AppDrawer, AppButton, AppBadge } from "@/components/ui"
import { CATEGORIAS_TERMO } from "@/constants/termoVariaveis"
import type { TermoEmitidoDetalhe } from "@/services/pacienteTermoService"

/**
 * Drawer de visualização de um termo emitido. Mostra:
 *  - cabeçalho com categoria + status + tipo de assinatura
 *  - metadados (emitido em, por quem, hash, data assinatura, IP)
 *  - snapshot HTML (já sanitizado server-side no momento da emissão — confiável)
 *  - bloco vermelho de revogação quando aplicável
 *  - rodapé com ação "Baixar PDF" / "Visualizar PDF" / "Gerar PDF" (delegado pra view pai)
 */
const props = defineProps<{
    aberto:    boolean
    termo:     TermoEmitidoDetalhe | null
    carregando?: boolean
}>()

defineEmits<{
    (e: "fechar"): void
    (e: "baixar-pdf-anexado"): void
    (e: "gerar-pdf"): void
}>()

const categoria = computed(() => {
    if (!props.termo) return null
    return CATEGORIAS_TERMO.find(c => c.chave === props.termo!.categoria) ?? null
})

const statusVariant = computed<"warning" | "success" | "error" | "muted" | "default">(() => {
    if (!props.termo) return "muted"
    switch (props.termo.status) {
        case "Pendente": return "warning"
        case "Assinado": return "success"
        case "Recusado": return "error"
        case "Revogado": return "error"
        case "Expirado": return "muted"
        default: return "default"
    }
})

const statusLabel = computed(() => statusLabels[props.termo?.status ?? ""] ?? props.termo?.status ?? "")
const tipoAssinaturaLabel = computed(() => {
    if (!props.termo) return ""
    return props.termo.assinaturaTipo === "PdfAnexado" ? "PDF anexado" : "Aceite por link"
})

const hashCurto = computed(() => {
    const h = props.termo?.hashIntegridade
    if (!h) return ""
    return `${h.slice(0, 16)}…`
})

function fmtDataHora(iso: string | null): string {
    if (!iso) return "—"
    const d = new Date(iso)
    if (Number.isNaN(d.getTime())) return "—"
    return d.toLocaleString("pt-BR", {
        day: "2-digit", month: "2-digit", year: "numeric",
        hour: "2-digit", minute: "2-digit",
    })
}

const statusLabels: Record<string, string> = {
    Pendente: "Pendente",
    Assinado: "Assinado",
    Recusado: "Recusado",
    Revogado: "Revogado",
    Expirado: "Expirado",
}
</script>

<template>
    <AppDrawer :aberto="aberto" titulo="Termo emitido" :largura="640" @fechar="$emit('fechar')">
        <template v-if="carregando">
            <p class="msg">Carregando…</p>
        </template>

        <template v-else-if="termo">
            <header class="tv-head">
                <h2 class="tv-titulo">{{ termo.termoModeloTitulo }}</h2>
                <div class="tv-pills">
                    <AppBadge v-if="categoria" :variant="categoria.cor" :label="categoria.label" />
                    <AppBadge :variant="statusVariant" :label="statusLabel" />
                    <span class="tv-tipo">{{ tipoAssinaturaLabel }}</span>
                </div>
            </header>

            <dl class="tv-meta">
                <div>
                    <dt>Versão do modelo</dt>
                    <dd>v{{ termo.versaoModelo }}</dd>
                </div>
                <div>
                    <dt>Emitido em</dt>
                    <dd>{{ fmtDataHora(termo.criadoEm) }}</dd>
                </div>
                <div>
                    <dt>Emitido por</dt>
                    <dd>{{ termo.emitidoPorNome || "—" }}</dd>
                </div>
                <div v-if="termo.status === 'Assinado'">
                    <dt>Assinado em</dt>
                    <dd>{{ fmtDataHora(termo.assinadoEm) }}</dd>
                </div>
                <div v-if="termo.ipAssinatura">
                    <dt>IP de assinatura</dt>
                    <dd>{{ termo.ipAssinatura }}</dd>
                </div>
                <div>
                    <dt>Hash de integridade</dt>
                    <dd :title="termo.hashIntegridade" class="tv-hash">{{ hashCurto }}</dd>
                </div>
            </dl>

            <div v-if="termo.status === 'Revogado'" class="tv-revog">
                <i class="fa-solid fa-ban"></i>
                <div>
                    <b>Termo revogado</b>
                    <p>{{ fmtDataHora(termo.revogadoEm) }}</p>
                    <p v-if="termo.revogadoMotivo" class="tv-revog-motivo">
                        <b>Motivo:</b> {{ termo.revogadoMotivo }}
                    </p>
                </div>
            </div>

            <section class="tv-conteudo">
                <h3>Conteúdo do termo</h3>
                <article class="tv-html" v-html="termo.conteudoSnapshotHtml"></article>
            </section>
        </template>

        <template v-else>
            <p class="msg">Termo não encontrado.</p>
        </template>

        <template #rodape v-if="termo">
            <AppButton variant="secondary" @click="$emit('fechar')">Fechar</AppButton>
            <AppButton
                v-if="termo.temPdf"
                icon="fa-solid fa-file-pdf"
                @click="$emit('baixar-pdf-anexado')"
            >
                Baixar PDF anexado
            </AppButton>
            <AppButton
                v-else
                icon="fa-solid fa-print"
                variant="secondary"
                @click="$emit('gerar-pdf')"
            >
                Gerar PDF para impressão
            </AppButton>
        </template>
    </AppDrawer>
</template>

<style scoped>
.msg {
    font-size: 14px;
    color: hsl(var(--secondary) / 0.7);
}

.tv-head {
    margin-bottom: 16px;
}
.tv-titulo {
    margin: 0 0 8px;
    font-size: 18px;
    font-weight: 700;
    color: hsl(var(--primary-dark));
}
.tv-pills {
    display: flex; align-items: center; flex-wrap: wrap; gap: 6px;
}
.tv-tipo {
    font-size: 11px; font-weight: 600;
    color: hsl(var(--secondary) / 0.7);
    background: hsl(var(--secondary) / 0.08);
    padding: 2px 8px; border-radius: 999px;
}

.tv-meta {
    margin: 0 0 16px;
    display: grid; grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 10px 18px;
    padding: 12px 14px;
    border-radius: 8px;
    background: hsl(var(--muted) / 0.4);
    border: 1px solid hsl(var(--secondary) / 0.08);
}
.tv-meta div { display: flex; flex-direction: column; gap: 2px; }
.tv-meta dt {
    font-size: 10.5px;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    color: hsl(var(--secondary) / 0.6);
    font-weight: 700;
}
.tv-meta dd {
    margin: 0; font-size: 13px; font-weight: 500;
    color: hsl(var(--foreground));
    word-break: break-word;
}
.tv-hash { font-family: ui-monospace, monospace; font-size: 11.5px; cursor: help; }

.tv-revog {
    display: flex; gap: 12px; align-items: flex-start;
    background: hsl(var(--error) / 0.07);
    border: 1px solid hsl(var(--error) / 0.25);
    border-radius: 8px;
    padding: 12px 14px;
    margin-bottom: 16px;
}
.tv-revog i { color: hsl(var(--error)); font-size: 18px; margin-top: 1px; }
.tv-revog b { font-weight: 700; color: hsl(var(--error)); font-size: 13px; }
.tv-revog p { margin: 2px 0 0; font-size: 12.5px; color: hsl(var(--foreground)); }
.tv-revog-motivo { margin-top: 6px !important; }

.tv-conteudo h3 {
    margin: 0 0 8px;
    font-size: var(--text-sm);
    font-weight: var(--font-weight-bold);
    color: hsl(var(--secondary));
    text-transform: uppercase;
    letter-spacing: 0.04em;
}

.tv-html {
    padding: 16px;
    border-radius: 8px;
    border: 1px solid hsl(var(--secondary) / 0.12);
    background: white;
    line-height: 1.6;
    font-size: 14px;
    color: hsl(var(--foreground));
}
.tv-html :deep(p) { margin: 0 0 10px; }
.tv-html :deep(h1) { font-size: var(--text-lg); margin: 16px 0 8px; color: hsl(var(--primary-dark)); }
.tv-html :deep(h2) { font-size: var(--text-md); margin: 14px 0 8px; color: hsl(var(--primary-dark)); }
.tv-html :deep(h3) { font-size: var(--text-base); margin: 12px 0 6px; color: hsl(var(--primary-dark)); }
.tv-html :deep(ul),
.tv-html :deep(ol) { margin: 0 0 10px 24px; }
.tv-html :deep(li) { margin-bottom: 4px; }
.tv-html :deep(strong) { font-weight: 700; }
.tv-html :deep(em) { font-style: italic; }
</style>
