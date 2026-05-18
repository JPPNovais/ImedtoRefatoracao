<!--
  Aba de pedidos de exame (MVP — sem modelos). Lista histórica + emissão.
  PDF gerado no front via usePedidoExamePdf, abertura em nova aba com
  fallback popup blocker.
-->
<script setup lang="ts">
import { computed, nextTick, onMounted, reactive, ref } from "vue"
import {
    pedidoExameService,
    type PedidoExame,
    type TipoPedidoExame,
} from "@/services/pedidoExameService"
import { pacienteService, type Paciente } from "@/services/pacienteService"
import { usePedidoExamePdf } from "@/composables/usePedidoExamePdf"
import {
    AppButton, AppEmptyState, AppField, AppInput, AppSelect, AppTextarea,
    AppModal, AppToast,
} from "@/components/ui"

const props = defineProps<{
    pacienteId: number
    pacienteNome: string
}>()

const { gerarPdf } = usePedidoExamePdf()

const pedidos = ref<PedidoExame[]>([])
const paciente = ref<Paciente | null>(null)
const carregando = ref(false)

const toast = ref<{ msg: string; variante: "info" | "success" | "error" } | null>(null)
function notificar(msg: string, variante: "info" | "success" | "error" = "success") {
    toast.value = { msg, variante }
}

// ─── Form de emissão ─────────────────────────────────────────────────────────
const emissaoAberta = ref(false)
const salvando = ref(false)

const TIPOS: { value: TipoPedidoExame; label: string }[] = [
    { value: "Laboratorial", label: "Laboratorial" },
    { value: "Imagem",       label: "Imagem" },
    { value: "Misto",        label: "Misto" },
]

const form = reactive<{
    tipo: TipoPedidoExame
    exames: string[]
    indicacaoClinica: string
    cid10: string
    observacoes: string
}>({
    tipo: "Laboratorial",
    exames: [],
    indicacaoClinica: "",
    cid10: "",
    observacoes: "",
})

const exameInput = ref("")
const exameInputRef = ref<HTMLInputElement | null>(null)

function resetForm() {
    form.tipo = "Laboratorial"
    form.exames = []
    form.indicacaoClinica = ""
    form.cid10 = ""
    form.observacoes = ""
    exameInput.value = ""
}

function adicionarExame() {
    const v = exameInput.value.trim()
    if (!v) return
    if (form.exames.includes(v)) {
        notificar("Exame já adicionado.", "info")
        exameInput.value = ""
        return
    }
    if (form.exames.length >= 50) {
        notificar("Limite de 50 exames por pedido.", "error")
        return
    }
    form.exames.push(v)
    exameInput.value = ""
    nextTick(() => exameInputRef.value?.focus())
}

function removerExame(idx: number) {
    form.exames.splice(idx, 1)
}

const CID_REGEX = /^[A-TV-Z]\d{2}(\.\d)?$/i
const erroCid = computed(() => {
    if (!form.cid10.trim()) return ""
    return CID_REGEX.test(form.cid10.trim()) ? "" : "Formato esperado: A00 ou A00.0"
})

const podeSalvar = computed(() =>
    !salvando.value &&
    form.exames.length > 0 &&
    form.indicacaoClinica.trim().length > 0 &&
    !erroCid.value,
)

function abrirEmissao() {
    resetForm()
    emissaoAberta.value = true
    nextTick(() => exameInputRef.value?.focus())
}

async function carregarHistorico() {
    carregando.value = true
    try {
        pedidos.value = await pedidoExameService.listarDoPaciente(props.pacienteId)
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Erro ao carregar pedidos de exame.", "error")
    } finally {
        carregando.value = false
    }
}

async function carregarPaciente() {
    if (paciente.value) return
    try {
        paciente.value = await pacienteService.obter(props.pacienteId)
    } catch { /* segue com nome básico */ }
}

onMounted(async () => {
    await Promise.all([carregarHistorico(), carregarPaciente()])
})

function abrirJanelaParaVisualizacao(): Window | null {
    return window.open("about:blank", "_blank")
}

async function emitirPedido() {
    if (!podeSalvar.value) return
    const janela = abrirJanelaParaVisualizacao()
    salvando.value = true
    try {
        const { pedidoExameId } = await pedidoExameService.emitir(props.pacienteId, {
            tipo: form.tipo,
            exames: form.exames,
            indicacaoClinica: form.indicacaoClinica,
            cid10: form.cid10.trim() || null,
            observacoes: form.observacoes.trim() || null,
        })
        notificar("Pedido de exame emitido.", "success")
        emissaoAberta.value = false

        const completo = await pedidoExameService.obter(pedidoExameId)
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
        notificar(e?.response?.data?.mensagem ?? "Erro ao emitir pedido de exame.", "error")
    } finally {
        salvando.value = false
    }
}

async function reimprimir(p: PedidoExame) {
    const janela = abrirJanelaParaVisualizacao()
    try {
        const pac = paciente.value ?? { nomeCompleto: props.pacienteNome } as Paciente
        if (!janela) {
            notificar("Permita pop-ups para visualizar o PDF. Baixando como alternativa.", "info")
            await gerarPdf(p, pac, "download")
            return
        }
        const { blobUrl } = await gerarPdf(p, pac, "visualizar")
        if (blobUrl) janela.location.href = blobUrl
    } catch (e: any) {
        if (janela) janela.close()
        notificar(e?.response?.data?.mensagem ?? "Erro ao gerar PDF.", "error")
    }
}

function formatarData(iso: string) {
    return new Date(iso).toLocaleString("pt-BR", {
        day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit",
    })
}

function tipoLabel(tipo: TipoPedidoExame): string {
    return TIPOS.find(t => t.value === tipo)?.label ?? tipo
}
</script>

<template>
    <section class="pex-tab">
        <header class="pex-header">
            <div>
                <h2 class="pex-titulo">
                    <i class="fa-solid fa-vial"></i>
                    Pedidos de exame
                </h2>
                <p class="pex-sub">{{ pedidos.length }} pedido(s) emitido(s) para este paciente.</p>
            </div>
            <AppButton icon="fa-solid fa-plus" @click="abrirEmissao">Solicitar exames</AppButton>
        </header>

        <div v-if="carregando" class="pex-estado">Carregando...</div>
        <AppEmptyState
            v-else-if="pedidos.length === 0"
            icone="fa-solid fa-vial"
            titulo="Nenhum pedido de exame"
            descricao="Os pedidos de exame emitidos aparecerão aqui."
        />
        <ul v-else class="pex-lista">
            <li v-for="p in pedidos" :key="p.id" class="pex-card">
                <div class="pex-card-head">
                    <span class="pex-tipo">{{ tipoLabel(p.tipo) }}</span>
                    <span v-if="p.cid10" class="pex-meta">CID-10 {{ p.cid10 }}</span>
                    <span class="pex-data">{{ formatarData(p.criadoEm) }}</span>
                </div>
                <p class="pex-indic">{{ p.indicacaoClinica }}</p>
                <ul class="pex-exames">
                    <li v-for="(e, i) in p.exames" :key="i">{{ e }}</li>
                </ul>
                <div class="pex-card-foot">
                    <span v-if="p.profissionalNome" class="pex-prof">Por: {{ p.profissionalNome }}</span>
                    <AppButton size="sm" variant="ghost" icon="fa-solid fa-file-pdf" @click="reimprimir(p)">
                        Gerar PDF
                    </AppButton>
                </div>
            </li>
        </ul>

        <!-- ─── Modal de emissão ──────────────────────────────────────── -->
        <AppModal :aberto="emissaoAberta" titulo="Solicitar exames" largura="lg" @fechar="emissaoAberta = false">
            <div class="form-emissao">
                <AppField label="Tipo">
                    <AppSelect v-model="form.tipo">
                        <option v-for="t in TIPOS" :key="t.value" :value="t.value">{{ t.label }}</option>
                    </AppSelect>
                </AppField>

                <AppField label="Exames">
                    <div class="exames-input-row">
                        <AppInput
                            ref="exameInputRef"
                            v-model="exameInput"
                            placeholder="Digite o exame e pressione Enter"
                            @keydown.enter.prevent="adicionarExame"
                        />
                        <AppButton size="sm" variant="ghost" icon="fa-solid fa-plus" @click="adicionarExame">
                            Adicionar
                        </AppButton>
                    </div>
                    <div v-if="form.exames.length > 0" class="exames-chips">
                        <span v-for="(e, i) in form.exames" :key="i" class="chip-exame">
                            <span>{{ i + 1 }}. {{ e }}</span>
                            <button type="button" class="chip-remover" @click="removerExame(i)" :aria-label="`Remover ${e}`">
                                <i class="fa-solid fa-times"></i>
                            </button>
                        </span>
                    </div>
                </AppField>

                <AppField label="Indicação clínica">
                    <AppTextarea v-model="form.indicacaoClinica" :rows="3" placeholder="Justificativa clínica do pedido..." />
                </AppField>

                <AppField label="CID-10 (opcional)" :erro="erroCid">
                    <AppInput v-model="form.cid10" placeholder="Ex: J06.9" />
                </AppField>

                <AppField label="Observações (opcional)">
                    <AppTextarea v-model="form.observacoes" :rows="3" placeholder="Observações adicionais para o paciente ou laboratório..." />
                </AppField>
            </div>

            <template #rodape>
                <AppButton variant="ghost" @click="emissaoAberta = false">Cancelar</AppButton>
                <AppButton icon="fa-solid fa-file-pdf"
                    :loading="salvando" :disabled="!podeSalvar"
                    @click="emitirPedido">
                    Salvar e gerar PDF
                </AppButton>
            </template>
        </AppModal>

        <AppToast v-if="toast" :mensagem="toast.msg" :variante="toast.variante" @fechar="toast = null" />
    </section>
</template>

<style scoped>
.pex-tab { display: flex; flex-direction: column; gap: 16px; }

.pex-header {
    display: flex; align-items: flex-start; justify-content: space-between;
    gap: 16px; flex-wrap: wrap;
}
.pex-titulo {
    font-size: 1.05rem; font-weight: 700; margin: 0;
    color: hsl(var(--primary-dark));
    display: inline-flex; align-items: center; gap: 8px;
}
.pex-titulo i { color: hsl(var(--primary)); }
.pex-sub { font-size: 0.85em; color: var(--text-muted); margin: 4px 0 0; }

.pex-estado { text-align: center; padding: 2rem 1rem; color: var(--text-muted); }
.pex-lista { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: 10px; }

.pex-card {
    background: var(--bg-card); border: 1px solid var(--border);
    border-radius: var(--radius-lg); padding: 14px 16px;
    display: flex; flex-direction: column; gap: 8px;
}
.pex-card-head { display: flex; gap: 10px; align-items: center; flex-wrap: wrap; }
.pex-tipo { font-weight: 700; color: hsl(var(--primary)); font-size: 0.92em; }
.pex-meta {
    font-size: 0.75em; padding: 2px 8px; border-radius: 99px;
    background: hsl(var(--secondary) / 0.08); color: var(--text-muted);
}
.pex-data { margin-left: auto; font-size: 0.78em; color: var(--text-muted); }
.pex-indic { font-size: 0.88em; color: var(--text); margin: 0; }
.pex-exames {
    list-style: decimal; margin: 0; padding-left: 22px;
    font-size: 0.86em; color: var(--text);
    display: flex; flex-direction: column; gap: 3px;
}
.pex-card-foot {
    display: flex; align-items: center; justify-content: space-between;
    padding-top: 6px; border-top: 1px dashed var(--border);
}
.pex-prof { font-size: 0.78em; color: var(--text-muted); }

.form-emissao { display: flex; flex-direction: column; gap: 12px; }
.exames-input-row { display: flex; gap: 8px; align-items: center; }
.exames-input-row :deep(.app-input-wrapper) { flex: 1; }
.exames-chips { display: flex; flex-wrap: wrap; gap: 6px; margin-top: 8px; }
.chip-exame {
    display: inline-flex; align-items: center; gap: 6px;
    background: hsl(var(--primary) / 0.08); color: hsl(var(--primary-dark));
    padding: 4px 6px 4px 10px; border-radius: 99px;
    font-size: 0.78em;
}
.chip-remover {
    background: transparent; border: 0; cursor: pointer;
    color: hsl(var(--primary)); padding: 2px 6px; border-radius: 99px;
    display: inline-flex; align-items: center; justify-content: center;
}
.chip-remover:hover { background: hsl(var(--primary) / 0.15); }
</style>
