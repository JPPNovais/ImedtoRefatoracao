<script setup lang="ts">
import { computed, ref, watch } from "vue"
import {
    AppAvatar, AppButton, AppInput, AppModal, AppPermissionMatrix, AppRolePill, AppSelect, AppStatusPill,
} from "@/components/ui"
import { permissaoService, type ModeloPermissao } from "@/services/permissaoService"
import { vinculoService, type ProfissionalVinculado } from "@/services/vinculoService"
import { useAuthStore } from "@/stores/authStore"
import { usePermissoesStore } from "@/stores/permissoesStore"

/**
 * Modal de detalhes do profissional. 2 abas:
 *  - **Perfil**: dados básicos (somente leitura — edição completa é via cadastro
 *    do profissional, fora do escopo) + ações administrativas.
 *  - **Permissões**: papel atual (combobox) + matriz read-only do papel.
 *
 * Aba "Histórico de acesso" do design não é incluída pois o backend não expõe
 * audit trail por profissional ainda — adiar para quando houver endpoint.
 */
const props = defineProps<{
    aberto: boolean
    profissional: ProfissionalVinculado | null
    modelos: ModeloPermissao[]
}>()

const emit = defineEmits<{
    (e: "fechar"): void
    (e: "atualizado", p: ProfissionalVinculado): void
    (e: "removido", p: ProfissionalVinculado): void
    (e: "reativado", p: ProfissionalVinculado): void
}>()

const auth = useAuthStore()
const permissoes = usePermissoesStore()
const aba = ref<"perfil" | "permissoes">("perfil")
const modeloSelecionadoId = ref<number | null>(null)
const salvando = ref(false)
const removendo = ref(false)
const reativando = ref(false)
const salvandoEspecialidade = ref(false)
const especialidadeEditada = ref<string>("")
const erro = ref<string | null>(null)

watch(() => [props.profissional, props.aberto] as const, ([p, aberto]) => {
    if (!aberto || !p) return
    aba.value = "perfil"
    modeloSelecionadoId.value = p.modeloPermissaoId
    especialidadeEditada.value = p.especialidade ?? ""
    erro.value = null
}, { immediate: true })

const modeloAtual = computed(() =>
    props.modelos.find(m => m.id === modeloSelecionadoId.value) ?? null,
)

const ehDono = computed(() => props.profissional?.status === "Dono")
const ehVinculoProprio = computed(() => props.profissional?.usuarioId === auth.usuario?.id)

const podeRemover = computed(() => props.profissional && !ehDono.value && !ehVinculoProprio.value)
const podeReativar = computed(() => props.profissional?.status === "Inativo" && !ehDono.value)

// Campo editável de especialidade: requer vínculo formal (vinculoId != null, CA9)
// E que o USUÁRIO LOGADO seja Dono (CA7). `ehDono` aqui é do profissional listado
// e não deve ser usado para RBAC — apenas `permissoes.ehDono` reflete o papel do logado.
const podeEditarEspecialidade = computed(() => props.profissional?.vinculoId != null && permissoes.ehDono)

function statusVariante(s: string): "success" | "warning" | "error" | "muted" {
    if (s === "Ativo" || s === "Dono")  return "success"
    if (s === "Convidado")              return "warning"
    if (s === "Bloqueado")              return "error"
    return "muted"
}

async function salvarEspecialidade() {
    if (!props.profissional || salvandoEspecialidade.value) return
    if (props.profissional.vinculoId == null) return  // Dono sintético — sem vínculo formal.
    salvandoEspecialidade.value = true
    erro.value = null
    try {
        await vinculoService.alterarEspecialidade(
            props.profissional.vinculoId,
            especialidadeEditada.value.trim() || null,
        )
        emit("atualizado", {
            ...props.profissional,
            especialidade: especialidadeEditada.value.trim() || null,
        })
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Não foi possível atualizar a especialidade."
    } finally {
        salvandoEspecialidade.value = false
    }
}

async function salvarPapel() {
    if (!props.profissional || salvando.value) return
    if (modeloSelecionadoId.value === props.profissional.modeloPermissaoId) {
        emit("fechar")
        return
    }
    if (!modeloSelecionadoId.value) return
    if (props.profissional.vinculoId == null) return  // Dono — não tem vínculo formal pra atribuir.
    salvando.value = true
    erro.value = null
    try {
        await permissaoService.atribuirAoVinculo(props.profissional.vinculoId, modeloSelecionadoId.value)
        const novo = props.modelos.find(m => m.id === modeloSelecionadoId.value)
        emit("atualizado", {
            ...props.profissional,
            modeloPermissaoId: modeloSelecionadoId.value,
            modeloPermissaoNome: novo?.nome ?? "",
        })
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Não foi possível atualizar a permissão."
    } finally {
        salvando.value = false
    }
}

async function remover() {
    if (!props.profissional || !podeRemover.value) return
    if (props.profissional.vinculoId == null) return  // Dono não pode ser removido (status="Dono" já bloqueia podeRemover).
    if (!confirm(`Remover ${props.profissional.nomeCompleto || props.profissional.email} do estabelecimento?`)) return
    removendo.value = true
    erro.value = null
    try {
        await vinculoService.inativarVinculo(props.profissional.vinculoId)
        emit("removido", props.profissional)
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Não foi possível remover."
    } finally {
        removendo.value = false
    }
}

async function reativar() {
    if (!props.profissional || !podeReativar.value) return
    if (props.profissional.vinculoId == null) return
    reativando.value = true
    erro.value = null
    try {
        await vinculoService.reativarVinculo(props.profissional.vinculoId)
        emit("reativado", { ...props.profissional, status: "Ativo" })
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Não foi possível reativar."
    } finally {
        reativando.value = false
    }
}

function fechar() {
    if (salvando.value || removendo.value || reativando.value) return
    emit("fechar")
}
</script>

<template>
    <AppModal :aberto="aberto && !!profissional" largura="lg" sem-padding-corpo @fechar="fechar">
        <template #titulo>
            <div v-if="profissional" class="ph-info">
                <div class="ph-avatar-wrap">
                    <AppAvatar
                        :nome="profissional.nomeCompleto || profissional.email"
                        :foto-url="profissional.fotoUrl"
                        tamanho="lg"
                        decorativo
                    />
                    <span v-if="ehDono" class="owner-crown" title="Dono"><i class="fa-solid fa-crown"></i></span>
                </div>
                <div>
                    <h2 class="ph-nome">{{ profissional.nomeCompleto || profissional.email }}</h2>
                    <div class="ph-meta">
                        <AppRolePill
                            v-if="modeloAtual"
                            :nome="modeloAtual.nome"
                            :icone="modeloAtual.icone"
                            :cor="modeloAtual.cor"
                        />
                        <AppStatusPill :label="profissional.status" :variante="statusVariante(profissional.status)" />
                        <span v-if="profissional.especialidade" class="ph-spec">{{ profissional.especialidade }}</span>
                        <span v-if="profissional.conselho" class="ph-spec">{{ profissional.conselho }}</span>
                    </div>
                </div>
            </div>
        </template>

        <template v-if="profissional">
            <!-- Tabs -->
            <div class="pro-tabs">
                <button
                    type="button"
                    class="pt"
                    :class="{ active: aba === 'perfil' }"
                    @click="aba = 'perfil'"
                >
                    <i class="fa-solid fa-user"></i> Perfil
                </button>
                <button
                    type="button"
                    class="pt"
                    :class="{ active: aba === 'permissoes' }"
                    @click="aba = 'permissoes'"
                >
                    <i class="fa-solid fa-shield-halved"></i> Permissão
                </button>
            </div>

            <div class="pro-body">
                <!-- Perfil -->
                <div v-if="aba === 'perfil'" class="dados-grid">
                    <div class="dado">
                        <span class="dado-label">E-mail</span>
                        <span class="dado-valor">{{ profissional.email }}</span>
                    </div>
                    <div class="dado">
                        <span class="dado-label">Nome completo</span>
                        <span class="dado-valor">{{ profissional.nomeCompleto || "—" }}</span>
                    </div>

                    <!-- Especialidade editável pelo Dono (vinculoId != null). Somente leitura para Dono sintético e não-Dono. -->
                    <div v-if="podeEditarEspecialidade" class="dado dado-full especialidade-edit">
                        <span class="dado-label">Especialidade neste estabelecimento</span>
                        <div class="esp-row">
                            <AppInput
                                v-model="especialidadeEditada"
                                placeholder="Ex.: Dermatologia (vazio = usa cadastro global)"
                                :disabled="salvandoEspecialidade"
                                maxlength="200"
                            />
                            <AppButton
                                size="sm"
                                icon="fa-solid fa-floppy-disk"
                                :loading="salvandoEspecialidade"
                                :disabled="salvandoEspecialidade"
                                @click="salvarEspecialidade"
                            >
                                Salvar
                            </AppButton>
                        </div>
                        <span class="da-hint">
                            <i class="fa-solid fa-circle-info"></i>
                            Deixe em branco para usar a especialidade do cadastro global do profissional.
                        </span>
                    </div>
                    <div v-else-if="profissional.especialidade" class="dado">
                        <span class="dado-label">Especialidade</span>
                        <span class="dado-valor">{{ profissional.especialidade }}</span>
                    </div>

                    <div v-if="profissional.conselho" class="dado">
                        <span class="dado-label">Conselho</span>
                        <span class="dado-valor">{{ profissional.conselho }}</span>
                    </div>

                    <div class="dado dado-full pro-danger">
                        <span class="dado-label danger">Ações administrativas</span>
                        <div class="da-actions">
                            <AppButton
                                v-if="podeReativar"
                                size="sm"
                                icon="fa-solid fa-circle-check"
                                :loading="reativando"
                                :disabled="reativando"
                                @click="reativar"
                            >
                                Reativar profissional
                            </AppButton>
                            <AppButton
                                v-if="podeRemover"
                                variant="danger"
                                size="icon-sm"
                                icon="fa-solid fa-trash"
                                :loading="removendo"
                                :disabled="removendo"
                                title="Remover do estabelecimento"
                                aria-label="Remover do estabelecimento"
                                @click="remover"
                            />
                        </div>
                        <span v-if="ehDono" class="da-hint">
                            <i class="fa-solid fa-crown"></i> Dono da clínica não pode ser removido nem ter a permissão alterada.
                        </span>
                        <span v-else-if="ehVinculoProprio" class="da-hint">
                            <i class="fa-solid fa-circle-info"></i> Este é o seu próprio vínculo.
                        </span>
                    </div>
                </div>

                <!-- Permissões -->
                <div v-else class="permissoes-bloco">
                    <div class="role-change">
                        <label>Permissão atual</label>
                        <AppSelect v-model="modeloSelecionadoId" :disabled="ehDono">
                            <option v-for="m in modelos" :key="m.id" :value="m.id">{{ m.nome }}</option>
                        </AppSelect>
                        <span v-if="ehDono" class="rc-hint">
                            <i class="fa-solid fa-crown"></i> Dono da clínica é sempre Administrador.
                        </span>
                    </div>

                    <div class="rps-head">
                        <h3>Acessos herdados da permissão "{{ modeloAtual?.nome ?? '—' }}"</h3>
                        <span>Para alterar acessos individuais, edite a permissão ou crie uma permissão customizada.</span>
                    </div>
                    <AppPermissionMatrix :model-value="modeloAtual?.permissoes ?? []" read-only />
                </div>
            </div>

            <p v-if="erro" class="msg-erro">{{ erro }}</p>
        </template>

        <template #rodape>
            <AppButton variant="ghost" :disabled="salvando || removendo" @click="fechar">Cancelar</AppButton>
            <AppButton
                v-if="aba === 'permissoes' && !ehDono"
                icon="fa-solid fa-floppy-disk"
                :loading="salvando"
                :disabled="salvando || modeloSelecionadoId === profissional?.modeloPermissaoId"
                @click="salvarPapel"
            >
                Salvar alterações
            </AppButton>
        </template>
    </AppModal>
</template>

<style scoped>
/* Header customizado */
.ph-info { display: flex; gap: 14px; align-items: center; }
.ph-avatar-wrap { position: relative; flex-shrink: 0; }
.owner-crown {
    position: absolute; bottom: -3px; right: -3px;
    width: 16px; height: 16px; border-radius: 50%;
    background: hsl(45 96% 50%); color: white;
    display: inline-flex; align-items: center; justify-content: center;
    font-size: 8px; border: 2px solid white;
}
.ph-nome { font-size: 16px; font-weight: 700; color: hsl(var(--primary-dark)); margin: 0 0 4px; }
.ph-meta { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }
.ph-spec { font-size: 11px; color: hsl(var(--secondary) / 0.65); }

/* Tabs */
.pro-tabs {
    display: flex; gap: 2px; padding: 0 24px;
    border-bottom: 1px solid hsl(var(--secondary) / 0.08);
    background: white;
    position: sticky; top: 0;
}
.pt {
    display: inline-flex; align-items: center; gap: 8px;
    background: transparent; border: none; padding: 12px 14px;
    font-family: inherit; font-size: 13px; font-weight: 600;
    color: hsl(var(--secondary) / 0.6); cursor: pointer;
    border-bottom: 2px solid transparent; transition: color 150ms;
}
.pt:hover { color: hsl(var(--primary-dark)); }
.pt.active { color: hsl(var(--primary)); border-bottom-color: hsl(var(--primary)); }

.pro-body { padding: 22px 24px 14px; }

.dados-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
.dado { display: flex; flex-direction: column; gap: 4px; }
.dado-full { grid-column: 1 / -1; }
.dado-label { font-size: 11px; font-weight: 600; color: hsl(var(--secondary) / 0.6); text-transform: uppercase; letter-spacing: 0.04em; }
.dado-label.danger { color: hsl(var(--error)); }
.dado-valor { font-size: 14px; color: hsl(var(--primary-dark)); font-weight: 500; }
.da-actions { display: flex; gap: 8px; flex-wrap: wrap; margin-top: 8px; }
.especialidade-edit { display: flex; flex-direction: column; gap: 6px; }
.esp-row { display: flex; gap: 8px; align-items: flex-end; flex-wrap: wrap; }
.esp-row > :first-child { flex: 1; min-width: 160px; }
.da-hint, .rc-hint {
    display: inline-flex; align-items: center; gap: 6px;
    margin-top: 8px; font-size: 12px;
    color: hsl(var(--secondary) / 0.65); font-style: italic;
}
.da-hint i { color: hsl(45 95% 50%); }

.permissoes-bloco { display: flex; flex-direction: column; gap: 16px; }
.role-change { display: flex; flex-direction: column; gap: 6px; max-width: 380px; }
.role-change label { font-size: 12px; font-weight: 600; color: hsl(var(--primary-dark)); }
.rps-head { display: flex; align-items: baseline; justify-content: space-between; flex-wrap: wrap; gap: 8px; }
.rps-head h3 { font-size: 14px; font-weight: 700; color: hsl(var(--primary-dark)); margin: 0; }
.rps-head span { font-size: 12px; color: hsl(var(--secondary) / 0.6); }

.msg-erro { color: hsl(var(--error)); font-size: 13px; margin: 12px 24px 0; }

@media (max-width: 720px) {
    .dados-grid { grid-template-columns: 1fr; }
}
</style>
