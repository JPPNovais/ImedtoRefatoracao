<script setup lang="ts">
import { computed, reactive } from "vue"
import {
    AppButton, AppField, AppInput, AppModal, AppSelect, AppTextarea,
} from "@/components/ui"
import { vinculoService } from "@/services/vinculoService"
import type { ModeloPermissao } from "@/services/permissaoService"

/**
 * Modal de convite de profissional. Usa o role-selector visual do design
 * (cards clicáveis com ícone/cor/descrição do papel).
 *
 * Backend hoje só envia por e-mail; o método "WhatsApp" do design é apresentado
 * como opção visual mas, ao salvar, o backend manda link por e-mail mesmo
 * (o telefone é registrado no convite para chat futuro).
 */
const props = defineProps<{
    aberto: boolean
    modelos: ModeloPermissao[]
}>()

const emit = defineEmits<{
    (e: "fechar"): void
    (e: "enviado", payload: { nome: string, email: string, actionLink?: string | null }): void
}>()

interface Form {
    metodo: "email" | "whatsapp"
    nome: string
    email: string
    telefone: string
    modeloId: number | null
    especialidade: string
    conselho: string
    mensagem: string
}

const form = reactive<Form>({
    metodo: "email",
    nome: "",
    email: "",
    telefone: "",
    modeloId: null,
    especialidade: "",
    conselho: "",
    mensagem: "",
})

const enviando = ref(false)
const erro = ref<string | null>(null)

import { ref } from "vue"

function reset() {
    form.metodo = "email"
    form.nome = ""
    form.email = ""
    form.telefone = ""
    form.modeloId = null
    form.especialidade = ""
    form.conselho = ""
    form.mensagem = ""
    erro.value = null
}

const modeloSelecionado = computed(() =>
    props.modelos.find(m => m.id === form.modeloId) ?? null,
)

// Especialidade só é exigida para papéis "Profissional" (médicos/dentistas etc.).
const exigeEspecialidade = computed(() =>
    modeloSelecionado.value?.tipoAcesso === "Profissional",
)

const valido = computed(() => {
    if (form.nome.trim().length < 2) return false
    if (!form.email.includes("@") || !form.email.includes(".")) return false
    if (!form.modeloId) return false
    if (exigeEspecialidade.value && form.especialidade.trim().length < 2) return false
    if (form.metodo === "whatsapp" && form.telefone.replace(/\D/g, "").length < 10) return false
    return true
})

async function enviar() {
    if (!valido.value || enviando.value) return
    enviando.value = true
    erro.value = null
    try {
        const r = await vinculoService.convidarProfissional({
            email: form.email.trim(),
            modeloPermissaoId: form.modeloId,
            nome: form.nome.trim() || null,
            telefone: form.telefone.trim() || null,
            especialidade: form.especialidade.trim() || null,
        })
        emit("enviado", { nome: form.nome, email: form.email, actionLink: r.actionLink })
        reset()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Não foi possível enviar o convite."
    } finally {
        enviando.value = false
    }
}

function bgIcone(cor?: string | null): string {
    return `color-mix(in srgb, ${cor ?? "hsl(0 0% 45%)"} 14%, white)`
}

function fechar() {
    if (enviando.value) return
    emit("fechar")
}
</script>

<template>
    <AppModal :aberto="aberto" largura="lg" titulo="Convidar profissional" @fechar="fechar">
        <p class="hint">
            Envie um convite para o profissional criar a conta dele e ter acesso ao estabelecimento.
        </p>

        <!-- Método -->
        <div class="invite-method">
            <label>Como enviar o convite?</label>
            <div class="method-toggles">
                <button
                    type="button"
                    class="mt"
                    :class="{ on: form.metodo === 'email' }"
                    @click="form.metodo = 'email'"
                >
                    <i class="fa-solid fa-envelope"></i>
                    <div>
                        <b>Por e-mail</b>
                        <span>Link enviado para o e-mail informado</span>
                    </div>
                </button>
                <button
                    type="button"
                    class="mt"
                    :class="{ on: form.metodo === 'whatsapp' }"
                    @click="form.metodo = 'whatsapp'"
                >
                    <i class="fa-brands fa-whatsapp"></i>
                    <div>
                        <b>Por WhatsApp</b>
                        <span>Mensagem com link para o telefone</span>
                    </div>
                </button>
            </div>
        </div>

        <div class="form-grid">
            <AppField label="Nome completo" required class="full">
                <AppInput v-model="form.nome" placeholder="Ex: Dra. Helena Castro" />
            </AppField>

            <AppField label="E-mail" required>
                <AppInput v-model="form.email" type="email" placeholder="profissional@exemplo.com" />
            </AppField>

            <AppField :label="form.metodo === 'whatsapp' ? 'Telefone (obrigatório)' : 'Telefone (opcional)'" :required="form.metodo === 'whatsapp'">
                <AppInput v-model="form.telefone" type="tel" placeholder="(11) 99999-9999" />
            </AppField>

            <AppField label="Função / Papel" required class="full">
                <div class="role-selector">
                    <button
                        v-for="m in modelos" :key="m.id"
                        type="button"
                        class="rs"
                        :class="{ on: form.modeloId === m.id }"
                        @click="form.modeloId = m.id"
                    >
                        <div class="rs-icon" :style="{ background: bgIcone(m.cor), color: m.cor || 'hsl(0 0% 45%)' }">
                            <i class="fa-solid" :class="m.icone || (m.tipoAcesso === 'Profissional' ? 'fa-user-doctor' : 'fa-headset')"></i>
                        </div>
                        <div class="rs-info">
                            <b>{{ m.nome }}</b>
                            <span>{{ m.descricao || (m.tipoAcesso === 'Profissional' ? 'Acesso a agenda, prontuário e pacientes' : 'Acesso a agenda e pacientes') }}</span>
                        </div>
                        <span v-if="!m.ehPadrao" class="rs-tag">Customizado</span>
                    </button>
                </div>
            </AppField>

            <template v-if="exigeEspecialidade">
                <AppField label="Especialidade" required>
                    <AppInput v-model="form.especialidade" placeholder="Ex: Cardiologia" />
                </AppField>

                <AppField label="Conselho profissional (opcional)">
                    <AppInput v-model="form.conselho" placeholder="CRM 12.345-SP" />
                </AppField>
            </template>

            <AppField label="Mensagem personalizada (opcional)" class="full">
                <AppTextarea
                    v-model="form.mensagem"
                    :rows="3"
                    placeholder="Olá, gostaríamos de te convidar para fazer parte da nossa equipe..."
                />
            </AppField>
        </div>

        <p v-if="erro" class="msg-erro">{{ erro }}</p>

        <template #rodape>
            <AppButton variant="ghost" :disabled="enviando" @click="fechar">Cancelar</AppButton>
            <AppButton
                :icon="form.metodo === 'whatsapp' ? 'fa-brands fa-whatsapp' : 'fa-solid fa-paper-plane'"
                :loading="enviando"
                :disabled="!valido || enviando"
                @click="enviar"
            >
                Enviar convite
            </AppButton>
        </template>
    </AppModal>
</template>

<style scoped>
.hint { font-size: 13px; color: hsl(var(--secondary) / 0.7); margin: 0 0 8px; }
.msg-erro { color: hsl(var(--error)); font-size: 13px; margin: 0; }

.invite-method { margin-bottom: 6px; }
.invite-method > label {
    display: block; font-size: 12px; font-weight: 600; color: hsl(var(--primary-dark));
    margin-bottom: 8px;
}
.method-toggles { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; }
.mt {
    display: flex; align-items: center; gap: 12px;
    background: white; border: 1.5px solid hsl(var(--secondary) / 0.12);
    padding: 12px 14px; border-radius: 8px;
    cursor: pointer; text-align: left; font-family: inherit;
    transition: all 150ms;
}
.mt:hover { border-color: hsl(var(--primary) / 0.4); }
.mt.on { border-color: hsl(var(--primary)); background: hsl(var(--primary) / 0.05); }
.mt > i { font-size: 22px; color: hsl(var(--secondary) / 0.5); width: 28px; text-align: center; }
.mt.on > i { color: hsl(var(--primary)); }
.mt > div { display: flex; flex-direction: column; gap: 2px; }
.mt b { font-size: 13px; color: hsl(var(--primary-dark)); font-weight: 700; }
.mt span { font-size: 11px; color: hsl(var(--secondary) / 0.6); }

.form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
.form-grid :deep(.full) { grid-column: 1 / -1; }
@media (max-width: 720px) { .form-grid { grid-template-columns: 1fr; } }

.role-selector { display: grid; grid-template-columns: 1fr 1fr; gap: 8px; }
.rs {
    display: flex; align-items: center; gap: 10px;
    background: white; border: 1.5px solid hsl(var(--secondary) / 0.12);
    padding: 10px 12px; border-radius: 8px;
    cursor: pointer; text-align: left; font-family: inherit;
    transition: all 150ms; position: relative;
}
.rs:hover { border-color: hsl(var(--primary) / 0.4); }
.rs.on {
    border-color: hsl(var(--primary)); background: hsl(var(--primary) / 0.05);
    box-shadow: 0 0 0 3px hsl(var(--primary) / 0.08);
}
.rs-icon {
    width: 32px; height: 32px; border-radius: 6px;
    display: inline-flex; align-items: center; justify-content: center;
    font-size: 13px; flex-shrink: 0;
}
.rs-info { flex: 1; min-width: 0; }
.rs-info b { display: block; font-size: 13px; color: hsl(var(--primary-dark)); font-weight: 700; }
.rs-info span {
    font-size: 11px; color: hsl(var(--secondary) / 0.65);
    display: -webkit-box; -webkit-line-clamp: 1; -webkit-box-orient: vertical; overflow: hidden;
}
.rs-tag {
    position: absolute; top: 6px; right: 8px;
    font-size: 9px; font-weight: 700; padding: 2px 6px; border-radius: 999px;
    background: hsl(var(--primary) / 0.12); color: hsl(var(--primary));
}
</style>
