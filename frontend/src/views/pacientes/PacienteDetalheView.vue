<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue"
import { useRoute, useRouter } from "vue-router"
import { AppCard, AppButton, AppEmptyState } from "@/components/ui"
import PacienteEditDrawer from "@/components/pacientes/PacienteEditDrawer.vue"
import { pacienteService, type Paciente } from "@/services/pacienteService"
import { prontuarioService } from "@/services/prontuarioService"
import { orcamentoService, type OrcamentoResumo } from "@/services/orcamentoService"

const route  = useRoute()
const router = useRouter()

const pacienteId = computed(() => Number(route.params.id))

const paciente    = ref<Paciente | null>(null)
const carregando  = ref(false)
const erro        = ref<string | null>(null)

type Aba = "info" | "prontuarios" | "orcamentos"
const aba = ref<Aba>("info")

// Contadores + listas laterais
const totalProntuarios = ref<number>(0)
const orcamentos       = ref<OrcamentoResumo[]>([])
const carregandoOrc    = ref(false)

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        paciente.value = await pacienteService.obter(pacienteId.value)
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Paciente não encontrado."
    } finally {
        carregando.value = false
    }
}

// ─── Carregamento sob demanda por aba ─────────────────────────────────────────
const abasCarregadas = new Set<Aba>()

async function garantirAba(a: Aba) {
    if (abasCarregadas.has(a)) return
    if (a === "prontuarios") {
        // TODO: criar endpoint GET /prontuario/{id}/contagem para evitar baixar o prontuário
        // completo apenas para exibir o badge de totalProntuarios.
        try {
            const p = await prontuarioService.obter(pacienteId.value)
            totalProntuarios.value = p?.evolucoes?.length ?? 0
        } catch { /* sem prontuário ainda */ }
        abasCarregadas.add("prontuarios")
    } else if (a === "orcamentos") {
        carregandoOrc.value = true
        try {
            orcamentos.value = await orcamentoService.listar({ pacienteId: pacienteId.value })
        } catch { /* ok */ }
        finally { carregandoOrc.value = false }
        abasCarregadas.add("orcamentos")
    }
}

watch(aba, garantirAba, { immediate: true })

onMounted(carregar)

// ─── Helpers ──────────────────────────────────────────────────────────────────
function fmtData(iso: string | null | undefined) {
    if (!iso) return "—"
    try { return new Date(iso).toLocaleDateString("pt-BR") }
    catch { return iso }
}

function fmtGenero(g: string | null | undefined) {
    const map: Record<string, string> = {
        Masculino: "Masculino",
        Feminino: "Feminino",
        Outro: "Outro",
        NaoInformado: "Não informado",
    }
    return g ? (map[g] ?? g) : "—"
}

function fmtMoeda(n: number) {
    return n.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })
}

// Parse best-effort do endereço para extrair cidade/UF.
const cidadeUf = computed(() => {
    const end = paciente.value?.endereco ?? ""
    if (!end) return "—"
    const m = end.match(/([^,]+?)\s*-\s*([A-Z]{2})/)
    if (m) return `${m[1].trim()} / ${m[2]}`
    return end
})

// ─── Ações ────────────────────────────────────────────────────────────────────
const drawerEditarAberto = ref(false)

function editar() {
    drawerEditarAberto.value = true
}

function onPacienteSalvo(p: Paciente) {
    paciente.value = p
    drawerEditarAberto.value = false
}

function abrirProntuario() {
    router.push({ name: "Prontuario", params: { id: pacienteId.value } })
}

function criarOrcamento() {
    // TODO Fase 6.2.b: tela de criação de orçamento (escolha de paciente já está
    // resolvida aqui). Por ora redireciona à lista filtrada por paciente.
    router.push({ name: "Orcamentos", query: { pacienteId: String(pacienteId.value) } })
}

function abrirOrcamento(o: OrcamentoResumo) {
    router.push({ name: "OrcamentoDetalhe", params: { id: String(o.id) } })
}
</script>

<template>
    <main class="app-page detalhe">
        <!-- Link voltar + cabeçalho -->
        <router-link :to="{ name: 'Pacientes' }" class="link-voltar">
            <i class="fa-solid fa-arrow-left"></i>
            Voltar para lista de pacientes
        </router-link>

        <div class="cabecalho">
            <div>
                <h1 class="titulo">{{ paciente?.nomeCompleto || "Paciente" }}</h1>
                <p class="subtitulo">Detalhes do paciente, histórico de consultas e orçamentos</p>
            </div>
        </div>

        <p v-if="carregando" class="msg-info">Carregando…</p>
        <p v-if="erro" class="msg-erro">{{ erro }}</p>

        <template v-if="!carregando && paciente">
            <!-- ── Tabs ── -->
            <AppCard padding="sm" class="tabs-card">
                <div class="tabs">
                    <button
                        type="button" class="tab"
                        :class="{ ativo: aba === 'info' }"
                        @click="aba = 'info'"
                    >
                        <i class="fa-solid fa-user" aria-hidden="true"></i>
                        <span>Informações</span>
                    </button>
                    <button
                        type="button" class="tab"
                        :class="{ ativo: aba === 'prontuarios' }"
                        @click="aba = 'prontuarios'"
                    >
                        <i class="fa-solid fa-file-medical" aria-hidden="true"></i>
                        <span>Prontuários</span>
                        <span class="tab-count">{{ totalProntuarios }}</span>
                    </button>
                    <button
                        type="button" class="tab"
                        :class="{ ativo: aba === 'orcamentos' }"
                        @click="aba = 'orcamentos'"
                    >
                        <i class="fa-solid fa-file-invoice-dollar" aria-hidden="true"></i>
                        <span>Orçamentos</span>
                        <span class="tab-count">{{ orcamentos.length }}</span>
                    </button>
                </div>
            </AppCard>

            <!-- ── Aba: Informações ── -->
            <AppCard v-if="aba === 'info'" padding="lg">
                <template #header-aside>
                    <AppButton variant="secondary" size="sm" icon="fa-solid fa-pen" @click="editar">
                        Editar dados
                    </AppButton>
                </template>
                <h2 class="secao-titulo">Dados do Paciente</h2>

                <div class="grid-2">
                    <div class="dado">
                        <span class="dado-label">CPF/CNPJ:</span>
                        <span class="dado-valor">{{ paciente.cpf || "—" }}</span>
                    </div>
                    <div class="dado">
                        <span class="dado-label">Data de nascimento:</span>
                        <span class="dado-valor">{{ fmtData(paciente.dataNascimento) }}</span>
                    </div>

                    <div class="dado">
                        <span class="dado-label">Sexo:</span>
                        <span class="dado-valor">{{ fmtGenero(paciente.genero) }}</span>
                    </div>
                    <div class="dado">
                        <span class="dado-label">Celular:</span>
                        <span class="dado-valor">{{ paciente.telefone || "—" }}</span>
                    </div>

                    <div class="dado">
                        <span class="dado-label">E-mail:</span>
                        <span class="dado-valor">{{ paciente.email || "—" }}</span>
                    </div>
                    <div class="dado">
                        <span class="dado-label">Cidade/UF:</span>
                        <span class="dado-valor">{{ cidadeUf }}</span>
                    </div>
                </div>
            </AppCard>

            <!-- ── Aba: Prontuários ── -->
            <AppCard v-else-if="aba === 'prontuarios'" padding="lg">
                <template #header-aside>
                    <AppButton variant="primary" size="sm" icon="fa-solid fa-notes-medical" @click="abrirProntuario">
                        Abrir prontuário
                    </AppButton>
                </template>
                <h2 class="secao-titulo">Histórico de consultas</h2>

                <AppEmptyState
                    v-if="totalProntuarios === 0"
                    icone="📋"
                    descricao="Nenhuma consulta registrada para este paciente."
                    compacto
                />
                <p v-else class="hint">
                    {{ totalProntuarios }} evolução(ões) registrada(s). Clique em "Abrir prontuário" para visualizar o histórico completo.
                </p>
            </AppCard>

            <!-- ── Aba: Orçamentos ── -->
            <AppCard v-else padding="lg">
                <template #header-aside>
                    <AppButton variant="ghost" size="sm" icon="fa-solid fa-plus" @click="criarOrcamento">
                        Criar novo orçamento
                    </AppButton>
                </template>
                <h2 class="secao-titulo">Orçamentos do Paciente</h2>

                <p v-if="carregandoOrc" class="msg-info">Carregando orçamentos…</p>

                <AppEmptyState
                    v-else-if="orcamentos.length === 0"
                    icone="💰"
                    descricao="Nenhum orçamento cadastrado para este paciente."
                    compacto
                />

                <ul v-else class="orc-lista">
                    <li
                        v-for="o in orcamentos" :key="o.id"
                        class="orc-item"
                        @click="abrirOrcamento(o)"
                    >
                        <div class="orc-info">
                            <span class="orc-num">Orçamento #{{ o.numero }}</span>
                            <span class="orc-data">Criado em {{ fmtData(o.criadoEm) }}</span>
                        </div>
                        <span class="orc-total">{{ fmtMoeda(o.total) }}</span>
                        <span :class="['orc-status', 'status-' + o.status.toLowerCase()]">{{ o.status }}</span>
                    </li>
                </ul>
            </AppCard>
        </template>

        <!-- Drawer de edição -->
        <PacienteEditDrawer
            :aberto="drawerEditarAberto"
            :paciente="paciente"
            @fechar="drawerEditarAberto = false"
            @salvo="onPacienteSalvo"
        />
    </main>
</template>

<style scoped>
.link-voltar {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    color: hsl(var(--primary));
    text-decoration: none;
    font-size: 0.95rem;
    font-weight: 600;
    width: fit-content;
}
.link-voltar:hover { text-decoration: underline; }

.cabecalho { margin-bottom: 0.25rem; }
.titulo {
    font-size: 2rem;
    font-weight: 800;
    color: hsl(var(--primary-dark));
    margin: 0 0 0.3rem;
    line-height: 1.1;
}
.subtitulo {
    margin: 0;
    color: hsl(var(--secondary) / 0.70);
    font-size: 0.95rem;
}

/* ── Tabs ── */
.tabs-card {
    padding: 0.5rem !important;
}
.tabs {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 0.5rem;
}
.tab {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 0.6rem;
    padding: 0.85rem 1rem;
    border: none;
    background: none;
    cursor: pointer;
    border-radius: 0.5rem;
    font-family: inherit;
    font-size: 0.95rem;
    font-weight: 600;
    color: hsl(var(--secondary) / 0.75);
    transition: all 0.15s;
}
.tab:hover:not(.ativo) {
    background: hsl(var(--secondary) / 0.04);
    color: hsl(var(--secondary));
}
.tab.ativo {
    background: hsl(var(--primary));
    color: hsl(var(--primary-foreground));
    box-shadow: var(--shadow);
}
.tab i { font-size: 1rem; }
.tab-count {
    font-size: 0.78rem;
    font-weight: 700;
    padding: 0.1rem 0.55rem;
    border-radius: 999px;
    background: hsl(var(--secondary) / 0.1);
    color: hsl(var(--secondary) / 0.85);
    line-height: 1.4;
}
.tab.ativo .tab-count {
    background: rgba(255, 255, 255, 0.25);
    color: hsl(var(--primary-foreground));
}

/* ── Seção ── */
.secao-titulo {
    font-size: 1.05rem;
    font-weight: 700;
    color: hsl(var(--primary-dark));
    margin: 0 0 0.2rem;
}

/* ── Dados (grade 2 colunas) ── */
.grid-2 {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1.5rem 2.5rem;
    margin-top: 0.5rem;
}
.dado {
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
}
.dado-label {
    font-size: 0.85rem;
    color: hsl(var(--secondary) / 0.65);
}
.dado-valor {
    font-size: 1rem;
    color: hsl(var(--secondary));
    font-weight: 500;
}

/* ── Orçamentos lista ── */
.orc-lista {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    list-style: none;
    padding: 0;
    margin: 0.5rem 0 0;
}
.orc-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 1rem;
    padding: 0.9rem 1rem;
    border: 1px solid hsl(var(--border));
    border-radius: 0.5rem;
    cursor: pointer;
    transition: background 0.12s;
}
.orc-item:hover { background: hsl(var(--muted) / 0.5); }
.orc-info { display: flex; flex-direction: column; gap: 0.15rem; flex: 1; }
.orc-num { font-weight: 700; font-size: 0.95rem; }
.orc-data { font-size: 0.78rem; color: hsl(var(--secondary) / 0.65); }
.orc-total { font-weight: 700; color: hsl(var(--primary)); }
.orc-status {
    font-size: 0.72rem;
    font-weight: 700;
    padding: 0.2rem 0.65rem;
    border-radius: 999px;
}
.status-pendente { background: #fef3c7; color: #92400e; }
.status-aprovado { background: #dcfce7; color: #15803d; }
.status-recusado { background: #fee2e2; color: #b91c1c; }
.status-expirado { background: #f1f5f9; color: #475569; }

/* ── Mensagens ── */
.msg-info { color: hsl(var(--secondary) / 0.70); margin: 0; }
.msg-erro { color: hsl(var(--error)); margin: 0; }
.hint { color: hsl(var(--secondary) / 0.70); margin: 0; font-size: 0.875rem; }

@media (max-width: 720px) {
    .tabs { grid-template-columns: 1fr; }
    .grid-2 { grid-template-columns: 1fr; gap: 1rem; }
}
</style>
