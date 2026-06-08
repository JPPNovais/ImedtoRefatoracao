<script setup lang="ts">
import { ref } from "vue"
import AppModal from "@/components/ui/AppModal.vue"
import AppRolePill from "@/components/ui/AppRolePill.vue"
import AppAvatar from "@/components/ui/AppAvatar.vue"
import type { EstabelecimentoListavel } from "@/stores/tenantStore"

const props = defineProps<{
    aberto: boolean
    estabelecimentos: EstabelecimentoListavel[]
    ativoId: number | null
}>()

const emit = defineEmits<{
    fechar: []
    selecionar: [id: number]
}>()

const salvando = ref(false)

async function selecionarEstabelecimento(id: number) {
    if (id === props.ativoId || salvando.value) return
    salvando.value = true
    emit("selecionar", id)
}
</script>

<template>
    <AppModal
        :aberto="aberto"
        titulo="Trocar estabelecimento"
        largura="sm"
        @fechar="emit('fechar')"
    >
        <ul class="estab-lista">
            <li
                v-for="estab in estabelecimentos"
                :key="estab.id"
                class="estab-item"
                :class="{
                    'estab-item--ativo': estab.id === ativoId,
                    'estab-item--selecionavel': estab.id !== ativoId,
                }"
            >
                <button
                    type="button"
                    class="estab-btn"
                    :disabled="estab.id === ativoId || salvando"
                    @click="selecionarEstabelecimento(estab.id)"
                >
                    <AppAvatar
                        :nome="estab.nomeFantasia"
                        :foto-url="estab.fotoUrl"
                        tamanho="md"
                        decorativo
                    />
                    <span class="estab-nome">{{ estab.nomeFantasia }}</span>
                    <AppRolePill
                        :nome="estab.papelDoUsuario"
                        :cor="estab.papelDoUsuario === 'Dono' ? 'hsl(258 60% 50%)' : 'hsl(200 70% 40%)'"
                        tamanho="sm"
                    />
                    <span v-if="estab.id === ativoId" class="estab-ativo-badge">Ativo</span>
                </button>
            </li>
        </ul>

        <template #rodape>
            <button type="button" class="btn-cancelar" @click="emit('fechar')">
                Cancelar
            </button>
        </template>
    </AppModal>
</template>

<style scoped>
.estab-lista {
    list-style: none;
    margin: 0;
    padding: 0;
    display: flex;
    flex-direction: column;
    gap: 4px;
}

.estab-btn {
    width: 100%;
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 12px 14px;
    border: 1px solid hsl(var(--border));
    border-radius: 8px;
    background: hsl(var(--card));
    cursor: pointer;
    text-align: left;
    transition: background 0.15s, border-color 0.15s;
}

.estab-item--selecionavel .estab-btn:hover:not(:disabled) {
    background: hsl(var(--accent));
    border-color: hsl(var(--ring));
}

.estab-item--ativo .estab-btn {
    border-color: hsl(var(--primary));
    background: hsl(var(--primary) / 0.06);
    cursor: default;
}

.estab-btn:disabled {
    cursor: default;
}

.estab-nome {
    flex: 1;
    font-weight: 500;
    color: hsl(var(--foreground));
    font-size: 14px;
}

.estab-ativo-badge {
    font-size: 11px;
    font-weight: 600;
    color: hsl(var(--primary));
    text-transform: uppercase;
    letter-spacing: 0.03em;
}

.btn-cancelar {
    padding: 8px 16px;
    border: 1px solid hsl(var(--border));
    border-radius: 6px;
    background: transparent;
    cursor: pointer;
    font-size: 14px;
    color: hsl(var(--foreground));
    transition: background 0.15s;
}

.btn-cancelar:hover {
    background: hsl(var(--accent));
}
</style>
