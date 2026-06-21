<script setup lang="ts">
import { ref, computed, onMounted } from "vue"
import { useRoute, useRouter } from "vue-router"
import { cobrancaService } from "@/services/cobranca.service"
import { financeiroService } from "@/services/financeiro.service"
import { useAuthStore } from "@/stores/auth"
import { useUiStore } from "@/stores/ui"
import { iniciais, moeda, toISODate } from "@/lib/format"
import { mensagemDeErro } from "@/lib/erros"
import { useMascaraMoeda } from "@/composables/useMascaraMoeda"
import type { FormaPagamentoDto, CobrancaDetalheDto } from "@/types"
import AppEmptyState from "@/components/ui/AppEmptyState.vue"
import BottomSheet from "@/components/ui/BottomSheet.vue"

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const ui = useUiStore()

// Parâmetros de entrada (vindos de query string)
const agendamentoId = computed(() => {
  const v = route.query.agendamentoId
  return v ? Number(v) : null
})
const pacienteNomeQuery = computed(() => (route.query.pacienteNome as string) || null)

// Estado
const carregando = ref(true)
const salvando = ref(false)
const formas = ref<FormaPagamentoDto[]>([])
const cobranca = ref<CobrancaDetalheDto | null>(null)
const pacienteNome = ref<string | null>(null)
const valorStr = ref("")
const formaSelecionadaId = ref<number | null>(null)
const parcelas = ref(1)
const observacoes = ref("")
const semCobranca = ref(false)
const sucessoOpen = ref(false)

// Forma selecionada
const formaSelecionada = computed(
  () => formas.value.find((f) => f.id === formaSelecionadaId.value) ?? null,
)
const isCartao = computed(
  () => formaSelecionada.value?.nome.toLowerCase().includes("cart") ?? false,
)

// Valor numérico a partir da string formatada
const valorNumerico = computed(() => {
  const s = valorStr.value.replace(/\./g, "").replace(",", ".")
  const n = parseFloat(s)
  return isNaN(n) ? 0 : n
})

// Label do vínculo
const vinculoLabel = computed(() => {
  if (cobranca.value) return `Cobrança #${cobranca.value.id}`
  if (agendamentoId.value) return `Agendamento #${agendamentoId.value}`
  return "Nenhum vínculo"
})

// Ícone por forma de pagamento
function iconeFormaFn(nome: string): string {
  const n = nome.toLowerCase()
  if (n.includes("pix")) return "fa-qrcode"
  if (n.includes("cart")) return "fa-credit-card"
  if (n.includes("din")) return "fa-money-bill-wave"
  return "fa-coins"
}

const mascaraMoeda = useMascaraMoeda()

function onValorInput(e: Event) {
  mascaraMoeda.onValorInput(e, valorStr)
}

function setValorRapido(v: number) {
  mascaraMoeda.setValorNumerico(v, valorStr)
}

function selecionarForma(id: number) {
  formaSelecionadaId.value = id
  if (!isCartao.value) parcelas.value = 1
}

async function carregar() {
  carregando.value = true
  semCobranca.value = false
  try {
    const fs = await financeiroService.listarFormasPagamento(true)
    formas.value = fs
    const padrao = fs.find((f) => f.padrao) ?? fs[0]
    if (padrao) formaSelecionadaId.value = padrao.id

    if (agendamentoId.value) {
      pacienteNome.value = pacienteNomeQuery.value
      const cob = await cobrancaService.obterPorAgendamento(agendamentoId.value)
      if (!cob) {
        semCobranca.value = true
        return
      }
      cobranca.value = cob
      pacienteNome.value = cob.pacienteNome

      const profId = auth.usuario?.id
      if (profId) {
        try {
          const vs = await cobrancaService.obterValorSugerido(profId)
          if (vs.valorSugerido) {
            setValorRapido(vs.valorSugerido)
          } else if (cob.valorCobrado > 0) {
            setValorRapido(cob.valorCobrado)
          }
        } catch {
          if (cob.valorCobrado > 0) setValorRapido(cob.valorCobrado)
        }
      }
    } else {
      pacienteNome.value = pacienteNomeQuery.value
    }
  } catch {
    ui.toast("Erro ao carregar dados de pagamento", "error")
  } finally {
    carregando.value = false
  }
}

async function salvar() {
  if (valorNumerico.value <= 0) {
    ui.toast("Informe um valor válido", "error")
    return
  }
  if (!formaSelecionadaId.value) {
    ui.toast("Escolha a forma de pagamento", "error")
    return
  }
  if (!cobranca.value) {
    ui.toast("Nenhuma cobrança associada. Faça o check-in primeiro.", "error")
    return
  }

  salvando.value = true
  try {
    await cobrancaService.registrarPagamentos(cobranca.value.id, {
      desconto: 0,
      dataPagamento: toISODate(new Date()),
      formas: [
        {
          formaPagamentoId: formaSelecionadaId.value,
          valor: valorNumerico.value,
          parcelas: isCartao.value ? parcelas.value : 1,
          juros: 0,
        },
      ],
    })
    // Mostra confirmação com opção de voltar — recibo disponível pela Caixa
    sucessoOpen.value = true
  } catch (err) {
    ui.toast(mensagemDeErro(err, "Não foi possível registrar o pagamento"), "error")
  } finally {
    salvando.value = false
  }
}

onMounted(carregar)
</script>

<template>
  <div class="push show">
    <div class="push-head">
      <button class="iconbtn" aria-label="Voltar" @click="router.back()">
        <i class="fa-solid fa-arrow-left"></i>
      </button>
      <div class="ph-title">Recebimento</div>
      <span style="width: 40px;"></span>
    </div>

    <!-- Sem cobrança (orientar check-in) -->
    <div v-if="!carregando && semCobranca" class="push-body">
      <AppEmptyState
        icon="fa-calendar-xmark"
        titulo="Check-in necessário"
        texto="Faça o check-in do paciente primeiro para registrar o pagamento."
      />
      <div style="text-align: center; margin-top: 16px;">
        <button class="btn-outline" @click="router.back()">
          <i class="fa-solid fa-arrow-left"></i> Voltar
        </button>
      </div>
    </div>

    <!-- Skeleton -->
    <div v-else-if="carregando" class="push-body">
      <div class="skeleton" style="height: 56px; border-radius: 12px; margin-bottom: 16px;"></div>
      <div class="skeleton" style="height: 80px; border-radius: 12px; margin-bottom: 16px;"></div>
      <div class="skeleton" style="height: 56px; border-radius: 12px;"></div>
    </div>

    <!-- Formulário -->
    <div v-else class="push-body">
      <!-- Paciente -->
      <div class="rc-patient" style="cursor: default;">
        <span class="av pay-av">
          <template v-if="pacienteNome">{{ iniciais(pacienteNome ?? '') }}</template>
          <i v-else class="fa-solid fa-user"></i>
        </span>
        <span class="rx">
          <b>{{ pacienteNome || "Paciente não informado" }}</b>
          <span>{{ vinculoLabel }}</span>
        </span>
      </div>

      <!-- Valor -->
      <div class="f-label">Valor</div>
      <div class="pay-amount">
        <span class="cur">R$</span>
        <input
          type="text"
          inputmode="decimal"
          placeholder="0,00"
          autocomplete="off"
          :value="valorStr"
          @input="onValorInput"
          class="pay-input"
        />
      </div>

      <!-- Valores rápidos -->
      <div class="pay-quick">
        <button
          v-for="v in [100, 150, 200, 300, 500]"
          :key="v"
          class="quick-btn"
          @click="setValorRapido(v)"
        >
          {{ moeda(v) }}
        </button>
      </div>

      <!-- Forma de pagamento -->
      <div class="f-label">Forma de pagamento</div>
      <div class="pay-method">
        <button
          v-for="forma in formas"
          :key="forma.id"
          class="pay-m"
          :class="{ on: formaSelecionadaId === forma.id }"
          @click="selecionarForma(forma.id)"
        >
          <i class="fa-solid" :class="iconeFormaFn(forma.nome)"></i>
          {{ forma.nome }}
        </button>
      </div>

      <!-- Parcelas (só para cartão) -->
      <div v-if="isCartao" class="parc-wrap">
        <div class="f-label">Parcelas</div>
        <div class="sel-wrap" style="margin-bottom: 20px;">
          <select class="msel" v-model="parcelas">
            <option v-for="n in [1, 2, 3, 4, 6, 10, 12]" :key="n" :value="n">
              {{ n }}x {{ n === 1 ? "sem juros" : "" }}
            </option>
          </select>
          <i class="fa-solid fa-chevron-down"></i>
        </div>
      </div>

      <!-- Vínculo (informativo) -->
      <div class="f-label">Vincular a (opcional)</div>
      <div class="tap-field" style="cursor: default; margin-bottom: 20px;">
        <i class="fa-solid fa-link lead"></i>
        <span>{{ vinculoLabel }}</span>
      </div>

      <!-- Observações -->
      <div class="f-label">Observações (opcional)</div>
      <textarea
        v-model="observacoes"
        placeholder="Ex.: referente à consulta · sinal de procedimento…"
        class="pay-obs"
      ></textarea>

      <!-- Gerar link (em breve) -->
      <button class="btn-outline" disabled style="opacity: 0.45; cursor: not-allowed; margin-top: 4px;">
        <i class="fa-solid fa-link"></i> Gerar link ·
        <span style="font-size: var(--fs-xs);">Em breve</span>
      </button>
    </div>

    <div v-if="!carregando && !semCobranca" class="push-foot">
      <button
        class="btn-primary-lg"
        style="margin: 0;"
        :disabled="salvando || valorNumerico <= 0"
        @click="salvar"
      >
        <i
          class="fa-solid"
          :class="salvando ? 'fa-circle-notch fa-spin' : 'fa-circle-check'"
        ></i>
        {{ salvando ? "Salvando…" : "Receber" }}
      </button>
    </div>

    <!-- Confirmação de sucesso (item 7) -->
    <BottomSheet v-model:open="sucessoOpen" titulo="Pagamento registrado">
      <div class="pay-ok-body">
        <div class="pay-ok-icon"><i class="fa-solid fa-circle-check"></i></div>
        <p>Pagamento de <b>{{ moeda(valorNumerico) }}</b> registrado com sucesso.</p>
        <p class="pay-ok-sub">O recibo fica disponível na aba Caixa.</p>
      </div>
      <div class="btn-row" style="margin: 0 0 12px;">
        <button class="btn-outline" style="flex: 1" @click="() => { sucessoOpen = false; router.back() }">
          <i class="fa-solid fa-arrow-left"></i> Voltar
        </button>
      </div>
    </BottomSheet>
  </div>
</template>

<style scoped>
.pay-ok-body {
  text-align: center;
  padding: 8px 0 16px;
}
.pay-ok-icon {
  font-size: var(--fs-4xl);
  color: hsl(var(--success));
  margin-bottom: 10px;
}
.pay-ok-sub {
  font-size: var(--fs-xs);
  color: var(--app-text-faint);
  margin-top: 4px;
}
.pay-av {
  width: 42px;
  height: 42px;
  border-radius: 50%;
  background: var(--brand-soft);
  color: var(--brand);
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: var(--fw-extrabold);
  font-size: var(--fs-sm);
  flex: none;
}
.pay-amount {
  display: flex;
  align-items: center;
  gap: 8px;
  background: var(--app-card);
  border: 1.5px solid var(--app-border);
  border-radius: var(--radius-xl);
  padding: 14px 16px;
  margin-bottom: 12px;
}
.cur {
  font-size: var(--fs-xl);
  font-weight: var(--fw-extrabold);
  color: var(--app-text-dim);
}
.pay-input {
  flex: 1;
  border: 0;
  background: transparent;
  font-family: var(--font-sans);
  font-size: var(--fs-2xl);
  font-weight: var(--fw-extrabold);
  color: var(--app-text);
  outline: none;
  font-variant-numeric: tabular-nums;
  min-width: 0;
}
.pay-quick {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
  margin-bottom: 20px;
}
.quick-btn {
  border: 1px solid var(--app-border);
  background: var(--app-card);
  border-radius: var(--radius-full);
  padding: 6px 14px;
  font-family: var(--font-sans);
  font-size: var(--fs-xs);
  font-weight: var(--fw-bold);
  color: var(--app-text);
  cursor: pointer;
  min-height: 32px;
}
.quick-btn:active {
  background: var(--brand-soft);
}
.pay-method {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
  margin-bottom: 20px;
}
.pay-m {
  flex: 1;
  min-width: 80px;
  border: 1.5px solid var(--app-border);
  background: var(--app-card);
  border-radius: var(--radius-xl);
  padding: 10px 12px;
  font-family: var(--font-sans);
  font-size: var(--fs-xs);
  font-weight: var(--fw-bold);
  color: var(--app-text);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  min-height: 44px;
}
.pay-m.on {
  border-color: var(--brand);
  background: var(--brand-soft);
  color: var(--brand);
}
.parc-wrap {
  margin-bottom: 0;
}
.pay-obs {
  width: 100%;
  min-height: 80px;
  background: var(--app-card);
  border: 1.5px solid var(--app-border);
  border-radius: var(--radius-xl);
  padding: 12px 14px;
  font-family: var(--font-sans);
  font-size: var(--fs-sm);
  font-weight: var(--fw-semibold);
  color: var(--app-text);
  outline: none;
  resize: none;
  margin-bottom: 12px;
  box-sizing: border-box;
}
.pay-obs::placeholder {
  color: var(--app-text-faint);
}
</style>
