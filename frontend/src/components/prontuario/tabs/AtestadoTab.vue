<!--
  Aba de Atestados dentro do prontuário. Lista histórica + emissão de novo +
  gerenciamento de modelos. PDF gerado no front via useAtestadoPdf.

  Decisões PO (2026-05-18):
  - Modelos no estabelecimento (não global do sistema).
  - "Começar do zero" no topo do select de modelos.
  - Botão "Salvar e gerar PDF" — abre em nova aba via about:blank antes do await
    (fallback download se popup bloqueado).
-->
<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from "vue"
import {
    atestadoService,
    type Atestado,
    type ModeloAtestado,
    type TipoAtestado,
} from "@/services/atestadoService"
import { pacienteService, type Paciente } from "@/services/pacienteService"
import { useAtestadoPdf } from "@/composables/useAtestadoPdf"
import { useProximosPassosStore } from "@/stores/proximosPassosStore"
import {
    AppButton, AppEmptyState, AppField, AppInput, AppPagination, AppSelect, AppTextarea,
    AppModal, AppDrawer, AppToast, AppConfirmDialog,
} from "@/components/ui"

const props = defineProps<{
    pacienteId: number
    pacienteNome: string
}>()

const { gerarPdf } = useAtestadoPdf()
const proximosPassos = useProximosPassosStore()

const atestados = ref<Atestado[]>([])
const total     = ref(0)
const modelos = ref<ModeloAtestado[]>([])
const paciente = ref<Paciente | null>(null)
const carregando = ref(false)

// ─── Paginação server-side ──────────────────────────────────────────────────
const pagina  = ref(1)
const tamanho = ref(10)
watch([pagina, tamanho], () => carregarHistorico())

// Toast simples
const toast = ref<{ msg: string; variante: "info" | "success" | "error" } | null>(null)
function notificar(msg: string, variante: "info" | "success" | "error" = "success") {
    toast.value = { msg, variante }
}

// ─── Form de emissão ─────────────────────────────────────────────────────────
const emissaoAberta = ref(false)
const salvandoEmissao = ref(false)
const modeloEscolhidoId = ref<number | null>(null)

const TIPOS: { value: TipoAtestado; label: string }[] = [
    { value: "Comparecimento", label: "Comparecimento" },
    { value: "Afastamento",    label: "Afastamento" },
    { value: "Aptidao",        label: "Aptidão" },
    { value: "Outro",          label: "Outro" },
]

const form = reactive<{
    tipo: TipoAtestado
    diasAfastamento: number | null
    cid10: string
    conteudo: string
}>({
    tipo: "Comparecimento",
    diasAfastamento: null,
    cid10: "",
    conteudo: "",
})

function resetForm() {
    form.tipo = "Comparecimento"
    form.diasAfastamento = null
    form.cid10 = ""
    form.conteudo = ""
    modeloEscolhidoId.value = null
}

function aplicarModelo(modeloId: number | null) {
    modeloEscolhidoId.value = modeloId
    if (modeloId === null) {
        form.conteudo = ""
        return
    }
    const m = modelos.value.find(x => x.id === modeloId)
    if (!m) return
    form.tipo = m.tipo
    form.conteudo = m.conteudo
}

const exibeDias = computed(() => form.tipo === "Afastamento")

// Regex CID-10 do backend, replicada para feedback imediato. Validação real é no back.
const CID_REGEX = /^[A-TV-Z]\d{2}(\.\d)?$/i
const erroCid = computed(() => {
    if (!form.cid10.trim()) return ""
    return CID_REGEX.test(form.cid10.trim()) ? "" : "Formato esperado: A00 ou A00.0"
})
const erroDias = computed(() => {
    if (!exibeDias.value) return ""
    if (form.diasAfastamento === null || form.diasAfastamento === undefined) return "Informe os dias de afastamento."
    if (form.diasAfastamento <= 0) return "Informe um número maior que zero."
    return ""
})

const podeSalvar = computed(() =>
    !salvandoEmissao.value &&
    form.conteudo.trim().length > 0 &&
    !erroCid.value &&
    !erroDias.value,
)

async function abrirEmissao() {
    resetForm()
    emissaoAberta.value = true
    if (modelos.value.length === 0) await carregarModelos()
}

async function carregarHistorico() {
    carregando.value = true
    try {
        const r = await atestadoService.listarDoPaciente(props.pacienteId, {
            pagina: pagina.value, tamanho: tamanho.value,
        })
        atestados.value = r.itens
        total.value = r.total
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Erro ao carregar atestados.", "error")
    } finally {
        carregando.value = false
    }
}

async function carregarModelos() {
    try {
        modelos.value = await atestadoService.listarModelos()
    } catch {
        // silencioso — usuário pode emitir sem modelos
    }
}

async function carregarPaciente() {
    if (paciente.value) return
    try {
        paciente.value = await pacienteService.obter(props.pacienteId)
    } catch {
        // continua com nome básico se falhar
    }
}

onMounted(async () => {
    await Promise.all([carregarHistorico(), carregarPaciente()])
})

function abrirJanelaParaVisualizacao(): Window | null {
    return window.open("about:blank", "_blank")
}

async function emitirAtestado() {
    if (!podeSalvar.value) return
    // Abre janela sincronamente — anti popup blocker.
    const janela = abrirJanelaParaVisualizacao()

    salvandoEmissao.value = true
    try {
        const { atestadoId } = await atestadoService.emitir(props.pacienteId, {
            tipo: form.tipo,
            diasAfastamento: form.tipo === "Afastamento" ? form.diasAfastamento : null,
            cid10: form.cid10.trim() || null,
            conteudo: form.conteudo,
        })
        notificar("Atestado emitido.", "success")
        emissaoAberta.value = false
        // Notifica o widget "Próximos passos" sobre a conclusão (bug 2 — CA63/CA64).
        void proximosPassos.atualizarAbertas()

        // Carrega o atestado completo (com profissionalNome) e gera PDF.
        const completo = await atestadoService.obter(atestadoId)
        const pac = paciente.value ?? { nomeCompleto: props.pacienteNome } as Paciente
        if (!janela) {
            notificar("Permita pop-ups para visualizar o PDF. Baixando como alternativa.", "info")
            await gerarPdf(completo, pac, "download")
        } else {
            const { blobUrl } = await gerarPdf(completo, pac, "visualizar")
            if (blobUrl) janela.location.href = blobUrl
        }
        await carregarHistorico()
    } catch (e: any) {
        if (janela) janela.close()
        notificar(e?.response?.data?.mensagem ?? "Erro ao emitir atestado.", "error")
    } finally {
        salvandoEmissao.value = false
    }
}

async function reimprimir(a: Atestado) {
    const janela = abrirJanelaParaVisualizacao()
    try {
        const pac = paciente.value ?? { nomeCompleto: props.pacienteNome } as Paciente
        if (!janela) {
            notificar("Permita pop-ups para visualizar o PDF. Baixando como alternativa.", "info")
            await gerarPdf(a, pac, "download")
            return
        }
        const { blobUrl } = await gerarPdf(a, pac, "visualizar")
        if (blobUrl) janela.location.href = blobUrl
    } catch (e: any) {
        if (janela) janela.close()
        notificar(e?.response?.data?.mensagem ?? "Erro ao gerar PDF.", "error")
    }
}

// ─── Gerenciador de modelos ──────────────────────────────────────────────────
const modelosAberto = ref(false)
const modeloEditando = ref<ModeloAtestado | null>(null)
const formModelo = reactive<{ nome: string; tipo: TipoAtestado; conteudo: string }>({
    nome: "", tipo: "Comparecimento", conteudo: "",
})
const salvandoModelo = ref(false)
const modeloExcluir = ref<ModeloAtestado | null>(null)

async function abrirModelos() {
    await carregarModelos()
    modelosAberto.value = true
    modeloEditando.value = null
    resetFormModelo()
}

function resetFormModelo() {
    formModelo.nome = ""
    formModelo.tipo = "Comparecimento"
    formModelo.conteudo = ""
}

function iniciarEdicaoModelo(m: ModeloAtestado) {
    modeloEditando.value = m
    formModelo.nome = m.nome
    formModelo.tipo = m.tipo
    formModelo.conteudo = m.conteudo
}

function iniciarNovoModelo() {
    modeloEditando.value = null
    resetFormModelo()
}

const podeSalvarModelo = computed(() =>
    !salvandoModelo.value &&
    formModelo.nome.trim().length > 0 &&
    formModelo.conteudo.trim().length > 0,
)

async function salvarModelo() {
    if (!podeSalvarModelo.value) return
    salvandoModelo.value = true
    try {
        if (modeloEditando.value) {
            await atestadoService.atualizarModelo(modeloEditando.value.id, {
                nome: formModelo.nome,
                tipo: formModelo.tipo,
                conteudo: formModelo.conteudo,
            })
        } else {
            await atestadoService.criarModelo({
                nome: formModelo.nome,
                tipo: formModelo.tipo,
                conteudo: formModelo.conteudo,
            })
        }
        await carregarModelos()
        modeloEditando.value = null
        resetFormModelo()
        notificar("Modelo salvo.", "success")
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Erro ao salvar modelo.", "error")
    } finally {
        salvandoModelo.value = false
    }
}

async function confirmarExclusao() {
    if (!modeloExcluir.value) return
    try {
        await atestadoService.excluirModelo(modeloExcluir.value.id)
        await carregarModelos()
        notificar("Modelo excluído.", "success")
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Erro ao excluir modelo.", "error")
    } finally {
        modeloExcluir.value = null
    }
}

function formatarData(iso: string) {
    return new Date(iso).toLocaleString("pt-BR", {
        day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit",
    })
}

function tipoLabel(tipo: TipoAtestado): string {
    return TIPOS.find(t => t.value === tipo)?.label ?? tipo
}
</script>

<template>
    <section class="atestado-tab">
        <header class="ata-header">
            <div>
                <h2 class="ata-titulo">
                    <i class="fa-solid fa-file-signature"></i>
                    Atestados
                </h2>
                <p class="ata-sub">{{ total }} atestado(s) emitido(s) para este paciente.</p>
            </div>
            <div class="ata-acoes">
                <AppButton variant="ghost" icon="fa-solid fa-list" @click="abrirModelos">
                    Gerenciar modelos
                </AppButton>
                <AppButton icon="fa-solid fa-plus" @click="abrirEmissao">
                    Emitir atestado
                </AppButton>
            </div>
        </header>

        <div v-if="carregando" class="ata-estado">Carregando...</div>
        <AppEmptyState
            v-else-if="atestados.length === 0"
            icone="fa-solid fa-file-signature"
            titulo="Nenhum atestado emitido"
            descricao="Os atestados emitidos para este paciente aparecerão aqui."
        />
        <ul v-else class="ata-lista">
            <li v-for="a in atestados" :key="a.id" class="ata-card">
                <div class="ata-card-head">
                    <span class="ata-tipo">{{ tipoLabel(a.tipo) }}</span>
                    <span v-if="a.diasAfastamento" class="ata-meta">{{ a.diasAfastamento }} dia(s)</span>
                    <span v-if="a.cid10" class="ata-meta">CID-10 {{ a.cid10 }}</span>
                    <span class="ata-data">{{ formatarData(a.criadoEm) }}</span>
                </div>
                <p class="ata-conteudo">{{ a.conteudo }}</p>
                <div class="ata-card-foot">
                    <span v-if="a.profissionalNome" class="ata-prof">Por: {{ a.profissionalNome }}</span>
                    <AppButton size="sm" variant="ghost" icon="fa-solid fa-file-pdf" @click="reimprimir(a)">
                        Gerar PDF
                    </AppButton>
                </div>
            </li>
        </ul>

        <AppPagination
            v-if="total > 0"
            v-model:pagina="pagina"
            v-model:tamanho="tamanho"
            :total="total"
            rotulo-itens="atestado(s)"
        />

        <!-- ─── Modal de emissão ──────────────────────────────────────── -->
        <AppModal :aberto="emissaoAberta" titulo="Emitir atestado" largura="lg" @fechar="emissaoAberta = false">
            <div class="form-emissao">
                <AppField label="Começar de um modelo (opcional)">
                    <AppSelect :model-value="modeloEscolhidoId"
                        @update:model-value="aplicarModelo($event === '' || $event === null ? null : Number($event))">
                        <option :value="null">— Começar do zero</option>
                        <option v-for="m in modelos" :key="m.id" :value="m.id">{{ m.nome }}</option>
                    </AppSelect>
                </AppField>

                <div class="grid-2">
                    <AppField label="Tipo">
                        <AppSelect v-model="form.tipo">
                            <option v-for="t in TIPOS" :key="t.value" :value="t.value">{{ t.label }}</option>
                        </AppSelect>
                    </AppField>
                    <AppField v-if="exibeDias" label="Dias de afastamento" :erro="erroDias">
                        <AppInput v-model.number="form.diasAfastamento" type="number" min="1" max="365" />
                    </AppField>
                </div>

                <AppField label="CID-10 (opcional)" :erro="erroCid">
                    <AppInput v-model="form.cid10" placeholder="Ex: J06.9" />
                </AppField>

                <AppField label="Conteúdo">
                    <AppTextarea v-model="form.conteudo" :rows="8" placeholder="Texto do atestado..." />
                </AppField>
            </div>

            <template #rodape>
                <AppButton variant="ghost" @click="emissaoAberta = false">Cancelar</AppButton>
                <AppButton icon="fa-solid fa-file-pdf"
                    :loading="salvandoEmissao" :disabled="!podeSalvar"
                    @click="emitirAtestado">
                    Salvar e gerar PDF
                </AppButton>
            </template>
        </AppModal>

        <!-- ─── Drawer de modelos ─────────────────────────────────────── -->
        <AppDrawer :aberto="modelosAberto" titulo="Modelos de atestado" :largura="640" @fechar="modelosAberto = false">
            <div class="modelos-body">
                <div class="modelos-lista">
                    <h3 class="modelos-h3">Salvos ({{ modelos.length }})</h3>
                    <p v-if="modelos.length === 0" class="modelos-vazio">
                        Nenhum modelo salvo. Crie um para agilizar emissões futuras.
                    </p>
                    <ul v-else class="modelos-ul">
                        <li v-for="m in modelos" :key="m.id"
                            :class="['mod-item', { ativo: modeloEditando?.id === m.id }]">
                            <button type="button" class="mod-acao" @click="iniciarEdicaoModelo(m)">
                                <strong>{{ m.nome }}</strong>
                                <span>{{ tipoLabel(m.tipo) }}</span>
                            </button>
                            <button type="button" class="btn-icon btn-icon-excluir" title="Excluir"
                                @click="modeloExcluir = m">
                                <i class="fa-solid fa-trash"></i>
                            </button>
                        </li>
                    </ul>

                    <AppButton size="sm" variant="ghost" icon="fa-solid fa-plus" @click="iniciarNovoModelo">
                        Novo modelo
                    </AppButton>
                </div>

                <div class="modelos-form">
                    <h3 class="modelos-h3">
                        {{ modeloEditando ? "Editar modelo" : "Novo modelo" }}
                    </h3>
                    <AppField label="Nome">
                        <AppInput v-model="formModelo.nome" placeholder="Ex: Comparecimento padrão" />
                    </AppField>
                    <AppField label="Tipo">
                        <AppSelect v-model="formModelo.tipo">
                            <option v-for="t in TIPOS" :key="t.value" :value="t.value">{{ t.label }}</option>
                        </AppSelect>
                    </AppField>
                    <AppField label="Conteúdo">
                        <AppTextarea v-model="formModelo.conteudo" :rows="6" placeholder="Texto do modelo..." />
                    </AppField>
                    <div class="modelos-form-acoes">
                        <AppButton variant="ghost" @click="iniciarNovoModelo">Limpar</AppButton>
                        <AppButton :loading="salvandoModelo" :disabled="!podeSalvarModelo" @click="salvarModelo">
                            {{ modeloEditando ? "Atualizar" : "Criar" }}
                        </AppButton>
                    </div>
                </div>
            </div>
        </AppDrawer>

        <AppConfirmDialog
            :aberto="modeloExcluir !== null"
            titulo="Excluir modelo?"
            :mensagem="`Tem certeza que deseja excluir o modelo &quot;${modeloExcluir?.nome ?? ''}&quot;?`"
            confirmar-rotulo="Excluir"
            variante="danger"
            @confirmar="confirmarExclusao"
            @cancelar="modeloExcluir = null"
        />

        <AppToast v-if="toast" :mensagem="toast.msg" :variante="toast.variante" @fechar="toast = null" />
    </section>
</template>

<style scoped>
.atestado-tab { display: flex; flex-direction: column; gap: 16px; }

.ata-header {
    display: flex; align-items: flex-start; justify-content: space-between;
    gap: 16px; flex-wrap: wrap;
}
.ata-titulo {
    font-size: var(--text-md); font-weight: var(--font-weight-bold); margin: 0;
    color: hsl(var(--primary-dark));
    display: inline-flex; align-items: center; gap: 8px;
}
.ata-titulo i { color: hsl(var(--primary)); }
.ata-sub { font-size: 0.85em; color: var(--text-muted); margin: 4px 0 0; }
.ata-acoes { display: flex; gap: 8px; }

.ata-estado { text-align: center; padding: 2rem 1rem; color: var(--text-muted); }
.ata-lista { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: 10px; }

.ata-card {
    background: var(--bg-card); border: 1px solid var(--border);
    border-radius: var(--radius-lg); padding: 14px 16px;
    display: flex; flex-direction: column; gap: 8px;
}
.ata-card-head { display: flex; gap: 10px; align-items: center; flex-wrap: wrap; }
.ata-tipo {
    font-weight: 700; color: hsl(var(--primary)); font-size: 0.92em;
}
.ata-meta {
    font-size: 0.75em; padding: 2px 8px; border-radius: 99px;
    background: hsl(var(--secondary) / 0.08); color: var(--text-muted);
}
.ata-data { margin-left: auto; font-size: 0.78em; color: var(--text-muted); }
.ata-conteudo { font-size: 0.9em; color: var(--text); margin: 0; white-space: pre-wrap; }
.ata-card-foot {
    display: flex; align-items: center; justify-content: space-between;
    padding-top: 6px; border-top: 1px dashed var(--border);
}
.ata-prof { font-size: 0.78em; color: var(--text-muted); }

.form-emissao { display: flex; flex-direction: column; gap: 12px; }
.grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
@media (max-width: 600px) { .grid-2 { grid-template-columns: 1fr; } }

.modelos-body {
    display: grid; grid-template-columns: 1fr 1.3fr; gap: 16px;
    padding: 16px; height: 100%;
}
@media (max-width: 700px) { .modelos-body { grid-template-columns: 1fr; } }
.modelos-h3 { font-size: var(--text-sm); font-weight: var(--font-weight-bold); color: hsl(var(--primary-dark)); margin: 0 0 8px; }
.modelos-vazio { font-size: 0.82em; color: var(--text-muted); margin: 6px 0 12px; }
.modelos-ul { list-style: none; margin: 0 0 12px; padding: 0; display: flex; flex-direction: column; gap: 6px; }
.mod-item {
    display: flex; align-items: center; gap: 6px;
    background: var(--bg-card); border: 1px solid var(--border); border-radius: var(--radius);
    padding: 4px 4px 4px 0;
}
.mod-item.ativo { border-color: hsl(var(--primary)); background: hsl(var(--primary) / 0.06); }
.mod-acao {
    flex: 1; padding: 8px 12px; background: transparent; border: 0; cursor: pointer;
    text-align: left; font: inherit; display: flex; flex-direction: column; gap: 2px;
}
.mod-acao strong { font-size: 0.88em; color: hsl(var(--primary-dark)); }
.mod-acao span { font-size: 0.72em; color: var(--text-muted); }

.modelos-form { display: flex; flex-direction: column; gap: 10px; }
.modelos-form-acoes { display: flex; justify-content: flex-end; gap: 8px; margin-top: 4px; }
</style>
