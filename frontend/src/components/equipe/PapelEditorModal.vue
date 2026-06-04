<script setup lang="ts">
import { computed, reactive, ref, watch } from "vue"
import {
    AppButton, AppConfirmDialog, AppField, AppInput, AppModal, AppPermissionMatrix, AppTextarea,
} from "@/components/ui"
import { permissaoService, type ModeloPermissao } from "@/services/permissaoService"

/**
 * Modal de criar/editar papel personalizado. Permite editar nome, descrição,
 * ícone, cor + matriz de permissões granulares.
 *
 * Modelos padrão (`ehPadrao`) abrem em modo somente-leitura no contexto tenant.
 * No contexto admin (`contexto='admin'`), padrões são editáveis e deletáveis.
 *
 * Para injetar um service admin diferente no contexto admin, passe `servicoSalvar`
 * e `servicoExcluir` como props (opcional — se não passados, usa `permissaoService` padrão).
 */
const props = withDefaults(defineProps<{
    aberto: boolean
    modelo: ModeloPermissao | null   // null = criar novo
    /** 'tenant' (padrão): padrão abre read-only. 'admin': padrão é editável. */
    contexto?: "tenant" | "admin"
    /** Override do service de salvar — injetado pelo contexto admin. */
    servicoSalvar?: (payload: unknown) => Promise<ModeloPermissao | void>
    /** Override do service de excluir — injetado pelo contexto admin. */
    servicoExcluir?: (id: number) => Promise<void>
}>(), {
    contexto: "tenant",
    servicoSalvar: undefined,
    servicoExcluir: undefined,
})

const emit = defineEmits<{
    (e: "fechar"): void
    (e: "salvo", m: ModeloPermissao): void
    (e: "excluido", m: ModeloPermissao): void
}>()

const ICONES = [
    "fa-user-tag", "fa-user-doctor", "fa-user-nurse", "fa-headset",
    "fa-sack-dollar", "fa-building-user", "fa-briefcase-medical",
    "fa-shield-halved", "fa-clipboard-user", "fa-user-gear",
]

const CORES = [
    "hsl(254 56% 38%)", "hsl(280 60% 50%)", "hsl(190 65% 45%)",
    "hsl(40 80% 50%)", "hsl(140 50% 40%)", "hsl(170 55% 40%)",
    "hsl(220 60% 50%)", "hsl(340 55% 55%)", "hsl(0 0% 40%)",
]

interface Form {
    nome: string
    descricao: string
    icone: string
    cor: string
    tipoAcesso: "Profissional" | "Recepcionista"
    permissoes: string[]
}

const form = reactive<Form>({
    nome: "",
    descricao: "",
    icone: "fa-user-tag",
    cor: CORES[0],
    tipoAcesso: "Profissional",
    permissoes: [],
})

const salvando = ref(false)
const excluindo = ref(false)
const erro = ref<string | null>(null)
const confirmExcluirAberto = ref(false)

watch(() => [props.modelo, props.aberto] as const, ([m, aberto]) => {
    if (!aberto) return
    if (m) {
        form.nome = m.nome
        form.descricao = m.descricao ?? ""
        form.icone = m.icone ?? "fa-user-tag"
        form.cor = m.cor ?? CORES[0]
        form.tipoAcesso = m.tipoAcesso
        form.permissoes = [...m.permissoes]
    } else {
        form.nome = ""
        form.descricao = ""
        form.icone = "fa-user-tag"
        form.cor = CORES[0]
        form.tipoAcesso = "Profissional"
        form.permissoes = []
    }
    erro.value = null
}, { immediate: true })

const ehNovo = computed(() => !props.modelo)
/** No contexto admin, padrões são editáveis — só read-only no tenant. */
const ehPadrao = computed(() => props.modelo?.ehPadrao === true && props.contexto !== "admin")
const ehContextoAdmin = computed(() => props.contexto === "admin")
const valido = computed(() => form.nome.trim().length > 1 && form.permissoes.length > 0)

const bgIconeAtual = computed(() => `color-mix(in srgb, ${form.cor} 14%, white)`)

async function salvar() {
    if (!valido.value || salvando.value) return
    salvando.value = true
    erro.value = null
    try {
        const payload = {
            nome: form.nome.trim(),
            tipoAcesso: form.tipoAcesso,
            permissoes: [...form.permissoes],
            icone: form.icone,
            cor: form.cor,
            descricao: form.descricao.trim() || null,
        }

        if (props.servicoSalvar) {
            // Contexto admin: delega ao service injetado (CA17 — reuso sem duplicar)
            await props.servicoSalvar(payload)
            emit("salvo", {
                id: props.modelo?.id ?? 0,
                ...payload,
                ehPadrao: props.modelo?.ehPadrao ?? false,
                criadoEm: props.modelo?.criadoEm ?? new Date().toISOString(),
            })
        } else if (ehNovo.value) {
            const r = await permissaoService.criar(payload)
            emit("salvo", {
                id: r.modeloId,
                ...payload,
                ehPadrao: false,
                criadoEm: new Date().toISOString(),
            })
        } else if (props.modelo) {
            await permissaoService.atualizar(props.modelo.id, payload)
            emit("salvo", { ...props.modelo, ...payload })
        }
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Não foi possível salvar a permissão."
    } finally {
        salvando.value = false
    }
}

function solicitarExclusao() {
    if (!props.modelo || excluindo.value || salvando.value) return
    if (ehContextoAdmin.value) {
        // No contexto admin, a PermissoesGlobaisListView já exibe confirmação de impacto
        // após receber o evento @excluido. Emitir diretamente sem segunda confirmação aqui.
        void executarExclusao()
    } else {
        // No contexto tenant, abre o AppConfirmDialog interno para proteção.
        confirmExcluirAberto.value = true
    }
}

async function executarExclusao() {
    if (!props.modelo) return
    excluindo.value = true
    erro.value = null
    try {
        if (props.servicoExcluir) {
            await props.servicoExcluir(props.modelo.id)
        } else {
            await permissaoService.excluir(props.modelo.id)
        }
        emit("excluido", props.modelo)
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Não foi possível excluir a permissão."
    } finally {
        excluindo.value = false
    }
}

function fechar() {
    if (salvando.value || excluindo.value) return
    emit("fechar")
}
</script>

<template>
    <AppModal
        :aberto="aberto"
        largura="lg"
        :titulo="ehPadrao ? 'Visualizar permissão do sistema' : (ehNovo ? (ehContextoAdmin ? 'Novo modelo de permissão' : 'Nova permissão personalizada') : (ehContextoAdmin ? 'Editar modelo de permissão' : 'Editar permissão'))"
        @fechar="fechar"
    >
        <!-- Identidade -->
        <div class="role-id-section">
            <div class="role-preview">
                <div class="rp-icon" :style="{ background: bgIconeAtual, color: form.cor }">
                    <i class="fa-solid" :class="form.icone"></i>
                </div>
                <div>
                    <b>{{ form.nome || "Nome da permissão" }}</b>
                    <span>{{ form.descricao || "Descrição curta..." }}</span>
                </div>
            </div>

            <div class="form-grid">
                <AppField label="Nome da permissão" required class="full">
                    <AppInput v-model="form.nome" placeholder="Ex: Coordenador de unidade" :disabled="ehPadrao" />
                </AppField>

                <AppField label="Descrição" class="full">
                    <AppInput v-model="form.descricao" placeholder="O que esta permissão faz na clínica?" :disabled="ehPadrao" />
                </AppField>

                <AppField label="Ícone">
                    <div class="icon-picker">
                        <button
                            v-for="ic in ICONES" :key="ic"
                            type="button"
                            class="ip-btn"
                            :class="{ active: form.icone === ic }"
                            :style="form.icone === ic ? { color: form.cor, borderColor: form.cor } : null"
                            :disabled="ehPadrao"
                            @click="form.icone = ic"
                        >
                            <i class="fa-solid" :class="ic"></i>
                        </button>
                    </div>
                </AppField>

                <AppField label="Cor">
                    <div class="color-picker">
                        <button
                            v-for="c in CORES" :key="c"
                            type="button"
                            class="cp-btn"
                            :class="{ active: form.cor === c }"
                            :style="{ background: c }"
                            :disabled="ehPadrao"
                            @click="form.cor = c"
                        ></button>
                    </div>
                </AppField>

                <AppField label="Tipo de acesso" class="full">
                    <select v-model="form.tipoAcesso" class="acesso-select" :disabled="ehPadrao">
                        <option value="Profissional">Profissional (médico, dentista, etc.)</option>
                        <option value="Recepcionista">Recepção / administrativo</option>
                    </select>
                </AppField>
            </div>
        </div>

        <!-- Permissões -->
        <div class="role-perm-section">
            <div class="rps-head">
                <h3>Acessos desta permissão</h3>
                <span>{{ form.permissoes.length }} acessos selecionados</span>
            </div>
            <AppPermissionMatrix v-model="form.permissoes" :read-only="ehPadrao" />
        </div>

        <p v-if="erro" class="msg-erro">{{ erro }}</p>

        <template #rodape>
            <!-- Excluir: para papéis customizados existentes (não padrão) no tenant;
                          no contexto admin, pode excluir padrões também. -->
            <AppButton
                v-if="!ehNovo && (ehContextoAdmin || !props.modelo?.ehPadrao)"
                variant="danger"
                icon="fa-solid fa-trash"
                :loading="excluindo"
                :disabled="excluindo || salvando"
                @click="solicitarExclusao"
            >
                Excluir permissão
            </AppButton>
            <div class="rodape-spacer"></div>
            <AppButton variant="ghost" :disabled="salvando || excluindo" @click="fechar">
                {{ ehPadrao ? "Fechar" : "Cancelar" }}
            </AppButton>
            <AppButton
                v-if="!ehPadrao"
                icon="fa-solid fa-floppy-disk"
                :loading="salvando"
                :disabled="!valido || salvando || excluindo"
                @click="salvar"
            >
                {{ ehNovo ? "Criar permissão" : "Salvar alterações" }}
            </AppButton>
        </template>
    </AppModal>

    <!-- Confirmação de exclusão no contexto tenant (no contexto admin, a list view confirma). -->
    <AppConfirmDialog
        v-model:aberto="confirmExcluirAberto"
        titulo="Excluir permissão?"
        :mensagem="`Excluir a permissão &quot;${props.modelo?.nome}&quot;? Esta ação é irreversível. Profissionais vinculados precisarão receber outra permissão.`"
        confirmar-rotulo="Excluir"
        variante="danger"
        icone="fa-solid fa-trash"
        :executando="excluindo"
        @confirmar="executarExclusao"
    />
</template>

<style scoped>
.role-id-section {
    display: grid;
    grid-template-columns: 280px 1fr;
    gap: 24px;
    padding-bottom: 20px;
    border-bottom: 1px solid hsl(var(--secondary) / 0.08);
    margin-bottom: 22px;
}

.role-preview {
    display: flex; flex-direction: column; align-items: center; gap: 10px;
    background: hsl(var(--secondary) / 0.03); border-radius: 12px;
    padding: 24px 16px; text-align: center;
}
.rp-icon {
    width: 64px; height: 64px; border-radius: 12px;
    display: inline-flex; align-items: center; justify-content: center;
    font-size: 28px;
}
.role-preview b { font-size: 15px; color: hsl(var(--primary-dark)); font-weight: 700; }
.role-preview span { font-size: 12px; color: hsl(var(--secondary) / 0.65); }

.form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
.form-grid :deep(.full) { grid-column: 1 / -1; }

.icon-picker, .color-picker { display: flex; flex-wrap: wrap; gap: 6px; }
.ip-btn {
    width: 36px; height: 36px; border-radius: 8px;
    background: white; border: 1.5px solid hsl(var(--secondary) / 0.12);
    cursor: pointer; font-size: 14px; color: hsl(var(--secondary) / 0.6);
    transition: all 150ms;
}
.ip-btn:hover { border-color: hsl(var(--primary) / 0.4); }
.ip-btn:disabled { opacity: 0.4; cursor: not-allowed; }
.ip-btn.active { border-width: 2px; }
.cp-btn {
    width: 32px; height: 32px; border-radius: 50%;
    border: 2px solid white; cursor: pointer;
    box-shadow: 0 0 0 1.5px hsl(var(--secondary) / 0.15);
    transition: all 150ms;
}
.cp-btn:hover { transform: scale(1.08); }
.cp-btn:disabled { opacity: 0.4; cursor: not-allowed; }
.cp-btn.active { box-shadow: 0 0 0 2.5px hsl(var(--primary-dark)); transform: scale(1.08); }

.acesso-select {
    padding: 10px 12px;
    border: 1px solid hsl(var(--secondary) / 0.15);
    border-radius: 8px;
    background: white;
    font-family: inherit;
    font-size: 13px;
    color: hsl(var(--secondary));
    width: 100%;
}
.acesso-select:disabled { background: hsl(var(--secondary) / 0.04); color: hsl(var(--secondary) / 0.5); }

.role-perm-section { margin-top: 4px; }
.rps-head {
    display: flex; align-items: baseline; justify-content: space-between;
    margin-bottom: 12px;
}
.rps-head h3 { font-size: 14px; font-weight: 700; color: hsl(var(--primary-dark)); margin: 0; }
.rps-head span { font-size: 12px; color: hsl(var(--secondary) / 0.6); }

.msg-erro { color: hsl(var(--error)); font-size: 13px; margin: 0; }

/* Empurra "Cancelar" e "Salvar" para a direita, mantendo "Excluir" à esquerda. */
.rodape-spacer { flex: 1; }

@media (max-width: 720px) {
    .role-id-section { grid-template-columns: 1fr; }
    .form-grid { grid-template-columns: 1fr; }
}
</style>
