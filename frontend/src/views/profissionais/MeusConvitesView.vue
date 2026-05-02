<script setup lang="ts">
import { onMounted, ref } from "vue"
import { vinculoService, type ConvitePendente } from "@/services/vinculoService"
import { AppButton } from "@/components/ui"

const convites   = ref<ConvitePendente[]>([])
const carregando = ref(false)
const erro       = ref<string | null>(null)
const msg        = ref<string | null>(null)
const processando= ref<Set<number>>(new Set())

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        convites.value = await vinculoService.listarMeusConvites()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar convites."
    } finally {
        carregando.value = false
    }
}

async function aceitar(c: ConvitePendente) {
    processando.value.add(c.vinculoId)
    try {
        await vinculoService.aceitarConvite(c.vinculoId)
        msg.value = `Convite de ${c.nomeFantasiaEstabelecimento} aceito. Você já pode selecionar esse estabelecimento.`
        await carregar()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao aceitar."
    } finally {
        processando.value.delete(c.vinculoId)
    }
}

async function recusar(c: ConvitePendente) {
    if (!confirm(`Recusar convite de ${c.nomeFantasiaEstabelecimento}?`)) return
    processando.value.add(c.vinculoId)
    try {
        await vinculoService.inativarVinculo(c.vinculoId)
        await carregar()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao recusar."
    } finally {
        processando.value.delete(c.vinculoId)
    }
}

function fmtData(iso: string) {
    try { return new Date(iso).toLocaleDateString("pt-BR") }
    catch { return iso }
}

onMounted(carregar)
</script>

<template>
    <div class="app-page app-page--narrow convites">
        <div class="page-header">
            <div>
                <h1 class="page-titulo">Meus convites</h1>
                <p class="page-sub">Convites pendentes para vincular-se a estabelecimentos.</p>
            </div>
        </div>

        <p v-if="erro" class="msg-erro">{{ erro }}</p>
        <p v-if="msg"  class="msg-ok">{{ msg }}</p>

        <div v-if="carregando" class="estado-msg">Carregando...</div>

        <div v-else-if="convites.length === 0" class="estado-msg vazio">
            <span class="vazio-icon">📭</span>
            <p>Você não tem convites pendentes.</p>
        </div>

        <div v-else class="lista">
            <div v-for="c in convites" :key="c.vinculoId" class="card-convite">
                <div class="c-info">
                    <h3 class="c-titulo">{{ c.nomeFantasiaEstabelecimento }}</h3>
                    <p class="c-desc">
                        <span v-if="c.convidadoPorNome">Convite enviado por {{ c.convidadoPorNome }}</span>
                        <span v-else>Convite recebido</span>
                    </p>
                    <p class="c-data">Recebido em {{ fmtData(c.convidadoEm) }}</p>
                </div>
                <div class="c-acoes">
                    <AppButton
                        variant="ghost"
                        :disabled="processando.has(c.vinculoId)"
                        @click="recusar(c)"
                    >Recusar</AppButton>
                    <AppButton
                        :disabled="processando.has(c.vinculoId)"
                        :loading="processando.has(c.vinculoId)"
                        @click="aceitar(c)"
                    >Aceitar</AppButton>
                </div>
            </div>
        </div>
    </div>
</template>

<style scoped>

.page-header { margin-bottom: 1.5rem; }
.page-titulo { font-size: 1.5rem; font-weight: 800; margin: 0 0 0.2rem; }
.page-sub    { margin: 0; color: var(--text-muted); font-size: 0.875em; }

.msg-erro { color: var(--danger); font-size: 0.875em; margin: 0 0 0.75rem; }
.msg-ok   { color: #15803d;      font-size: 0.875em; margin: 0 0 0.75rem; }

.estado-msg { text-align: center; color: var(--text-muted); padding: 3rem 1rem; font-size: 0.9em; }
.vazio { display: flex; flex-direction: column; align-items: center; gap: 0.5rem; }
.vazio-icon { font-size: 2.5rem; }

.lista { display: flex; flex-direction: column; gap: 0.75rem; }

.card-convite {
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 1.1rem 1.3rem;
    display: flex; justify-content: space-between; align-items: center;
    gap: 1rem;
}

.c-info { flex: 1; min-width: 0; }
.c-titulo { font-size: 1rem; font-weight: 700; margin: 0 0 0.25rem; }
.c-desc   { font-size: 0.875em; color: var(--text); margin: 0 0 0.1rem; }
.c-data   { font-size: 0.78em; color: var(--text-muted); margin: 0; }

.c-acoes { display: flex; gap: 0.5rem; flex-shrink: 0; }

</style>
