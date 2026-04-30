<script setup lang="ts">
import { computed } from 'vue'

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
}>()

const sv = computed(() => props.dadosGerais.sinais_vitais)
const ant = computed(() => props.dadosGerais.antropometria)
const ect = computed(() => props.dadosGerais.ectoscopia)

const MAP_ESTADO_GERAL: Record<string, string> = {
  bom: 'Bom', regular: 'Regular', comprometido: 'Comprometido',
}
const MAP_CONSCIENCIA: Record<string, string> = {
  lucido: 'Lúcido(a) e orientado(a)', confuso: 'Confuso(a)',
  sonolento: 'Sonolento(a)', torporoso: 'Torporoso(a)', comatoso: 'Comatoso(a)',
}
const MAP_ESTADO_NUTRICIONAL: Record<string, string> = {
  desnutrido: 'Desnutrido(a)', eutrofico: 'Eutrófico(a)', sobrepeso: 'Sobrepeso', obeso: 'Obeso(a)',
}
const MAP_COLORACAO: Record<string, string> = {
  hipocorado4: 'Hipocorado(a) ++++/4+', hipocorado3: 'Hipocorado(a) +++/4+',
  hipocorado2: 'Hipocorado(a) ++/4+', hipocorado1: 'Hipocorado(a) +/4+',
  normocorado: 'Normocorado(a)', hipercorado1: 'Hipercorado(a) +/4+', hipercorado2: 'Hipercorado(a) ++/4+',
}
const MAP_HIDRATACAO: Record<string, string> = {
  desidratado4: 'Desidratado(a) ++++/4+', desidratado3: 'Desidratado(a) +++/4+',
  desidratado2: 'Desidratado(a) ++/4+', desidratado1: 'Desidratado(a) +/4+',
  limiar: 'Hidratado(a) no limiar', hidratado: 'Hidratado(a)', hipervolemico: 'Hipervolêmico(a)',
}
const MAP_CIANOSE: Record<string, string> = {
  acianotico: 'Acianótico(a)',
  periferico1: 'Cianótico(a) periférico +/4+', periferico2: 'Cianótico(a) periférico ++/4+',
  periferico3: 'Cianótico(a) periférico +++/4+', periferico4: 'Cianótico(a) periférico ++++/4+',
  central1: 'Cianótico(a) central +/4+', central2: 'Cianótico(a) central ++/4+',
  central3: 'Cianótico(a) central +++/4+', central4: 'Cianótico(a) central ++++/4+',
}
const MAP_ICTERICIA: Record<string, string> = {
  anict: 'Anictérico(a)', leve: 'Ictérico(a) +/4+', moderado: 'Ictérico(a) ++/4+',
  grave: 'Ictérico(a) +++/4+', intenso: 'Ictérico(a) ++++/4+',
}
const MAP_TEMPERATURA: Record<string, string> = {
  afebril: 'Afebril', subfebril: 'Subfebril', febril: 'Febril',
}
const MAP_BATIMENTOS: Record<string, string> = {
  bradicardico: 'Bradicárdico(a)', normocardico: 'Normocárdico(a)', taquicardico: 'Taquicárdico(a)',
}
const MAP_RESPIRACAO: Record<string, string> = {
  bradipneico: 'Bradipneico(a)', eupneico: 'Eupneico(a)', taquipneico: 'Taquipneico(a)', dispneico: 'Dispneico(a)',
}

function resolveLabel(value: string, map: Record<string, string>): string {
  return map[value] || value
}

const paSv = computed(() => {
  const s = sv.value.pa_sistolica
  const d = sv.value.pa_diastolica
  if (!s && !d) return ''
  if (s && d) return `${s}/${d} mmHg`
  return `${s || d} mmHg`
})

const temSinaisVitais = computed(() =>
  !!(paSv.value || sv.value.fc || sv.value.fr || sv.value.temp || sv.value.spo2 || sv.value.glicemia),
)
const temAntropometria = computed(() =>
  !!(ant.value.peso || ant.value.altura || ant.value.imc),
)
const temEctoscopia = computed(() =>
  !!(ect.value.estado_geral || ect.value.consciencia || ect.value.estado_nutricional ||
    ect.value.coloracao || ect.value.hidratacao || ect.value.cianose || ect.value.ictericia ||
    ect.value.temperatura_estado || ect.value.batimentos_cardiacos || ect.value.respiracao ||
    ect.value.descricao),
)
</script>

<template>
  <div class="space-y-3">
    <!-- Sinais vitais -->
    <div v-if="temSinaisVitais" class="border border-border rounded-lg overflow-hidden">
      <div class="bg-muted/30 px-4 py-2.5 border-b border-border">
        <h4 class="text-xs font-semibold flex items-center gap-2">
          <span class="h-1.5 w-1.5 rounded-full bg-destructive" />
          Sinais vitais
        </h4>
      </div>
      <div class="p-4">
        <div class="grid grid-cols-2 md:grid-cols-3 gap-x-6 gap-y-2">
          <div v-if="paSv">
            <p class="field-label-dense">PA</p>
            <p class="text-xs text-foreground">{{ paSv }}</p>
          </div>
          <div v-if="sv.fc">
            <p class="field-label-dense">FC</p>
            <p class="text-xs text-foreground">{{ sv.fc }} bpm</p>
          </div>
          <div v-if="sv.fr">
            <p class="field-label-dense">FR</p>
            <p class="text-xs text-foreground">{{ sv.fr }} irpm</p>
          </div>
          <div v-if="sv.temp">
            <p class="field-label-dense">Temperatura</p>
            <p class="text-xs text-foreground">{{ sv.temp }} °C</p>
          </div>
          <div v-if="sv.spo2">
            <p class="field-label-dense">SpO2</p>
            <p class="text-xs text-foreground">{{ sv.spo2 }}%</p>
          </div>
          <div v-if="sv.glicemia">
            <p class="field-label-dense">Glicemia capilar</p>
            <p class="text-xs text-foreground">{{ sv.glicemia }} mg/dL</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Antropometria -->
    <div v-if="temAntropometria" class="border border-border rounded-lg overflow-hidden">
      <div class="bg-muted/30 px-4 py-2.5 border-b border-border">
        <h4 class="text-xs font-semibold flex items-center gap-2">
          <span class="h-1.5 w-1.5 rounded-full bg-success" />
          Antropometria
        </h4>
      </div>
      <div class="p-4">
        <div class="grid grid-cols-2 md:grid-cols-4 gap-x-6 gap-y-2">
          <div v-if="ant.peso">
            <p class="field-label-dense">Peso</p>
            <p class="text-xs text-foreground">{{ ant.peso }} kg</p>
          </div>
          <div v-if="ant.altura">
            <p class="field-label-dense">Altura</p>
            <p class="text-xs text-foreground">{{ ant.altura }} cm</p>
          </div>
          <div v-if="ant.imc">
            <p class="field-label-dense">IMC</p>
            <p class="text-xs text-foreground">{{ ant.imc }}</p>
          </div>
          <div v-if="ant.imc_classificacao">
            <p class="field-label-dense">Classificação</p>
            <p class="text-xs text-foreground">{{ ant.imc_classificacao }}</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Ectoscopia -->
    <div v-if="temEctoscopia" class="border border-border rounded-lg overflow-hidden">
      <div class="bg-muted/30 px-4 py-2.5 border-b border-border">
        <h4 class="text-xs font-semibold flex items-center gap-2">
          <span class="h-1.5 w-1.5 rounded-full bg-info" />
          Ectoscopia (inspeção geral)
        </h4>
      </div>
      <div class="p-4 space-y-3">
        <div class="grid grid-cols-2 md:grid-cols-3 gap-x-6 gap-y-2">
          <div v-if="ect.estado_geral">
            <p class="field-label-dense">Estado geral</p>
            <p class="text-xs text-foreground">{{ resolveLabel(ect.estado_geral, MAP_ESTADO_GERAL) }}</p>
          </div>
          <div v-if="ect.consciencia">
            <p class="field-label-dense">Consciência</p>
            <p class="text-xs text-foreground">{{ resolveLabel(ect.consciencia, MAP_CONSCIENCIA) }}</p>
          </div>
          <div v-if="ect.estado_nutricional">
            <p class="field-label-dense">Estado nutricional</p>
            <p class="text-xs text-foreground">{{ resolveLabel(ect.estado_nutricional, MAP_ESTADO_NUTRICIONAL) }}</p>
          </div>
          <div v-if="ect.coloracao">
            <p class="field-label-dense">Coloração</p>
            <p class="text-xs text-foreground">{{ resolveLabel(ect.coloracao, MAP_COLORACAO) }}</p>
          </div>
          <div v-if="ect.hidratacao">
            <p class="field-label-dense">Hidratação</p>
            <p class="text-xs text-foreground">{{ resolveLabel(ect.hidratacao, MAP_HIDRATACAO) }}</p>
          </div>
          <div v-if="ect.cianose">
            <p class="field-label-dense">Cianose</p>
            <p class="text-xs text-foreground">{{ resolveLabel(ect.cianose, MAP_CIANOSE) }}</p>
          </div>
          <div v-if="ect.ictericia">
            <p class="field-label-dense">Icterícia</p>
            <p class="text-xs text-foreground">{{ resolveLabel(ect.ictericia, MAP_ICTERICIA) }}</p>
          </div>
          <div v-if="ect.temperatura_estado">
            <p class="field-label-dense">Temperatura</p>
            <p class="text-xs text-foreground">{{ resolveLabel(ect.temperatura_estado, MAP_TEMPERATURA) }}</p>
          </div>
          <div v-if="ect.batimentos_cardiacos">
            <p class="field-label-dense">Batimentos cardíacos</p>
            <p class="text-xs text-foreground">{{ resolveLabel(ect.batimentos_cardiacos, MAP_BATIMENTOS) }}</p>
          </div>
          <div v-if="ect.respiracao">
            <p class="field-label-dense">Respiração</p>
            <p class="text-xs text-foreground">{{ resolveLabel(ect.respiracao, MAP_RESPIRACAO) }}</p>
          </div>
        </div>
        <div v-if="ect.descricao" class="pt-1">
          <p class="field-label-dense">Descrição da ectoscopia</p>
          <p class="text-xs text-foreground whitespace-pre-wrap leading-relaxed">{{ ect.descricao }}</p>
        </div>
      </div>
    </div>
  </div>
</template>
