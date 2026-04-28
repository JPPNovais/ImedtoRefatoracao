<script setup lang="ts">
import { ref, watch } from "vue"
import { useRouter } from "vue-router"
import PacienteEditDrawer from "@/components/pacientes/PacienteEditDrawer.vue"
import { AppButton, AppPagination } from "@/components/ui"
import { useDebouncedRef } from "@/composables/useDebouncedRef"
import {
    pacienteService,
    type Paciente,
    type PacienteListaItem,
    type PaginaPacientes,
} from "@/services/pacienteService"

const router = useRouter()
// `buscaInput` é o que o input edita (atualização imediata na UI).
// `busca` é a versão debounced que dispara a chamada à API.
const buscaInput = ref("")
const busca      = useDebouncedRef(buscaInput, 300)
const pagina     = ref(1)
const tamanho    = ref(20)
const dados      = ref<PaginaPacientes | null>(null)
const carregando = ref(false)
const erro       = ref<string | null>(null)
const excluindoId = ref<number | null>(null)

watch(busca, () => {
    // Ao buscar, volta para a primeira página.
    pagina.value = 1
})

watch([busca, pagina, tamanho], () => void carregar(), { immediate: true })

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        dados.value = await pacienteService.listar(busca.value, pagina.value, tamanho.value)
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar pacientes."
    } finally {
        carregando.value = false
    }
}

// ─── Drawer de edição/criação ────────────────────────────────────────────────
const drawerAberto    = ref(false)
const pacienteEmEdicao = ref<Paciente | null>(null)

function verDetalhe(p: PacienteListaItem) {
    router.push({ name: "PacienteDetalhe", params: { id: p.id } })
}

async function editar(p: PacienteListaItem) {
    try {
        pacienteEmEdicao.value = await pacienteService.obter(p.id)
        drawerAberto.value = true
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar paciente."
    }
}

function onPacienteSalvo() {
    drawerAberto.value = false
    pacienteEmEdicao.value = null
    void carregar()
}

async function excluir(p: PacienteListaItem) {
    if (!confirm(`Excluir ${p.nomeCompleto}? Esta ação é irreversível.`)) return
    excluindoId.value = p.id
    try {
        await pacienteService.deletar(p.id)
        await carregar()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao excluir paciente."
    } finally {
        excluindoId.value = null
    }
}

function novo() {
    pacienteEmEdicao.value = null
    drawerAberto.value = true
}

// ─── Formatadores ────────────────────────────────────────────────────────────
function formatarCpf(cpf: string | null) {
    if (!cpf) return "—"
    return cpf.replace(/(\d{3})(\d{3})(\d{3})(\d{2})/, "$1.$2.$3-$4")
}

function formatarTelefone(tel: string | null) {
    if (!tel) return "—"
    const d = tel.replace(/\D/g, "")
    if (d.length === 11) return `(${d.slice(0,2)}) ${d.slice(2,7)}-${d.slice(7)}`
    if (d.length === 10) return `(${d.slice(0,2)}) ${d.slice(2,6)}-${d.slice(6)}`
    return tel
}

function formatarData(iso: string | null) {
    if (!iso) return "—"
    const d = new Date(iso)
    return d.toLocaleDateString("pt-BR")
}

// Paginação: lógica numérica/ellipsis está em AppPagination.
</script>

<template>
    <main class="app-page app-page--wide pacientes">
        <header class="page-header">
            <div>
                <h1 class="page-titulo">Pacientes do estabelecimento</h1>
                <p class="page-sub">Veja todos os pacientes vinculados a este estabelecimento.</p>
            </div>
            <AppButton icon="fa-solid fa-plus" @click="novo">Novo paciente</AppButton>
        </header>

        <div class="card">
            <div class="card-top">
                <span class="contador">
                    {{ dados?.total ?? 0 }} paciente(s) encontrado(s).
                </span>
                <div class="busca-wrap">
                    <label class="busca-label">Buscar:</label>
                    <input
                        v-model="buscaInput"
                        class="input-field busca-input"
                        placeholder="Nome ou CPF/CNPJ"
                        type="search"
                    />
                </div>
            </div>

            <p v-if="erro" class="msg-erro">{{ erro }}</p>

            <div class="tabela-wrap">
                <div v-if="carregando && !dados" class="estado-msg">Carregando...</div>

                <div v-else-if="dados && dados.itens.length === 0" class="estado-msg">
                    Nenhum paciente vinculado a este estabelecimento.
                </div>

                <table v-else-if="dados" class="tabela">
                    <thead>
                        <tr>
                            <th>Nome</th>
                            <th>CPF / CNPJ</th>
                            <th>Celular</th>
                            <th>Data de nascimento</th>
                            <th>Sexo</th>
                            <th class="col-acoes">Ações</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr
                            v-for="p in dados.itens"
                            :key="p.id"
                            class="linha"
                        >
                            <td class="cel-nome">{{ p.nomeCompleto }}</td>
                            <td>{{ formatarCpf(p.cpf) }}</td>
                            <td>{{ formatarTelefone(p.telefone) }}</td>
                            <td>{{ formatarData(p.dataNascimento) }}</td>
                            <td>—</td>
                            <td class="cel-acoes">
                                <button
                                    class="btn-icon btn-icon-ver" title="Ver detalhes"
                                    @click.stop="verDetalhe(p)"
                                >
                                    <i class="fa-solid fa-eye"></i>
                                </button>
                                <button
                                    class="btn-icon btn-icon-editar" title="Editar"
                                    @click.stop="editar(p)"
                                >
                                    <i class="fa-solid fa-pen"></i>
                                </button>
                                <button
                                    class="btn-icon btn-icon-excluir" title="Excluir"
                                    :disabled="excluindoId === p.id"
                                    @click.stop="excluir(p)"
                                >
                                    <i class="fa-solid fa-trash"></i>
                                </button>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>

            <AppPagination
                v-if="dados"
                v-model:pagina="pagina"
                v-model:tamanho="tamanho"
                :total="dados.total"
                rotulo-itens="paciente(s)"
            />
        </div>

        <!-- Drawer de edição/criação -->
        <PacienteEditDrawer
            :aberto="drawerAberto"
            :paciente="pacienteEmEdicao"
            @fechar="drawerAberto = false"
            @salvo="onPacienteSalvo"
        />
    </main>
</template>

<style scoped>

.page-header {
    display: flex; justify-content: space-between; align-items: flex-start; gap: 1rem;
    margin-bottom: 1.25rem; flex-wrap: wrap;
}
.page-titulo { font-size: 1.5rem; font-weight: 800; margin: 0 0 0.2rem; }
.page-sub    { margin: 0; color: var(--text-muted); font-size: 0.875em; }

.card {
    background: var(--bg-card); border: 1px solid var(--border);
    border-radius: var(--radius); padding: 1rem 1.25rem;
    display: flex; flex-direction: column; gap: 0.75rem;
}

.card-top {
    display: flex; justify-content: space-between; align-items: center;
    gap: 1rem; flex-wrap: wrap;
}
.contador { font-size: 0.85em; color: var(--text-muted); }

.busca-wrap { display: flex; align-items: center; gap: 0.5rem; }
.busca-label { font-size: 0.85em; color: var(--text-muted); }
.busca-input { min-width: 240px; }

.input-field {
    padding: 0.45rem 0.75rem; border: 1px solid var(--border-strong);
    border-radius: var(--radius); font-family: inherit; font-size: 0.875em;
    background: var(--bg-card); color: var(--text);
}
.input-field:focus { outline: none; border-color: var(--primary); }

.tabela-wrap { overflow-x: auto; }
.tabela { width: 100%; border-collapse: collapse; font-size: 0.875em; }
.tabela th {
    text-align: left; padding: 0.65rem 1rem;
    font-size: 0.75em; font-weight: 700;
    color: var(--text-muted); border-bottom: 1px solid var(--border);
}
.tabela td { padding: 0.85rem 1rem; border-bottom: 1px solid var(--border); vertical-align: middle; }
.tabela tr:last-child td { border-bottom: none; }

.cel-nome  { font-weight: 600; }
.col-acoes { width: 140px; text-align: right; }
.cel-acoes { white-space: nowrap; text-align: right; }

.linha:hover { background: var(--bg-hover); }

.estado-msg { text-align: center; color: var(--text-muted); padding: 2rem 1rem; font-size: 0.9em; }

.msg-erro { color: var(--danger); font-size: 0.875em; margin: 0; }

/* Botões de ação (.btn-icon-*) e paginação (AppPagination) e .btn-primary
   vêm do design system — não duplicar aqui (ver main.css / components/ui/). */
</style>
