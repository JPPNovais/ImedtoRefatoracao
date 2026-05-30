<script setup lang="ts">
import { ref, computed, onMounted } from "vue"
import { useRouter, useRoute } from "vue-router"
import { usePlanosStore } from "../stores/planosStore"

const router = useRouter()
const route = useRoute()
const store = usePlanosStore()

const isEdicao = computed(() => !!route.params.id)
const idEdicao = computed(() => route.params.id as string | undefined)

const nome = ref("")
const descricaoCurta = ref("")
const precoTexto = ref("") // Em reais, ex: "99.90"
const gratuito = ref(false)
const limitesJson = ref("{}")
const motivo = ref("")
const erroLimitesJson = ref("")
const erroGeral = ref("")
const salvando = ref(false)

onMounted(async () => {
    if (isEdicao.value && idEdicao.value) {
        await store.carregarPlano(idEdicao.value)
        const p = store.planoAtual
        if (p) {
            nome.value = p.nome
            descricaoCurta.value = p.descricaoCurta ?? ""
            precoTexto.value = p.precoMensalCentavos != null
                ? (p.precoMensalCentavos / 100).toFixed(2)
                : ""
            gratuito.value = p.gratuito
            limitesJson.value = p.limitesJson ?? "{}"
        }
    }
})

function validarJson(): boolean {
    try {
        JSON.parse(limitesJson.value)
        erroLimitesJson.value = ""
        return true
    } catch {
        erroLimitesJson.value = "JSON inválido. Corrija o formato antes de salvar."
        return false
    }
}

function calcularCentavos(): number | null {
    if (!precoTexto.value.trim()) return null
    const valor = parseFloat(precoTexto.value.replace(",", "."))
    if (isNaN(valor) || valor < 0) return null
    return Math.round(valor * 100)
}

async function salvar() {
    if (!validarJson()) return
    if (!motivo.value.trim()) {
        erroGeral.value = "Motivo é obrigatório."
        return
    }

    salvando.value = true
    erroGeral.value = ""

    const payload = {
        nome: nome.value.trim(),
        descricaoCurta: descricaoCurta.value.trim() || null,
        precoMensalCentavos: calcularCentavos(),
        gratuito: gratuito.value,
        limitesJson: limitesJson.value,
        motivo: motivo.value.trim(),
    }

    try {
        if (isEdicao.value && idEdicao.value) {
            await store.atualizar(idEdicao.value, payload)
        } else {
            await store.criar(payload)
        }
        router.push({ name: "AdminPlanosList" })
    } catch (err: unknown) {
        const msg = (err as { response?: { data?: { mensagem?: string } } })?.response?.data?.mensagem
        erroGeral.value = msg ?? "Não foi possível salvar o plano."
    } finally {
        salvando.value = false
    }
}
</script>

<template>
    <div class="admin-planos-form">
        <div class="admin-form-header">
            <button class="admin-btn-voltar" @click="router.back()">← Voltar</button>
            <h1 class="admin-page-title">{{ isEdicao ? "Editar plano" : "Novo plano" }}</h1>
        </div>

        <div v-if="store.carregando" class="admin-carregando">Carregando...</div>
        <form v-else @submit.prevent="salvar" class="admin-form">
            <div class="admin-campo">
                <label class="admin-label">Nome do plano *</label>
                <input v-model="nome" class="admin-input" placeholder="Ex: Plano Profissional" required maxlength="100" />
            </div>

            <div class="admin-campo">
                <label class="admin-label">Descrição curta</label>
                <input v-model="descricaoCurta" class="admin-input" placeholder="Descrição opcional" maxlength="200" />
            </div>

            <div class="admin-campo">
                <label class="admin-label">Preço mensal (R$)</label>
                <input
                    v-model="precoTexto"
                    class="admin-input"
                    placeholder="Ex: 99.90 — deixe vazio para 'sob consulta'"
                    type="text"
                    pattern="^\d+([.,]\d{1,2})?$"
                />
            </div>

            <div class="admin-campo admin-campo-checkbox">
                <label class="admin-label admin-label-inline">
                    <input v-model="gratuito" type="checkbox" />
                    Plano gratuito (sem cobrança)
                </label>
            </div>

            <div class="admin-campo">
                <label class="admin-label">Limites (JSON)</label>
                <textarea
                    v-model="limitesJson"
                    class="admin-textarea"
                    rows="5"
                    placeholder='{"profissionais": 5, "pacientes": 100}'
                    @blur="validarJson"
                />
                <p v-if="erroLimitesJson" class="admin-campo-erro">{{ erroLimitesJson }}</p>
                <p class="admin-campo-hint">JSON com limites e configurações do plano.</p>
            </div>

            <div class="admin-campo">
                <label class="admin-label">Motivo da alteração *</label>
                <textarea v-model="motivo" class="admin-textarea" rows="2" placeholder="Informe o motivo..." required />
            </div>

            <p v-if="erroGeral" class="admin-campo-erro admin-erro-geral">{{ erroGeral }}</p>

            <div class="admin-form-actions">
                <button type="button" class="admin-btn-secondary" @click="router.back()">Cancelar</button>
                <button type="submit" class="admin-btn-primary" :disabled="salvando">
                    {{ salvando ? "Salvando..." : "Salvar" }}
                </button>
            </div>
        </form>
    </div>
</template>

<style scoped>
.admin-planos-form {
    max-width: 640px;
    color: hsl(var(--foreground));
}

.admin-form-header {
    display: flex;
    align-items: center;
    gap: 1rem;
    margin-bottom: 1.5rem;
}

.admin-btn-voltar {
    background: none;
    border: none;
    color: hsl(var(--muted-foreground));
    cursor: pointer;
    font-size: 0.875rem;
    padding: 0;
}

.admin-btn-voltar:hover {
    color: hsl(var(--foreground));
}

.admin-page-title {
    font-size: 1.5rem;
    font-weight: 700;
    color: hsl(var(--foreground));
    margin: 0;
}

.admin-form {
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
}

.admin-campo {
    display: flex;
    flex-direction: column;
    gap: 0.375rem;
}

.admin-campo-checkbox {
    flex-direction: row;
    align-items: center;
}

.admin-label {
    color: hsl(var(--muted-foreground));
    font-size: 0.8125rem;
    font-weight: 600;
}

.admin-label-inline {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    cursor: pointer;
}

.admin-input,
.admin-textarea {
    background: hsl(var(--card));
    border: 1px solid hsl(var(--border));
    color: hsl(var(--foreground));
    border-radius: 6px;
    padding: 0.5rem 0.75rem;
    font-size: 0.875rem;
    width: 100%;
    box-sizing: border-box;
}

.admin-textarea {
    resize: vertical;
}

.admin-campo-erro {
    color: hsl(var(--destructive));
    font-size: 0.8125rem;
    margin: 0;
}

.admin-campo-hint {
    color: hsl(var(--muted-foreground));
    font-size: 0.8125rem;
    margin: 0;
}

.admin-erro-geral {
    padding: 0.75rem 1rem;
    background: rgba(248, 113, 113, 0.1);
    border: 1px solid rgba(248, 113, 113, 0.3);
    border-radius: 6px;
}

.admin-form-actions {
    display: flex;
    justify-content: flex-end;
    gap: 0.75rem;
}

.admin-btn-primary {
    background: hsl(var(--primary));
    color: hsl(var(--card));
    border: none;
    border-radius: 6px;
    padding: 0.625rem 1.25rem;
    font-size: 0.875rem;
    font-weight: 600;
    cursor: pointer;
}

.admin-btn-primary:hover:not(:disabled) {
    background: hsl(var(--primary));
}

.admin-btn-primary:disabled {
    opacity: 0.6;
    cursor: not-allowed;
}

.admin-btn-secondary {
    background: hsl(var(--border));
    color: hsl(var(--foreground));
    border: none;
    border-radius: 6px;
    padding: 0.625rem 1.25rem;
    font-size: 0.875rem;
    cursor: pointer;
}

.admin-btn-secondary:hover {
    background: hsl(var(--border));
}

.admin-carregando {
    color: hsl(var(--muted-foreground));
    text-align: center;
    padding: 2rem 0;
}
</style>
