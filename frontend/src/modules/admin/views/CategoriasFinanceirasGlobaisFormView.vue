<script setup lang="ts">
/**
 * CategoriasFinanceirasGlobaisFormView — criação de categoria financeira padrão.
 * Briefing 2026-06-22_003 M3. Sem edição de nome (R5 — imutável após criação).
 * Ao criar, exibe feedback com número de instâncias propagadas.
 */
import { ref } from "vue"
import { useRouter } from "vue-router"
import { AppPageHeader, AppCard, AppField, AppInput, AppButton } from "@/components/ui"
import { useCategoriasFinanceirasGlobaisStore } from "../stores/categoriasFinanceirasGlobaisStore"
import type { TipoCategoriaFinanceira } from "../services/catalogosService"

const router = useRouter()
const store = useCategoriasFinanceirasGlobaisStore()

const nome = ref("")
const tipo = ref<TipoCategoriaFinanceira>("Receita")
const erros = ref<Record<string, string>>({})
const salvando = ref(false)
const erroGeral = ref("")
const feedbackSucesso = ref<string | null>(null)

function validar(): boolean {
    const e: Record<string, string> = {}
    if (!nome.value.trim()) e.nome = "Nome é obrigatório."
    erros.value = e
    return Object.keys(e).length === 0
}

async function salvar() {
    if (!validar()) return
    salvando.value = true
    erroGeral.value = ""
    feedbackSucesso.value = null
    try {
        const resultado = await store.criar({
            nome: nome.value.trim(),
            tipo: tipo.value,
        })
        const n = resultado.instanciasPropagadas
        if (n !== undefined && n !== null) {
            feedbackSucesso.value = `Categoria padrão criada e propagada para ${n} estabelecimento${n !== 1 ? "s" : ""}.`
        } else {
            feedbackSucesso.value = "Categoria padrão criada com sucesso."
        }
        nome.value = ""
    } catch (err: unknown) {
        const msg = (err as { response?: { data?: { mensagem?: string } } })?.response?.data?.mensagem
        erroGeral.value = msg ?? "Não foi possível criar a categoria."
    } finally {
        salvando.value = false
    }
}
</script>

<template>
    <main class="app-page app-page--narrow">
        <AppPageHeader titulo="Nova categoria financeira padrão" />

        <AppCard>
            <p class="aviso-imutavel">
                <i class="fa-solid fa-circle-info" aria-hidden="true"></i>
                O nome é imutável após a criação. Para "renomear", inative a antiga e crie uma nova com o nome correto.
            </p>

            <form @submit.prevent="salvar" class="form-campos">
                <AppField label="Nome" required :hint="erros.nome">
                    <AppInput
                        v-model="nome"
                        placeholder="Ex: Telemedicina"
                        maxlength="80"
                        :disabled="salvando"
                    />
                </AppField>

                <AppField label="Tipo" required>
                    <div class="tipo-opcoes">
                        <label class="tipo-radio">
                            <input v-model="tipo" type="radio" value="Receita" :disabled="salvando" />
                            Receita
                        </label>
                        <label class="tipo-radio">
                            <input v-model="tipo" type="radio" value="Despesa" :disabled="salvando" />
                            Despesa
                        </label>
                    </div>
                </AppField>

                <div v-if="feedbackSucesso" class="feedback-sucesso" role="status">
                    <i class="fa-solid fa-circle-check" aria-hidden="true"></i>
                    {{ feedbackSucesso }}
                </div>

                <p v-if="erroGeral" class="campo-erro" role="alert">{{ erroGeral }}</p>

                <div class="form-acoes">
                    <AppButton
                        variant="ghost"
                        type="button"
                        @click="router.push({ name: 'AdminCategoriasFinanceirasGlobais' })"
                    >
                        Voltar à lista
                    </AppButton>
                    <AppButton
                        type="submit"
                        :loading="salvando"
                        :disabled="salvando"
                    >
                        Criar categoria
                    </AppButton>
                </div>
            </form>
        </AppCard>
    </main>
</template>

<style scoped>
.aviso-imutavel {
    display: flex;
    align-items: flex-start;
    gap: 0.5rem;
    padding: 0.75rem 1rem;
    background: hsl(var(--warning, 40 100% 50%) / 0.1);
    border: 1px solid hsl(var(--warning, 40 100% 50%) / 0.3);
    border-radius: calc(var(--radius) - 2px);
    font-size: var(--text-sm);
    color: hsl(var(--foreground));
    margin-bottom: 1.25rem;
}

.form-campos {
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
}

.tipo-opcoes {
    display: flex;
    gap: 1.5rem;
    align-items: center;
    padding: 0.5rem 0;
}

.tipo-radio {
    display: flex;
    align-items: center;
    gap: 0.4rem;
    font-size: var(--text-sm);
    cursor: pointer;
}

.feedback-sucesso {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.75rem 1rem;
    background: hsl(var(--success, 140 60% 50%) / 0.1);
    border: 1px solid hsl(var(--success, 140 60% 50%) / 0.3);
    border-radius: calc(var(--radius) - 2px);
    font-size: var(--text-sm);
    color: hsl(var(--success, 140 60% 30%));
}

.campo-erro {
    padding: 0.75rem 1rem;
    background: hsl(var(--destructive) / 0.1);
    color: hsl(var(--destructive));
    border: 1px solid hsl(var(--destructive) / 0.3);
    border-radius: calc(var(--radius) - 2px);
    font-size: var(--text-sm);
    margin: 0;
}

.form-acoes {
    display: flex;
    gap: 0.75rem;
    justify-content: flex-end;
    padding-top: 0.5rem;
}
</style>
