<script setup lang="ts">
import { computed, onMounted, ref } from "vue"
import { useRoute, useRouter } from "vue-router"
import { receitaService } from "@/services/documentos.service"
import { pacienteService } from "@/services/paciente.service"
import type { ItemReceita, MedicamentoFavorito, MedicamentoFavoritoBackend, TipoReceita } from "@/types"
import { useUiStore } from "@/stores/ui"
import { iniciais } from "@/lib/format"
import BottomSheet from "@/components/ui/BottomSheet.vue"
import AppSearchInput from "@/components/ui/AppSearchInput.vue"
import AssinaturaFlow from "@/components/AssinaturaFlow.vue"

const route = useRoute()
const router = useRouter()
const ui = useUiStore()

// Favoritos reais carregados do backend (GET /api/receitas/favoritos) — substituem o array hardcoded.
// Nota: exames e CID continuam locais — o backend não expõe endpoint de exames/CID.
const favoritosBackend = ref<MedicamentoFavoritoBackend[]>([])
const favoritosCarregando = ref(false)

// Fallback local usado apenas se o backend retornar vazio (profissional sem histórico ainda)
const FAVS_FALLBACK: MedicamentoFavorito[] = [
  { medicamento: "Amoxicilina 500mg", posologia: "1 cápsula · 8/8h · 7 dias" },
  { medicamento: "Dipirona 1g", posologia: "1 comprimido · 6/6h se dor" },
  { medicamento: "Losartana 50mg", posologia: "1 comprimido · 1x/dia" },
  { medicamento: "Omeprazol 20mg", posologia: "1 cápsula · em jejum · 14 dias" },
]

// Converte favoritos do backend para o shape da view; usa fallback local se lista vazia
const FAVS = computed<MedicamentoFavorito[]>(() => {
  if (favoritosBackend.value.length) {
    return favoritosBackend.value.map((f) => ({
      id: f.id,
      medicamento: f.medicamento,
      posologia: f.posologia ?? "1 comprimido",
    }))
  }
  return FAVS_FALLBACK
})

const MEDS = ["Amoxicilina 500mg", "Dipirona 1g", "Losartana 50mg", "Omeprazol 20mg", "Ibuprofeno 600mg", "Azitromicina 500mg", "Metformina 850mg", "Prednisona 20mg", "Paracetamol 750mg", "Cefalexina 500mg"]

const pacienteIdRaw = Number(route.query.pacienteId)
if (!pacienteIdRaw || pacienteIdRaw <= 0) {
  ui.toast("Paciente não identificado", "error")
  router.back()
}
const pacienteId = pacienteIdRaw
const pacienteNome = ref("Paciente")
const tipo = ref<TipoReceita>("Simples")
const itens = ref<ItemReceita[]>([])

const medSheet = ref(false)
const buscaMed = ref("")
const posSheet = ref(false)
const posIdx = ref<number | null>(null)
const posQtd = ref("1 comprimido")
const posFreq = ref("8/8h")
const posDur = ref("7 dias")

const flow = ref<InstanceType<typeof AssinaturaFlow> | null>(null)

const medsFiltrados = computed(() =>
  MEDS.filter((m) => m.toLowerCase().includes(buscaMed.value.toLowerCase())),
)

onMounted(async () => {
  // Carrega nome do paciente e favoritos reais em paralelo
  favoritosCarregando.value = true
  const [p, favs] = await Promise.all([
    pacienteService.obter(pacienteId).catch(() => null),
    receitaService.listarFavoritos().catch(() => [] as MedicamentoFavoritoBackend[]),
  ])
  if (p) pacienteNome.value = p.nomeCompleto
  favoritosBackend.value = favs
  favoritosCarregando.value = false

  // Pré-seleciona o favorito mais usado como 1º item (se houver)
  if (FAVS.value.length && !itens.value.length) {
    itens.value = [{ medicamento: FAVS.value[0].medicamento, posologia: FAVS.value[0].posologia }]
  }
})

function addFav(f: MedicamentoFavorito) {
  itens.value.push({ medicamento: f.medicamento, posologia: f.posologia })
}
function addMed(nome: string) {
  itens.value.push({ medicamento: nome, posologia: "1 comprimido · 8/8h · 7 dias" })
  medSheet.value = false
  buscaMed.value = ""
}
function removerItem(i: number) {
  itens.value.splice(i, 1)
}
function editarPos(i: number) {
  posIdx.value = i
  const pos = itens.value[i].posologia.split(" · ")
  posQtd.value = pos[0] || "1 comprimido"
  posFreq.value = pos[1] || "8/8h"
  posDur.value = pos[2] || "7 dias"
  posSheet.value = true
}
function aplicarPos() {
  if (posIdx.value !== null) {
    itens.value[posIdx.value].posologia = `${posQtd.value} · ${posFreq.value} · ${posDur.value}`
  }
  posSheet.value = false
}

async function assinar() {
  if (!itens.value.length) return ui.toast("Adicione ao menos um medicamento", "error")
  await flow.value?.iniciar(async () => {
    const { receitaId } = await receitaService.emitir({
      pacienteId,
      tipo: tipo.value,
      itens: itens.value,
    })
    await receitaService.assinar(receitaId)

    // Polling real: aguarda até AssinadaIcp / erro ou timeout ~30s
    const TIMEOUT_MS = 30_000
    const INTERVALO_MS = 2_000
    const inicio = Date.now()
    while (Date.now() - inicio < TIMEOUT_MS) {
      await new Promise((r) => setTimeout(r, INTERVALO_MS))
      const s = await receitaService.statusAssinatura(receitaId)
      if (s.status === "AssinadaIcp" || s.status === "AssinadaMemed") {
        return { url: s.pdfAssinadoUrl ?? undefined, pdfPath: receitaService.pdfUrl(receitaId) }
      }
      if (s.status === "FalhaAssinatura" || s.status === "Erro") {
        throw new Error("Falha na assinatura digital")
      }
    }
    // Timeout: retorna com pdfPath disponível (sem URL pré-assinada)
    return { pdfPath: receitaService.pdfUrl(receitaId) }
  })
}
function concluir() {
  router.back()
}
</script>

<template>
  <div class="push show">
    <div class="push-head">
      <button class="iconbtn" @click="router.back()"><i class="fa-solid fa-arrow-left"></i></button>
      <div class="ph-title">Nova receita</div>
      <span style="width: 40px"></span>
    </div>

    <div class="push-body">
      <button class="rc-patient">
        <span class="av">{{ iniciais(pacienteNome) }}</span>
        <span class="rx"><b>{{ pacienteNome }}</b><span>Paciente</span></span>
        <i class="fa-solid fa-chevron-right chev"></i>
      </button>

      <div class="f-label">Tipo de receita</div>
      <div class="sel-wrap">
        <select v-model="tipo" class="msel">
          <option value="Simples">Simples</option>
          <option value="Controlada">Controlada (especial)</option>
          <option value="Antimicrobiano">Antimicrobiano</option>
        </select>
        <i class="fa-solid fa-chevron-down"></i>
      </div>

      <div class="f-label"><i class="fa-solid fa-star" style="color: hsl(var(--warning)); font-size: 11px"></i> Favoritos</div>
      <div class="fav-chips">
        <button v-for="f in FAVS" :key="f.medicamento" class="fav-chip" @click="addFav(f)">
          <i class="fa-solid fa-star" style="color: hsl(var(--warning))"></i> {{ f.medicamento }}
        </button>
        <button class="fav-chip add" @click="medSheet = true"><i class="fa-solid fa-magnifying-glass"></i> Buscar</button>
      </div>

      <div class="f-label">Itens da receita</div>
      <div class="rc-items">
        <div v-for="(it, i) in itens" :key="i" class="rc-item">
          <div class="rd"><b>{{ it.medicamento }}</b><div class="pos">{{ it.posologia }}</div></div>
          <button class="ed" @click="editarPos(i)"><i class="fa-solid fa-pen"></i></button>
          <button class="rm2" @click="removerItem(i)"><i class="fa-solid fa-xmark"></i></button>
        </div>
        <div v-if="!itens.length" class="rc-empty">Nenhum medicamento. Toque num favorito ou em "Adicionar".</div>
      </div>
      <button class="rc-add" @click="medSheet = true"><i class="fa-solid fa-plus"></i> Adicionar medicamento</button>
    </div>

    <div class="push-foot">
      <button class="btn-primary-lg" style="margin: 0" @click="assinar"><i class="fa-solid fa-signature"></i> Assinar e gerar</button>
    </div>

    <!-- med search sheet -->
    <BottomSheet v-model:open="medSheet" titulo="Adicionar medicamento" tall>
      <AppSearchInput v-model="buscaMed" placeholder="Buscar medicamento…" />
      <div v-for="m in medsFiltrados" :key="m" class="med-row" @click="addMed(m)">
        <div class="mi"><i class="fa-solid fa-pills"></i></div>
        <b>{{ m }}</b>
        <i class="fa-solid fa-plus add-i"></i>
      </div>
    </BottomSheet>

    <!-- posologia sheet -->
    <BottomSheet v-model:open="posSheet" titulo="Posologia" sub="Ajuste a administração e a duração." closable>
      <div class="pos-grid">
        <label><span class="field-label2">Quantidade e forma</span><input v-model="posQtd" type="text" /></label>
        <label><span class="field-label2">Frequência</span><input v-model="posFreq" type="text" /></label>
        <label><span class="field-label2">Duração</span><input v-model="posDur" type="text" /></label>
      </div>
      <button class="btn-primary-lg" style="margin-top: 14px" @click="aplicarPos"><i class="fa-solid fa-check"></i> Aplicar</button>
    </BottomSheet>

    <AssinaturaFlow
      ref="flow"
      titulo-sucesso="Receita assinada"
      :resumo="`${pacienteNome} · ${itens.length} ${itens.length === 1 ? 'item' : 'itens'} · receita ${tipo.toLowerCase()}`"
      copy-send="Enviar ao paciente"
      @concluir="concluir"
    />
  </div>
</template>
