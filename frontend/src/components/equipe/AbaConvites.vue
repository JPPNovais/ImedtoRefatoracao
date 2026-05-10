<script setup lang="ts">
import { AppButton, AppEmptyState, AppRolePill } from "@/components/ui"
import type { ProfissionalVinculado } from "@/services/vinculoService"
import type { ModeloPermissao } from "@/services/permissaoService"

/**
 * Aba "Convites pendentes": lista de convites enviados pelo dono que ainda
 * não foram aceitos pelo profissional. Permite reenviar e cancelar.
 *
 * Reenviar: chama POST /api/estabelecimento/{id}/profissionais/{vinculoId}/reenviar-convite
 * — o backend aplica cooldown de 5 min e regera o token de convite.
 */
const props = defineProps<{
    convites: ProfissionalVinculado[]
    modelos: ModeloPermissao[]
    reenviandoId?: number | null
}>()

const emit = defineEmits<{
    (e: "abrir-convite"): void
    (e: "cancelar", c: ProfissionalVinculado): void
    (e: "reenviar", c: ProfissionalVinculado): void
}>()

function modelo(c: ProfissionalVinculado): ModeloPermissao | undefined {
    return props.modelos.find(m => m.id === c.modeloPermissaoId)
}
</script>

<template>
    <div class="aba-convites">
        <AppEmptyState
            v-if="convites.length === 0"
            icone="📨"
            titulo="Nenhum convite pendente"
            descricao="Quando você convidar profissionais, eles aparecerão aqui até aceitarem."
        >
            <template #acao>
                <AppButton icon="fa-solid fa-user-plus" @click="emit('abrir-convite')">
                    Convidar profissional
                </AppButton>
            </template>
        </AppEmptyState>

        <template v-else>
            <div class="invites-header">
                <p>
                    <i class="fa-solid fa-circle-info"></i>
                    Profissionais precisam aceitar pelo link enviado para criar a conta e ter acesso.
                </p>
                <AppButton variant="secondary" icon="fa-solid fa-paper-plane" @click="emit('abrir-convite')">
                    Enviar novo convite
                </AppButton>
            </div>

            <div class="invites-list">
                <div
                    v-for="c in convites" :key="c.vinculoId"
                    class="invite-card"
                >
                    <div class="ic-icon">
                        <i class="fa-solid fa-envelope"></i>
                    </div>
                    <div class="ic-info">
                        <div class="ic-name">
                            <b>{{ c.nomeCompleto || c.email }}</b>
                        </div>
                        <div class="ic-meta">
                            <span><i class="fa-solid fa-envelope"></i> {{ c.email }}</span>
                        </div>
                        <div class="ic-tags">
                            <AppRolePill
                                v-if="modelo(c)"
                                :nome="modelo(c)!.nome"
                                :icone="modelo(c)!.icone"
                                :cor="modelo(c)!.cor"
                            />
                            <span v-if="c.profissao" class="ic-spec ic-spec--profissao">
                                <i class="fa-solid fa-user-doctor"></i> {{ c.profissao }}
                            </span>
                            <span v-if="c.especialidade" class="ic-spec">{{ c.especialidade }}</span>
                            <span v-if="c.conselho" class="ic-spec">{{ c.conselho }}</span>
                        </div>
                    </div>
                    <div class="ic-actions">
                        <AppButton
                            variant="secondary"
                            size="sm"
                            icon="fa-solid fa-paper-plane"
                            :disabled="reenviandoId === c.vinculoId"
                            @click="emit('reenviar', c)"
                        >
                            {{ reenviandoId === c.vinculoId ? "Reenviando…" : "Reenviar" }}
                        </AppButton>
                        <AppButton variant="danger" size="sm" icon="fa-solid fa-xmark" @click="emit('cancelar', c)">
                            Cancelar
                        </AppButton>
                    </div>
                </div>
            </div>
        </template>
    </div>
</template>

<style scoped>
.aba-convites { display: flex; flex-direction: column; gap: 14px; }

.invites-header {
    display: flex; align-items: center; justify-content: space-between; gap: 16px;
    background: hsl(var(--info) / 0.06); border: 1px solid hsl(var(--info) / 0.2);
    border-radius: 8px; padding: 12px 16px;
    flex-wrap: wrap;
}
.invites-header p {
    display: flex; align-items: center; gap: 8px;
    font-size: 13px; color: hsl(199 80% 32%); margin: 0;
}

.invites-list { display: flex; flex-direction: column; gap: 10px; }

.invite-card {
    display: grid;
    grid-template-columns: 44px 1fr auto;
    gap: 16px; align-items: center;
    background: white;
    border: 1px solid hsl(var(--secondary) / 0.08);
    border-radius: 12px; padding: 14px 18px;
    box-shadow: 0 1px 2px 0 rgb(0 0 0 / 0.04);
    transition: all 150ms;
}
.invite-card:hover {
    border-color: hsl(var(--primary) / 0.25);
    box-shadow: 0 2px 8px -2px rgb(0 0 0 / 0.06);
}

.ic-icon {
    width: 44px; height: 44px; border-radius: 50%;
    background: hsl(var(--primary) / 0.1); color: hsl(var(--primary));
    display: inline-flex; align-items: center; justify-content: center;
    font-size: 18px;
}

.ic-info { display: flex; flex-direction: column; gap: 4px; min-width: 0; }
.ic-name b { font-size: 14px; color: hsl(var(--primary-dark)); font-weight: 700; }
.ic-meta { display: flex; gap: 14px; font-size: 12px; color: hsl(var(--secondary) / 0.65); flex-wrap: wrap; }
.ic-meta i { margin-right: 4px; }
.ic-tags { display: flex; gap: 6px; align-items: center; flex-wrap: wrap; margin-top: 2px; }
.ic-spec {
    font-size: 11px; color: hsl(var(--secondary) / 0.6);
    padding: 2px 8px; background: hsl(var(--secondary) / 0.05); border-radius: 999px;
}
.ic-spec--profissao {
    color: hsl(var(--info));
    background: hsl(var(--info) / 0.08);
    font-weight: 600;
    display: inline-flex; align-items: center; gap: 4px;
}
.ic-spec--profissao i { font-size: 10px; }

.ic-actions { display: flex; gap: 6px; align-items: center; }
</style>
