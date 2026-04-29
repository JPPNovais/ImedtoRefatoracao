<script setup lang="ts">
/**
 * ReceitaDrawer — drawer de 2 etapas para emissão de receitas via backend.
 * Etapa 1: escolher tipo (Comum / Controlada / Antibiotico / Especial).
 * Etapa 2: adicionar itens (medicamentos) + observações.
 *
 * Integra com o backend (/api/receitas) quando disponível.
 * Fallback: usa receitaLocalService para compatibilidade durante migração.
 */
import { ref, reactive, computed } from "vue"
import {
    receitaService,
    FORMAS_FARMACEUTICAS,
    VIAS_ADMINISTRACAO,
    TIPOS_RECEITA,
    type TipoReceita,
    type ReceitaItem,
} from "@/services/receitaService"
import {
    AppDrawer, AppButton, AppField, AppInput, AppSelect,
    AppTextarea, AppPillToggle, AppModal,
} from "@/components/ui"

const props = defineProps<{
    aberto: boolean
    prontuarioId: number
    pacienteId: number
}>()

const emit = defineEmits<{
    fechar: []
    emitida: []
}>()

// ── Estado ──────────────────────────────────────────────────────────────────
type Etapa = 1 | 2

const etapa = ref<Etapa>(1)
const tipo = ref<TipoReceita>("Comum")
const observacoes = ref("")
const itens = ref<Array<Omit<ReceitaItem, "id" | "ordem">>>([])
const emitindo = ref(false)
const erroEmitir = ref<string | null>(null)

const mostrandoFormItem = ref(false)
const editandoIdx = ref<number | null>(null)
const erroItem = ref<string | null>(null)
const confirmandoCancelar = ref(false)

const formItem = reactive({
    medicamento: "",
    concentracao: "",
    formaFarmaceutica: "",
    quantidade: "",
    viaAdministracao: "",
    posologia: "",
    duracao: "",
    observacao: "",
})

function limparFormItem() {
    formItem.medicamento = ""
    formItem.concentracao = ""
    formItem.formaFarmaceutica = ""
    formItem.quantidade = ""
    formItem.viaAdministracao = ""
    formItem.posologia = ""
    formItem.duracao = ""
    formItem.observacao = ""
}

// ── Navegação entre etapas ──────────────────────────────────────────────────
function avancarEtapa() {
    etapa.value = 2
}

function voltarEtapa() {
    etapa.value = 1
}

// ── Gerenciar itens ──────────────────────────────────────────────────────────
function abrirFormNovoItem() {
    limparFormItem()
    editandoIdx.value = null
    erroItem.value = null
    mostrandoFormItem.value = true
}

function abrirFormEditarItem(idx: number) {
    const item = itens.value[idx]
    Object.assign(formItem, item)
    editandoIdx.value = idx
    erroItem.value = null
    mostrandoFormItem.value = true
}

function salvarItem() {
    if (!formItem.medicamento.trim() || !formItem.posologia.trim()) {
        erroItem.value = "Medicamento e posologia são obrigatórios."
        return
    }
    erroItem.value = null
    const itemSalvo = {
        medicamento: formItem.medicamento,
        concentracao: formItem.concentracao,
        formaFarmaceutica: formItem.formaFarmaceutica,
        quantidade: formItem.quantidade,
        viaAdministracao: formItem.viaAdministracao,
        posologia: formItem.posologia,
        duracao: formItem.duracao,
        observacao: formItem.observacao,
    }
    if (editandoIdx.value !== null) {
        itens.value[editandoIdx.value] = itemSalvo
    } else {
        itens.value.push(itemSalvo)
    }
    mostrandoFormItem.value = false
    editandoIdx.value = null
}

function removerItem(idx: number) {
    itens.value.splice(idx, 1)
}

function cancelarFormItem() {
    mostrandoFormItem.value = false
    editandoIdx.value = null
    erroItem.value = null
}

// ── Emissão / fechamento ─────────────────────────────────────────────────────
const podeEmitir = computed(() => itens.value.length > 0 && !emitindo.value)

async function emitir() {
    if (itens.value.length === 0) {
        erroEmitir.value = "Adicione ao menos um medicamento."
        return
    }
    emitindo.value = true
    erroEmitir.value = null
    try {
        await receitaService.emitir({
            prontuarioId: props.prontuarioId,
            pacienteId: props.pacienteId,
            tipo: tipo.value,
            observacoes: observacoes.value || undefined,
            itens: itens.value.map((it, i) => ({ ...it, ordem: i + 1 })),
        })
        emit("emitida")
        fechar()
    } catch (e: any) {
        erroEmitir.value = e?.response?.data?.mensagem ?? "Erro ao emitir receita."
    } finally {
        emitindo.value = false
    }
}

function fechar() {
    // Resetar estado ao fechar
    etapa.value = 1
    tipo.value = "Comum"
    observacoes.value = ""
    itens.value = []
    erroEmitir.value = null
    mostrandoFormItem.value = false
    emit("fechar")
}
</script>

<template>
    <AppDrawer
        :aberto="aberto"
        :largura="600"
        @fechar="fechar"
    >
        <template #titulo>
            <div class="drawer-titulo">
                <span>Nova receita</span>
                <span class="etapa-badge">Etapa {{ etapa }} de 2</span>
            </div>
        </template>

        <!-- ── ETAPA 1: Tipo ─────────────────────────────────────────────── -->
        <div v-if="etapa === 1" class="etapa-conteudo">
            <p class="etapa-desc">Selecione o tipo de receita para prosseguir.</p>

            <AppField label="Tipo de receita" required for="receita-tipo">
                <div class="tipos-grid">
                    <button
                        v-for="t in TIPOS_RECEITA"
                        :key="t.valor"
                        type="button"
                        class="tipo-btn"
                        :class="{ 'tipo-btn--ativo': tipo === t.valor }"
                        @click="tipo = t.valor"
                        :aria-pressed="tipo === t.valor"
                    >
                        <i
                            :class="{
                                'fa-solid fa-prescription': t.valor === 'Comum',
                                'fa-solid fa-shield-halved': t.valor === 'Controlada',
                                'fa-solid fa-virus': t.valor === 'Antibiotico',
                                'fa-solid fa-star': t.valor === 'Especial',
                            }"
                            aria-hidden="true"
                        ></i>
                        {{ t.label }}
                    </button>
                </div>
            </AppField>
        </div>

        <!-- ── ETAPA 2: Itens e observações ─────────────────────────────── -->
        <div v-else class="etapa-conteudo">
            <div class="tipo-selecionado">
                <span class="tipo-label">Tipo: </span>
                <strong>{{ tipo }}</strong>
                <button type="button" class="btn-trocar" @click="voltarEtapa">Trocar</button>
            </div>

            <!-- Mensagem de erro da emissão -->
            <div v-if="erroEmitir" class="erro-banner" role="alert">
                <i class="fa-solid fa-triangle-exclamation" aria-hidden="true"></i>
                {{ erroEmitir }}
            </div>

            <!-- Lista de itens -->
            <div class="secao">
                <div class="secao-header">
                    <h4 class="secao-titulo">Medicamentos</h4>
                    <AppButton
                        v-if="!mostrandoFormItem"
                        size="sm"
                        icon="fa-solid fa-plus"
                        @click="abrirFormNovoItem"
                    >
                        Adicionar
                    </AppButton>
                </div>

                <!-- Form de item -->
                <div v-if="mostrandoFormItem" class="form-item">
                    <p v-if="erroItem" class="erro-inline" role="alert">{{ erroItem }}</p>

                    <div class="grid-2">
                        <AppField label="Medicamento" required for="it-med" class="col-span-2">
                            <AppInput
                                id="it-med"
                                v-model="formItem.medicamento"
                                placeholder="Ex: Dipirona sodica"
                            />
                        </AppField>
                        <AppField label="Concentracao" for="it-conc">
                            <AppInput id="it-conc" v-model="formItem.concentracao" placeholder="Ex: 500mg" />
                        </AppField>
                        <AppField label="Forma farmaceutica" for="it-forma">
                            <AppSelect id="it-forma" v-model="formItem.formaFarmaceutica">
                                <option value="">Selecione...</option>
                                <option v-for="f in FORMAS_FARMACEUTICAS" :key="f" :value="f">{{ f }}</option>
                            </AppSelect>
                        </AppField>
                        <AppField label="Quantidade" for="it-qtd">
                            <AppInput id="it-qtd" v-model="formItem.quantidade" placeholder="Ex: 1 caixa" />
                        </AppField>
                        <AppField label="Via de administracao" for="it-via">
                            <AppSelect id="it-via" v-model="formItem.viaAdministracao">
                                <option value="">Selecione...</option>
                                <option v-for="v in VIAS_ADMINISTRACAO" :key="v" :value="v">{{ v }}</option>
                            </AppSelect>
                        </AppField>
                        <AppField label="Posologia" required for="it-pos" class="col-span-2">
                            <AppTextarea
                                id="it-pos"
                                v-model="formItem.posologia"
                                :rows="2"
                                placeholder="Ex: Tomar 1 comprimido de 8 em 8 horas"
                            />
                        </AppField>
                        <AppField label="Duracao do tratamento" for="it-dur">
                            <AppInput id="it-dur" v-model="formItem.duracao" placeholder="Ex: 7 dias" />
                        </AppField>
                        <AppField label="Observacao" for="it-obs">
                            <AppInput id="it-obs" v-model="formItem.observacao" placeholder="Instrucoes extras..." />
                        </AppField>
                    </div>

                    <div class="form-item-footer">
                        <AppButton variant="ghost" size="sm" @click="cancelarFormItem">Cancelar</AppButton>
                        <AppButton size="sm" @click="salvarItem">
                            {{ editandoIdx !== null ? "Salvar alteracoes" : "Adicionar medicamento" }}
                        </AppButton>
                    </div>
                </div>

                <!-- Lista de itens adicionados -->
                <div v-if="itens.length === 0 && !mostrandoFormItem" class="sem-itens">
                    Nenhum medicamento adicionado ainda.
                </div>

                <ol v-else-if="!mostrandoFormItem" class="itens-lista">
                    <li v-for="(it, idx) in itens" :key="idx" class="item-card">
                        <div class="item-info">
                            <div class="item-nome">
                                <strong>{{ idx + 1 }}. {{ it.medicamento }}</strong>
                                <span v-if="it.concentracao" class="item-conc">{{ it.concentracao }}</span>
                            </div>
                            <div v-if="it.formaFarmaceutica" class="item-detalhe">
                                {{ it.formaFarmaceutica }}<span v-if="it.quantidade"> — {{ it.quantidade }}</span>
                            </div>
                            <div class="item-posologia">{{ it.posologia }}</div>
                            <div v-if="it.viaAdministracao" class="item-detalhe">Via: {{ it.viaAdministracao }}</div>
                            <div v-if="it.duracao" class="item-detalhe">Duracao: {{ it.duracao }}</div>
                        </div>
                        <div class="item-acoes">
                            <button class="btn-icon btn-icon-editar" title="Editar" @click="abrirFormEditarItem(idx)">
                                <i class="fa-solid fa-pen" aria-hidden="true"></i>
                            </button>
                            <button class="btn-icon btn-icon-excluir" title="Remover" @click="removerItem(idx)">
                                <i class="fa-solid fa-trash" aria-hidden="true"></i>
                            </button>
                        </div>
                    </li>
                </ol>
            </div>

            <!-- Observações -->
            <AppField label="Observacoes gerais" for="receita-obs">
                <AppTextarea
                    id="receita-obs"
                    v-model="observacoes"
                    :rows="3"
                    placeholder="Instrucoes gerais para o paciente (dieta, atividade fisica...)"
                />
            </AppField>
        </div>

        <!-- ── Footer ─────────────────────────────────────────────────────── -->
        <template #rodape>
            <AppButton variant="secondary" @click="fechar">Cancelar</AppButton>

            <template v-if="etapa === 1">
                <AppButton icon="fa-solid fa-arrow-right" icon-right="fa-solid fa-arrow-right" @click="avancarEtapa">
                    Proximo
                </AppButton>
            </template>
            <template v-else>
                <AppButton variant="ghost" icon="fa-solid fa-arrow-left" @click="voltarEtapa">
                    Voltar
                </AppButton>
                <AppButton
                    icon="fa-solid fa-check"
                    :loading="emitindo"
                    :disabled="!podeEmitir"
                    @click="emitir"
                >
                    Emitir receita
                </AppButton>
            </template>
        </template>
    </AppDrawer>
</template>

<style scoped>
.drawer-titulo {
    display: flex;
    align-items: center;
    gap: 0.75rem;
}
.etapa-badge {
    font-size: 0.72em;
    background: hsl(var(--muted));
    color: hsl(var(--muted-foreground));
    padding: 0.15rem 0.5rem;
    border-radius: 999px;
    font-weight: 500;
}

.etapa-conteudo {
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
    padding: 0.25rem 0;
}

.etapa-desc {
    margin: 0;
    font-size: 0.9em;
    color: hsl(var(--muted-foreground));
}

.tipos-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 0.75rem;
}

.tipo-btn {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.85rem 1rem;
    border: 2px solid hsl(var(--border));
    border-radius: var(--radius);
    background: transparent;
    color: hsl(var(--foreground));
    font-size: 0.9em;
    cursor: pointer;
    transition: all 0.12s;
    font-family: inherit;
    font-weight: 500;
}
.tipo-btn:hover {
    border-color: hsl(var(--primary) / 0.5);
    background: hsl(var(--accent));
}
.tipo-btn--ativo {
    border-color: hsl(var(--primary));
    background: hsl(var(--accent));
    color: hsl(var(--primary-dark));
}

.tipo-selecionado {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.5rem 0.75rem;
    background: hsl(var(--accent));
    border-radius: var(--radius-sm);
    font-size: 0.85em;
}
.tipo-label { color: hsl(var(--muted-foreground)); }
.btn-trocar {
    margin-left: auto;
    font-size: 0.82em;
    color: hsl(var(--primary));
    background: transparent;
    border: none;
    cursor: pointer;
    font-family: inherit;
    text-decoration: underline;
}

.erro-banner {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.75rem 1rem;
    background: hsl(var(--destructive) / 0.08);
    border: 1px solid hsl(var(--destructive) / 0.2);
    border-radius: var(--radius-sm);
    color: hsl(var(--destructive));
    font-size: 0.85em;
}

.secao {
    background: hsl(var(--card));
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius);
    padding: 1rem 1.1rem;
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}
.secao-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
}
.secao-titulo { font-size: 0.9em; font-weight: 700; margin: 0; }

.form-item {
    border: 1px solid hsl(var(--primary) / 0.25);
    border-radius: var(--radius-sm);
    padding: 0.85rem;
    background: hsl(var(--accent) / 0.5);
    display: flex;
    flex-direction: column;
    gap: 0.65rem;
}
.grid-2 {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 0.65rem;
}
.col-span-2 { grid-column: span 2; }

.erro-inline {
    color: hsl(var(--destructive));
    font-size: 0.82em;
    margin: 0;
}

.form-item-footer {
    display: flex;
    justify-content: flex-end;
    gap: 0.5rem;
    padding-top: 0.5rem;
    border-top: 1px solid hsl(var(--border));
}

.sem-itens {
    padding: 1.25rem;
    text-align: center;
    font-size: 0.85em;
    color: hsl(var(--muted-foreground));
    background: hsl(var(--muted) / 0.4);
    border-radius: var(--radius-sm);
}

.itens-lista {
    list-style: none;
    padding: 0;
    margin: 0;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}
.item-card {
    display: flex;
    gap: 0.75rem;
    padding: 0.65rem 0.85rem;
    background: hsl(var(--accent) / 0.4);
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius-sm);
}
.item-info { flex: 1; display: flex; flex-direction: column; gap: 0.2rem; min-width: 0; }
.item-nome { display: flex; align-items: baseline; gap: 0.35rem; flex-wrap: wrap; font-size: 0.9em; }
.item-conc { font-size: 0.82em; color: hsl(var(--primary)); font-weight: 600; }
.item-posologia { font-size: 0.82em; font-style: italic; color: hsl(var(--primary-dark)); }
.item-detalhe { font-size: 0.78em; color: hsl(var(--muted-foreground)); }
.item-acoes { display: flex; gap: 0.2rem; flex-shrink: 0; align-items: flex-start; }

@media (max-width: 640px) {
    .tipos-grid { grid-template-columns: 1fr; }
    .grid-2 { grid-template-columns: 1fr; }
    .col-span-2 { grid-column: span 1; }
}
</style>
