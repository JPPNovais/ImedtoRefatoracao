<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from "vue"
import { vMaska } from "maska/vue"
import {
    AppButton, AppField, AppInput, AppModal, AppSelect, AppTextarea,
} from "@/components/ui"
import { vinculoService } from "@/services/vinculoService"
import { catalogoService } from "@/services/catalogoService"
import type { ProfissaoCatalogo, EspecialidadeCatalogo } from "@/services/catalogoService"
import type { ModeloPermissao } from "@/services/permissaoService"

const LIMITE_MENSAGEM = 1000

const TEMPLATES = [
    {
        id: "formal",
        label: "Formal",
        texto: "Olá! Você foi convidado(a) para integrar a equipe da nossa clínica em nosso sistema de gestão. Ao aceitar, você terá acesso à agenda e aos recursos necessários para o seu atendimento. Ficamos à disposição para qualquer dúvida.",
    },
    {
        id: "amigavel",
        label: "Amigável",
        texto: "Oi! Que bom ter você com a gente 😊 Estamos te convidando para fazer parte da equipe da clínica. É só aceitar o convite para começar. Qualquer dúvida, estamos por aqui!",
    },
    {
        id: "curto",
        label: "Curto e direto",
        texto: "Você foi convidado(a) para a equipe da clínica. Aceite o convite para acessar o sistema.",
    },
] as const

type TemplateId = typeof TEMPLATES[number]["id"]

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
    profissaoId: number | null
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
    profissaoId: null,
    especialidade: "",
    conselho: "",
    mensagem: "",
})

const enviando = ref(false)
const erro = ref<string | null>(null)
const templateAtivo = ref<TemplateId | null>(null)

function selecionarTemplate(id: TemplateId) {
    if (templateAtivo.value === id) {
        // segundo clique no mesmo chip: deseleciona e limpa
        templateAtivo.value = null
        form.mensagem = ""
        return
    }
    templateAtivo.value = id
    form.mensagem = TEMPLATES.find(t => t.id === id)!.texto
}

const charCount = computed(() => form.mensagem.length)
const mensagemInvalida = computed(() => charCount.value > LIMITE_MENSAGEM)

const profissoes = ref<ProfissaoCatalogo[]>([])
const especialidades = ref<EspecialidadeCatalogo[]>([])
const carregandoEspecialidades = ref(false)

function reset() {
    form.metodo = "email"
    form.nome = ""
    form.email = ""
    form.telefone = ""
    form.modeloId = null
    form.profissaoId = null
    form.especialidade = ""
    form.conselho = ""
    form.mensagem = ""
    especialidades.value = []
    erro.value = null
    templateAtivo.value = null
}

onMounted(async () => {
    try {
        profissoes.value = await catalogoService.listarProfissoes()
    } catch {
        // falha silenciosa — campo fica vazio mas não bloqueia o modal
    }
})

watch(() => form.profissaoId, async (id) => {
    form.especialidade = ""
    especialidades.value = []
    if (!id) return
    carregandoEspecialidades.value = true
    try {
        especialidades.value = await catalogoService.listarEspecialidades(id)
    } catch {
        // falha silenciosa
    } finally {
        carregandoEspecialidades.value = false
    }
})

// Profissão/Especialidade são CAMPOS DE IDENTIFICAÇÃO do profissional (não de
// permissão). Sempre opcionais — a permissão escolhida acima nao gateia esses
// campos. O backend valida apenas que profissaoId vem se especialidade for
// informada (ConvidarProfissionalCommandHandler).
const modeloSelecionado = computed(() =>
    props.modelos.find(m => m.id === form.modeloId) ?? null,
)

const profissaoTemEspecialidades = computed(() =>
    form.profissaoId !== null && (carregandoEspecialidades.value || especialidades.value.length > 0),
)

const valido = computed(() => {
    if (form.nome.trim().length < 2) return false
    if (!form.email.includes("@") || !form.email.includes(".")) return false
    if (!form.modeloId) return false
    if (form.metodo === "whatsapp" && form.telefone.replace(/\D/g, "").length < 10) return false
    if (mensagemInvalida.value) return false
    return true
})

async function enviar() {
    if (!valido.value || enviando.value) return
    enviando.value = true
    erro.value = null
    try {
        const mensagemFinal = form.mensagem.trim() || null
        const r = await vinculoService.convidarProfissional({
            email: form.email.trim(),
            modeloPermissaoId: form.modeloId,
            nome: form.nome.trim() || null,
            telefone: form.telefone.trim() || null,
            especialidade: form.especialidade.trim() || null,
            profissaoId: form.profissaoId,
            mensagemPersonalizada: mensagemFinal,
        })
        emit("enviado", { nome: form.nome, email: form.email, actionLink: r.actionLink })
        reset()
    } catch {
        // CA11: mensagem genérica — nunca expõe o conteúdo da mensagem no erro
        erro.value = "Não foi possível enviar o convite. Tente novamente."
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
                <AppInput v-model="form.telefone" v-maska="'(##) #####-####'" type="tel" placeholder="(11) 99999-9999" />
            </AppField>

            <AppField label="Permissão" required class="full">
                <AppSelect
                    :model-value="form.modeloId"
                    @update:model-value="form.modeloId = $event ? Number($event) : null"
                >
                    <option :value="null">Selecione...</option>
                    <option v-for="m in modelos" :key="m.id" :value="m.id">
                        {{ m.nome }}{{ m.ehPadrao ? '' : ' (customizado)' }}
                    </option>
                </AppSelect>
                <div v-if="modeloSelecionado" class="modelo-preview">
                    <div class="mp-icon" :style="{ background: bgIcone(modeloSelecionado.cor), color: modeloSelecionado.cor || 'hsl(0 0% 45%)' }">
                        <i class="fa-solid" :class="modeloSelecionado.icone || (modeloSelecionado.tipoAcesso === 'Profissional' ? 'fa-user-doctor' : 'fa-headset')"></i>
                    </div>
                    <span>{{ modeloSelecionado.descricao || (modeloSelecionado.tipoAcesso === 'Profissional' ? 'Acesso a agenda, prontuário e pacientes' : 'Acesso a agenda e pacientes') }}</span>
                </div>
            </AppField>

            <AppField label="Profissão (opcional)">
                <AppSelect
                    :model-value="form.profissaoId"
                    @update:model-value="form.profissaoId = $event ? Number($event) : null"
                >
                    <option :value="null">Selecione...</option>
                    <option v-for="p in profissoes" :key="p.id" :value="p.id">{{ p.nome }}</option>
                </AppSelect>
            </AppField>

            <AppField v-if="profissaoTemEspecialidades" label="Especialidade (opcional)">
                <AppSelect
                    :model-value="form.especialidade"
                    :disabled="carregandoEspecialidades"
                    @update:model-value="form.especialidade = String($event)"
                >
                    <option value="">
                        {{ carregandoEspecialidades ? 'Carregando...' : 'Selecione...' }}
                    </option>
                    <option v-for="e in especialidades" :key="e.id" :value="e.nome">{{ e.nome }}</option>
                </AppSelect>
            </AppField>

            <AppField label="Conselho profissional (opcional)">
                <AppInput v-model="form.conselho" placeholder="CRM 12.345-SP" />
            </AppField>

            <!-- Seção de mensagem personalizada com chips de template e contador -->
            <div class="full mensagem-section">
                <label class="mensagem-label">Mensagem personalizada (opcional)</label>
                <div class="template-chips">
                    <AppButton
                        v-for="t in TEMPLATES"
                        :key="t.id"
                        size="sm"
                        :variant="templateAtivo === t.id ? 'primary' : 'ghost'"
                        @click="selecionarTemplate(t.id)"
                    >
                        {{ t.label }}
                    </AppButton>
                </div>
                <div :class="{ 'textarea-wrapper--erro': mensagemInvalida }">
                    <AppTextarea
                        v-model="form.mensagem"
                        :rows="4"
                        placeholder="Escolha um template acima ou escreva sua mensagem..."
                    />
                </div>
                <div class="mensagem-footer">
                    <span v-if="mensagemInvalida" class="contador-erro">
                        Limite de {{ LIMITE_MENSAGEM }} caracteres atingido
                    </span>
                    <span v-else class="contador-neutro" />
                    <span class="contador" :class="{ 'contador--erro': mensagemInvalida }">
                        {{ charCount }}/{{ LIMITE_MENSAGEM }}
                    </span>
                </div>
            </div>
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

.modelo-preview {
    display: flex; align-items: center; gap: 10px;
    margin-top: 8px; padding: 8px 12px;
    background: hsl(var(--primary) / 0.04);
    border: 1px solid hsl(var(--primary) / 0.12);
    border-radius: 6px;
}
.mp-icon {
    width: 28px; height: 28px; border-radius: 6px;
    display: inline-flex; align-items: center; justify-content: center;
    font-size: 12px; flex-shrink: 0;
}
.modelo-preview > span {
    font-size: 12px; color: hsl(var(--secondary) / 0.75); line-height: 1.4;
}

/* Seção de mensagem personalizada */
.mensagem-section { display: flex; flex-direction: column; gap: 8px; }
.mensagem-label {
    display: block; font-size: 12px; font-weight: 600;
    color: hsl(var(--primary-dark)); margin-bottom: 0;
}
.template-chips { display: flex; flex-wrap: wrap; gap: 6px; }
.mensagem-footer {
    display: flex; align-items: center; justify-content: flex-end; gap: 8px; min-height: 16px;
}
.contador {
    font-size: 11px; color: hsl(var(--secondary) / 0.55); flex-shrink: 0;
}
.contador--erro { color: hsl(var(--error)); font-weight: 600; }
.contador-erro {
    font-size: 11px; color: hsl(var(--error)); flex: 1;
}
.contador-neutro { flex: 1; }
/* Wrapper de textarea com estado de erro */
.textarea-wrapper--erro :deep(textarea) {
    border-color: hsl(var(--error));
    outline-color: hsl(var(--error));
}
</style>
