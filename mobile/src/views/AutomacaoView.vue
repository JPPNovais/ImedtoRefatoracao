<script setup lang="ts">
import { ref, onMounted } from "vue"
import { useRouter } from "vue-router"
import { automacaoService } from "@/services/automacao.service"
import { usePermissoesStore } from "@/stores/permissoes"
import { useUiStore } from "@/stores/ui"
import type { ConfiguracaoAutomacaoDto } from "@/types"

const router = useRouter()
const permissoes = usePermissoesStore()
const ui = useUiStore()

// Edição exige permissão extra "automacao_config"
const podeEditar = () => permissoes.podeExtra("automacao_config")

const carregando = ref(true)
const erro = ref(false)
const config = ref<ConfiguracaoAutomacaoDto | null>(null)
const salvando = ref(false)

async function carregar() {
  carregando.value = true
  erro.value = false
  try {
    config.value = await automacaoService.obterConfiguracao()
  } catch {
    erro.value = true
    ui.toast("Não foi possível carregar a configuração", "error")
  } finally {
    carregando.value = false
  }
}

onMounted(carregar)

async function alternarLembrete() {
  if (!config.value || !podeEditar()) return
  config.value.lembretesHabilitados = !config.value.lembretesHabilitados
  await persistir()
}

async function alternarWhatsapp() {
  if (!config.value || !podeEditar()) return
  config.value.lembretesWhatsappHabilitados = !config.value.lembretesWhatsappHabilitados
  await persistir()
}

async function persistir() {
  if (!config.value) return
  salvando.value = true
  try {
    await automacaoService.salvarConfiguracao({
      lembretesHabilitados: config.value.lembretesHabilitados,
      lembretesWhatsappHabilitados: config.value.lembretesWhatsappHabilitados,
      horasAntecedenciaLembrete: config.value.horasAntecedenciaLembrete,
      expiracaoOrcamentosHabilitada: config.value.expiracaoOrcamentosHabilitada,
    })
    ui.toast("Automação atualizada")
  } catch {
    ui.toast("Não foi possível salvar. Tente novamente.", "error")
    // reverte localmente
    await carregar()
  } finally {
    salvando.value = false
  }
}
</script>

<template>
  <div class="push">
    <!-- Cabeçalho -->
    <div class="push-head">
      <button class="iconbtn" @click="router.back()">
        <i class="fa-solid fa-arrow-left"></i>
      </button>
      <div class="ph-title">Automação</div>
      <span style="width: 40px"></span>
    </div>

    <div class="push-body">
      <!-- Skeleton -->
      <template v-if="carregando">
        <div class="sk" style="height: 80px; border-radius: var(--radius-xl); margin-bottom: 20px"></div>
        <div class="sk sk-l" style="width: 40%; margin-bottom: 16px"></div>
        <div v-for="n in 3" :key="n" class="plist" style="margin-bottom: 8px">
          <div class="skrow"><div class="sk" style="flex: 1; height: 44px; border-radius: 10px"></div></div>
        </div>
      </template>

      <!-- Erro -->
      <div v-else-if="erro" class="empty">
        <i class="fa-solid fa-triangle-exclamation"></i>
        <b>Não foi possível carregar</b>
        <p>Verifique a conexão e tente novamente.</p>
        <button class="btn-primary-lg" style="margin-top: 12px" @click="carregar">Tentar novamente</button>
      </div>

      <template v-else-if="config">
        <!-- Intro -->
        <div class="auto-intro">
          <div class="ai"><i class="fa-solid fa-bolt"></i></div>
          <p>
            <b>Lembretes automáticos</b> enviados pelo WhatsApp. O paciente confirma com um toque e a agenda atualiza sozinha.
          </p>
        </div>

        <!-- Sem permissão de edição: aviso -->
        <div v-if="!podeEditar()" class="perm-notice">
          <i class="fa-solid fa-lock"></i>
          Apenas o responsável pelo estabelecimento pode alterar as automações.
        </div>

        <!-- Lembretes e confirmações -->
        <div class="f-label">Lembretes e confirmações</div>
        <div class="set-list">
          <!-- Lembrete 24h ↔ LembretesHabilitados (persiste) -->
          <div class="set-row">
            <span class="si"><i class="fa-regular fa-bell"></i></span>
            <span class="st">
              Lembrete de consulta
              <small>
                24h antes ·
                <span class="chan-wa"><i class="fa-brands fa-whatsapp"></i> WhatsApp</span>
              </small>
            </span>
            <button
              class="switch"
              :class="{ on: config.lembretesHabilitados }"
              :disabled="!podeEditar() || salvando"
              @click="alternarLembrete"
            ></button>
          </div>

          <!-- Canal WhatsApp ↔ LembretesWhatsappHabilitados (persiste) -->
          <div class="set-row">
            <span class="si"><i class="fa-brands fa-whatsapp"></i></span>
            <span class="st">
              Canal WhatsApp
              <small>Enviar lembretes via WhatsApp</small>
            </span>
            <button
              class="switch"
              :class="{ on: config.lembretesWhatsappHabilitados }"
              :disabled="!podeEditar() || salvando"
              @click="alternarWhatsapp"
            ></button>
          </div>

          <!-- Confirmação de presença — "em breve" (sem contraparte no backend) -->
          <div class="set-row soon">
            <span class="si"><i class="fa-solid fa-circle-check"></i></span>
            <span class="st">
              Confirmação de presença
              <small>Pede confirmação · atualiza a agenda</small>
            </span>
            <span class="em-breve">Em breve</span>
          </div>

          <!-- Lembrete no dia — "em breve" -->
          <div class="set-row soon">
            <span class="si"><i class="fa-regular fa-clock"></i></span>
            <span class="st">
              Lembrete no dia
              <small>2h antes da consulta</small>
            </span>
            <span class="em-breve">Em breve</span>
          </div>
        </div>

        <!-- Relacionamento — todos "em breve" -->
        <div class="f-label">Relacionamento</div>
        <div class="set-list">
          <div class="set-row soon">
            <span class="si"><i class="fa-solid fa-cake-candles"></i></span>
            <span class="st">Aniversário do paciente<small>Mensagem automática no dia</small></span>
            <span class="em-breve">Em breve</span>
          </div>
          <div class="set-row soon">
            <span class="si"><i class="fa-solid fa-arrows-rotate"></i></span>
            <span class="st">Retorno / recall<small>Após período sem consulta</small></span>
            <span class="em-breve">Em breve</span>
          </div>
          <div class="set-row soon">
            <span class="si"><i class="fa-regular fa-paper-plane"></i></span>
            <span class="st">Pós-consulta<small>Orientações e cuidados</small></span>
            <span class="em-breve">Em breve</span>
          </div>
        </div>

        <div class="audit-foot" style="margin-top: 16px">
          <i class="fa-solid fa-shield-halved"></i>
          Mensagens seguem a LGPD e o consentimento do paciente
        </div>
      </template>
    </div>
  </div>
</template>

<style scoped>
.auto-intro {
  display: flex;
  align-items: flex-start;
  gap: 14px;
  background: var(--brand-soft);
  border-radius: var(--radius-xl);
  padding: 16px;
  margin-bottom: 22px;
}
.auto-intro .ai {
  width: 40px;
  height: 40px;
  border-radius: 12px;
  background: var(--brand);
  color: #fff;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: var(--fs-base);
  flex: none;
}
.auto-intro p {
  font-size: var(--fs-sm);
  color: var(--app-text);
  line-height: 1.5;
  margin: 0;
  font-weight: var(--fw-semibold);
}
.auto-intro p b {
  color: var(--brand);
}
.chan-wa {
  color: #25d366;
}
.em-breve {
  font-size: var(--fs-xs);
  font-weight: var(--fw-bold);
  color: var(--app-text-faint);
  background: var(--app-card-2);
  padding: 3px 8px;
  border-radius: var(--radius-full);
  white-space: nowrap;
  flex: none;
}
.set-row.soon {
  opacity: 0.7;
}
.perm-notice {
  display: flex;
  align-items: center;
  gap: 8px;
  background: hsl(var(--warning) / 0.1);
  border: 1px solid hsl(var(--warning) / 0.3);
  border-radius: var(--radius-lg);
  padding: 10px 14px;
  font-size: var(--fs-sm);
  font-weight: var(--fw-semibold);
  color: hsl(35 88% 36%);
  margin-bottom: 16px;
}
</style>
