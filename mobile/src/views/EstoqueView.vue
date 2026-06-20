<script setup lang="ts">
import { ref, computed, watch, onMounted } from "vue"
import { useRouter } from "vue-router"
import { useDebouncedRef } from "@/composables/useDebouncedRef"
import { inventarioService } from "@/services/inventario.service"
import { useUiStore } from "@/stores/ui"
import AppEmptyState from "@/components/ui/AppEmptyState.vue"
import BottomSheet from "@/components/ui/BottomSheet.vue"
import type { ItemInventarioDto } from "@/types"

const router = useRouter()
const ui = useUiStore()

// ── Estado ───────────────────────────────────────────────────────────────────
const carregando = ref(true)
const erro = ref(false)
const itens = ref<ItemInventarioDto[]>([])
const total = ref(0)
const totalBaixa = ref(0)
const valorEstoque = ref(0)

const busca = useDebouncedRef("", 300)
const filtro = ref<"todos" | "baixo">("todos")

// ── Sheet de ajuste ──────────────────────────────────────────────────────────
const sheetAberta = ref(false)
const itemSelecionado = ref<ItemInventarioDto | null>(null)
const novaQuantidade = ref(0)
const salvando = ref(false)

// ── Listas filtradas (filtro "em baixa" feito na API; busca local rápida) ───
const itensFiltrados = computed(() => {
  const q = busca.value.trim().toLowerCase()
  if (!q) return itens.value
  return itens.value.filter((i) => i.nome.toLowerCase().includes(q))
})

// ── Carregar ─────────────────────────────────────────────────────────────────
async function carregar() {
  carregando.value = true
  erro.value = false
  try {
    const apenasAbaixoMinimo = filtro.value === "baixo" ? true : undefined
    const res = await inventarioService.listarItens({ apenasAbaixoMinimo, tamanho: 100 })
    itens.value = res.itens
    total.value = res.total

    // Para o subtítulo do Mais precisamos carregar também os em baixa quando exibindo todos
    if (filtro.value === "todos") {
      totalBaixa.value = res.itens.filter((i) => i.estoqueAbaixoMinimo).length
    }

    valorEstoque.value = res.itens.reduce(
      (acc, i) => acc + i.quantidadeAtual * (i.custoMedio || i.custoUnitario || 0),
      0,
    )
  } catch {
    erro.value = true
    ui.toast("Não foi possível carregar o estoque", "error")
  } finally {
    carregando.value = false
  }
}

// Recarregar ao trocar filtro
watch(filtro, carregar)

onMounted(carregar)

// ── Abrir sheet de ajuste ────────────────────────────────────────────────────
function abrirAjuste(item: ItemInventarioDto) {
  itemSelecionado.value = item
  novaQuantidade.value = item.quantidadeAtual
  sheetAberta.value = true
}

function decrementar() {
  if (novaQuantidade.value > 0) novaQuantidade.value--
}
function incrementar() {
  novaQuantidade.value++
}

async function salvarQuantidade() {
  const item = itemSelecionado.value
  if (!item) return

  const delta = novaQuantidade.value - item.quantidadeAtual
  if (delta === 0) {
    sheetAberta.value = false
    return
  }

  salvando.value = true
  try {
    const tipo = delta > 0 ? "Entrada" : "Saída"
    await inventarioService.registrarMovimentacao({
      itemInventarioId: item.id,
      tipo,
      quantidade: Math.abs(delta),
      custoUnitario: item.custoMedio || item.custoUnitario || 0,
    })
    // Atualiza local para feedback imediato
    const idx = itens.value.findIndex((i) => i.id === item.id)
    if (idx >= 0) {
      itens.value[idx] = {
        ...itens.value[idx],
        quantidadeAtual: novaQuantidade.value,
        estoqueAbaixoMinimo: novaQuantidade.value < item.quantidadeMinima,
      }
    }
    sheetAberta.value = false
    ui.toast("Estoque atualizado")
  } catch {
    ui.toast("Não foi possível atualizar o estoque", "error")
  } finally {
    salvando.value = false
  }
}

async function repor() {
  const item = itemSelecionado.value
  if (!item) return

  // "Repor" = entrada para atingir no mínimo o dobro do mínimo
  const alvoRepor = Math.max(item.quantidadeMinima * 2, item.quantidadeAtual)
  novaQuantidade.value = alvoRepor
}

function formatarValor(v: number) {
  return v.toLocaleString("pt-BR", { style: "currency", currency: "BRL", maximumFractionDigits: 0 })
}
function formatarQtd(n: number) {
  return Number.isInteger(n) ? String(Math.round(n)) : n.toFixed(1).replace(".", ",")
}
</script>

<template>
  <div class="push">
    <!-- Cabeçalho -->
    <div class="push-head">
      <button class="iconbtn" @click="router.back()">
        <i class="fa-solid fa-arrow-left"></i>
      </button>
      <div class="ph-title">Estoque</div>
      <span style="width: 40px"></span>
    </div>

    <div class="push-body">
      <!-- Resumo -->
      <div class="est-summary">
        <div class="est-sum">
          <div class="n">{{ carregando ? '—' : total }}</div>
          <div class="l">Itens</div>
        </div>
        <div class="est-sum warn">
          <div class="n">{{ carregando ? '—' : totalBaixa }}</div>
          <div class="l">Em baixa</div>
        </div>
        <div class="est-sum">
          <div class="n">{{ carregando ? '—' : formatarValor(valorEstoque) }}</div>
          <div class="l">Em estoque</div>
        </div>
      </div>

      <!-- Busca -->
      <div class="psearch ex-search" style="margin-bottom: 10px">
        <i class="fa-solid fa-magnifying-glass"></i>
        <input
          v-model="busca"
          type="text"
          placeholder="Buscar material…"
          autocomplete="off"
        />
        <button v-if="busca" class="clr" @click="busca = ''">
          <i class="fa-solid fa-xmark"></i>
        </button>
      </div>

      <!-- Chips de filtro -->
      <div class="fav-chips" style="margin-bottom: 14px">
        <button
          class="fav-chip"
          :class="{ on: filtro === 'todos' }"
          @click="filtro = 'todos'"
        >
          Todos
        </button>
        <button
          class="fav-chip"
          :class="{ on: filtro === 'baixo' }"
          @click="filtro = 'baixo'"
        >
          <i class="fa-solid fa-triangle-exclamation" style="color: hsl(var(--warning)); font-size: var(--fs-xs)"></i>
          Em baixa
        </button>
      </div>

      <!-- Skeleton -->
      <template v-if="carregando">
        <div class="plist">
          <div v-for="n in 5" :key="n" class="skrow">
            <div class="sk sk-av" style="width: 36px; height: 36px; border-radius: 9px"></div>
            <div style="flex: 1">
              <div class="sk sk-l" style="width: 55%; margin-bottom: 6px"></div>
              <div class="sk sk-l" style="width: 35%"></div>
            </div>
          </div>
        </div>
      </template>

      <!-- Erro -->
      <div v-else-if="erro">
        <AppEmptyState
          icon="fa-solid fa-triangle-exclamation"
          titulo="Não foi possível carregar"
          texto="Verifique a conexão e tente novamente."
        />
        <button class="btn-primary-lg" style="margin-top: 12px" @click="carregar">
          Tentar novamente
        </button>
      </div>

      <!-- Vazio -->
      <div v-else-if="itensFiltrados.length === 0">
        <AppEmptyState
          :icon="filtro === 'baixo' ? 'fa-solid fa-box-open' : 'fa-solid fa-boxes-stacked'"
          :titulo="filtro === 'baixo' ? 'Nenhum item em baixa' : 'Nenhum material'"
          :texto="filtro === 'baixo' ? 'Todos os itens estão acima do mínimo.' : 'Cadastre materiais no painel web.'"
        />
      </div>

      <!-- Lista -->
      <div v-else class="plist">
        <div
          v-for="item in itensFiltrados"
          :key="item.id"
          class="prow"
          @click="abrirAjuste(item)"
        >
          <div class="est-icon" :style="item.categoriaCor ? `background: ${item.categoriaCor}22; color: ${item.categoriaCor}` : ''">
            <i :class="item.categoriaIcone || 'fa-solid fa-box'"></i>
          </div>
          <div class="pinfo" style="flex: 1">
            <div class="pn">
              <b>{{ item.nome }}</b>
              <span v-if="item.estoqueAbaixoMinimo" class="pill p-warning" style="margin-left: auto">
                <i class="fa-solid fa-triangle-exclamation"></i> Baixo
              </span>
            </div>
            <div class="psub">
              {{ formatarQtd(item.quantidadeAtual) }} {{ item.unidadeMedida }}
              <span v-if="item.categoria"> · {{ item.categoria }}</span>
            </div>
          </div>
          <i class="fa-solid fa-chevron-right chev"></i>
        </div>
      </div>
    </div>

    <!-- Sheet de ajuste de quantidade -->
    <BottomSheet :open="sheetAberta" @close="sheetAberta = false">
      <template v-if="itemSelecionado">
        <div class="sh-row">
          <div class="sh-title">{{ itemSelecionado.nome }}</div>
          <button class="sheet-x" @click="sheetAberta = false">
            <i class="fa-solid fa-xmark"></i>
          </button>
        </div>
        <div class="sh-sub" style="margin-top: 2px">Ajuste a quantidade em estoque.</div>

        <div class="est-cur">
          Quantidade atual
          <b>
            <span>{{ formatarQtd(itemSelecionado.quantidadeAtual) }}</span>
            <span style="font-size: var(--fs-sm); color: var(--app-text-dim)"> {{ itemSelecionado.unidadeMedida }}</span>
          </b>
        </div>

        <div class="stepper" style="margin: 0 auto 18px">
          <button :disabled="novaQuantidade <= 0" @click="decrementar">
            <i class="fa-solid fa-minus"></i>
          </button>
          <div class="val">
            <span>{{ formatarQtd(novaQuantidade) }}</span>
          </div>
          <button @click="incrementar">
            <i class="fa-solid fa-plus"></i>
          </button>
        </div>

        <button
          class="btn-primary-lg"
          :disabled="salvando"
          @click="salvarQuantidade"
        >
          <i class="fa-solid fa-floppy-disk"></i>
          {{ salvando ? 'Salvando…' : 'Salvar quantidade' }}
        </button>
        <button class="btn-outline" style="margin-bottom: 0" @click="repor">
          <i class="fa-solid fa-truck-ramp-box"></i> Repor estoque
        </button>
      </template>
    </BottomSheet>
  </div>
</template>

<style scoped>
.est-summary {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 8px;
  margin-bottom: 16px;
}
.est-sum {
  background: var(--app-card);
  border: 1px solid var(--app-border);
  border-radius: var(--radius-xl);
  padding: 12px 10px;
  text-align: center;
  box-shadow: var(--shadow-card);
}
.est-sum .n {
  font-size: var(--fs-xl);
  font-weight: var(--fw-extrabold);
  color: var(--app-text);
  line-height: 1;
}
.est-sum .l {
  font-size: var(--fs-xs);
  font-weight: var(--fw-bold);
  color: var(--app-text-dim);
  margin-top: 4px;
}
.est-sum.warn .n {
  color: hsl(var(--warning));
}
.est-icon {
  width: 36px;
  height: 36px;
  border-radius: 9px;
  background: var(--brand-soft);
  color: var(--brand);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: var(--fs-sm);
  flex: none;
}
.fav-chips {
  display: flex;
  gap: 8px;
  overflow-x: auto;
  padding-bottom: 2px;
}
.fav-chips::-webkit-scrollbar {
  height: 0;
}
.fav-chip {
  flex: none;
  padding: 8px 14px;
  border-radius: var(--radius-full);
  border: 1px solid var(--app-border);
  background: var(--app-card);
  font: inherit;
  font-size: var(--fs-sm);
  font-weight: var(--fw-bold);
  color: var(--app-text-dim);
  cursor: pointer;
  white-space: nowrap;
  display: inline-flex;
  align-items: center;
  gap: 7px;
  min-height: 44px;
}
.fav-chip.on {
  background: var(--brand);
  border-color: var(--brand);
  color: #fff;
}
.est-cur {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 2px;
  font-size: var(--fs-sm);
  color: var(--app-text-dim);
  font-weight: var(--fw-semibold);
  margin-bottom: 12px;
}
.est-cur b {
  font-size: var(--fs-lg);
  font-weight: var(--fw-extrabold);
  color: var(--app-text);
}
.stepper {
  display: flex;
  align-items: center;
  gap: 0;
  background: var(--app-card-2);
  border-radius: var(--radius-xl);
  overflow: hidden;
  width: fit-content;
}
.stepper button {
  width: 52px;
  height: 52px;
  border: 0;
  background: transparent;
  color: var(--app-text);
  font-size: var(--fs-lg);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
}
.stepper button:disabled {
  opacity: 0.35;
}
.stepper button:active {
  background: var(--brand-soft);
}
.stepper .val {
  min-width: 72px;
  text-align: center;
  font-size: var(--fs-xl);
  font-weight: var(--fw-extrabold);
  color: var(--app-text);
  padding: 0 4px;
}
</style>
