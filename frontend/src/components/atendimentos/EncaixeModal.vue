<!--
    Modal de Novo Encaixe (atendimento emergencial). Busca paciente existente
    ou abre drawer lateral para cadastrar um novo. Emite `selecionado` com o
    paciente escolhido ou recém-cadastrado.
-->
<script setup lang="ts">
import { ref, watch } from "vue"
import AppModal from "@/components/ui/AppModal.vue"
import PacienteFormSidePanel from "@/components/pacientes/PacienteFormSidePanel.vue"
import { AppButton } from "@/components/ui"
import { pacienteService, type PacienteListaItem } from "@/services/pacienteService"
import { useDebouncedRef } from "@/composables/useDebouncedRef"

const props = defineProps<{
    aberto: boolean
    desabilitado?: boolean
    erro?: string | null
}>()

const emit = defineEmits<{
    fechar: []
    selecionado: [paciente: PacienteListaItem]
}>()

const busca         = ref("")
const buscaDebounced = useDebouncedRef(busca, 300)
const buscando      = ref(false)
const resultados    = ref<PacienteListaItem[]>([])
const cadastroAberto = ref(false)

watch(buscaDebounced, executarBusca)

async function executarBusca() {
    const q = buscaDebounced.value.trim()
    if (q.length < 2) {
        resultados.value = []
        return
    }
    buscando.value = true
    try {
        const pg = await pacienteService.listar(q, 1, 20)
        resultados.value = pg.itens
    } catch {
        resultados.value = []
    } finally {
        buscando.value = false
    }
}

function selecionar(p: PacienteListaItem) {
    emit("selecionado", p)
}

function abrirCadastro() { cadastroAberto.value = true }
function onPacienteCriado(p: PacienteListaItem) {
    cadastroAberto.value = false
    emit("selecionado", p)
}

watch(() => props.aberto, (a) => {
    if (a) {
        busca.value = ""
        resultados.value = []
        cadastroAberto.value = false
    }
})

function mascararCpf(cpf: string | null) {
    if (!cpf) return ""
    const d = cpf.replace(/\D/g, "")
    if (d.length < 4) return cpf
    return "***.***." + d.slice(-5, -2) + "-" + d.slice(-2)
}
</script>

<template>
    <AppModal :aberto="aberto" titulo="Novo encaixe" largura="md" @fechar="$emit('fechar')">
        <div class="alerta">
            <i class="fa-solid fa-bolt" aria-hidden="true"></i>
            <p>
                Selecione o paciente para iniciar um atendimento por encaixe. Um
                agendamento será criado automaticamente e você será redirecionado
                para o prontuário.
            </p>
        </div>

        <div v-if="erro" class="erro-box" role="alert">
            <i class="fa-solid fa-circle-exclamation"></i>
            <span>{{ erro }}</span>
        </div>

        <div v-if="desabilitado" class="loading-box" role="status">
            <i class="fa-solid fa-spinner fa-spin"></i>
            <span>Criando encaixe...</span>
        </div>

        <div class="campo">
            <label class="field-label">Buscar paciente</label>
            <div class="input-busca">
                <i class="fa-solid fa-magnifying-glass" aria-hidden="true"></i>
                <input
                    v-model="busca"
                    class="input-field"
                    placeholder="Digite o nome ou CPF do paciente..."
                    :disabled="desabilitado"
                />
            </div>
        </div>

        <div v-if="busca.trim().length >= 2" class="resultados">
            <div v-if="buscando" class="estado">
                <i class="fa-solid fa-spinner fa-spin"></i>
                <p>Buscando pacientes...</p>
            </div>

            <div v-else-if="resultados.length === 0" class="estado">
                <i class="fa-solid fa-user-slash"></i>
                <p class="estado-titulo">Nenhum paciente encontrado.</p>
                <p class="estado-sub">Verifique o nome ou CPF ou cadastre um novo paciente.</p>
            </div>

            <ul v-else class="lista">
                <li v-for="p in resultados" :key="p.id">
                    <button
                        type="button"
                        class="item"
                        :disabled="desabilitado"
                        @click="selecionar(p)"
                    >
                        <div class="item-info">
                            <span class="item-nome">{{ p.nomeCompleto }}</span>
                            <div class="item-meta">
                                <span v-if="p.cpf">
                                    <i class="fa-solid fa-id-card"></i>
                                    {{ mascararCpf(p.cpf) }}
                                </span>
                                <span v-if="p.telefone">
                                    <i class="fa-solid fa-phone"></i>
                                    {{ p.telefone }}
                                </span>
                            </div>
                        </div>
                        <i class="fa-solid fa-chevron-right item-seta"></i>
                    </button>
                </li>
            </ul>
        </div>

        <AppButton
            type="button"
            variant="ghost"
            :block="true"
            icon="fa-solid fa-user-plus"
            :disabled="desabilitado"
            @click="abrirCadastro"
        >
            Cadastrar novo paciente
        </AppButton>
    </AppModal>

    <PacienteFormSidePanel
        :aberto="cadastroAberto"
        :nome-inicial="busca"
        @fechar="cadastroAberto = false"
        @criado="onPacienteCriado"
    />
</template>

<style scoped>
.alerta {
    display: flex; gap: 0.75rem; align-items: flex-start;
    background: hsl(var(--warning) / 0.12); color: hsl(var(--warning) / 0.95);
    border: 1px solid hsl(var(--warning) / 0.4);
    border-radius: var(--radius); padding: 0.85rem 1rem;
    font-size: 0.85em; line-height: 1.45;
}
.alerta i { margin-top: 0.15rem; }
.alerta p { margin: 0; }

.erro-box {
    display: flex; gap: 0.6rem; align-items: center;
    background: hsl(var(--destructive) / 0.1); color: hsl(var(--destructive));
    border: 1px solid hsl(var(--destructive) / 0.4);
    border-radius: var(--radius); padding: 0.7rem 0.9rem;
    font-size: 0.85em; line-height: 1.4;
}

.loading-box {
    display: flex; gap: 0.6rem; align-items: center; justify-content: center;
    color: var(--text-muted); font-size: 0.85em;
    padding: 0.6rem;
}

.campo { display: flex; flex-direction: column; gap: 0.35rem; }

.input-busca { position: relative; }
.input-busca > i {
    position: absolute; left: 0.85rem; top: 50%; transform: translateY(-50%);
    color: var(--text-faint); font-size: 0.85em;
}
.input-field {
    width: 100%; padding: 0.6rem 0.85rem 0.6rem 2.15rem;
    border: 1px solid var(--border-strong); border-radius: var(--radius);
    font-family: inherit; font-size: 0.9em; background: var(--bg-card); color: var(--text);
}
.input-field:focus { outline: none; border-color: hsl(var(--primary)); box-shadow: 0 0 0 2px hsl(var(--primary) / 0.25); }

.resultados {
    border: 1px solid var(--border);
    border-radius: var(--radius); overflow: hidden; max-height: 260px; overflow-y: auto;
}

.estado {
    padding: 1.5rem 1rem; text-align: center; color: var(--text-muted);
    display: flex; flex-direction: column; align-items: center; gap: 0.25rem;
}
.estado > i { font-size: 1.4rem; margin-bottom: 0.3rem; }
.estado-titulo { font-size: 0.9em; margin: 0; font-weight: 600; color: var(--text); }
.estado-sub { font-size: 0.78em; margin: 0; }

.lista { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; }
.lista li + li { border-top: 1px solid var(--border); }

.item {
    width: 100%; display: flex; align-items: center; gap: 0.75rem;
    padding: 0.75rem 1rem; border: none; background: var(--bg-card); cursor: pointer;
    font-family: inherit; text-align: left; transition: background 0.12s;
}
.item:hover:not(:disabled) { background: var(--bg-hover); }
.item:disabled { opacity: 0.6; cursor: not-allowed; }

.item-info { flex: 1; display: flex; flex-direction: column; gap: 0.15rem; min-width: 0; }
.item-nome { font-size: 0.9em; font-weight: 600; color: var(--text); }
.item-meta { display: flex; gap: 0.9rem; font-size: 0.78em; color: var(--text-muted); }
.item-meta i { margin-right: 0.3rem; }

.item-seta { color: var(--text-faint); font-size: 0.8em; }
.item:hover:not(:disabled) .item-seta { color: hsl(var(--primary)); }
</style>
