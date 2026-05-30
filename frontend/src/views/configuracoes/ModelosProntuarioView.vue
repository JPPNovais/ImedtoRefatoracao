<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from "vue"
import { useRouter } from "vue-router"
import { prontuarioService, type ModeloProntuario, type SecaoModelo, type ModeloGlobalDto } from "@/services/prontuarioService"
import { useTenantStore } from "@/stores/tenantStore"
import {
    AppBadge, AppButton, AppEmptyState, AppField, AppInput, AppPageHeader, AppTextarea,
} from "@/components/ui"

const router = useRouter()
const tenant = useTenantStore()

if (tenant.papel !== "Dono") {
    router.replace({ name: "Home" })
}

const secoesList = [
    { key: "queixa",                  label: "Queixa principal (QP)",            tipo: "texto_longo", info: "Motivo pelo qual o paciente procurou atendimento, descrito com suas próprias palavras." },
    { key: "hda",                     label: "História da doença atual (HDA)",    tipo: "texto_longo", info: "Detalhamento cronológico dos sintomas e evolução da condição atual do paciente." },
    { key: "hpp",                     label: "História pregressa (HPP)",           tipo: "texto_longo", info: "Doenças anteriores, cirurgias, internações, alergias e medicamentos em uso contínuo." },
    { key: "h-familiar",              label: "História familiar",                  tipo: "texto_longo", info: "Doenças hereditárias ou de alta prevalência nos familiares diretos do paciente." },
    { key: "h-social",                label: "História social e hábitos de vida",  tipo: "texto_longo", info: "Ocupação, hábitos, tabagismo, consumo de álcool, atividade física e condições socioeconômicas." },
    { key: "exame-fisico",            label: "Exame físico",                       tipo: "texto_longo", info: "Dados vitais e avaliação clínica dos sistemas do organismo." },
    { key: "exames-realizados",       label: "Exames realizados",                  tipo: "texto_longo", info: "Resultados de exames laboratoriais, de imagem e outros já realizados." },
    { key: "procedimentos-indicados", label: "Procedimentos indicados",            tipo: "texto_longo", info: "Procedimentos, cirurgias ou intervenções recomendadas." },
    { key: "evolucao-pos-op",         label: "Evolução pós-operatória",            tipo: "texto_longo", info: "Acompanhamento e recuperação do paciente após cirurgia ou procedimento." },
    { key: "desc-cirurgica",          label: "Descrição cirúrgica",                tipo: "texto_longo", info: "Relato detalhado do ato cirúrgico realizado, incluindo técnica e intercorrências." },
    { key: "procedimento-consultorio",label: "Procedimento em consultório",        tipo: "texto_longo", info: "Procedimentos menores realizados no consultório durante o atendimento." },
    { key: "ficha-anestesica",        label: "Ficha anestésica",                   tipo: "texto_longo", info: "Avaliação pré-anestésica e execução da anestesia no procedimento." },
    { key: "equipe-cirurgica",        label: "Equipe cirúrgica",                   tipo: "texto_longo", info: "Profissionais que participaram do procedimento cirúrgico." },
    { key: "fotos-paciente",          label: "Fotos do paciente",                  tipo: "texto_longo", info: "Registro fotográfico do paciente (pré e pós-operatório)." },
    { key: "anexos",                  label: "Anexos",                             tipo: "texto_longo", info: "Documentos e arquivos complementares ao prontuário." },
    { key: "cid10",                   label: "CID-10",                             tipo: "texto",       info: "Classificação Internacional de Doenças para codificação do diagnóstico." },
    { key: "conduta",                 label: "Conduta",                            tipo: "texto_longo", info: "Plano terapêutico definido pelo profissional." },
]

type FormState = {
    id: number | null
    nome: string
    descricao: string
    secoes: Record<string, boolean>
    ordem: string[]
}

const emptySecoes = (): Record<string, boolean> =>
    secoesList.reduce((acc, s) => { acc[s.key] = false; return acc }, {} as Record<string, boolean>)

const form = reactive<FormState>({
    id: null,
    nome: "",
    descricao: "",
    secoes: emptySecoes(),
    ordem: [],
})

const modelos = ref<ModeloProntuario[]>([])
const carregando = ref(false)
const carregandoModelos = ref(false)
const salvando = ref(false)
const excluindoId = ref<number | null>(null)

const modelosPadroes = computed(() => modelos.value.filter(m => m.ehPadraoSistema))
const modelosPersonalizados = computed(() => modelos.value.filter(m => !m.ehPadraoSistema))
const secoesAtivas = computed(() => form.ordem.filter(k => form.secoes[k]))

watch(
    () => ({ ...form.secoes }),
    () => sincronizarOrdem(),
    { deep: true },
)

function sincronizarOrdem() {
    const ativas = new Set(secoesList.map(s => s.key).filter(k => form.secoes[k]))
    const novaOrdem: string[] = []
    form.ordem.forEach(k => { if (ativas.has(k)) { novaOrdem.push(k); ativas.delete(k) } })
    ativas.forEach(k => novaOrdem.push(k))
    form.ordem = novaOrdem
}

function resetarForm() {
    form.id = null
    form.nome = ""
    form.descricao = ""
    secoesList.forEach(s => { form.secoes[s.key] = false })
    form.ordem = []
}

function preencherForm(modelo: ModeloProntuario) {
    form.id = modelo.id
    form.nome = modelo.nome
    form.descricao = modelo.descricao ?? ""

    const estrutura = Array.isArray(modelo.estrutura) ? modelo.estrutura as SecaoModelo[] : []
    const chavesSelecionadas = new Set(estrutura.map(s => s.chave))

    secoesList.forEach(s => { form.secoes[s.key] = chavesSelecionadas.has(s.key) })

    if (estrutura.length > 0) {
        const ordenados = [...estrutura].sort((a, b) => a.ordem - b.ordem)
        form.ordem = ordenados.map(s => s.chave).filter(k => chavesSelecionadas.has(k))
    } else {
        form.ordem = secoesList.map(s => s.key).filter(k => chavesSelecionadas.has(k))
    }
}

function moverSecao(key: string, direcao: "cima" | "baixo") {
    const idx = form.ordem.indexOf(key)
    if (idx === -1) return
    const novoIdx = direcao === "cima" ? idx - 1 : idx + 1
    if (novoIdx < 0 || novoIdx >= form.ordem.length) return
    const arr = [...form.ordem]
    const [item] = arr.splice(idx, 1)
    arr.splice(novoIdx, 0, item)
    form.ordem = arr
}

function editarModelo(modelo: ModeloProntuario) {
    if (modelo.ehPadraoSistema) return
    preencherForm(modelo)
    window.scrollTo({ top: 0, behavior: "smooth" })
}

async function carregarModelos() {
    carregandoModelos.value = true
    try {
        modelos.value = await prontuarioService.listarModelos()
    } catch {
        // silencioso — lista vazia
    } finally {
        carregandoModelos.value = false
    }
}

async function salvar() {
    if (!form.nome.trim()) return

    const secoesPayload = secoesAtivas.value
    if (secoesPayload.length === 0) {
        alert("Selecione pelo menos uma seção para o modelo.")
        return
    }

    const estrutura: SecaoModelo[] = secoesPayload.map((key, idx) => {
        const def = secoesList.find(s => s.key === key)!
        return { chave: key, titulo: def.label, tipo: def.tipo, ordem: idx }
    })

    const payload = {
        nome: form.nome.trim(),
        descricao: form.descricao.trim() || undefined,
        estruturaJson: JSON.stringify(estrutura),
    }

    salvando.value = true
    try {
        if (form.id) {
            await prontuarioService.atualizarModelo(form.id, payload)
        } else {
            await prontuarioService.criarModelo(payload)
        }
        await carregarModelos()
        resetarForm()
    } catch (e: any) {
        alert(e?.response?.data?.mensagem ?? "Erro ao salvar modelo.")
    } finally {
        salvando.value = false
    }
}

async function excluir(modelo: ModeloProntuario) {
    if (modelo.ehPadraoSistema) return
    if (!confirm(`Deseja excluir o modelo "${modelo.nome}"?`)) return

    excluindoId.value = modelo.id
    try {
        await prontuarioService.excluirModelo(modelo.id)
        if (form.id === modelo.id) resetarForm()
        await carregarModelos()
    } catch (e: any) {
        alert(e?.response?.data?.mensagem ?? "Não foi possível excluir o modelo.")
    } finally {
        excluindoId.value = null
    }
}

// ── Templates do sistema (W2-CA24/25) ───────────────────────────────────────

const abaAtiva = ref<"meus" | "globais">("meus")
const modelosGlobais = ref<ModeloGlobalDto[]>([])
const carregandoGlobais = ref(false)
const importandoId = ref<string | null>(null)
const msgImportacao = ref<string | null>(null)

async function carregarGlobais() {
    if (carregandoGlobais.value) return
    carregandoGlobais.value = true
    try {
        const result = await prontuarioService.listarModelosGlobais({ tamanhoPagina: 100 })
        modelosGlobais.value = result.itens
    } catch {
        // lista vazia silenciosa
    } finally {
        carregandoGlobais.value = false
    }
}

async function importarGlobal(id: string) {
    importandoId.value = id
    msgImportacao.value = null
    try {
        await prontuarioService.importarModeloDoGlobal(id)
        msgImportacao.value = "Modelo importado com sucesso! Esta é uma cópia editável independente."
        await carregarModelos()
        abaAtiva.value = "meus"
    } catch (e: unknown) {
        const msg = (e as { response?: { data?: { mensagem?: string } } })?.response?.data?.mensagem
        msgImportacao.value = msg ?? "Erro ao importar modelo."
    } finally {
        importandoId.value = null
    }
}

watch(abaAtiva, (val) => {
    if (val === "globais" && modelosGlobais.value.length === 0) {
        void carregarGlobais()
    }
})

onMounted(async () => {
    carregando.value = true
    try {
        await carregarModelos()
    } finally {
        carregando.value = false
    }
})
</script>

<template>
    <div class="app-page">
        <AppPageHeader
            titulo="Modelos de prontuário"
            subtitulo="Crie e organize os modelos de prontuário utilizados nos atendimentos."
        />

        <div v-if="carregando" class="estado-loading">Carregando...</div>

        <div v-else class="layout-dois-paineis">
            <!-- ── Painel esquerdo: lista de modelos ── -->
            <aside class="painel painel-lista">
                <div class="painel-cabecalho">
                    <span class="painel-titulo">Modelos cadastrados</span>
                    <AppButton variant="ghost" size="sm" @click="resetarForm">+ Novo modelo</AppButton>
                </div>

                <!-- Abas -->
                <div class="abas-nav">
                    <button
                        class="aba-btn"
                        :class="{ 'aba-btn--ativa': abaAtiva === 'meus' }"
                        @click="abaAtiva = 'meus'"
                    >Meus modelos</button>
                    <button
                        class="aba-btn"
                        :class="{ 'aba-btn--ativa': abaAtiva === 'globais' }"
                        @click="abaAtiva = 'globais'"
                    >Templates do sistema</button>
                </div>

                <!-- Aba: Templates do sistema -->
                <div v-if="abaAtiva === 'globais'" class="aba-globais">
                    <p class="aba-globais-info">
                        Templates criados pela equipe Imedto. Clique em "Importar" para criar uma cópia editável independente na sua clínica.
                    </p>
                    <div v-if="carregandoGlobais" class="estado-loading-sm">Carregando templates...</div>
                    <div v-else-if="!modelosGlobais.length" class="estado-loading-sm">Nenhum template disponível.</div>
                    <div v-else class="lista-globais">
                        <div
                            v-for="g in modelosGlobais"
                            :key="g.id"
                            class="item-global"
                        >
                            <div class="item-global-info">
                                <span class="item-nome">{{ g.nome }}</span>
                                <span v-if="g.descricao" class="item-desc">{{ g.descricao }}</span>
                            </div>
                            <AppButton
                                variant="secondary"
                                size="sm"
                                :disabled="importandoId === g.id"
                                @click="importarGlobal(g.id)"
                            >
                                {{ importandoId === g.id ? "Importando..." : "Importar" }}
                            </AppButton>
                        </div>
                    </div>
                    <p v-if="msgImportacao" class="msg-importacao">{{ msgImportacao }}</p>
                </div>

                <!-- Aba: Meus modelos -->
                <template v-if="abaAtiva === 'meus'">
                <div v-if="carregandoModelos" class="estado-loading-sm">Carregando modelos...</div>

                <div v-else-if="!modelos.length">
                    <AppEmptyState
                        icone="📋"
                        titulo="Nenhum modelo cadastrado"
                        descricao="Clique em &quot;Novo modelo&quot; para criar o primeiro."
                    />
                </div>

                <div v-else class="lista-modelos">
                    <!-- Modelos padrão do sistema -->
                    <div v-if="modelosPadroes.length">
                        <div class="grupo-titulo">
                            <span>Padrões do sistema</span>
                            <div class="grupo-linha" />
                        </div>
                        <ul class="lista-itens">
                            <li v-for="m in modelosPadroes" :key="m.id" class="item-modelo item-padrao">
                                <div class="item-info">
                                    <div class="item-nome-row">
                                        <span class="item-nome">{{ m.nome }}</span>
                                        <AppBadge variant="info" label="Padrão do sistema" />
                                    </div>
                                    <p v-if="m.descricao" class="item-desc">{{ m.descricao }}</p>
                                </div>
                            </li>
                        </ul>
                    </div>

                    <!-- Modelos personalizados -->
                    <div v-if="modelosPersonalizados.length">
                        <div class="grupo-titulo">
                            <span>Personalizados</span>
                            <div class="grupo-linha" />
                        </div>
                        <ul class="lista-itens">
                            <li
                                v-for="m in modelosPersonalizados"
                                :key="m.id"
                                class="item-modelo"
                                :class="{ 'item-selecionado': form.id === m.id }"
                            >
                                <div class="item-info">
                                    <span class="item-nome">{{ m.nome }}</span>
                                    <p v-if="m.descricao" class="item-desc">{{ m.descricao }}</p>
                                </div>
                                <div class="item-acoes">
                                    <button class="btn-icon btn-icon-editar" title="Editar" @click="editarModelo(m)">
                                        <i class="fa-solid fa-pen" />
                                    </button>
                                    <button
                                        class="btn-icon btn-icon-excluir"
                                        title="Excluir"
                                        :disabled="excluindoId === m.id"
                                        @click="excluir(m)"
                                    >
                                        <i class="fa-solid fa-trash" />
                                    </button>
                                </div>
                            </li>
                        </ul>
                    </div>
                </div>
                </template>
            </aside>

            <!-- ── Painel direito: formulário ── -->
            <section class="painel painel-form">
                <h2 class="painel-titulo painel-titulo-form">
                    {{ form.id ? "Editar modelo" : "Novo modelo" }}
                </h2>

                <form class="form-corpo" @submit.prevent="salvar">
                    <AppField label="Nome do modelo" required>
                        <AppInput
                            v-model="form.nome"
                            placeholder="Ex.: Primeira consulta, Evolução pós-operatória"
                        />
                    </AppField>

                    <AppField label="Descrição (opcional)">
                        <AppTextarea
                            v-model="form.descricao"
                            placeholder="Descrição breve do objetivo deste modelo."
                            :rows="3"
                        />
                    </AppField>

                    <!-- Seções disponíveis -->
                    <div class="secoes-bloco">
                        <label class="secoes-label">Seções incluídas no modelo</label>
                        <div class="secoes-grid">
                            <label
                                v-for="s in secoesList"
                                :key="s.key"
                                class="secao-item"
                                :title="s.info"
                            >
                                <input
                                    v-model="form.secoes[s.key]"
                                    type="checkbox"
                                    class="secao-check"
                                />
                                <span>{{ s.label }}</span>
                            </label>
                        </div>
                    </div>

                    <!-- Ordem das seções ativas -->
                    <div v-if="secoesAtivas.length" class="ordem-bloco">
                        <label class="secoes-label">Ordem das seções</label>
                        <ul class="ordem-lista">
                            <li
                                v-for="key in secoesAtivas"
                                :key="key"
                                class="ordem-item"
                            >
                                <span>{{ secoesList.find(s => s.key === key)?.label ?? key }}</span>
                                <div class="ordem-btns">
                                    <button type="button" class="ordem-btn" @click="moverSecao(key, 'cima')">↑</button>
                                    <button type="button" class="ordem-btn" @click="moverSecao(key, 'baixo')">↓</button>
                                </div>
                            </li>
                        </ul>
                    </div>

                    <div class="form-rodape">
                        <AppButton variant="ghost" type="button" @click="resetarForm">Cancelar</AppButton>
                        <AppButton
                            type="submit"
                            :disabled="salvando || !form.nome.trim()"
                            :loading="salvando"
                        >
                            {{ salvando ? "Salvando..." : "Salvar modelo" }}
                        </AppButton>
                    </div>
                </form>
            </section>
        </div>
    </div>
</template>

<style scoped>
.estado-loading {
    text-align: center;
    color: var(--text-muted);
    padding: 3rem 1rem;
    font-size: 0.9em;
}

.layout-dois-paineis {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1.5rem;
    align-items: start;
}

@media (max-width: 860px) {
    .layout-dois-paineis { grid-template-columns: 1fr; }
}

/* ── Painéis ── */
.painel {
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 1.25rem;
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.painel-cabecalho {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.5rem;
}

.painel-titulo {
    font-size: 0.88em;
    font-weight: 700;
    color: var(--text);
}

.painel-titulo-form {
    margin: 0 0 0.25rem;
}

.estado-loading-sm {
    font-size: 0.82em;
    color: var(--text-muted);
    text-align: center;
    padding: 1rem 0;
}

/* ── Grupos de modelos ── */
.grupo-titulo {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.72em;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--text-muted);
    margin-bottom: 0.5rem;
}

.grupo-linha {
    flex: 1;
    height: 1px;
    background: var(--border);
}

.lista-modelos { display: flex; flex-direction: column; gap: 1rem; }

.lista-itens { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: 0.4rem; }

.item-modelo {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 0.75rem;
    padding: 0.6rem 0.75rem;
    border-radius: calc(var(--radius) - 2px);
    border: 1px solid var(--border);
    background: var(--bg);
    transition: border-color 0.12s;
}

.item-modelo:hover { border-color: var(--border-strong); }

.item-padrao { background: color-mix(in srgb, var(--info) 5%, transparent); border-color: color-mix(in srgb, var(--info) 20%, transparent); }

.item-selecionado { border-color: var(--primary); background: color-mix(in srgb, var(--primary) 5%, transparent); }

.item-info { flex: 1; display: flex; flex-direction: column; gap: 0.2rem; min-width: 0; }

.item-nome-row { display: flex; align-items: center; gap: 0.5rem; flex-wrap: wrap; }

.item-nome { font-size: 0.85em; font-weight: 600; color: var(--text); }

.item-desc { font-size: 0.78em; color: var(--text-muted); margin: 0; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }

.item-acoes { display: flex; align-items: center; gap: 0.25rem; flex-shrink: 0; }

/* ── Formulário ── */
.form-corpo { display: flex; flex-direction: column; gap: 1rem; }

/* ── Seções ── */
.secoes-bloco { display: flex; flex-direction: column; gap: 0.5rem; }

.secoes-label {
    font-size: 0.78em;
    font-weight: 700;
    color: var(--text-muted);
}

.secoes-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 0.35rem 1rem;
}

@media (max-width: 500px) {
    .secoes-grid { grid-template-columns: 1fr; }
}

.secao-item {
    display: flex;
    align-items: center;
    gap: 0.4rem;
    font-size: 0.8em;
    cursor: pointer;
    color: var(--text);
}

.secao-check {
    accent-color: var(--primary);
    width: 14px;
    height: 14px;
    flex-shrink: 0;
    cursor: pointer;
}

/* ── Ordem ── */
.ordem-bloco { display: flex; flex-direction: column; gap: 0.5rem; }

.ordem-lista { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: 0.3rem; }

.ordem-item {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.5rem;
    padding: 0.3rem 0.6rem;
    border: 1px solid var(--border);
    border-radius: calc(var(--radius) - 4px);
    background: var(--bg);
    font-size: 0.8em;
    color: var(--text);
}

.ordem-btns { display: flex; gap: 0.2rem; }

.ordem-btn {
    border: none;
    background: none;
    cursor: pointer;
    font-size: 0.85em;
    color: var(--text-muted);
    padding: 0.1rem 0.3rem;
    border-radius: 3px;
    transition: color 0.1s, background 0.1s;
}

.ordem-btn:hover { color: var(--primary); background: color-mix(in srgb, var(--primary) 8%, transparent); }

/* ── Rodapé ── */
.form-rodape {
    display: flex;
    justify-content: flex-end;
    gap: 0.75rem;
    padding-top: 0.5rem;
}

/* ── Abas Templates do sistema ── */
.abas-nav {
    display: flex;
    gap: 0;
    border-bottom: 1px solid var(--border);
    margin-bottom: 0.5rem;
}

.aba-btn {
    background: none;
    border: none;
    padding: 0.5rem 0.75rem;
    font-size: 0.82em;
    font-weight: 600;
    color: var(--text-muted);
    cursor: pointer;
    border-bottom: 2px solid transparent;
    margin-bottom: -1px;
    transition: color 0.15s, border-color 0.15s;
}

.aba-btn--ativa {
    color: var(--primary);
    border-bottom-color: var(--primary);
}

.aba-globais-info {
    font-size: 0.8em;
    color: var(--text-muted);
    margin: 0 0 0.75rem;
    line-height: 1.5;
}

.lista-globais { display: flex; flex-direction: column; gap: 0.5rem; }

.item-global {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.75rem;
    padding: 0.5rem 0.6rem;
    border: 1px solid var(--border);
    border-radius: calc(var(--radius) - 2px);
    background: var(--bg);
}

.item-global-info {
    flex: 1;
    display: flex;
    flex-direction: column;
    gap: 0.1rem;
    min-width: 0;
}

.msg-importacao {
    font-size: 0.8em;
    color: var(--success, var(--primary));
    margin: 0.5rem 0 0;
}
</style>
