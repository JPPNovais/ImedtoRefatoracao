<script setup lang="ts">
/**
 * Bloco de "Produtos vinculados" do drawer de procedimento (design ConfigOrcamento).
 */
import { ref, computed, onMounted, watch } from "vue"
import {
    orcamentoCatalogoService,
    type CatalogoProduto,
    type CatalogoCirurgiaProdutoVinculo,
} from "@/services/orcamentoCatalogoService"
import { formatarMoedaBrl } from "@/utils/format"
import { AppButton, AppSelect, AppInput, AppToast, AppConfirmDialog } from "@/components/ui"

const props = defineProps<{
    cirurgiaId: number
    valorBase: number
}>()

const vinculos = ref<CatalogoCirurgiaProdutoVinculo[]>([])
const produtos = ref<CatalogoProduto[]>([])
const novoProdutoId = ref<number | null>(null)
const novoQtd = ref(1)
const carregando = ref(false)

// Toast e confirmação (substituem window.alert/confirm).
const toast = ref<{ mensagem: string, variante: "info" | "success" | "error" } | null>(null)
function notificar(mensagem: string, variante: "info" | "success" | "error" = "success") {
    toast.value = { mensagem, variante }
}
const confirmacao = ref<{ aberto: boolean, alvo: CatalogoCirurgiaProdutoVinculo | null, executando: boolean }>({
    aberto: false, alvo: null, executando: false,
})

const produtosDisponiveis = computed(() => {
    const usados = new Set(vinculos.value.map(v => v.catalogoProdutoId))
    return produtos.value.filter(p => p.ativo && !usados.has(p.id))
})

const opcoesProduto = computed(() => [
    { value: "", label: "Selecione…" },
    ...produtosDisponiveis.value.map(p => ({
        value: String(p.id),
        label: `${p.nome}${p.valorReferencia ? ` — ${formatarMoedaBrl(p.valorReferencia)}` : ""}`,
    })),
])

const totalSugerido = computed(() => {
    const inclusos = vinculos.value.filter(v => v.incluido).reduce((s, v) => {
        const valor = v.produtoValorReferencia ?? 0
        return s + valor * v.quantidadePadrao
    }, 0)
    return props.valorBase + inclusos
})

async function carregar() {
    carregando.value = true
    try {
        const [v, p] = await Promise.all([
            orcamentoCatalogoService.listarProdutosDaCirurgia(props.cirurgiaId),
            orcamentoCatalogoService.listarProdutos(true),
        ])
        vinculos.value = v
        produtos.value = p
    } catch {
        // silenciar
    } finally {
        carregando.value = false
    }
}

onMounted(carregar)
watch(() => props.cirurgiaId, carregar)

async function vincular() {
    if (!novoProdutoId.value || novoQtd.value <= 0) return
    try {
        await orcamentoCatalogoService.vincularProdutoCirurgia(props.cirurgiaId, {
            produtoId: novoProdutoId.value,
            quantidadePadrao: novoQtd.value,
            obrigatorio: true,
            incluido: true,
        })
        novoProdutoId.value = null
        novoQtd.value = 1
        await carregar()
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Falha ao vincular produto.", "error")
    }
}

async function atualizarVinculo(v: CatalogoCirurgiaProdutoVinculo, patch: Partial<CatalogoCirurgiaProdutoVinculo>) {
    Object.assign(v, patch)
    try {
        await orcamentoCatalogoService.atualizarVinculoProduto(v.id, {
            quantidadePadrao: v.quantidadePadrao,
            obrigatorio: v.obrigatorio,
            incluido: v.incluido,
        })
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Falha ao atualizar.", "error")
        await carregar()
    }
}

function pedirDesvincular(v: CatalogoCirurgiaProdutoVinculo) {
    confirmacao.value = { aberto: true, alvo: v, executando: false }
}

async function executarDesvincular() {
    const alvo = confirmacao.value.alvo
    if (!alvo) return
    confirmacao.value.executando = true
    try {
        await orcamentoCatalogoService.desvincularProdutoCirurgia(alvo.id)
        confirmacao.value = { aberto: false, alvo: null, executando: false }
        notificar("Produto desvinculado.", "success")
        await carregar()
    } catch (e: any) {
        confirmacao.value.executando = false
        notificar(e?.response?.data?.mensagem ?? "Falha ao desvincular.", "error")
    }
}
</script>

<template>
    <div class="prods-link">
        <div class="header">
            <h4>Produtos vinculados</h4>
            <span class="count">{{ vinculos.length }}</span>
        </div>

        <div v-if="vinculos.length" class="vincs">
            <div v-for="v in vinculos" :key="v.id" class="vinc-row">
                <div class="vinc-name">
                    <div class="vinc-title">{{ v.produtoNome }}</div>
                    <div class="vinc-sub">
                        {{ v.produtoValorReferencia ? formatarMoedaBrl(v.produtoValorReferencia) : "Sem valor de referência" }}
                    </div>
                </div>
                <div class="vinc-qty">
                    <button type="button" class="qty-btn" @click="atualizarVinculo(v, { quantidadePadrao: Math.max(1, v.quantidadePadrao - 1) })">−</button>
                    <span class="qty-val">{{ v.quantidadePadrao }}</span>
                    <button type="button" class="qty-btn" @click="atualizarVinculo(v, { quantidadePadrao: v.quantidadePadrao + 1 })">+</button>
                </div>
                <button
                    type="button"
                    class="toggle"
                    :class="{ on: v.incluido }"
                    :title="v.incluido ? 'Incluído no total' : 'Opcional (cobrado à parte)'"
                    @click="atualizarVinculo(v, { incluido: !v.incluido })"
                >{{ v.incluido ? "Incluído" : "Opcional" }}</button>
                <button type="button" class="btn-icon btn-icon-excluir" title="Desvincular" @click="pedirDesvincular(v)">
                    <i class="fa-solid fa-trash"></i>
                </button>
            </div>
        </div>
        <div v-else class="empty">Nenhum produto vinculado.</div>

        <div class="add-row">
            <AppSelect v-model="novoProdutoId" :options="opcoesProduto" />
            <AppInput
                type="number"
                :model-value="novoQtd"
                @update:model-value="(v: any) => novoQtd = Math.max(1, Number(v) || 1)"
                style="max-width: 80px;"
            />
            <AppButton variant="secondary" size="sm" icon="fa-solid fa-plus" @click="vincular" :disabled="!novoProdutoId">Vincular</AppButton>
        </div>

        <div class="summary">
            <div class="summary-row">
                <span>Valor base</span>
                <strong>{{ formatarMoedaBrl(valorBase) }}</strong>
            </div>
            <div class="summary-row">
                <span>Produtos inclusos</span>
                <strong>{{ formatarMoedaBrl(totalSugerido - valorBase) }}</strong>
            </div>
            <div class="summary-row total">
                <span>Total sugerido</span>
                <strong>{{ formatarMoedaBrl(totalSugerido) }}</strong>
            </div>
        </div>

        <AppConfirmDialog
            v-model:aberto="confirmacao.aberto"
            titulo="Desvincular produto?"
            :mensagem="confirmacao.alvo ? `Deseja desvincular “${confirmacao.alvo.produtoNome}” do procedimento?` : ''"
            confirmar-rotulo="Desvincular"
            variante="danger"
            icone="fa-solid fa-trash"
            :executando="confirmacao.executando"
            @confirmar="executarDesvincular"
        />

        <AppToast
            v-if="toast"
            :mensagem="toast.mensagem"
            :variante="toast.variante"
            @fechar="toast = null"
        />
    </div>
</template>

<style scoped>
.prods-link {
    border-top: 1px solid hsl(var(--secondary) / 0.1);
    padding-top: 16px;
    display: flex; flex-direction: column; gap: 12px;
}
.header { display: flex; align-items: center; gap: 8px; }
.header h4 { margin: 0; font-size: 14px; font-weight: 600; }
.count {
    background: hsl(var(--primary) / 0.1); color: hsl(var(--primary));
    padding: 2px 8px; border-radius: 999px; font-size: 12px; font-weight: 600;
}
.vincs { display: flex; flex-direction: column; gap: 6px; }
.vinc-row {
    display: grid; grid-template-columns: 1fr auto auto auto;
    align-items: center; gap: 12px;
    padding: 10px 12px;
    background: hsl(var(--secondary) / 0.04); border-radius: 8px;
}
.vinc-title { font-size: 14px; font-weight: 500; }
.vinc-sub { font-size: 12px; color: hsl(var(--secondary) / 0.6); }
.vinc-qty {
    display: inline-flex; align-items: center; gap: 6px;
    background: white; border: 1px solid hsl(var(--secondary) / 0.15);
    border-radius: 6px; padding: 2px;
}
.qty-btn { border: none; background: transparent; cursor: pointer; padding: 0 8px; font-size: 16px; color: hsl(var(--secondary) / 0.7); }
.qty-btn:hover { color: hsl(var(--primary)); }
.qty-val { font-weight: 600; min-width: 24px; text-align: center; }
.toggle {
    border: 1px solid hsl(var(--secondary) / 0.2); background: white;
    padding: 4px 10px; border-radius: 999px; font-size: 12px; font-weight: 600;
    cursor: pointer; color: hsl(var(--secondary) / 0.7);
}
.toggle.on {
    background: hsl(var(--success) / 0.12);
    border-color: hsl(var(--success) / 0.3);
    color: hsl(var(--success));
}
.empty {
    padding: 16px; text-align: center; color: hsl(var(--secondary) / 0.6);
    background: hsl(var(--secondary) / 0.04); border-radius: 8px; font-size: 13px;
}
.add-row { display: grid; grid-template-columns: 1fr auto auto; gap: 8px; align-items: center; }
.summary {
    background: hsl(var(--primary) / 0.06); border-radius: 8px;
    padding: 12px; display: flex; flex-direction: column; gap: 4px;
}
.summary-row { display: flex; justify-content: space-between; font-size: 13px; }
.summary-row.total {
    border-top: 1px solid hsl(var(--primary) / 0.2);
    padding-top: 6px; margin-top: 4px; font-size: 15px;
}
</style>
