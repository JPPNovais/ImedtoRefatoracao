<script setup lang="ts">
import { computed, watch } from 'vue'

export interface DadosGeraisExame {
  sinais_vitais: {
    pa_sistolica: string
    pa_diastolica: string
    fc: string
    fr: string
    temp: string
    spo2: string
    glicemia: string
  }
  antropometria: {
    peso: string
    altura: string
    imc: string
    imc_classificacao: string
  }
  ectoscopia: {
    estado_geral: string
    consciencia: string
    estado_nutricional: string
    coloracao: string
    hidratacao: string
    cianose: string
    ictericia: string
    temperatura_estado: string
    batimentos_cardiacos: string
    respiracao: string
    descricao: string
  }
}

const props = defineProps<{
  dadosGerais: DadosGeraisExame
  readonly?: boolean
}>()

const sv = computed(() => props.dadosGerais.sinais_vitais)
const ant = computed(() => props.dadosGerais.antropometria)
const ect = computed(() => props.dadosGerais.ectoscopia)

// Cálculo automático do IMC
watch(
  () => [ant.value.peso, ant.value.altura],
  ([peso, altura]) => {
    if (!peso || !altura) {
      ant.value.imc = ''
      ant.value.imc_classificacao = ''
      return
    }
    const pesoNum = parseFloat(String(peso).replace(',', '.'))
    const alturaNum = parseFloat(String(altura).replace(',', '.'))
    if (!Number.isFinite(pesoNum) || !Number.isFinite(alturaNum) || alturaNum <= 0) {
      ant.value.imc = ''
      ant.value.imc_classificacao = ''
      return
    }
    const alturaM = alturaNum > 3 ? alturaNum / 100 : alturaNum
    const imc = pesoNum / (alturaM * alturaM)
    ant.value.imc = imc.toFixed(2)

    if (imc < 18.5) ant.value.imc_classificacao = 'Baixo peso'
    else if (imc < 25) ant.value.imc_classificacao = 'Eutrófico(a)'
    else if (imc < 30) ant.value.imc_classificacao = 'Sobrepeso'
    else if (imc < 35) ant.value.imc_classificacao = 'Obesidade grau I'
    else if (imc < 40) ant.value.imc_classificacao = 'Obesidade grau II'
    else ant.value.imc_classificacao = 'Obesidade grau III'
  },
  { immediate: true },
)
</script>

<template>
  <div class="space-y-3">
    <!-- Sinais vitais -->
    <div class="border border-border rounded-lg overflow-hidden">
      <div class="bg-muted/30 px-4 py-2.5 border-b border-border">
        <h4 class="text-xs font-semibold flex items-center gap-2">
          <span class="h-1.5 w-1.5 rounded-full bg-destructive" />
          Sinais vitais
        </h4>
      </div>
      <div class="p-4">
        <div class="grid grid-cols-1 md:grid-cols-3 gap-3">
          <div class="space-y-1">
            <label class="field-label-compact">PA (mmHg)</label>
            <div class="flex items-center gap-1">
              <input
                v-model="sv.pa_sistolica"
                class="flex h-8 w-full rounded-md border border-input bg-background px-3 text-xs shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:opacity-50"
                inputmode="numeric"
                type="number"
                min="0"
                placeholder="120"
                :readonly="readonly"
              />
              <span class="text-[11px] font-semibold text-muted-foreground">/</span>
              <input
                v-model="sv.pa_diastolica"
                class="flex h-8 w-full rounded-md border border-input bg-background px-3 text-xs shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:opacity-50"
                inputmode="numeric"
                type="number"
                min="0"
                placeholder="80"
                :readonly="readonly"
              />
            </div>
          </div>
          <div class="space-y-1">
            <label class="field-label-compact">FC (bpm)</label>
            <input
              v-model="sv.fc"
              class="flex h-8 w-full rounded-md border border-input bg-background px-3 text-xs shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:opacity-50"
              inputmode="numeric"
              type="number"
              min="0"
              placeholder="72"
              :readonly="readonly"
            />
          </div>
          <div class="space-y-1">
            <label class="field-label-compact">FR (irpm)</label>
            <input
              v-model="sv.fr"
              class="flex h-8 w-full rounded-md border border-input bg-background px-3 text-xs shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:opacity-50"
              inputmode="numeric"
              type="number"
              min="0"
              placeholder="16"
              :readonly="readonly"
            />
          </div>
          <div class="space-y-1">
            <label class="field-label-compact">Temp (°C)</label>
            <input
              v-model="sv.temp"
              class="flex h-8 w-full rounded-md border border-input bg-background px-3 text-xs shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:opacity-50"
              inputmode="decimal"
              type="number"
              step="0.1"
              placeholder="36,5"
              :readonly="readonly"
            />
          </div>
          <div class="space-y-1">
            <label class="field-label-compact">SpO2 (%)</label>
            <input
              v-model="sv.spo2"
              class="flex h-8 w-full rounded-md border border-input bg-background px-3 text-xs shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:opacity-50"
              inputmode="numeric"
              type="number"
              min="0"
              max="100"
              placeholder="98"
              :readonly="readonly"
            />
          </div>
          <div class="space-y-1">
            <label class="field-label-compact">GC (mg/dL)</label>
            <input
              v-model="sv.glicemia"
              class="flex h-8 w-full rounded-md border border-input bg-background px-3 text-xs shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:opacity-50"
              inputmode="numeric"
              type="number"
              min="0"
              placeholder="105"
              :readonly="readonly"
            />
          </div>
        </div>
      </div>
    </div>

    <!-- Antropometria -->
    <div class="border border-border rounded-lg overflow-hidden">
      <div class="bg-muted/30 px-4 py-2.5 border-b border-border">
        <h4 class="text-xs font-semibold flex items-center gap-2">
          <span class="h-1.5 w-1.5 rounded-full bg-success" />
          Antropometria
        </h4>
      </div>
      <div class="p-4">
        <div class="grid grid-cols-1 md:grid-cols-4 gap-3">
          <div class="space-y-1">
            <label class="field-label-compact">Peso (kg)</label>
            <input
              v-model="ant.peso"
              class="flex h-8 w-full rounded-md border border-input bg-background px-3 text-xs shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:opacity-50"
              placeholder="65,5"
              type="text"
              :readonly="readonly"
            />
          </div>
          <div class="space-y-1">
            <label class="field-label-compact">Altura (cm)</label>
            <input
              v-model="ant.altura"
              class="flex h-8 w-full rounded-md border border-input bg-background px-3 text-xs shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:opacity-50"
              placeholder="165"
              type="text"
              :readonly="readonly"
            />
          </div>
          <div class="space-y-1">
            <label class="field-label-compact">IMC</label>
            <input
              :value="ant.imc"
              class="flex h-8 w-full rounded-md border border-input bg-muted/20 px-3 text-xs shadow-sm"
              readonly
              type="text"
            />
          </div>
          <div class="space-y-1">
            <label class="field-label-compact">Classificação</label>
            <input
              :value="ant.imc_classificacao"
              class="flex h-8 w-full rounded-md border border-input bg-muted/20 px-3 text-xs shadow-sm"
              readonly
              type="text"
            />
          </div>
        </div>
      </div>
    </div>

    <!-- Ectoscopia -->
    <div class="border border-border rounded-lg overflow-hidden">
      <div class="bg-muted/30 px-4 py-2.5 border-b border-border">
        <h4 class="text-xs font-semibold flex items-center gap-2">
          <span class="h-1.5 w-1.5 rounded-full bg-info" />
          Ectoscopia (inspeção geral)
        </h4>
      </div>
      <div class="p-4 space-y-4">
        <div class="grid grid-cols-1 md:grid-cols-3 gap-3">
          <div class="space-y-1">
            <label class="field-label-compact">Estado geral</label>
            <select
              v-model="ect.estado_geral"
              class="flex h-8 w-full rounded-md border border-input bg-background px-3 text-xs shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:opacity-50"
              :disabled="readonly"
            >
              <option value="">Selecione</option>
              <option value="bom">Bom</option>
              <option value="regular">Regular</option>
              <option value="comprometido">Comprometido</option>
            </select>
          </div>
          <div class="space-y-1">
            <label class="field-label-compact">Consciência</label>
            <select
              v-model="ect.consciencia"
              class="flex h-8 w-full rounded-md border border-input bg-background px-3 text-xs shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:opacity-50"
              :disabled="readonly"
            >
              <option value="">Selecione</option>
              <option value="lucido">Lúcido(a) e orientado(a)</option>
              <option value="confuso">Confuso(a)</option>
              <option value="sonolento">Sonolento(a)</option>
              <option value="torporoso">Torporoso(a)</option>
              <option value="comatoso">Comatoso(a)</option>
            </select>
          </div>
          <div class="space-y-1">
            <label class="field-label-compact">Estado nutricional</label>
            <select
              v-model="ect.estado_nutricional"
              class="flex h-8 w-full rounded-md border border-input bg-background px-3 text-xs shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:opacity-50"
              :disabled="readonly"
            >
              <option value="">Selecione</option>
              <option value="desnutrido">Desnutrido(a)</option>
              <option value="eutrofico">Eutrófico(a)</option>
              <option value="sobrepeso">Sobrepeso</option>
              <option value="obeso">Obeso(a)</option>
            </select>
          </div>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-4 gap-3">
          <div class="space-y-1">
            <label class="field-label-compact">Coloração</label>
            <select
              v-model="ect.coloracao"
              class="flex h-8 w-full rounded-md border border-input bg-background px-3 text-xs shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:opacity-50"
              :disabled="readonly"
            >
              <option value="">Selecione</option>
              <option value="hipocorado4">Hipocorado(a) ++++/4+</option>
              <option value="hipocorado3">Hipocorado(a) +++/4+</option>
              <option value="hipocorado2">Hipocorado(a) ++/4+</option>
              <option value="hipocorado1">Hipocorado(a) +/4+</option>
              <option value="normocorado">Normocorado(a)</option>
              <option value="hipercorado1">Hipercorado(a) +/4+</option>
              <option value="hipercorado2">Hipercorado(a) ++/4+</option>
            </select>
          </div>
          <div class="space-y-1">
            <label class="field-label-compact">Hidratação</label>
            <select
              v-model="ect.hidratacao"
              class="flex h-8 w-full rounded-md border border-input bg-background px-3 text-xs shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:opacity-50"
              :disabled="readonly"
            >
              <option value="">Selecione</option>
              <option value="desidratado4">Desidratado(a) ++++/4+</option>
              <option value="desidratado3">Desidratado(a) +++/4+</option>
              <option value="desidratado2">Desidratado(a) ++/4+</option>
              <option value="desidratado1">Desidratado(a) +/4+</option>
              <option value="limiar">Hidratado(a) no limiar</option>
              <option value="hidratado">Hidratado(a)</option>
              <option value="hipervolemico">Hipervolêmico(a)</option>
            </select>
          </div>
          <div class="space-y-1">
            <label class="field-label-compact">Cianose</label>
            <select
              v-model="ect.cianose"
              class="flex h-8 w-full rounded-md border border-input bg-background px-3 text-xs shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:opacity-50"
              :disabled="readonly"
            >
              <option value="">Selecione</option>
              <option value="acianotico">Acianótico(a)</option>
              <option value="periferico1">Cianótico(a) periférico +/4+</option>
              <option value="periferico2">Cianótico(a) periférico ++/4+</option>
              <option value="periferico3">Cianótico(a) periférico +++/4+</option>
              <option value="periferico4">Cianótico(a) periférico ++++/4+</option>
              <option value="central1">Cianótico(a) central +/4+</option>
              <option value="central2">Cianótico(a) central ++/4+</option>
              <option value="central3">Cianótico(a) central +++/4+</option>
              <option value="central4">Cianótico(a) central ++++/4+</option>
            </select>
          </div>
          <div class="space-y-1">
            <label class="field-label-compact">Icterícia</label>
            <select
              v-model="ect.ictericia"
              class="flex h-8 w-full rounded-md border border-input bg-background px-3 text-xs shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:opacity-50"
              :disabled="readonly"
            >
              <option value="">Selecione</option>
              <option value="anict">Anictérico(a)</option>
              <option value="leve">Ictérico(a) +/4+</option>
              <option value="moderado">Ictérico(a) ++/4+</option>
              <option value="grave">Ictérico(a) +++/4+</option>
              <option value="intenso">Ictérico(a) ++++/4+</option>
            </select>
          </div>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-3 gap-3">
          <div class="space-y-1">
            <label class="field-label-compact">Temperatura</label>
            <select
              v-model="ect.temperatura_estado"
              class="flex h-8 w-full rounded-md border border-input bg-background px-3 text-xs shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:opacity-50"
              :disabled="readonly"
            >
              <option value="">Selecione</option>
              <option value="afebril">Afebril</option>
              <option value="subfebril">Subfebril</option>
              <option value="febril">Febril</option>
            </select>
          </div>
          <div class="space-y-1">
            <label class="field-label-compact">Batimentos cardíacos</label>
            <select
              v-model="ect.batimentos_cardiacos"
              class="flex h-8 w-full rounded-md border border-input bg-background px-3 text-xs shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:opacity-50"
              :disabled="readonly"
            >
              <option value="">Selecione</option>
              <option value="bradicardico">Bradicárdico(a)</option>
              <option value="normocardico">Normocárdico(a)</option>
              <option value="taquicardico">Taquicárdico(a)</option>
            </select>
          </div>
          <div class="space-y-1">
            <label class="field-label-compact">Respiração</label>
            <select
              v-model="ect.respiracao"
              class="flex h-8 w-full rounded-md border border-input bg-background px-3 text-xs shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:opacity-50"
              :disabled="readonly"
            >
              <option value="">Selecione</option>
              <option value="bradipneico">Bradipneico(a)</option>
              <option value="eupneico">Eupneico(a)</option>
              <option value="taquipneico">Taquipneico(a)</option>
              <option value="dispneico">Dispneico(a)</option>
            </select>
          </div>
        </div>

        <div class="space-y-1">
          <label class="field-label-compact">Descrição da ectoscopia</label>
          <textarea
            v-model="ect.descricao"
            class="flex w-full rounded-md border border-input bg-background px-3 py-2 text-xs shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:opacity-50 min-h-[72px] resize-y"
            placeholder="Observações adicionais sobre a inspeção geral..."
            :readonly="readonly"
          />
        </div>
      </div>
    </div>
  </div>
</template>
