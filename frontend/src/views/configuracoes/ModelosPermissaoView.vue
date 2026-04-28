<script setup lang="ts">
import { computed, onMounted, reactive, ref } from "vue"
import { useRouter } from "vue-router"
import { permissaoService, type ModeloPermissao } from "@/services/permissaoService"
import { vinculoService, type ProfissionalVinculado } from "@/services/vinculoService"
import { useTenantStore } from "@/stores/tenantStore"
import { PERMISSIONS, permissionLabel } from "@/constants/permissions"
import {
    AppButton, AppCard, AppField, AppInput, AppModal, AppPageHeader, AppPillToggle, AppEmptyState, AppSelect,
} from "@/components/ui"

const router = useRouter()
const tenant = useTenantStore()

// Acesso restrito ao Dono.
if (tenant.papel !== "Dono") {
    router.replace({ name: "Home" })
}

type Aba = "modelos" | "profissionais"
const aba = ref<Aba>("modelos")

const modelos = ref<ModeloPermissao[]>([])
const profissionais = ref<ProfissionalVinculado[]>([])
const carregando = ref(false)
const erro = ref<string | null>(null)
const sucesso = ref<string | null>(null)
const buscaProf = ref("")

// ─── Form: criar novo modelo (inline na própria aba) ──────────────────────────
const novo = reactive({
    nome: "",
    permissoes: [] as string[],
})
const salvandoNovo = ref(false)

// ─── Modal: editar modelo personalizado ───────────────────────────────────────
const modalEditarAberto = ref(false)
const editando = reactive({
    id: 0,
    nome: "",
    permissoes: [] as string[],
})
const salvandoEdicao = ref(false)

// ─── Estado: exclusão e atribuição ────────────────────────────────────────────
const excluindoId = ref<number | null>(null)
const atribuindoVinculoId = ref<number | null>(null)

const modelosPadroes = computed(() => modelos.value.filter(m => m.ehPadrao))
const modelosPersonalizados = computed(() => modelos.value.filter(m => !m.ehPadrao))

const profissionaisFiltrados = computed(() => {
    const termo = buscaProf.value.trim().toLowerCase()
    if (!termo) return profissionais.value
    return profissionais.value.filter(p =>
        (p.nomeCompleto ?? "").toLowerCase().includes(termo)
        || (p.email ?? "").toLowerCase().includes(termo),
    )
})

const formNovoValido = computed(() => novo.nome.trim().length > 0)
const formEdicaoValido = computed(() => editando.nome.trim().length > 0)

onMounted(carregarTudo)

async function carregarTudo() {
    carregando.value = true
    erro.value = null
    try {
        const [m, p] = await Promise.all([
            permissaoService.listar(),
            vinculoService.listarProfissionais(),
        ])
        modelos.value = m
        profissionais.value = p
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Não foi possível carregar."
    } finally {
        carregando.value = false
    }
}

function tipoAcessoDerivado(permissoes: string[]): string {
    // O tipo_acesso espelha o legado: se há "prontuario", é Profissional; senão Recepcionista.
    return permissoes.includes("prontuario") ? "Profissional" : "Recepcionista"
}

function mostrarSucesso(msg: string) {
    sucesso.value = msg
    setTimeout(() => { sucesso.value = null }, 3000)
}

async function criarNovo() {
    if (!formNovoValido.value) return
    salvandoNovo.value = true
    erro.value = null
    try {
        await permissaoService.criar({
            nome: novo.nome.trim(),
            tipoAcesso: tipoAcessoDerivado(novo.permissoes),
            permissoes: [...novo.permissoes],
        })
        novo.nome = ""
        novo.permissoes = []
        await carregarTudo()
        mostrarSucesso("Modelo criado com sucesso.")
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Não foi possível criar o modelo."
    } finally {
        salvandoNovo.value = false
    }
}

function abrirEdicao(m: ModeloPermissao) {
    if (m.ehPadrao) return
    editando.id = m.id
    editando.nome = m.nome
    editando.permissoes = [...(m.permissoes ?? [])]
    modalEditarAberto.value = true
}

function fecharEdicao() {
    modalEditarAberto.value = false
    editando.id = 0
    editando.nome = ""
    editando.permissoes = []
}

async function salvarEdicao() {
    if (!formEdicaoValido.value) return
    salvandoEdicao.value = true
    erro.value = null
    try {
        await permissaoService.atualizar(editando.id, {
            nome: editando.nome.trim(),
            tipoAcesso: tipoAcessoDerivado(editando.permissoes),
            permissoes: [...editando.permissoes],
        })
        fecharEdicao()
        await carregarTudo()
        mostrarSucesso("Modelo atualizado.")
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Não foi possível atualizar o modelo."
    } finally {
        salvandoEdicao.value = false
    }
}

async function excluir(m: ModeloPermissao) {
    if (m.ehPadrao) return
    if (!window.confirm(`Excluir o modelo "${m.nome}"? Profissionais vinculados a ele precisarão receber outro modelo antes.`)) return

    excluindoId.value = m.id
    erro.value = null
    try {
        await permissaoService.excluir(m.id)
        await carregarTudo()
        mostrarSucesso("Modelo excluído.")
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Não foi possível excluir o modelo."
    } finally {
        excluindoId.value = null
    }
}

function togglePermissao(lista: string[], id: string, marcar: boolean) {
    const idx = lista.indexOf(id)
    if (marcar && idx === -1) lista.push(id)
    else if (!marcar && idx !== -1) lista.splice(idx, 1)
}

async function atribuirModelo(p: ProfissionalVinculado, modeloId: number) {
    if (modeloId === p.modeloPermissaoId) return
    atribuindoVinculoId.value = p.vinculoId
    erro.value = null
    try {
        await permissaoService.atribuirAoVinculo(p.vinculoId, modeloId)
        // Atualiza local sem refetch para não piscar a tabela.
        const novo = modelos.value.find(m => m.id === modeloId)
        if (novo) {
            p.modeloPermissaoId = novo.id
            p.modeloPermissaoNome = novo.nome
        }
        mostrarSucesso(`Permissões de ${p.nomeCompleto || p.email} atualizadas.`)
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Não foi possível atualizar as permissões."
    } finally {
        atribuindoVinculoId.value = null
    }
}

function ehDono(p: ProfissionalVinculado) {
    return p.status === "Dono"
}
</script>

<template>
    <main class="page-permissoes">
        <AppPageHeader
            titulo="Permissões dos profissionais"
            subtitulo="Defina modelos de permissão e atribua quais áreas do sistema cada profissional pode acessar."
        />

        <p v-if="erro" class="alerta alerta--erro">{{ erro }}</p>
        <p v-if="sucesso" class="alerta alerta--ok">{{ sucesso }}</p>

        <AppCard padding="md">
            <div class="abas">
                <AppPillToggle
                    v-model="aba"
                    :opcoes="[
                        { valor: 'modelos',       label: 'Lista de modelos' },
                        { valor: 'profissionais', label: 'Por profissional' },
                    ]"
                />
            </div>

            <p v-if="carregando" class="muted">Carregando…</p>

            <!-- ── Aba: Modelos ─────────────────────────────────── -->
            <section v-show="aba === 'modelos' && !carregando" class="bloco">
                <div>
                    <h2 class="bloco-titulo">Modelos de permissão</h2>
                    <p class="bloco-sub">
                        Utilize modelos para padronizar o acesso de administradores, médicos,
                        recepção e outros perfis.
                    </p>
                </div>

                <!-- Modelos padrão -->
                <div v-if="modelosPadroes.length" class="grupo">
                    <header class="grupo-titulo">
                        <span>Modelos padrões</span>
                        <span class="linha-divisora" />
                    </header>
                    <ul class="lista-modelos">
                        <li v-for="m in modelosPadroes" :key="m.id" class="modelo-item modelo-item--padrao">
                            <div class="modelo-info">
                                <div class="modelo-cabecalho">
                                    <span class="modelo-nome">{{ m.nome }}</span>
                                    <span class="badge badge--info">
                                        <i class="fa-solid fa-shield-halved" aria-hidden="true"></i>
                                        Padrão do sistema
                                    </span>
                                </div>
                                <p class="modelo-permissoes">
                                    {{ m.permissoes.length
                                        ? m.permissoes.map(permissionLabel).join(", ")
                                        : "Sem permissões definidas" }}
                                </p>
                            </div>
                        </li>
                    </ul>
                </div>

                <!-- Modelos personalizados -->
                <div v-if="modelosPersonalizados.length" class="grupo">
                    <header class="grupo-titulo">
                        <span>Modelos personalizados</span>
                        <span class="linha-divisora" />
                    </header>
                    <ul class="lista-modelos">
                        <li v-for="m in modelosPersonalizados" :key="m.id" class="modelo-item">
                            <div class="modelo-info">
                                <span class="modelo-nome">{{ m.nome }}</span>
                                <p class="modelo-permissoes">
                                    {{ m.permissoes.length
                                        ? m.permissoes.map(permissionLabel).join(", ")
                                        : "Sem permissões definidas" }}
                                </p>
                            </div>
                            <div class="modelo-acoes">
                                <AppButton type="button" variant="secondary" size="sm" @click="abrirEdicao(m)">
                                    Editar
                                </AppButton>
                                <AppButton
                                    type="button"
                                    variant="danger"
                                    size="sm"
                                    :disabled="excluindoId === m.id"
                                    :loading="excluindoId === m.id"
                                    @click="excluir(m)"
                                >
                                    Excluir
                                </AppButton>
                            </div>
                        </li>
                    </ul>
                </div>

                <!-- Form: criar novo -->
                <div class="form-novo">
                    <h3 class="form-novo-titulo">Criar novo modelo</h3>
                    <form @submit.prevent="criarNovo" class="form-grid">
                        <AppField label="Nome do modelo" required>
                            <AppInput
                                v-model="novo.nome"
                                placeholder="Ex.: Faturamento, Coordenador"
                            />
                        </AppField>

                        <AppField
                            label="Telas e áreas liberadas"
                            hint="Selecione as telas que este modelo poderá ver e acessar."
                            class="form-grid-larga"
                        >
                            <div class="grid-permissoes">
                                <label
                                    v-for="perm in PERMISSIONS"
                                    :key="perm.id"
                                    class="checkbox"
                                >
                                    <input
                                        type="checkbox"
                                        :checked="novo.permissoes.includes(perm.id)"
                                        @change="togglePermissao(novo.permissoes, perm.id, ($event.target as HTMLInputElement).checked)"
                                    />
                                    <span>{{ perm.label }}</span>
                                </label>
                            </div>
                        </AppField>

                        <div class="form-acoes">
                            <AppButton
                                type="submit"
                                size="sm"
                                :loading="salvandoNovo"
                                :disabled="!formNovoValido"
                            >
                                Criar modelo
                            </AppButton>
                        </div>
                    </form>
                </div>
            </section>

            <!-- ── Aba: Por profissional ────────────────────────── -->
            <section v-show="aba === 'profissionais' && !carregando" class="bloco">
                <div>
                    <h2 class="bloco-titulo">Permissões por profissional</h2>
                    <p class="bloco-sub">
                        Escolha qual modelo de permissão se aplica para cada profissional
                        vinculado ao estabelecimento.
                    </p>
                </div>

                <AppEmptyState
                    v-if="!profissionais.length"
                    icone="👥"
                    titulo="Nenhum profissional vinculado"
                    descricao="Convide profissionais na página de Profissionais para começar a configurar permissões."
                />

                <template v-else>
                    <div class="busca-prof">
                        <div class="busca-prof-input">
                            <i class="fa-solid fa-search" aria-hidden="true"></i>
                            <AppInput
                                v-model="buscaProf"
                                type="search"
                                placeholder="Buscar profissional por nome ou e-mail…"
                            />
                        </div>
                        <span v-if="buscaProf" class="busca-prof-meta">
                            {{ profissionaisFiltrados.length }} de {{ profissionais.length }} profissional(is)
                        </span>
                    </div>

                    <div v-if="!profissionaisFiltrados.length" class="muted">
                        Nenhum profissional encontrado com o termo "{{ buscaProf }}".
                    </div>
                    <table v-else class="tabela-prof">
                        <thead>
                            <tr>
                                <th>Profissional</th>
                                <th>Modelo de permissão</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr v-for="p in profissionaisFiltrados" :key="p.vinculoId || p.usuarioId">
                                <td>
                                    <div class="prof-cell">
                                        <span>{{ p.nomeCompleto || p.email }}</span>
                                        <span v-if="ehDono(p)" class="badge badge--primary">
                                            <i class="fa-solid fa-crown" aria-hidden="true"></i>
                                            Proprietário
                                        </span>
                                    </div>
                                </td>
                                <td>
                                    <div v-if="ehDono(p)" class="prof-cell muted-inline">
                                        <strong>Admin</strong>
                                        <span>(acesso total)</span>
                                    </div>
                                    <AppSelect
                                        v-else
                                        :value="p.modeloPermissaoId"
                                        :disabled="atribuindoVinculoId === p.vinculoId"
                                        @change="atribuirModelo(p, Number(($event.target as HTMLSelectElement).value))"
                                    >
                                        <option v-for="m in modelos" :key="m.id" :value="m.id">
                                            {{ m.nome }}
                                        </option>
                                    </AppSelect>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </template>
            </section>
        </AppCard>

        <!-- ── Modal: editar modelo personalizado ─────────────── -->
        <AppModal :aberto="modalEditarAberto" titulo="Editar modelo de permissão" largura="md" @fechar="fecharEdicao">
            <form @submit.prevent="salvarEdicao" class="form-grid">
                <AppField label="Nome do modelo" required>
                    <AppInput v-model="editando.nome" />
                </AppField>
                <AppField label="Telas e áreas liberadas">
                    <div class="grid-permissoes">
                        <label
                            v-for="perm in PERMISSIONS"
                            :key="perm.id"
                            class="checkbox"
                        >
                            <input
                                type="checkbox"
                                :checked="editando.permissoes.includes(perm.id)"
                                @change="togglePermissao(editando.permissoes, perm.id, ($event.target as HTMLInputElement).checked)"
                            />
                            <span>{{ perm.label }}</span>
                        </label>
                    </div>
                </AppField>
            </form>
            <template #rodape>
                <AppButton variant="secondary" @click="fecharEdicao">Cancelar</AppButton>
                <AppButton :loading="salvandoEdicao" :disabled="!formEdicaoValido" @click="salvarEdicao">
                    Salvar
                </AppButton>
            </template>
        </AppModal>
    </main>
</template>

<style scoped>
.page-permissoes {
    padding: 1.75rem 2rem;
    max-width: 1080px;
    margin: 0 auto;
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
}

.alerta {
    padding: 0.65rem 0.9rem;
    border-radius: var(--radius);
    font-size: 0.85rem;
    margin: 0;
}
.alerta--erro { background: hsl(var(--error) / 0.1);   color: hsl(var(--error)); }
.alerta--ok   { background: hsl(var(--success) / 0.12); color: hsl(var(--success)); }

.abas { margin-bottom: 1rem; }

.bloco {
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
}
.bloco-titulo {
    font-size: 0.9rem;
    font-weight: 700;
    color: hsl(var(--primary-dark));
    margin: 0 0 0.25rem;
}
.bloco-sub {
    font-size: 0.78rem;
    color: hsl(var(--secondary) / 0.7);
    margin: 0;
}

.muted { color: hsl(var(--secondary) / 0.7); font-size: 0.875rem; }
.muted-inline { color: hsl(var(--secondary) / 0.7); font-size: 0.78rem; }

.grupo { display: flex; flex-direction: column; gap: 0.6rem; }
.grupo-titulo {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    font-size: 0.7rem;
    font-weight: 700;
    color: hsl(var(--secondary));
    text-transform: uppercase;
    letter-spacing: 0.05em;
}
.linha-divisora {
    flex: 1;
    height: 1px;
    background: hsl(var(--secondary) / 0.1);
}

.lista-modelos {
    list-style: none;
    padding: 0;
    margin: 0;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}
.modelo-item {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 1rem;
    padding: 0.85rem 1rem;
    border-radius: 0.6rem;
    background: hsl(var(--secondary) / 0.04);
    border: 1px solid hsl(var(--border));
    transition: border-color 0.15s;
}
.modelo-item:hover { border-color: hsl(var(--secondary) / 0.25); }
.modelo-item--padrao {
    background: hsl(var(--info) / 0.06);
    border-color: hsl(var(--info) / 0.25);
}
.modelo-info { flex: 1; min-width: 0; display: flex; flex-direction: column; gap: 0.25rem; }
.modelo-cabecalho { display: flex; align-items: center; gap: 0.5rem; flex-wrap: wrap; }
.modelo-nome { font-weight: 700; font-size: 0.875rem; color: hsl(var(--primary-dark)); }
.modelo-permissoes { font-size: 0.75rem; color: hsl(var(--secondary) / 0.7); margin: 0; line-height: 1.45; }

.modelo-acoes { display: flex; gap: 0.5rem; align-items: center; flex-shrink: 0; }

.badge {
    display: inline-flex;
    align-items: center;
    gap: 0.3rem;
    font-size: 0.65rem;
    font-weight: 600;
    padding: 0.15rem 0.55rem;
    border-radius: 999px;
}
.badge--info    { background: hsl(var(--info) / 0.12);    color: hsl(var(--info)); }
.badge--primary { background: hsl(var(--primary) / 0.12); color: hsl(var(--primary)); }

.form-novo {
    border-top: 1px solid hsl(var(--border));
    padding-top: 1.25rem;
}
.form-novo-titulo {
    font-size: 0.78rem;
    font-weight: 700;
    color: hsl(var(--primary-dark));
    margin: 0 0 0.75rem;
}
.form-grid {
    display: grid;
    grid-template-columns: 1fr 2fr;
    gap: 1rem;
}
.form-grid-larga { grid-column: 1 / -1; }
.form-acoes { grid-column: 1 / -1; display: flex; justify-content: flex-end; }
@media (max-width: 720px) {
    .form-grid { grid-template-columns: 1fr; }
}

.grid-permissoes {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
    gap: 0.45rem 1rem;
}
.checkbox {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.82rem;
    color: hsl(var(--secondary));
    cursor: pointer;
}
.checkbox input[type="checkbox"] {
    width: 16px; height: 16px;
    accent-color: hsl(var(--primary));
}

.busca-prof {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    flex-wrap: wrap;
}
.busca-prof-input {
    position: relative;
    flex: 1;
    max-width: 380px;
}
.busca-prof-input i {
    position: absolute;
    left: 0.85rem;
    top: 50%;
    transform: translateY(-50%);
    color: hsl(var(--secondary) / 0.45);
    font-size: 0.78rem;
}
.busca-prof-input :deep(input) { padding-left: 2.2rem; }
.busca-prof-meta {
    font-size: 0.72rem;
    color: hsl(var(--secondary) / 0.6);
}

.tabela-prof {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.85rem;
}
.tabela-prof th {
    text-align: left;
    padding: 0.6rem 0.5rem;
    font-size: 0.72rem;
    font-weight: 600;
    color: hsl(var(--secondary) / 0.7);
    border-bottom: 1px solid hsl(var(--border));
}
.tabela-prof td {
    padding: 0.6rem 0.5rem;
    border-bottom: 1px solid hsl(var(--border) / 0.6);
    vertical-align: middle;
}
.prof-cell { display: inline-flex; align-items: center; gap: 0.5rem; flex-wrap: wrap; }
.tabela-prof td :deep(select) { max-width: 280px; }
</style>
