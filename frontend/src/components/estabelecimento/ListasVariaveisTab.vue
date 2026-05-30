<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue"
import { AppButton, AppCard, AppField, AppInput, AppPillToggle } from "@/components/ui"
import {
    variavelPoolService,
    type TipoVariavelPool,
    type VariavelPool,
} from "@/services/variavelPoolService"
import { prontuarioService, type VariavelGlobalTenantDto } from "@/services/prontuarioService"

defineProps<{
    podeEditar: boolean
}>()

const TIPOS: { valor: TipoVariavelPool; label: string; descricao: string }[] = [
    { valor: "Alergia",         label: "Alergias",              descricao: "Substâncias e materiais que causam reação alérgica nos pacientes." },
    { valor: "Medicamento",     label: "Medicamentos",          descricao: "Medicamentos em uso ou prescritos." },
    { valor: "Doenca",          label: "Doenças",               descricao: "Doenças e condições crônicas, hereditárias ou agudas." },
    { valor: "Cirurgia",        label: "Cirurgias",             descricao: "Procedimentos cirúrgicos realizados ou previstos." },
    { valor: "Droga",           label: "Drogas",                descricao: "Drogas e substâncias ilícitas de uso relatado pelo paciente." },
    { valor: "RelacaoFamiliar", label: "Relações familiares",   descricao: "Parentesco e relações familiares relevantes para o histórico clínico." },
    { valor: "Expectativa",     label: "Expectativas",          descricao: "Expectativas do paciente em relação ao tratamento." },
    { valor: "AtividadeFisica", label: "Atividades físicas",    descricao: "Atividades físicas praticadas regularmente pelo paciente." },
]

const tipoAtivo = ref<TipoVariavelPool>("Alergia")
const tipoSelecionado = computed(() => TIPOS.find(t => t.valor === tipoAtivo.value)!)

const itens = ref<VariavelPool[]>([])
const carregando = ref(false)
const erro = ref<string | null>(null)
const msgOk = ref<string | null>(null)

const novoNome = ref("")
const salvandoNovo = ref(false)

// ─── Edição inline ─────────────────────────────────────────────────────────────
const editandoId = ref<number | null>(null)
const nomeEditado = ref("")
const salvandoEdit = ref(false)

const itensOrdenados = computed(() =>
    [...itens.value].sort((a, b) => a.nome.localeCompare(b.nome, "pt-BR")),
)

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        itens.value = await variavelPoolService.listar(tipoAtivo.value)
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar opções."
    } finally {
        carregando.value = false
    }
}

watch(tipoAtivo, () => {
    novoNome.value = ""
    editandoId.value = null
    void carregar()
})

async function adicionar() {
    erro.value = null
    msgOk.value = null
    const nome = novoNome.value.trim()
    if (!nome) {
        erro.value = "Informe um nome."
        return
    }
    salvandoNovo.value = true
    try {
        await variavelPoolService.adicionar(tipoAtivo.value, nome)
        novoNome.value = ""
        msgOk.value = "Opção adicionada."
        await carregar()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao adicionar opção."
    } finally {
        salvandoNovo.value = false
    }
}

function iniciarEdicao(item: VariavelPool) {
    editandoId.value = item.id
    nomeEditado.value = item.nome
}

function cancelarEdicao() {
    editandoId.value = null
    nomeEditado.value = ""
}

async function salvarEdicao(item: VariavelPool) {
    erro.value = null
    msgOk.value = null
    const nome = nomeEditado.value.trim()
    if (!nome) {
        erro.value = "Informe um nome."
        return
    }
    salvandoEdit.value = true
    try {
        await variavelPoolService.atualizar(item.id, nome)
        editandoId.value = null
        msgOk.value = "Opção atualizada."
        await carregar()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao atualizar opção."
    } finally {
        salvandoEdit.value = false
    }
}

async function excluir(item: VariavelPool) {
    if (!confirm(`Deseja realmente excluir "${item.nome}"? Ela deixará de aparecer nos prontuários.`))
        return
    erro.value = null
    msgOk.value = null
    try {
        await variavelPoolService.excluir(item.id)
        msgOk.value = "Opção excluída."
        await carregar()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao excluir opção."
    }
}

// ── Templates do sistema (W2-CA28) ──────────────────────────────────────────

const mostrarGlobais = ref(false)
const variaveisGlobais = ref<VariavelGlobalTenantDto[]>([])
const carregandoGlobais = ref(false)
const importandoIdGlobal = ref<string | null>(null)
const msgGlobal = ref<string | null>(null)

async function carregarGlobais() {
    if (carregandoGlobais.value) return
    carregandoGlobais.value = true
    try {
        const result = await prontuarioService.listarVariaveisGlobais({ tamanhoPagina: 100 })
        variaveisGlobais.value = result.itens
    } catch {
        // silencioso
    } finally {
        carregandoGlobais.value = false
    }
}

async function importarGlobal(id: string) {
    importandoIdGlobal.value = id
    msgGlobal.value = null
    try {
        await prontuarioService.importarVariavelDoGlobal(id)
        msgGlobal.value = "Variável importada! Esta é uma cópia editável independente."
        await carregar()
        mostrarGlobais.value = false
    } catch (e: unknown) {
        const msg = (e as { response?: { data?: { mensagem?: string } } })?.response?.data?.mensagem
        msgGlobal.value = msg ?? "Erro ao importar variável."
    } finally {
        importandoIdGlobal.value = null
    }
}

watch(mostrarGlobais, (val) => {
    if (val && variaveisGlobais.value.length === 0) {
        void carregarGlobais()
    }
})

onMounted(carregar)
</script>

<template>
    <div class="listas">
        <p v-if="!podeEditar" class="aviso-leitura">
            Apenas o dono pode editar listas. Você está visualizando em modo leitura.
        </p>

        <p v-if="erro" class="msg-erro">{{ erro }}</p>
        <p v-if="msgOk" class="msg-ok">{{ msgOk }}</p>

        <!-- ── Abas ── -->
        <div class="abas-nav">
            <button
                class="aba-btn"
                :class="{ 'aba-btn--ativa': !mostrarGlobais }"
                @click="mostrarGlobais = false"
            >Minhas listas</button>
            <button
                class="aba-btn"
                :class="{ 'aba-btn--ativa': mostrarGlobais }"
                @click="mostrarGlobais = true"
            >Templates do sistema</button>
        </div>

        <!-- ── Templates do sistema ── -->
        <template v-if="mostrarGlobais">
            <p class="aba-globais-info">
                Variáveis criadas pela equipe Imedto. Clique em "Importar" para criar uma cópia na sua lista.
            </p>
            <div v-if="carregandoGlobais" class="estado-msg">Carregando templates...</div>
            <div v-else-if="!variaveisGlobais.length" class="estado-msg">Nenhum template disponível.</div>
            <AppCard v-else padding="md">
                <ul class="lista">
                    <li v-for="g in variaveisGlobais" :key="g.id" class="item">
                        <div class="item-info">
                            <span class="item-nome">{{ g.nome }}</span>
                            <span class="badge-padrao">{{ g.tipo }}</span>
                        </div>
                        <AppButton
                            variant="secondary"
                            size="sm"
                            :disabled="importandoIdGlobal === g.id"
                            @click="importarGlobal(g.id)"
                        >{{ importandoIdGlobal === g.id ? "Importando..." : "Importar" }}</AppButton>
                    </li>
                </ul>
            </AppCard>
            <p v-if="msgGlobal" class="msg-ok">{{ msgGlobal }}</p>
        </template>

        <!-- ── Pills de tipo (apenas aba Minhas listas) ── -->
        <template v-if="!mostrarGlobais">
        <div class="pills-row">
            <AppPillToggle
                v-model="tipoAtivo"
                :opcoes="TIPOS.map(t => ({ valor: t.valor, label: t.label }))"
            />
        </div>

        <AppCard padding="md">
            <h3 class="secao-titulo">{{ tipoSelecionado.label }}</h3>
            <p class="secao-sub">{{ tipoSelecionado.descricao }}</p>

            <!-- Form de adicionar -->
            <div v-if="podeEditar" class="form-novo">
                <AppField label="Nova opção">
                    <div class="form-novo-row">
                        <AppInput
                            v-model="novoNome"
                            :placeholder="`ex: ${tipoSelecionado.label.toLowerCase().slice(0, -1)}...`"
                            @keyup.enter="adicionar"
                        />
                        <AppButton
                            icon="fa-solid fa-plus"
                            :disabled="salvandoNovo"
                            @click="adicionar"
                        >Adicionar</AppButton>
                    </div>
                </AppField>
            </div>

            <!-- Lista -->
            <div v-if="carregando" class="estado-msg">Carregando...</div>

            <div v-else-if="itensOrdenados.length === 0" class="estado-msg">
                Nenhuma opção cadastrada ainda para esta lista.
            </div>

            <ul v-else class="lista">
                <li v-for="item in itensOrdenados" :key="item.id" class="item">
                    <!-- Modo edição inline -->
                    <template v-if="editandoId === item.id">
                        <AppInput
                            v-model="nomeEditado"
                            @keyup.enter="salvarEdicao(item)"
                            @keyup.esc="cancelarEdicao"
                        />
                        <div class="item-acoes">
                            <AppButton
                                variant="secondary"
                                @click="cancelarEdicao"
                            >Cancelar</AppButton>
                            <AppButton
                                :disabled="salvandoEdit"
                                @click="salvarEdicao(item)"
                            >{{ salvandoEdit ? "Salvando..." : "Salvar" }}</AppButton>
                        </div>
                    </template>

                    <!-- Modo visualização -->
                    <template v-else>
                        <div class="item-info">
                            <span class="item-nome">{{ item.nome }}</span>
                            <span v-if="item.ehPadraoSistema" class="badge-padrao">Padrão do sistema</span>
                        </div>
                        <div v-if="podeEditar && !item.ehPadraoSistema" class="item-acoes">
                            <button
                                type="button"
                                class="btn-icon"
                                title="Editar"
                                @click="iniciarEdicao(item)"
                            >
                                <i class="fa-solid fa-pen"></i>
                            </button>
                            <button
                                type="button"
                                class="btn-icon btn-danger"
                                title="Excluir"
                                @click="excluir(item)"
                            >
                                <i class="fa-solid fa-trash"></i>
                            </button>
                        </div>
                    </template>
                </li>
            </ul>
        </AppCard>
        </template>
    </div>
</template>

<style scoped>
.listas { display: flex; flex-direction: column; gap: 1rem; }

.aviso-leitura {
    background: #fef3c7; color: #92400e;
    padding: 0.65rem 0.9rem; border-radius: var(--radius);
    font-size: 0.82em; margin: 0;
}

.pills-row { display: flex; }

.secao-titulo { font-size: 0.95em; font-weight: 700; margin: 0 0 0.3rem; }
.secao-sub    { font-size: 0.82em; color: var(--text-muted); margin: 0 0 1rem; }

.form-novo { margin-bottom: 1rem; }
.form-novo-row {
    display: flex; gap: 0.5rem; align-items: center;
}
.form-novo-row :deep(.app-input), .form-novo-row :deep(input) { flex: 1; }

@media (max-width: 540px) {
    .form-novo-row { flex-direction: column; align-items: stretch; }
}

.lista {
    list-style: none; margin: 0; padding: 0;
    display: flex; flex-direction: column;
}
.item {
    display: flex; align-items: center; justify-content: space-between;
    gap: 0.75rem;
    padding: 0.65rem 0;
    border-bottom: 1px solid var(--border);
}
.item:last-child { border-bottom: none; }

.item-info {
    display: flex; align-items: center; gap: 0.6rem;
    flex-wrap: wrap; min-width: 0;
}
.item-nome { font-weight: 500; font-size: 0.9em; }

.badge-padrao {
    display: inline-flex; align-items: center;
    background: hsl(var(--secondary) / 0.08);
    color: hsl(var(--secondary));
    padding: 0.1rem 0.5rem;
    border-radius: 999px;
    font-size: 0.7em; font-weight: 600;
    letter-spacing: 0.02em;
}

.item-acoes {
    display: flex; gap: 0.4rem;
    align-items: center;
}

.btn-icon {
    width: 30px; height: 30px;
    display: inline-flex; align-items: center; justify-content: center;
    border-radius: var(--radius);
    border: 1px solid var(--border);
    background: var(--bg-card);
    color: var(--text-muted);
    cursor: pointer;
    transition: all 0.15s;
    font-size: 0.8em;
}
.btn-icon:hover { color: hsl(var(--primary)); border-color: hsl(var(--primary)); }
.btn-icon.btn-danger:hover {
    color: var(--danger); border-color: var(--danger);
    background: hsl(0 90% 60% / 0.06);
}


.estado-msg { text-align: center; color: var(--text-muted); padding: 1.5rem 1rem; font-size: 0.88em; }

.msg-erro { color: var(--danger); font-size: 0.85em; margin: 0; }
.msg-ok   { color: #15803d;       font-size: 0.85em; margin: 0; }

/* ── Abas Templates do sistema ── */
.abas-nav {
    display: flex;
    border-bottom: 1px solid var(--border);
    margin-bottom: 0.25rem;
}
.aba-btn {
    background: none; border: none; padding: 0.45rem 0.75rem;
    font-size: 0.82em; font-weight: 600; color: var(--text-muted);
    cursor: pointer; border-bottom: 2px solid transparent;
    margin-bottom: -1px; transition: color 0.15s, border-color 0.15s;
}
.aba-btn--ativa { color: var(--primary); border-bottom-color: var(--primary); }
.aba-globais-info { font-size: 0.8em; color: var(--text-muted); margin: 0 0 0.75rem; line-height: 1.5; }
</style>
