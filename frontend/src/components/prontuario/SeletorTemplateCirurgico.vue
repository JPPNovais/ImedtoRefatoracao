<!--
  Seletor de modelos de descrição cirúrgica.
  Aberto pelo ConsultaAtualTab quando o profissional clica em "Usar template"
  na seção desc-cirurgica.

  Fluxo:
  1. Ao abrir (aberto = true), busca modelos (lazy — só ao abrir, CA15).
  2. Ao escolher modelo: emite "aplicar" com o corpo do modelo.
  3. Confirmação de substituição via AppConfirmDialog (R8/CA2/CA3).
  4. Atalho "Cadastrar novo modelo" → router.push de deep-link (CA17).
-->
<script setup lang="ts">
import { ref, watch } from "vue"
import { useRouter } from "vue-router"
import {
    AppButton, AppDrawer, AppConfirmDialog, AppEmptyState, AppToast,
} from "@/components/ui"
import {
    modeloDescricaoCirurgicaService,
    type ModeloDescricaoCirurgica,
} from "@/services/modeloDescricaoCirurgicaService"

const props = defineProps<{
    aberto: boolean
    /** Valor atual do campo desc-cirurgica (para saber se precisa confirmar substituição). */
    valorAtual: string
}>()

const emit = defineEmits<{
    "update:aberto": [v: boolean]
    /** Emitido quando o profissional confirma a aplicação do template. */
    aplicar: [corpo: string]
}>()

const router = useRouter()

const modelos = ref<ModeloDescricaoCirurgica[]>([])
const carregando = ref(false)
const erroCarregar = ref(false)

const toast = ref<{ mensagem: string; variante: "info" | "success" | "error" } | null>(null)

// ─── Lazy load: só busca ao abrir (CA15) ──────────────────────────────────────
watch(() => props.aberto, async (aberto) => {
    if (!aberto) return
    carregando.value = true
    erroCarregar.value = false
    try {
        modelos.value = await modeloDescricaoCirurgicaService.listar()
    } catch {
        erroCarregar.value = true
    } finally {
        carregando.value = false
    }
})

// ─── Confirmação de substituição (R8/CA2/CA3) ─────────────────────────────────
const modeloPendente = ref<ModeloDescricaoCirurgica | null>(null)
const confirmSubstituicao = ref(false)

function escolherModelo(m: ModeloDescricaoCirurgica) {
    if (props.valorAtual.trim()) {
        // Campo já tem texto → pede confirmação
        modeloPendente.value = m
        confirmSubstituicao.value = true
    } else {
        // Campo vazio → aplica direto, sem diálogo (CA2)
        aplicarModelo(m)
    }
}

function aplicarModelo(m: ModeloDescricaoCirurgica) {
    emit("aplicar", m.corpo)
    emit("update:aberto", false)
    toast.value = { mensagem: "Template aplicado.", variante: "success" }
}

function confirmarSubstituicao() {
    if (modeloPendente.value) {
        aplicarModelo(modeloPendente.value)
    }
    modeloPendente.value = null
    confirmSubstituicao.value = false
}

function cancelarSubstituicao() {
    modeloPendente.value = null
    confirmSubstituicao.value = false
}

// ─── Atalho "Cadastrar novo modelo" (CA17) ────────────────────────────────────
function irParaConfiguracao() {
    emit("update:aberto", false)
    router.push({ path: "/estabelecimento", query: { secao: "modelos-cirurgia" } })
}
</script>

<template>
    <AppDrawer
        :aberto="aberto"
        titulo="Usar template de descrição cirúrgica"
        :largura="520"
        @fechar="emit('update:aberto', false)"
    >
        <div class="stc-body">
            <!-- Loading -->
            <div v-if="carregando" class="stc-estado">Carregando...</div>

            <!-- Erro -->
            <div v-else-if="erroCarregar" class="stc-estado stc-erro">
                Não foi possível carregar os modelos.
            </div>

            <!-- Vazio (CA13) -->
            <AppEmptyState
                v-else-if="modelos.length === 0"
                icone="fa-solid fa-file-pen"
                titulo="Nenhum modelo cadastrado."
                descricao="Cadastre modelos em Configurações para reutilizá-los aqui."
            >
                <template #acoes>
                    <AppButton variant="ghost" icon="fa-solid fa-arrow-right" @click="irParaConfiguracao">
                        Cadastrar novo modelo
                    </AppButton>
                </template>
            </AppEmptyState>

            <!-- Lista de modelos -->
            <ul v-else class="stc-lista">
                <li
                    v-for="m in modelos"
                    :key="m.id"
                    class="stc-item"
                    role="button"
                    tabindex="0"
                    @click="escolherModelo(m)"
                    @keydown.enter="escolherModelo(m)"
                    @keydown.space.prevent="escolherModelo(m)"
                >
                    <div class="stc-item-titulo">
                        <span>{{ m.titulo }}</span>
                        <span v-if="m.ehPadraoSistema" class="badge-padrao">Padrão do sistema</span>
                    </div>
                    <p class="stc-preview">{{ m.corpo.slice(0, 150) }}{{ m.corpo.length > 150 ? "…" : "" }}</p>
                </li>
            </ul>
        </div>

        <!-- Rodapé com atalho (CA17) -->
        <template #rodape>
            <AppButton variant="ghost" icon="fa-solid fa-arrow-up-right-from-square" @click="irParaConfiguracao">
                Cadastrar novo modelo
            </AppButton>
        </template>
    </AppDrawer>

    <!-- Confirmação de substituição (R8/CA3) -->
    <AppConfirmDialog
        :aberto="confirmSubstituicao"
        titulo="Substituir conteúdo atual?"
        mensagem="Substituir o conteúdo atual da descrição cirúrgica pelo template selecionado?"
        confirmar-rotulo="Substituir"
        variante="primary"
        @confirmar="confirmarSubstituicao"
        @cancelar="cancelarSubstituicao"
    />

    <AppToast
        v-if="toast"
        :mensagem="toast.mensagem"
        :variante="toast.variante"
        @fechar="toast = null"
    />
</template>

<style scoped>
.stc-body { display: flex; flex-direction: column; gap: 12px; padding: 16px; }

.stc-estado {
    text-align: center; padding: 2rem 1rem;
    color: var(--text-muted); font-size: var(--text-sm);
}
.stc-erro { color: var(--danger); }

.stc-lista { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 8px; }

.stc-item {
    padding: 12px 14px;
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: var(--radius-lg);
    cursor: pointer;
    transition: border-color 120ms, box-shadow 120ms;
    display: flex; flex-direction: column; gap: 6px;
    outline: none;
}
.stc-item:hover,
.stc-item:focus-visible {
    border-color: hsl(var(--primary) / 0.5);
    box-shadow: 0 0 0 3px hsl(var(--primary) / 0.08);
}

.stc-item-titulo {
    display: flex; align-items: center; gap: 8px; flex-wrap: wrap;
    font-weight: var(--font-weight-semibold); font-size: var(--text-sm);
    color: hsl(var(--primary-dark));
}

.badge-padrao {
    display: inline-flex; align-items: center;
    background: hsl(var(--secondary) / 0.08); color: hsl(var(--secondary));
    padding: 0.1rem 0.5rem; border-radius: 999px;
    font-size: var(--text-xs); font-weight: var(--font-weight-semibold);
}

.stc-preview {
    font-size: var(--text-xs); color: var(--text-muted); margin: 0;
    white-space: pre-line;
    display: -webkit-box; -webkit-line-clamp: 3; -webkit-box-orient: vertical;
    overflow: hidden;
}
</style>
