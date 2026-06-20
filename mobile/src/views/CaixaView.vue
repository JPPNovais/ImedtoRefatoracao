<script setup lang="ts">
import { ref, computed, onMounted } from "vue"
import { useRouter } from "vue-router"
import { financeiroService } from "@/services/financeiro.service"
import { useUiStore } from "@/stores/ui"
import { useShare } from "@/native/useShare"
import { moeda, toISODate } from "@/lib/format"
import type { CaixaDiarioDto, LancamentoExtratoDto } from "@/types"
import AppEmptyState from "@/components/ui/AppEmptyState.vue"

const router = useRouter()
const ui = useUiStore()
const share = useShare()

const caixa = ref<CaixaDiarioDto | null>(null)
const movimentacoes = ref<LancamentoExtratoDto[]>([])
const carregando = ref(true)
const erro = ref(false)

const hoje = new Date()
const dataExibicao = computed(() => {
  const dias = ["dom", "seg", "ter", "qua", "qui", "sex", "sáb"]
  const d = hoje.getDate().toString().padStart(2, "0")
  const m = (hoje.getMonth() + 1).toString().padStart(2, "0")
  return `${dias[hoje.getDay()]}, ${d}/${m}`
})

function totalPorForma(nome: string): number {
  if (!caixa.value) return 0
  const r = caixa.value.resumoPorForma.find(
    (f) => f.formaPagamento.toLowerCase().includes(nome.toLowerCase()),
  )
  return r?.total ?? 0
}

function iconeForma(formaPagamento?: string | null): string {
  const f = (formaPagamento ?? "").toLowerCase()
  if (f.includes("pix")) return "fa-qrcode"
  if (f.includes("cart")) return "fa-credit-card"
  if (f.includes("din")) return "fa-money-bill-wave"
  return "fa-coins"
}

async function carregar() {
  carregando.value = true
  erro.value = false
  const dataHoje = toISODate(new Date())
  try {
    const [cx, extrato] = await Promise.all([
      financeiroService.obterCaixa(),
      financeiroService.listarExtrato({ dataInicio: dataHoje, dataFim: dataHoje, pagina: 1, tamanho: 50 }),
    ])
    caixa.value = cx
    movimentacoes.value = extrato.itens.filter((i) => i.tipo === "Receita" && i.status === "Pago")
  } catch {
    erro.value = true
    ui.toast("Erro ao carregar o caixa", "error")
  } finally {
    carregando.value = false
  }
}

async function compartilhar() {
  if (!caixa.value) return
  const total = moeda(caixa.value.totalDia)
  const linhas = caixa.value.resumoPorForma
    .filter((r) => r.total > 0)
    .map((r) => `  ${r.formaPagamento}: ${moeda(r.total)}`)
    .join("\n")
  await share.compartilhar({
    title: "Caixa do dia",
    text: `Caixa do dia (${dataExibicao.value})\nTotal: ${total}\n${linhas}`,
  })
}

function novoRecebimento() {
  router.push("/pagamento")
}

onMounted(carregar)
</script>

<template>
  <div class="push show">
    <div class="push-head">
      <button class="iconbtn" @click="router.back()">
        <i class="fa-solid fa-arrow-left"></i>
      </button>
      <div class="ph-title">Caixa do dia</div>
      <button class="iconbtn" @click="compartilhar" title="Compartilhar">
        <i class="fa-regular fa-share-from-square"></i>
      </button>
    </div>

    <div class="push-body">
      <!-- Skeleton -->
      <template v-if="carregando">
        <div class="skeleton" style="height: 120px; border-radius: 16px; margin-bottom: 18px;"></div>
        <div class="f-label">Movimentações</div>
        <div class="skeleton" style="height: 200px; border-radius: 12px;"></div>
      </template>

      <!-- Erro -->
      <template v-else-if="erro">
        <AppEmptyState icon="fa-circle-exclamation" titulo="Erro ao carregar" texto="Verifique a conexão." />
        <div style="text-align:center; margin-top: 12px;">
          <button class="btn-outline" @click="carregar">
            <i class="fa-solid fa-rotate-right"></i> Tentar novamente
          </button>
        </div>
      </template>

      <!-- Conteúdo -->
      <template v-else>
        <!-- Card de total (gradiente roxo) -->
        <div class="caixa-total">
          <div class="ct-lbl">Recebido hoje · {{ dataExibicao }}</div>
          <div class="ct-val">{{ moeda(caixa?.totalDia ?? 0) }}</div>
          <div class="ct-split">
            <div>PIX<b>{{ moeda(totalPorForma("pix")) }}</b></div>
            <div>Cartão<b>{{ moeda(totalPorForma("cart")) }}</b></div>
            <div>Dinheiro<b>{{ moeda(totalPorForma("din")) }}</b></div>
          </div>
        </div>

        <div class="f-label">Movimentações</div>

        <!-- Lista de movimentações -->
        <div v-if="movimentacoes.length > 0" class="caixa-list">
          <div v-for="item in movimentacoes" :key="item.id" class="caixa-row">
            <div class="ci">
              <i class="fa-solid" :class="iconeForma(item.formaPagamento)"></i>
            </div>
            <div class="cx">
              <b>{{ item.descricao }}</b>
              <span>{{ item.formaPagamento || "—" }} · {{ item.categoria }}</span>
            </div>
            <div class="cv">+ {{ moeda(item.valor).replace("R$ ", "") }}</div>
          </div>
        </div>
        <AppEmptyState
          v-else
          icon="fa-money-bill-1"
          titulo="Nenhum recebimento"
          texto="Ainda não há recebimentos registrados hoje."
        />

        <div class="audit-foot-cx">
          <i class="fa-solid fa-shield-halved"></i> Valores sincronizados com o financeiro
        </div>
      </template>
    </div>

    <div class="push-foot">
      <button class="btn-primary-lg" style="margin: 0;" @click="novoRecebimento">
        <i class="fa-solid fa-plus"></i> Novo recebimento
      </button>
    </div>
  </div>
</template>

<style scoped>
.audit-foot-cx {
  font-size: var(--fs-xs);
  color: var(--app-text-faint);
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 0 2px;
  margin-top: 16px;
}
</style>
