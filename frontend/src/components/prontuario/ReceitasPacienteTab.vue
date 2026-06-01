<!--
  Aba de Receitas dentro do prontuário. Backend é fonte da verdade: lista
  paginada server-side via /api/pacientes/{id}/receitas e CRUD via aggregate
  Receita. Fluxo: novo rascunho → autosave (atualizar-rascunho) → finalizar.
  Receitas finalizadas podem ser canceladas, duplicadas e impressas.
-->
<script setup lang="ts">
import { computed, onMounted, onUnmounted, reactive, ref, watch } from "vue"
import {
    receitaService,
    FORMAS_FARMACEUTICAS,
    VIAS_ADMINISTRACAO,
    TIPOS_NOTIFICACAO,
    type ItemReceita,
    type Receita,
    type ReceitaResumo,
    type StatusReceita,
    type TipoNotificacao,
    type TipoReceita,
} from "@/services/receitaService"
import {
    AppButton, AppConfirmDialog, AppInput, AppPagination, AppSelect, AppTextarea, AppToast,
} from "@/components/ui"
import CancelarReceitaModal from "@/components/prontuario/CancelarReceitaModal.vue"
import AssinaturaStatusBadge from "@/components/ui/AssinaturaStatusBadge.vue"
import AssinaturaOnboardingModal from "@/components/prontuario/AssinaturaOnboardingModal.vue"
import AssinaturaPollingIndicator from "@/components/prontuario/AssinaturaPollingIndicator.vue"
import { assinaturaDigitalService, ASSINATURA_DIGITAL_HABILITADA, type StatusAssinaturaDigital } from "@/services/assinaturaDigitalService"
import { useAssinaturaDigitalStore } from "@/stores/assinaturaDigitalStore"
import { useAuthStore } from "@/stores/authStore"

const props = defineProps<{
    pacienteId: number
    pacienteNome: string
}>()

// ─── Assinatura digital ───────────────────────────────────────────────────────
const assinaturaStore = useAssinaturaDigitalStore()
const authStore = useAuthStore()
const modalOnboarding = ref(false)
const pollingAtivo = ref<number | null>(null) // receitaId com polling ativo
const timeoutPolling = ref(false) // flag de timeout 5 min

onMounted(() => assinaturaStore.carregarCertificado())
onUnmounted(() => {
    if (pollingAtivo.value !== null)
        assinaturaStore.pararPolling(String(pollingAtivo.value))
})

function ehPrescritorDaReceitaAberta(): boolean {
    if (!receitaAberta.value) return false
    return receitaAberta.value.profissionalUsuarioId === authStore.usuario?.id
}

const temCertificado = computed(() => assinaturaStore.certificadoVinculado !== null)

function statusAssinaturaDeReceita(r: ReceitaResumo): StatusAssinaturaDigital {
    const cached = assinaturaStore.statusPorReceita[String(r.id)]
    if (cached) return cached
    return r.assinaturaDigitalStatus ?? "NaoAssinada"
}

async function dispararAssinatura(receitaId: number) {
    salvandoAcao.value = true
    timeoutPolling.value = false
    try {
        await assinaturaDigitalService.dispararAssinatura(receitaId)
        assinaturaStore.setStatus(String(receitaId), "AssinaturaPendente")
        pollingAtivo.value = receitaId
        assinaturaStore.iniciarPolling(
            String(receitaId),
            (status) => {
                pollingAtivo.value = null
                notificar(
                    status === "AssinadaIcp"
                        ? "Receita assinada com sucesso!"
                        : "Falha na assinatura. Tente novamente.",
                    status === "AssinadaIcp" ? "success" : "error",
                )
                carregar()
                if (receitaAberta.value?.id === receitaId)
                    abrirReceita(receitaId)
            },
            () => {
                pollingAtivo.value = null
                timeoutPolling.value = true
            },
        )
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Erro ao disparar assinatura.", "error")
    } finally {
        salvandoAcao.value = false
    }
}

function cancelarPolling() {
    if (pollingAtivo.value !== null) {
        const id = pollingAtivo.value
        assinaturaStore.pararPolling(String(id))
        pollingAtivo.value = null
        // Não altera o status: o webhook do BirdID pode ainda chegar.
        // O status ficará como AssinaturaPendente até o job de expiração agir.
    }
}

async function baixarPdfAssinado(receitaId: number) {
    try {
        const res = await assinaturaDigitalService.obterStatus(receitaId)
        if (res.pdfAssinadoUrl) {
            window.open(res.pdfAssinadoUrl, "_blank", "noopener,noreferrer")
        } else {
            notificar("URL do PDF assinado não disponível.", "error")
        }
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Erro ao obter PDF assinado.", "error")
    }
}

// ─── Estado de lista (paginação server-side) ─────────────────────────────────
const receitas = ref<ReceitaResumo[]>([])
const total    = ref(0)
const pagina   = ref(1)
const tamanho  = ref(10)
const carregandoLista = ref(false)

// ─── Estado de editor ────────────────────────────────────────────────────────
const receitaAberta   = ref<Receita | null>(null)
const carregandoEditor = ref(false)
const salvandoAcao    = ref(false)

// ─── Confirms / toast ────────────────────────────────────────────────────────
const toast = ref<{ msg: string; variante: "info" | "success" | "error" } | null>(null)
function notificar(msg: string, variante: "info" | "success" | "error" = "success") {
    toast.value = { msg, variante }
}

const confirmar = ref<{
    titulo: string
    mensagem: string
    variante?: "primary" | "danger"
    rotulo?: string
    onConfirm: () => void | Promise<void>
} | null>(null)

function pedirConfirmacao(payload: NonNullable<typeof confirmar.value>) {
    confirmar.value = payload
}
async function confirmarAcao() {
    if (!confirmar.value) return
    const fn = confirmar.value.onConfirm
    confirmar.value = null
    await fn()
}

// ─── Modal de cancelamento de receita ────────────────────────────────────────
const modalCancelar = ref(false)

async function onCancelarConfirmado(motivo: string) {
    if (!receitaAberta.value) return
    modalCancelar.value = false
    salvandoAcao.value = true
    try {
        await receitaService.cancelar(receitaAberta.value.id, motivo)
        await abrirReceita(receitaAberta.value.id)
        notificar("Receita cancelada.", "success")
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Erro ao cancelar.", "error")
    } finally {
        salvandoAcao.value = false
    }
}

function viaLabel(via: string | null | undefined): string {
    if (!via) return ""
    return VIAS_ADMINISTRACAO.find(v => v.valor === via)?.label ?? via
}

// ─── Carregamento da lista ──────────────────────────────────────────────────
async function carregar() {
    carregandoLista.value = true
    try {
        const r = await receitaService.listarDoPaciente(props.pacienteId, {
            pagina: pagina.value, tamanho: tamanho.value,
        })
        receitas.value = r.itens
        total.value = r.total
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Erro ao carregar receitas.", "error")
    } finally {
        carregandoLista.value = false
    }
}

watch([pagina, tamanho], carregar)
onMounted(carregar)

// ─── Criar nova receita (rascunho) ──────────────────────────────────────────
async function novaReceita(tipo: TipoReceita) {
    salvandoAcao.value = true
    try {
        const { receitaId } = await receitaService.iniciarRascunho({
            pacienteId: props.pacienteId,
            tipo,
            // Para Controlada, default sugerido. Usuário pode trocar depois.
            tipoNotificacao: tipo === "Controlada" ? "B" : null,
        })
        await abrirReceita(receitaId)
        await carregar()
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Erro ao iniciar receita.", "error")
    } finally {
        salvandoAcao.value = false
    }
}

async function abrirReceita(id: number) {
    carregandoEditor.value = true
    try {
        receitaAberta.value = await receitaService.obter(id)
        formObs.value = receitaAberta.value.observacoes ?? ""
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Erro ao carregar receita.", "error")
    } finally {
        carregandoEditor.value = false
    }
}

function fecharEditor() {
    receitaAberta.value = null
    mostrandoFormItem.value = false
    editandoItemId.value = null
    carregar()
}

// ─── Editor de item ──────────────────────────────────────────────────────────
const formObs = ref("")
const mostrandoFormItem = ref(false)
const editandoItemId    = ref<number | null>(null)
const erro = ref<string | null>(null)

const form = reactive<{
    medicamento: string
    concentracao: string
    formaFarmaceutica: string
    quantidade: string
    via: string
    posologia: string
    duracao: string
    observacao: string
}>({
    medicamento: "", concentracao: "", formaFarmaceutica: "", quantidade: "",
    via: "", posologia: "", duracao: "", observacao: "",
})

function limparForm() {
    form.medicamento = ""
    form.concentracao = ""
    form.formaFarmaceutica = ""
    form.quantidade = ""
    form.via = ""
    form.posologia = ""
    form.duracao = ""
    form.observacao = ""
}

function abrirFormNovoItem() {
    limparForm()
    editandoItemId.value = null
    mostrandoFormItem.value = true
    erro.value = null
}

function abrirFormEdicaoItem(item: ItemReceita) {
    editandoItemId.value = item.id ?? null
    form.medicamento = item.medicamento
    form.concentracao = item.concentracao ?? ""
    form.formaFarmaceutica = item.formaFarmaceutica ?? ""
    form.quantidade = item.quantidade ?? ""
    form.via = item.via ?? ""
    form.posologia = item.posologia
    form.duracao = item.duracao ?? ""
    form.observacao = item.observacao ?? ""
    mostrandoFormItem.value = true
    erro.value = null
}

/**
 * Backend recebe o array completo de itens no autosave (PUT /rascunho).
 * Construímos a próxima versão da lista localmente, enviamos e sincronizamos
 * com a resposta (id do item gerado/atualizado pelo backend).
 */
async function salvarItem() {
    if (!receitaAberta.value) return
    if (!form.medicamento.trim() || !form.posologia.trim()) {
        erro.value = "Informe ao menos o medicamento e a posologia."
        return
    }
    erro.value = null

    const itemNovo: Omit<ItemReceita, "id" | "ordem"> = {
        medicamento: form.medicamento.trim(),
        posologia: form.posologia.trim(),
        quantidade: form.quantidade.trim() || null,
        via: form.via.trim() || null,
        observacao: form.observacao.trim() || null,
        concentracao: form.concentracao.trim() || null,
        formaFarmaceutica: form.formaFarmaceutica.trim() || null,
        duracao: form.duracao.trim() || null,
    }

    const itensAtuais = receitaAberta.value.itens.map(stripIds)
    const proximaLista = editandoItemId.value
        ? itensAtuais.map((it, idx) =>
            receitaAberta.value!.itens[idx].id === editandoItemId.value ? itemNovo : it,
          )
        : [...itensAtuais, itemNovo]

    await persistirRascunho(proximaLista, formObs.value)
    mostrandoFormItem.value = false
    editandoItemId.value = null
    limparForm()
}

function stripIds(it: ItemReceita): Omit<ItemReceita, "id" | "ordem"> {
    return {
        medicamento: it.medicamento,
        posologia: it.posologia,
        quantidade: it.quantidade ?? null,
        via: it.via ?? null,
        observacao: it.observacao ?? null,
        concentracao: it.concentracao ?? null,
        formaFarmaceutica: it.formaFarmaceutica ?? null,
        duracao: it.duracao ?? null,
    }
}

async function removerItem(item: ItemReceita) {
    if (!receitaAberta.value) return
    pedirConfirmacao({
        titulo: "Remover medicamento?",
        mensagem: `Remover &quot;${item.medicamento}&quot; desta receita?`,
        variante: "danger",
        rotulo: "Remover",
        onConfirm: async () => {
            const proxima = receitaAberta.value!.itens
                .filter(it => it.id !== item.id)
                .map(stripIds)
            await persistirRascunho(proxima, formObs.value)
        },
    })
}

async function persistirRascunho(
    itens: Omit<ItemReceita, "id" | "ordem">[],
    observacoes: string,
) {
    if (!receitaAberta.value) return
    salvandoAcao.value = true
    try {
        await receitaService.atualizarRascunho(receitaAberta.value.id, {
            observacoes: observacoes.trim() || null,
            itens,
        })
        // Recarrega para refletir ordem/ids atribuídos pelo backend.
        receitaAberta.value = await receitaService.obter(receitaAberta.value.id)
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Erro ao salvar rascunho.", "error")
    } finally {
        salvandoAcao.value = false
    }
}

// ─── Observações: debounced autosave ────────────────────────────────────────
let obsTimer: ReturnType<typeof setTimeout> | null = null
function onObsChange(v: string) {
    formObs.value = v
    if (obsTimer) clearTimeout(obsTimer)
    obsTimer = setTimeout(() => {
        if (!receitaAberta.value) return
        if (receitaAberta.value.status !== "Rascunho") return
        const itens = receitaAberta.value.itens.map(stripIds)
        persistirRascunho(itens, formObs.value)
    }, 600)
}

// ─── Ações na receita ────────────────────────────────────────────────────────
async function finalizar() {
    if (!receitaAberta.value) return
    if (receitaAberta.value.itens.length === 0) {
        notificar("Adicione ao menos um medicamento antes de finalizar.", "error")
        return
    }
    pedirConfirmacao({
        titulo: "Finalizar receita?",
        mensagem: "Após finalizada, só será possível cancelar ou duplicar para gerar uma nova versão.",
        rotulo: "Finalizar",
        onConfirm: async () => {
            salvandoAcao.value = true
            try {
                await receitaService.finalizar(receitaAberta.value!.id)
                await abrirReceita(receitaAberta.value!.id)
                notificar("Receita finalizada.", "success")
            } catch (e: any) {
                notificar(e?.response?.data?.mensagem ?? "Erro ao finalizar.", "error")
            } finally {
                salvandoAcao.value = false
            }
        },
    })
}

function cancelar() {
    if (!receitaAberta.value) return
    modalCancelar.value = true
}

async function duplicar() {
    if (!receitaAberta.value) return
    salvandoAcao.value = true
    try {
        const { receitaId } = await receitaService.duplicar(receitaAberta.value.id)
        await abrirReceita(receitaId)
        await carregar()
        notificar("Nova versão criada como rascunho.", "success")
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Erro ao duplicar.", "error")
    } finally {
        salvandoAcao.value = false
    }
}

// ─── Impressão (HTML simples — assinatura digital ainda não implementada) ───
function imprimir() {
    const r = receitaAberta.value
    if (!r) return
    const w = window.open("", "_blank", "width=800,height=900")
    if (!w) {
        notificar("Permita pop-ups para imprimir.", "info")
        return
    }
    w.document.write(gerarHtmlImpressao(r))
    w.document.close()
    w.onload = () => { w.focus(); w.print() }
}

function gerarHtmlImpressao(r: Receita): string {
    const itensHtml = r.itens.map((it, i) => `
        <div class="item">
            <strong>${i + 1}. ${escape(it.medicamento)}${it.concentracao ? " — " + escape(it.concentracao) : ""}</strong>
            ${it.formaFarmaceutica ? `<div>${escape(it.formaFarmaceutica)}${it.quantidade ? " · " + escape(it.quantidade) : ""}</div>` : ""}
            <div class="pos"><em>${escape(it.posologia)}</em></div>
            ${it.via ? `<div>Via: ${escape(viaLabel(it.via))}</div>` : ""}
            ${it.duracao ? `<div>Duração: ${escape(it.duracao)}</div>` : ""}
            ${it.observacao ? `<div class="obs">${escape(it.observacao)}</div>` : ""}
        </div>
    `).join("")
    const dataRef = formatarData(r.emitidaEm ?? new Date().toISOString())
    return `<!doctype html><html><head><meta charset="utf-8"><title>Receita — ${escape(props.pacienteNome)}</title>
      <style>
        body{font-family:Nunito,sans-serif;max-width:720px;margin:40px auto;padding:0 1rem;color:#111;}
        h1{font-size:1.4rem;border-bottom:2px solid #452b97;padding-bottom:.3rem;color:#452b97}
        .meta{color:#666;font-size:.85rem;margin-bottom:1.5rem}
        .item{padding:.75rem 0;border-bottom:1px solid #eee}
        .pos{margin:.3rem 0;}
        .obs{font-size:.85rem;color:#555;margin-top:.2rem}
        .aviso-print{margin-top:2rem;padding:.75rem 1rem;background:#fef3c7;border:1px solid #fbbf24;border-radius:6px;font-size:.85rem;color:#7c2d12;line-height:1.45}
        .rodape{margin-top:3rem;padding-top:1rem;border-top:1px dashed #ccc;font-size:.85rem;color:#555;text-align:right}
        .tipo{display:inline-block;padding:.15rem .5rem;border-radius:4px;font-size:.75rem;font-weight:700;background:#dbeafe;color:#1e40af}
        .tipo.ctrl{background:#fee2e2;color:#991b1b}
      </style></head><body>
      <h1>Receita médica <span class="tipo ${r.tipo === "Controlada" ? "ctrl" : ""}">${r.tipo}${r.tipoNotificacao ? " — " + r.tipoNotificacao : ""}</span></h1>
      <div class="meta">
        <strong>Paciente:</strong> ${escape(props.pacienteNome)}<br>
        ${r.profissionalNome ? `<strong>Profissional:</strong> ${escape(r.profissionalNome)}<br>` : ""}
        <strong>Data:</strong> ${dataRef}<br>
      </div>
      ${itensHtml}
      ${r.observacoes ? `<p><strong>Observações:</strong> ${escape(r.observacoes)}</p>` : ""}
      <div class="aviso-print">
        <strong>Atenção:</strong> esta receita não foi assinada digitalmente (ICP-Brasil / Memed).
        Para validade jurídica plena em farmácias que exigem assinatura digital,
        o profissional deve assinar manualmente o documento impresso (CFM 2.299/2021).
      </div>
      <div class="rodape">___________________________<br>Assinatura</div>
    </body></html>`
}

function escape(s: string) {
    return s.replace(/[&<>"']/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", "\"": "&quot;", "'": "&#39;" }[c] as string))
}

function formatarData(iso: string) {
    return new Date(iso).toLocaleDateString("pt-BR", {
        day: "2-digit", month: "2-digit", year: "numeric",
        hour: "2-digit", minute: "2-digit",
    })
}

// ─── Helpers visuais ─────────────────────────────────────────────────────────
function statusLabel(s: StatusReceita) {
    return s === "Rascunho" ? "Rascunho"
        : s === "Emitida"   ? "Emitida"
        : s === "Cancelada" ? "Cancelada"
        : "Substituída"
}
function statusBadgeClass(s: StatusReceita) {
    return s === "Rascunho"  ? "badge-warning"
        : s === "Emitida"    ? "badge-success"
        : s === "Cancelada"  ? "badge-muted"
        : "badge-info"
}
const podeAtualizarNotificacao = computed(() =>
    receitaAberta.value?.tipo === "Controlada" && receitaAberta.value?.status === "Rascunho",
)

async function atualizarTipoNotificacao(tn: TipoNotificacao) {
    if (!receitaAberta.value) return
    // O backend só aceita tipoNotificacao via Emitir/IniciarRascunho. Para
    // mudar depois, recriamos via cancel+novo. UX simplificada: o usuário
    // confirma e seguimos. (Versão futura: endpoint dedicado.)
    notificar("Para alterar tipo de notificação, cancele esta receita e crie uma nova.", "info")
}
</script>

<template>
    <!-- ══════════════════ LISTA DE RECEITAS ══════════════════ -->
    <div v-if="!receitaAberta" class="receitas-lista">
        <div class="receitas-header">
            <h3 class="titulo">Receitas do paciente</h3>
            <div class="header-acoes">
                <AppButton
                    variant="secondary" size="sm"
                    icon="fa-solid fa-shield-halved"
                    :loading="salvandoAcao"
                    @click="novaReceita('Controlada')"
                >
                    Nova controlada
                </AppButton>
                <AppButton
                    size="sm" icon="fa-solid fa-plus"
                    :loading="salvandoAcao"
                    @click="novaReceita('Comum')"
                >
                    Nova receita
                </AppButton>
            </div>
        </div>

        <div v-if="carregandoLista" class="estado-info">Carregando...</div>

        <div v-else-if="receitas.length === 0" class="estado-vazio">
            <i class="fa-solid fa-prescription icone-vazio"></i>
            <p>Nenhuma receita emitida para este paciente.</p>
            <AppButton size="sm" @click="novaReceita('Comum')">Criar primeira receita</AppButton>
        </div>

        <ul v-else class="receitas">
            <li
                v-for="r in receitas" :key="r.id"
                class="receita-card"
                :class="{ finalizada: r.status === 'Emitida' }"
                @click="abrirReceita(r.id)"
            >
                <div class="receita-principal">
                    <div class="receita-header-linha">
                        <span :class="['badge', r.tipo === 'Controlada' ? 'badge-error' : 'badge-info']">
                            {{ r.tipo }}
                        </span>
                        <span :class="['badge', statusBadgeClass(r.status)]">
                            {{ statusLabel(r.status) }}
                        </span>
                        <span v-if="r.tipoNotificacao" class="badge badge-muted">
                            Notif. {{ r.tipoNotificacao }}
                        </span>
                        <span v-if="r.requerRetencao" class="badge badge-warning">RETER</span>
                        <!-- Badge de assinatura digital — CA-13 -->
                        <AssinaturaStatusBadge v-if="ASSINATURA_DIGITAL_HABILITADA" :status="statusAssinaturaDeReceita(r)" />
                    </div>
                    <div class="receita-data">
                        {{ formatarData(r.emitidaEm ?? new Date().toISOString()) }}
                    </div>
                    <div class="receita-meta">
                        {{ r.quantidadeItens }}
                        {{ r.quantidadeItens === 1 ? "medicamento" : "medicamentos" }}
                        <span v-if="r.profissionalNome"> · por {{ r.profissionalNome }}</span>
                    </div>
                </div>
                <i class="fa-solid fa-chevron-right seta"></i>
            </li>
        </ul>

        <AppPagination
            v-if="total > 0"
            v-model:pagina="pagina"
            v-model:tamanho="tamanho"
            :total="total"
            rotulo-itens="receita(s)"
        />
    </div>

    <!-- ══════════════════ EDITOR DE RECEITA ══════════════════ -->
    <div v-else class="editor">
        <div class="editor-header">
            <AppButton variant="ghost" icon="fa-solid fa-arrow-left" @click="fecharEditor">
                Voltar às receitas
            </AppButton>
            <div class="header-badges">
                <span :class="['badge', receitaAberta.tipo === 'Controlada' ? 'badge-error' : 'badge-info']">
                    {{ receitaAberta.tipo }}
                </span>
                <span :class="['badge', statusBadgeClass(receitaAberta.status)]">
                    {{ statusLabel(receitaAberta.status) }}
                </span>
                <span v-if="receitaAberta.tipoNotificacao" class="badge badge-muted">
                    Notif. {{ receitaAberta.tipoNotificacao }}
                </span>
                <span v-if="receitaAberta.requerRetencao" class="badge badge-warning">RETER</span>
            </div>
        </div>

        <p v-if="erro" class="msg-erro">{{ erro }}</p>

        <!-- Banner condicional por status de assinatura -->
        <div
            v-if="ASSINATURA_DIGITAL_HABILITADA && (assinaturaStore.statusPorReceita[String(receitaAberta.id)] ?? receitaAberta.assinaturaDigitalStatus) === 'AssinadaIcp'"
            class="aviso-assinatura aviso-assinatura--ok"
            role="note"
        >
            <i class="fa-solid fa-circle-check"></i>
            <div>
                <b>Receita assinada digitalmente (ICP-Brasil).</b>
                <span>Esta receita possui assinatura digital com validade jurídica plena.</span>
            </div>
        </div>
        <div
            v-else-if="ASSINATURA_DIGITAL_HABILITADA && (assinaturaStore.statusPorReceita[String(receitaAberta.id)] ?? receitaAberta.assinaturaDigitalStatus) !== 'AssinaturaPendente'"
            class="aviso-assinatura"
            role="note"
        >
            <i class="fa-solid fa-circle-exclamation"></i>
            <div>
                <b>Receita não assinada digitalmente.</b>
                <span>
                    Esta receita inclui apenas a identificação do profissional. Para validade jurídica plena
                    em farmácias que exigem assinatura digital (ICP-Brasil / Memed), imprima e assine
                    manualmente.
                </span>
            </div>
        </div>

        <!-- Medicamentos -->
        <div class="secao">
            <div class="secao-header">
                <h4 class="secao-titulo">Medicamentos</h4>
                <AppButton
                    v-if="receitaAberta.status === 'Rascunho' && !mostrandoFormItem"
                    size="sm" icon="fa-solid fa-plus"
                    :loading="salvandoAcao"
                    @click="abrirFormNovoItem"
                >
                    Adicionar medicamento
                </AppButton>
            </div>

            <!-- Form de adicionar/editar item -->
            <div v-if="mostrandoFormItem" class="form-item-card">
                <h5 class="form-item-titulo">
                    {{ editandoItemId ? "Editar medicamento" : "Novo medicamento" }}
                </h5>
                <div class="grid-item">
                    <div class="campo campo-span-2">
                        <label class="field-label">Medicamento *</label>
                        <AppInput v-model="form.medicamento" placeholder="Ex: Dipirona sódica" />
                    </div>
                    <div class="campo">
                        <label class="field-label">Concentração</label>
                        <AppInput v-model="form.concentracao" placeholder="Ex: 500mg" />
                    </div>
                    <div class="campo">
                        <label class="field-label">Forma farmacêutica</label>
                        <AppSelect v-model="form.formaFarmaceutica">
                            <option value="">Selecione...</option>
                            <option v-for="f in FORMAS_FARMACEUTICAS" :key="f" :value="f">{{ f }}</option>
                        </AppSelect>
                    </div>
                    <div class="campo">
                        <label class="field-label">Quantidade</label>
                        <AppInput v-model="form.quantidade" placeholder="Ex: 1 caixa" />
                    </div>
                    <div class="campo">
                        <label class="field-label">Via de administração</label>
                        <AppSelect v-model="form.via">
                            <option value="">Selecione...</option>
                            <option v-for="v in VIAS_ADMINISTRACAO" :key="v.valor" :value="v.valor">{{ v.label }}</option>
                        </AppSelect>
                    </div>
                </div>
                <div class="campo">
                    <label class="field-label">Posologia *</label>
                    <AppTextarea
                        v-model="form.posologia"
                        :rows="2"
                        placeholder="Ex: Tomar 1 comprimido de 8 em 8 horas"
                    />
                </div>
                <div class="grid-item">
                    <div class="campo">
                        <label class="field-label">Duração do tratamento</label>
                        <AppInput v-model="form.duracao" placeholder="Ex: 7 dias" />
                    </div>
                    <div class="campo campo-span-2">
                        <label class="field-label">Instruções adicionais</label>
                        <AppInput v-model="form.observacao" placeholder="Ex: Tomar em jejum, evitar sol..." />
                    </div>
                </div>
                <div class="form-footer">
                    <AppButton variant="ghost" size="sm" @click="mostrandoFormItem = false; editandoItemId = null">
                        Cancelar
                    </AppButton>
                    <AppButton
                        size="sm"
                        :loading="salvandoAcao"
                        :disabled="!form.medicamento.trim() || !form.posologia.trim()"
                        @click="salvarItem"
                    >
                        {{ editandoItemId ? "Salvar alterações" : "Adicionar medicamento" }}
                    </AppButton>
                </div>
            </div>

            <!-- Lista de itens -->
            <div v-if="receitaAberta.itens.length === 0 && !mostrandoFormItem" class="sem-itens">
                Nenhum medicamento adicionado ainda.
            </div>
            <ol v-else-if="!mostrandoFormItem" class="itens-lista">
                <li v-for="(it, i) in receitaAberta.itens" :key="it.id ?? i" class="item-card">
                    <div class="item-conteudo">
                        <div class="item-titulo">
                            <strong>{{ i + 1 }}. {{ it.medicamento }}</strong>
                            <span v-if="it.concentracao" class="item-conc">{{ it.concentracao }}</span>
                        </div>
                        <div v-if="it.formaFarmaceutica" class="item-linha">
                            {{ it.formaFarmaceutica }}<span v-if="it.quantidade"> · {{ it.quantidade }}</span>
                        </div>
                        <div class="item-linha item-posologia">{{ it.posologia }}</div>
                        <div v-if="it.via" class="item-linha"><strong>Via:</strong> {{ viaLabel(it.via) }}</div>
                        <div v-if="it.duracao" class="item-linha"><strong>Duração:</strong> {{ it.duracao }}</div>
                        <div v-if="it.observacao" class="item-instrucoes">💡 {{ it.observacao }}</div>
                    </div>
                    <div v-if="receitaAberta.status === 'Rascunho'" class="item-acoes">
                        <button class="btn-icon" title="Editar" @click="abrirFormEdicaoItem(it)">
                            <i class="fa-solid fa-pen"></i>
                        </button>
                        <button class="btn-icon btn-icon-danger" title="Remover" @click="removerItem(it)">
                            <i class="fa-solid fa-trash"></i>
                        </button>
                    </div>
                </li>
            </ol>
        </div>

        <!-- Observações -->
        <div class="secao">
            <h4 class="secao-titulo">Observações</h4>
            <AppTextarea
                :model-value="formObs"
                :rows="3"
                placeholder="Orientações gerais para o paciente (dieta, atividade física...)"
                :disabled="receitaAberta.status !== 'Rascunho'"
                @update:model-value="(v) => onObsChange(String(v))"
            />
            <p v-if="receitaAberta.status === 'Cancelada' && receitaAberta.motivoCancelamento" class="motivo-cancelamento">
                <strong>Motivo do cancelamento:</strong> {{ receitaAberta.motivoCancelamento }}
            </p>
        </div>

        <!-- Ações -->
        <div class="acoes-footer">
            <template v-if="receitaAberta.status === 'Rascunho'">
                <AppButton variant="ghost" :loading="salvandoAcao" @click="cancelar">Cancelar receita</AppButton>
                <AppButton
                    icon="fa-solid fa-check"
                    :loading="salvandoAcao"
                    :disabled="receitaAberta.itens.length === 0"
                    @click="finalizar"
                >
                    Finalizar receita
                </AppButton>
            </template>
            <template v-else-if="receitaAberta.status === 'Emitida'">
                <AppButton variant="ghost" :loading="salvandoAcao" @click="cancelar">Cancelar receita</AppButton>
                <AppButton variant="secondary" icon="fa-solid fa-copy" :loading="salvandoAcao" @click="duplicar">
                    Nova versão
                </AppButton>
                <AppButton icon="fa-solid fa-print" @click="imprimir">
                    Imprimir
                </AppButton>

                <!-- Ações de assinatura digital — CA-04/CA-05/CA-06/CA-13 -->
                <template v-if="ASSINATURA_DIGITAL_HABILITADA && ehPrescritorDaReceitaAberta()">
                    <!-- Polling ativo: exibe indicador -->
                    <AssinaturaPollingIndicator
                        v-if="pollingAtivo === receitaAberta.id"
                        :receita-id="receitaAberta.id"
                        @cancelar="cancelarPolling"
                    />
                    <!-- Timeout: mensagem orientativa (CA-20) -->
                    <div v-else-if="timeoutPolling" class="aviso-timeout">
                        <i class="fa-solid fa-clock-rotate-left"></i>
                        Não recebemos confirmação do BirdID. Verifique o app e clique em "Verificar status" para atualizar.
                        <AppButton size="sm" variant="secondary" @click="dispararAssinatura(receitaAberta.id)">
                            Verificar status
                        </AppButton>
                    </div>
                    <!-- Sem certificado: onboarding (CA-19) -->
                    <AppButton
                        v-else-if="!temCertificado && receitaAberta.assinaturaDigitalStatus === 'NaoAssinada'"
                        size="sm"
                        variant="secondary"
                        icon="fa-solid fa-certificate"
                        @click="modalOnboarding = true"
                    >
                        Cadastrar certificado
                    </AppButton>
                    <!-- Com certificado + NaoAssinada / FalhaAssinatura / AssinaturaExpirada -->
                    <AppButton
                        v-else-if="
                            temCertificado &&
                            (receitaAberta.assinaturaDigitalStatus === 'NaoAssinada' ||
                             receitaAberta.assinaturaDigitalStatus === 'FalhaAssinatura' ||
                             receitaAberta.assinaturaDigitalStatus === 'AssinaturaExpirada')
                        "
                        size="sm"
                        icon="fa-solid fa-signature"
                        :loading="salvandoAcao"
                        @click="dispararAssinatura(receitaAberta.id)"
                    >
                        {{
                            receitaAberta.assinaturaDigitalStatus === 'NaoAssinada'
                                ? 'Assinar digitalmente'
                                : 'Reenviar assinatura'
                        }}
                    </AppButton>
                    <!-- AssinadaIcp: download do PDF assinado (CA-06/CA-16) -->
                    <AppButton
                        v-else-if="receitaAberta.assinaturaDigitalStatus === 'AssinadaIcp'"
                        size="sm"
                        variant="secondary"
                        icon="fa-solid fa-file-arrow-down"
                        @click="baixarPdfAssinado(receitaAberta.id)"
                    >
                        Baixar PDF assinado
                    </AppButton>
                </template>
            </template>
            <template v-else>
                <p class="aviso-cancelada">
                    {{ receitaAberta.status === "Cancelada" ? "Esta receita foi cancelada." : "Esta receita foi substituída." }}
                </p>
            </template>
        </div>
    </div>

    <CancelarReceitaModal
        :aberto="modalCancelar"
        :receita-id="receitaAberta?.id ?? null"
        @fechar="modalCancelar = false"
        @confirmar="onCancelarConfirmado"
    />

    <!-- Modal de vinculação de certificado digital -->
    <AssinaturaOnboardingModal
        :aberto="modalOnboarding"
        @fechar="modalOnboarding = false"
        @vinculado="notificar('Certificado vinculado com sucesso!', 'success')"
    />

    <AppConfirmDialog
        :aberto="confirmar !== null"
        :titulo="confirmar?.titulo ?? ''"
        :mensagem="confirmar?.mensagem ?? ''"
        :confirmar-rotulo="confirmar?.rotulo ?? 'Confirmar'"
        :variante="confirmar?.variante ?? 'primary'"
        @confirmar="confirmarAcao"
        @cancelar="confirmar = null"
    />

    <AppToast v-if="toast" :mensagem="toast.msg" :variante="toast.variante" @fechar="toast = null" />
</template>

<style scoped>
/* ── Lista ────────────────────────────────────────────────── */
.receitas-lista { display: flex; flex-direction: column; gap: 1rem; }
.receitas-header {
    display: flex; justify-content: space-between; align-items: center;
    gap: 1rem; flex-wrap: wrap;
}
.titulo { font-size: 1rem; font-weight: 700; margin: 0; }
.header-acoes { display: flex; gap: 0.5rem; }

.estado-info { padding: 1rem; text-align: center; color: hsl(var(--muted-foreground)); font-size: 0.9em; }

.estado-vazio {
    text-align: center; padding: 3rem 1rem;
    background: hsl(var(--card)); border: 1px dashed hsl(var(--border));
    border-radius: 0.5rem;
    display: flex; flex-direction: column; align-items: center; gap: 0.75rem;
}
.icone-vazio { font-size: 2.5rem; color: hsl(var(--muted-foreground)); opacity: 0.5; }
.estado-vazio p { color: hsl(var(--muted-foreground)); margin: 0; font-size: 0.9em; }

.receitas { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: 0.5rem; }
.receita-card {
    display: flex; align-items: center; gap: 1rem;
    padding: 0.9rem 1.1rem;
    background: hsl(var(--card));
    border: 1px solid hsl(var(--border));
    border-radius: 0.5rem;
    cursor: pointer; transition: all 0.12s;
}
.receita-card:hover {
    border-color: hsl(var(--primary));
    background: hsl(var(--accent));
}
.receita-card.finalizada { background: hsl(var(--success) / 0.05); }
.receita-principal { flex: 1; display: flex; flex-direction: column; gap: 0.3rem; min-width: 0; }
.receita-header-linha { display: flex; gap: 0.35rem; flex-wrap: wrap; }
.receita-data { font-size: 0.85em; color: hsl(var(--foreground)); font-weight: 600; }
.receita-meta { font-size: 0.78em; color: hsl(var(--muted-foreground)); }
.seta { color: hsl(var(--muted-foreground)); flex-shrink: 0; }

/* ── Editor ───────────────────────────────────────────────── */
.editor { display: flex; flex-direction: column; gap: 1rem; }

.editor-header {
    display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap;
    padding-bottom: 0.5rem; border-bottom: 1px solid hsl(var(--border));
}
.header-badges { display: flex; gap: 0.35rem; flex-wrap: wrap; flex: 1; }

.msg-erro {
    color: hsl(var(--destructive));
    background: hsl(var(--destructive) / 0.08);
    padding: 0.5rem 0.75rem; border-radius: 0.375rem;
    font-size: 0.85em; margin: 0;
}

.aviso-assinatura {
    display: flex;
    gap: 0.75rem;
    align-items: flex-start;
    background: hsl(45 95% 95%);
    border: 1px solid hsl(45 85% 70%);
    color: hsl(30 70% 25%);
    padding: 0.75rem 1rem;
    border-radius: 8px;
    font-size: 0.875rem;
    line-height: 1.5;
}
.aviso-assinatura > i { margin-top: 2px; color: hsl(38 90% 45%); }
.aviso-assinatura b { display: block; margin-bottom: 2px; }
.aviso-assinatura--ok {
    background: hsl(var(--success) / 0.1);
    border-color: hsl(var(--success) / 0.4);
    color: hsl(var(--success));
}
.aviso-assinatura--ok > i { color: hsl(var(--success)); }

.secao {
    background: hsl(var(--card));
    border: 1px solid hsl(var(--border));
    border-radius: 0.5rem;
    padding: 1rem 1.25rem;
    display: flex; flex-direction: column; gap: 0.75rem;
}
.secao-header {
    display: flex; justify-content: space-between; align-items: center;
    flex-wrap: wrap; gap: 0.5rem;
}
.secao-titulo { font-size: 0.95em; font-weight: 700; margin: 0; }

.sem-itens {
    text-align: center; padding: 1.5rem; color: hsl(var(--muted-foreground));
    font-size: 0.9em; background: hsl(var(--muted) / 0.4); border-radius: 0.375rem;
}

.itens-lista { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: 0.6rem; }
.item-card {
    display: flex; gap: 1rem; padding: 0.75rem 1rem;
    background: hsl(var(--accent) / 0.4);
    border: 1px solid hsl(var(--border)); border-radius: 0.5rem;
}
.item-conteudo { flex: 1; display: flex; flex-direction: column; gap: 0.2rem; min-width: 0; }
.item-titulo { font-size: 0.95em; display: flex; align-items: baseline; gap: 0.4rem; flex-wrap: wrap; }
.item-conc { font-size: 0.85em; color: hsl(var(--primary)); font-weight: 600; }
.item-linha { font-size: 0.85em; color: hsl(var(--foreground) / 0.85); }
.item-posologia { font-style: italic; color: hsl(var(--primary-dark)); font-weight: 500; }
.item-instrucoes {
    margin-top: 0.3rem; font-size: 0.82em; color: hsl(var(--muted-foreground));
    background: hsl(var(--warning) / 0.12); padding: 0.35rem 0.55rem; border-radius: 0.375rem;
}
.item-acoes { display: flex; gap: 0.25rem; flex-shrink: 0; }
.btn-icon {
    width: 30px; height: 30px; border-radius: 6px;
    border: none; background: transparent; cursor: pointer;
    display: flex; align-items: center; justify-content: center;
    color: hsl(var(--muted-foreground)); transition: all 0.12s;
}
.btn-icon:hover { background: hsl(var(--muted)); color: hsl(var(--foreground)); }
.btn-icon-danger:hover { background: hsl(var(--destructive) / 0.1); color: hsl(var(--destructive)); }

/* ── Form de item ─────────────────────────────────────────── */
.form-item-card {
    background: hsl(var(--accent) / 0.5);
    border: 1px solid hsl(var(--primary) / 0.25); border-radius: 0.5rem;
    padding: 1rem; display: flex; flex-direction: column; gap: 0.6rem;
}
.form-item-titulo { font-size: 0.9em; font-weight: 700; margin: 0 0 0.25rem; color: hsl(var(--primary-dark)); }
.grid-item {
    display: grid; gap: 0.6rem;
    grid-template-columns: repeat(4, 1fr);
}
.campo { display: flex; flex-direction: column; gap: 0.2rem; }
.campo-span-2 { grid-column: span 2; }
.form-footer {
    display: flex; justify-content: flex-end; gap: 0.5rem;
    padding-top: 0.5rem; border-top: 1px solid hsl(var(--border));
}

.motivo-cancelamento {
    font-size: 0.85em; color: hsl(var(--muted-foreground));
    margin: 0.5rem 0 0;
}

.acoes-footer {
    display: flex; justify-content: flex-end; gap: 0.5rem;
    padding: 0.75rem 0; flex-wrap: wrap;
}
.aviso-cancelada {
    color: hsl(var(--muted-foreground)); font-style: italic; margin: 0;
}

.aviso-timeout {
    display: flex; align-items: center; gap: 0.5rem; flex-wrap: wrap;
    font-size: 0.85rem; color: hsl(var(--foreground));
    background: hsl(var(--warning) / 0.15);
    border: 1px solid hsl(var(--warning) / 0.4);
    border-radius: 0.5rem; padding: 0.5rem 0.75rem;
}

@media (max-width: 768px) {
    .grid-item { grid-template-columns: 1fr 1fr; }
    .campo-span-2 { grid-column: span 2; }
}
</style>
