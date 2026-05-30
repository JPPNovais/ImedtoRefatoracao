<script setup lang="ts">
/**
 * Formulário de criação/edição de modelo de prontuário global.
 * conteudoJson deve ser JSON válido — validado antes de salvar.
 * W3-CA7 a W3-CA11: app-page--narrow + AppPageHeader + AppCard + AppField + AppInput + AppTextarea + AppButton.
 */
import { ref, onMounted, computed } from "vue"
import { useRouter } from "vue-router"
import { AppPageHeader, AppCard, AppField, AppInput, AppTextarea, AppButton } from "@/components/ui"
import { useModelosGlobaisStore } from "../stores/modelosGlobaisStore"

const props = defineProps<{ id?: string }>()
const router = useRouter()
const store = useModelosGlobaisStore()

const editando = computed(() => !!props.id)

const nome = ref("")
const descricao = ref("")
const conteudoJson = ref("{\n  \n}")
const motivo = ref("")
const erros = ref<Record<string, string>>({})
const salvando = ref(false)
const erroGeral = ref("")

onMounted(async () => {
    if (props.id) {
        await store.carregarItem(props.id)
        if (store.itemAtual) {
            nome.value = store.itemAtual.nome
            descricao.value = store.itemAtual.descricao ?? ""
            conteudoJson.value = store.itemAtual.conteudoJson
        }
    }
})

function validarJson(valor: string): boolean {
    try { JSON.parse(valor); return true } catch { return false }
}

function validar(): boolean {
    const e: Record<string, string> = {}
    if (!nome.value.trim()) e.nome = "Nome é obrigatório."
    if (!conteudoJson.value.trim()) e.conteudoJson = "Conteúdo JSON é obrigatório."
    else if (!validarJson(conteudoJson.value)) e.conteudoJson = "JSON inválido."
    if (motivo.value.trim().length < 10) e.motivo = "Motivo deve ter ao menos 10 caracteres."
    erros.value = e
    return Object.keys(e).length === 0
}

async function salvar() {
    if (!validar()) return
    salvando.value = true
    erroGeral.value = ""
    try {
        const payload = {
            nome: nome.value.trim(),
            descricao: descricao.value.trim() || null,
            conteudoJson: conteudoJson.value,
            motivo: motivo.value.trim(),
        }
        if (editando.value && props.id) {
            await store.atualizar(props.id, payload)
        } else {
            await store.criar(payload)
        }
        router.push({ name: "AdminModelosGlobais" })
    } catch (err: unknown) {
        const msg = (err as { response?: { data?: { mensagem?: string } } })?.response?.data?.mensagem
        erroGeral.value = msg ?? "Não foi possível salvar o modelo."
    } finally {
        salvando.value = false
    }
}

function formatarJson() {
    try {
        conteudoJson.value = JSON.stringify(JSON.parse(conteudoJson.value), null, 2)
        if (erros.value.conteudoJson) delete erros.value.conteudoJson
    } catch {
        erros.value.conteudoJson = "JSON inválido."
    }
}
</script>

<template>
    <main class="app-page app-page--narrow">
        <AppPageHeader
            :titulo="editando ? 'Editar modelo de prontuário' : 'Novo modelo de prontuário'"
        />

        <div v-if="store.carregando && editando" class="estado-info">
            <i class="fa-solid fa-spinner fa-spin"></i> Carregando...
        </div>
        <p v-else-if="store.erro && editando" class="estado-erro">{{ store.erro }}</p>

        <AppCard v-else>
            <form @submit.prevent="salvar" class="form-campos">
                <AppField label="Nome" required :hint="erros.nome">
                    <AppInput
                        v-model="nome"
                        placeholder="Ex.: Consulta médica padrão"
                        maxlength="200"
                        :disabled="salvando"
                    />
                </AppField>

                <AppField label="Descrição">
                    <AppInput
                        v-model="descricao"
                        placeholder="Descrição opcional"
                        maxlength="500"
                        :disabled="salvando"
                    />
                </AppField>

                <AppField label="Conteúdo JSON" required :hint="erros.conteudoJson || 'Cole a estrutura JSON do template de prontuário.'">
                    <template #label-aside>
                        <AppButton variant="ghost" size="sm" type="button" @click="formatarJson">Formatar</AppButton>
                    </template>
                    <AppTextarea
                        v-model="conteudoJson"
                        :rows="16"
                        placeholder='{ "secoes": [] }'
                        :disabled="salvando"
                        style="font-family: monospace; font-size: 0.8125rem;"
                    />
                </AppField>

                <AppField label="Motivo da alteração" required hint="Mínimo 10 caracteres.">
                    <AppInput
                        v-model="motivo"
                        placeholder="Descreva o motivo..."
                        :disabled="salvando"
                    />
                </AppField>

                <p v-if="erroGeral" class="campo-erro" role="alert">{{ erroGeral }}</p>

                <div class="form-acoes">
                    <AppButton variant="ghost" type="button" @click="router.push({ name: 'AdminModelosGlobais' })">Cancelar</AppButton>
                    <AppButton
                        type="submit"
                        :loading="salvando"
                        :disabled="motivo.trim().length < 10"
                    >
                        {{ editando ? "Salvar alterações" : "Criar modelo" }}
                    </AppButton>
                </div>
            </form>
        </AppCard>
    </main>
</template>

<style scoped>
.estado-info {
    text-align: center;
    padding: 2rem 0;
    color: hsl(var(--muted-foreground));
    font-size: 0.875rem;
}

.estado-erro {
    padding: 0.75rem 1rem;
    background: hsl(var(--destructive) / 0.1);
    color: hsl(var(--destructive));
    border: 1px solid hsl(var(--destructive) / 0.3);
    border-radius: calc(var(--radius) - 2px);
    font-size: 0.875rem;
}

.form-campos {
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
}

.campo-erro {
    padding: 0.75rem 1rem;
    background: hsl(var(--destructive) / 0.1);
    color: hsl(var(--destructive));
    border: 1px solid hsl(var(--destructive) / 0.3);
    border-radius: calc(var(--radius) - 2px);
    font-size: 0.875rem;
    margin: 0;
}

.form-acoes {
    display: flex;
    gap: 0.75rem;
    justify-content: flex-end;
    padding-top: 0.5rem;
}
</style>
