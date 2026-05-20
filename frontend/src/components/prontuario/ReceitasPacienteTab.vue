<!--
  Aba de Receitas dentro do prontuário — replica o fluxo do legado
  (ReceitasTabSection.vue → ReceitaEditor.vue → ReceitaItemForm.vue).
  Enquanto não há endpoint no backend, persistimos em localStorage via
  receitaLocalService. Quando o backend criar o módulo, basta trocar o service.
-->
<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from "vue"
import {
    receitaLocalService,
    FORMAS_FARMACEUTICAS,
    VIAS_ADMINISTRACAO,
    type Receita,
    type ReceitaItem,
    type TipoReceita,
} from "@/services/receitaLocalService"
import { useAuthStore } from "@/stores/authStore"
import { useTenantStore } from "@/stores/tenantStore"
import { AppButton, AppInput, AppPagination, AppSelect, AppTextarea } from "@/components/ui"

const props = defineProps<{
    pacienteId: number
    pacienteNome: string
}>()

const auth   = useAuthStore()
const tenant = useTenantStore()

const receitas = ref<Receita[]>([])
const abertaId = ref<string | null>(null)   // null = lista; preenchido = editando
const erro     = ref<string | null>(null)

const receitaAberta = computed<Receita | null>(() =>
    abertaId.value ? receitas.value.find(r => r.id === abertaId.value) ?? null : null,
)

// ─── Paginação client-side da lista de receitas ─────────────────────────────
const pagina  = ref(1)
const tamanho = ref(10)
watch(() => receitas.value.length, () => { pagina.value = 1 })
const receitasPagina = computed(() => {
    const inicio = (pagina.value - 1) * tamanho.value
    return receitas.value.slice(inicio, inicio + tamanho.value)
})

function recarregar() {
    if (!tenant.ativo) return
    receitas.value = receitaLocalService.listarDoPaciente(tenant.ativo.id, props.pacienteId)
}

onMounted(recarregar)

function novaReceita(tipo: TipoReceita = "SIMPLES") {
    if (!tenant.ativo || !auth.usuario) return
    const r = receitaLocalService.criar({
        estabelecimentoId: tenant.ativo.id,
        pacienteId: props.pacienteId,
        autor: auth.usuario.nomeCompleto ?? auth.usuario.email,
        tipo,
    })
    recarregar()
    abertaId.value = r.id
}

function abrirReceita(r: Receita) { abertaId.value = r.id }
function fecharEditor()           { abertaId.value = null; recarregar() }

function formatarData(iso: string) {
    return new Date(iso).toLocaleDateString("pt-BR", {
        day: "2-digit", month: "2-digit", year: "numeric",
        hour: "2-digit", minute: "2-digit",
    })
}

// ─── Editor: form de item ────────────────────────────────────────────────────
const editandoItemId = ref<string | null>(null)
const form = reactive<Omit<ReceitaItem, "id">>({
    medicamento: "", concentracao: "", formaFarmaceutica: "",
    quantidade: "", viaAdministracao: "", posologia: "",
    duracao: "", instrucoes: "",
})
const mostrandoFormItem = ref(false)

function limparForm() {
    form.medicamento = ""; form.concentracao = ""
    form.formaFarmaceutica = ""; form.quantidade = ""
    form.viaAdministracao = ""; form.posologia = ""
    form.duracao = ""; form.instrucoes = ""
}

function abrirFormNovoItem() {
    limparForm(); editandoItemId.value = null; mostrandoFormItem.value = true
}
function abrirFormEdicaoItem(item: ReceitaItem) {
    editandoItemId.value = item.id
    Object.assign(form, item)
    mostrandoFormItem.value = true
}
function salvarItem() {
    if (!receitaAberta.value) return
    if (!form.medicamento.trim() || !form.posologia.trim()) {
        erro.value = "Informe ao menos o medicamento e a posologia."; return
    }
    erro.value = null
    if (editandoItemId.value) {
        receitaLocalService.atualizarItem(receitaAberta.value.id, editandoItemId.value, { ...form })
    } else {
        receitaLocalService.adicionarItem(receitaAberta.value.id, { ...form })
    }
    recarregar()
    mostrandoFormItem.value = false
    editandoItemId.value = null
    limparForm()
}
function removerItem(item: ReceitaItem) {
    if (!receitaAberta.value) return
    if (!confirm(`Remover medicamento "${item.medicamento}"?`)) return
    receitaLocalService.removerItem(receitaAberta.value.id, item.id)
    recarregar()
}

// ─── Ações na receita ────────────────────────────────────────────────────────
function atualizarObs(v: string) {
    if (!receitaAberta.value) return
    receitaLocalService.atualizar(receitaAberta.value.id, { observacoes: v })
    recarregar()
}
function togglePDF(v: boolean) {
    if (!receitaAberta.value) return
    receitaLocalService.atualizar(receitaAberta.value.id, { incluirDataNoPdf: v })
    recarregar()
}
function finalizar() {
    if (!receitaAberta.value) return
    if (receitaAberta.value.itens.length === 0) {
        erro.value = "Adicione ao menos um medicamento antes de finalizar."; return
    }
    if (!confirm("Finalizar receita? Após finalizada só pode ser cancelada ou gerar nova versão.")) return
    receitaLocalService.finalizar(receitaAberta.value.id)
    recarregar()
}
function cancelar() {
    if (!receitaAberta.value) return
    if (!confirm("Cancelar esta receita?")) return
    receitaLocalService.cancelar(receitaAberta.value.id)
    recarregar()
}
function excluir() {
    if (!receitaAberta.value) return
    if (!confirm("Excluir receita em rascunho? Esta ação é irreversível.")) return
    receitaLocalService.excluir(receitaAberta.value.id)
    abertaId.value = null
    recarregar()
}
function criarNovaVersao() {
    if (!receitaAberta.value) return
    const nova = receitaLocalService.novaVersao(receitaAberta.value.id)
    recarregar()
    if (nova) abertaId.value = nova.id
}

// ─── Impressão / PDF ─────────────────────────────────────────────────────────
async function imprimir() {
    const r = receitaAberta.value
    if (!r) return
    // Abrimos uma janela com conteúdo formatado e chamamos print()
    const w = window.open("", "_blank", "width=800,height=900")
    if (!w) return
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
            ${it.viaAdministracao ? `<div>Via: ${escape(it.viaAdministracao)}</div>` : ""}
            ${it.duracao ? `<div>Duração: ${escape(it.duracao)}</div>` : ""}
            ${it.instrucoes ? `<div class="obs">${escape(it.instrucoes)}</div>` : ""}
        </div>
    `).join("")
    const dataRef = r.incluirDataNoPdf ? formatarData(r.criadaEm) : ""
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
        .tipo{display:inline-block;padding:.15rem .5rem;border-radius:4px;font-size:.75rem;font-weight:700}
        .tipo.SIMPLES{background:#dbeafe;color:#1e40af}
        .tipo.CONTROLADA{background:#fee2e2;color:#991b1b}
      </style></head><body>
      <h1>Receita médica <span class="tipo ${r.tipo}">${r.tipo}</span></h1>
      <div class="meta">
        <strong>Paciente:</strong> ${escape(props.pacienteNome)}<br>
        <strong>Profissional:</strong> ${escape(r.autor)}<br>
        ${dataRef ? `<strong>Data:</strong> ${dataRef}<br>` : ""}
        <strong>Versão:</strong> ${r.versao}
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

// Status helpers
function statusLabel(s: string) {
    return s === "DRAFT" ? "Rascunho" : s === "FINALIZED" ? "Finalizada" : "Cancelada"
}
function statusBadgeClass(s: string) {
    return s === "DRAFT" ? "badge-warning" : s === "FINALIZED" ? "badge-success" : "badge-muted"
}
</script>

<template>
    <!-- ══════════════════ LISTA DE RECEITAS ══════════════════ -->
    <div v-if="!receitaAberta" class="receitas-lista">
        <div class="receitas-header">
            <h3 class="titulo">Receitas do paciente</h3>
            <div class="header-acoes">
                <AppButton variant="secondary" size="sm" icon="fa-solid fa-shield-halved" @click="novaReceita('CONTROLADA')">
                    Nova controlada
                </AppButton>
                <AppButton size="sm" icon="fa-solid fa-plus" @click="novaReceita('SIMPLES')">
                    Nova receita
                </AppButton>
            </div>
        </div>

        <div v-if="receitas.length === 0" class="estado-vazio">
            <i class="fa-solid fa-prescription icone-vazio"></i>
            <p>Nenhuma receita emitida para este paciente.</p>
            <AppButton size="sm" @click="novaReceita('SIMPLES')">Criar primeira receita</AppButton>
        </div>

        <ul v-else class="receitas">
            <li
                v-for="r in receitasPagina" :key="r.id"
                class="receita-card" :class="{ finalizada: r.status === 'FINALIZED' }"
                @click="abrirReceita(r)"
            >
                <div class="receita-principal">
                    <div class="receita-header-linha">
                        <span :class="['badge', r.tipo === 'CONTROLADA' ? 'badge-error' : 'badge-info']">
                            {{ r.tipo }}
                        </span>
                        <span :class="['badge', statusBadgeClass(r.status)]">
                            {{ statusLabel(r.status) }}
                        </span>
                        <span v-if="r.versao > 1" class="badge badge-muted">v{{ r.versao }}</span>
                    </div>
                    <div class="receita-data">{{ formatarData(r.criadaEm) }}</div>
                    <div class="receita-meta">
                        {{ r.itens.length }} {{ r.itens.length === 1 ? "medicamento" : "medicamentos" }}
                        · por {{ r.autor }}
                    </div>
                </div>
                <i class="fa-solid fa-chevron-right seta"></i>
            </li>
        </ul>

        <AppPagination
            v-if="receitas.length > 0"
            v-model:pagina="pagina"
            v-model:tamanho="tamanho"
            :total="receitas.length"
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
                <span :class="['badge', receitaAberta.tipo === 'CONTROLADA' ? 'badge-error' : 'badge-info']">
                    {{ receitaAberta.tipo }}
                </span>
                <span :class="['badge', statusBadgeClass(receitaAberta.status)]">
                    {{ statusLabel(receitaAberta.status) }}
                </span>
                <span class="badge badge-muted">v{{ receitaAberta.versao }}</span>
            </div>
            <AppButton
                v-if="receitaAberta.status === 'DRAFT'"
                variant="danger" size="sm" icon="fa-solid fa-trash" @click="excluir"
            >
                Excluir
            </AppButton>
        </div>

        <p v-if="erro" class="msg-erro">{{ erro }}</p>

        <!--
          Aviso obrigatório: o sistema ainda NÃO assina a receita digitalmente
          (sem integração ICP-Brasil / Memed). Para uso em farmácias que exigem
          assinatura digital, imprima e assine manualmente. CFM 2.299/2021.
        -->
        <div class="aviso-assinatura" role="note">
            <i class="fa-solid fa-circle-exclamation"></i>
            <div>
                <b>Receita não assinada digitalmente.</b>
                <span>
                    Esta receita inclui apenas a identificação do profissional. Para validade jurídica plena
                    em farmácias que exigem assinatura digital (ICP-Brasil / Memed), imprima e assine
                    manualmente. A integração com provedores de assinatura está em desenvolvimento.
                </span>
            </div>
        </div>

        <!-- Medicamentos -->
        <div class="secao">
            <div class="secao-header">
                <h4 class="secao-titulo">Medicamentos</h4>
                <AppButton
                    v-if="receitaAberta.status === 'DRAFT' && !mostrandoFormItem"
                    size="sm" icon="fa-solid fa-plus" @click="abrirFormNovoItem"
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
                        <AppSelect v-model="form.viaAdministracao">
                            <option value="">Selecione...</option>
                            <option v-for="v in VIAS_ADMINISTRACAO" :key="v" :value="v">{{ v }}</option>
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
                        <AppInput v-model="form.instrucoes" placeholder="Ex: Tomar em jejum, evitar sol..." />
                    </div>
                </div>
                <div class="form-footer">
                    <AppButton variant="ghost" size="sm" @click="mostrandoFormItem = false; editandoItemId = null">
                        Cancelar
                    </AppButton>
                    <AppButton
                        size="sm"
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
                <li v-for="(it, i) in receitaAberta.itens" :key="it.id" class="item-card">
                    <div class="item-conteudo">
                        <div class="item-titulo">
                            <strong>{{ i + 1 }}. {{ it.medicamento }}</strong>
                            <span v-if="it.concentracao" class="item-conc">{{ it.concentracao }}</span>
                        </div>
                        <div v-if="it.formaFarmaceutica" class="item-linha">
                            {{ it.formaFarmaceutica }}<span v-if="it.quantidade"> · {{ it.quantidade }}</span>
                        </div>
                        <div class="item-linha item-posologia">{{ it.posologia }}</div>
                        <div v-if="it.viaAdministracao" class="item-linha"><strong>Via:</strong> {{ it.viaAdministracao }}</div>
                        <div v-if="it.duracao" class="item-linha"><strong>Duração:</strong> {{ it.duracao }}</div>
                        <div v-if="it.instrucoes" class="item-instrucoes">💡 {{ it.instrucoes }}</div>
                    </div>
                    <div v-if="receitaAberta.status === 'DRAFT'" class="item-acoes">
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
                :model-value="receitaAberta.observacoes"
                :rows="3"
                placeholder="Orientações gerais para o paciente (dieta, atividade física...)"
                :disabled="receitaAberta.status !== 'DRAFT'"
                @update:model-value="(v) => atualizarObs(String(v))"
            />

            <label class="check-inline">
                <input
                    type="checkbox" :checked="receitaAberta.incluirDataNoPdf"
                    :disabled="receitaAberta.status !== 'DRAFT'"
                    @change="(e) => togglePDF((e.target as HTMLInputElement).checked)"
                />
                Incluir data no PDF da receita
            </label>
        </div>

        <!-- Ações -->
        <div class="acoes-footer">
            <template v-if="receitaAberta.status === 'DRAFT'">
                <AppButton variant="ghost" @click="cancelar">Cancelar receita</AppButton>
                <AppButton
                    icon="fa-solid fa-check"
                    :disabled="receitaAberta.itens.length === 0"
                    @click="finalizar"
                >
                    Finalizar receita
                </AppButton>
            </template>
            <template v-else-if="receitaAberta.status === 'FINALIZED'">
                <AppButton variant="secondary" icon="fa-solid fa-copy" @click="criarNovaVersao">
                    Nova versão
                </AppButton>
                <AppButton icon="fa-solid fa-print" @click="imprimir">
                    Imprimir / Baixar PDF
                </AppButton>
            </template>
            <template v-else>
                <p class="aviso-cancelada">Esta receita foi cancelada.</p>
            </template>
        </div>
    </div>
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

/* Aviso fixo (LGPD/CFM): receita sem assinatura digital. */
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

.check-inline {
    display: inline-flex; align-items: center; gap: 0.5rem;
    font-size: 0.85em; color: hsl(var(--muted-foreground)); cursor: pointer;
}
.check-inline input { cursor: pointer; }

.acoes-footer {
    display: flex; justify-content: flex-end; gap: 0.5rem;
    padding: 0.75rem 0; flex-wrap: wrap;
}
.aviso-cancelada {
    color: hsl(var(--muted-foreground)); font-style: italic; margin: 0;
}

@media (max-width: 768px) {
    .grid-item { grid-template-columns: 1fr 1fr; }
    .campo-span-2 { grid-column: span 2; }
}
</style>
