<script setup lang="ts">
import { ref, onMounted } from "vue"
import { useRouter } from "vue-router"
import { useAuthStore } from "@/stores/authStore"
import { lgpdService, type Consentimento, type TipoConsentimento } from "@/services/lgpdService"
import { AppButton, AppCard, AppModal, AppPageHeader, AppBadge } from "@/components/ui"

const auth   = useAuthStore()
const router = useRouter()

// ─── Consentimentos ───────────────────────────────────────────────────────────

const consentimentos   = ref<Consentimento[]>([])
const carregando       = ref(false)
const erroCarregar     = ref<string | null>(null)

async function carregarConsentimentos() {
    carregando.value = true
    erroCarregar.value = null
    try {
        consentimentos.value = await lgpdService.listarConsentimentos()
    } catch (e: any) {
        erroCarregar.value = e?.response?.data?.mensagem ?? "Erro ao carregar consentimentos."
    } finally {
        carregando.value = false
    }
}

// ─── Exportar dados ───────────────────────────────────────────────────────────

const exportando  = ref(false)
const erroExportar = ref<string | null>(null)

async function exportarDados() {
    exportando.value = true
    erroExportar.value = null
    try {
        await lgpdService.exportarDados()
    } catch (e: any) {
        erroExportar.value = e?.response?.data?.mensagem ?? "Erro ao exportar dados."
    } finally {
        exportando.value = false
    }
}

// ─── Excluir conta ────────────────────────────────────────────────────────────

const confirmandoExcluir = ref(false)
const confirmacaoTexto   = ref("")
const excluindo          = ref(false)
const erroExcluir        = ref<string | null>(null)

const TEXTO_CONFIRMACAO = "EXCLUIR MINHA CONTA"

async function excluirConta() {
    if (confirmacaoTexto.value !== TEXTO_CONFIRMACAO) return
    excluindo.value = true
    erroExcluir.value = null
    try {
        await lgpdService.excluirConta()
        await auth.logout()
        router.push({ name: "Landing" })
    } catch (e: any) {
        erroExcluir.value = e?.response?.data?.mensagem ?? "Erro ao excluir conta."
        excluindo.value = false
    }
}

// ─── Helpers ──────────────────────────────────────────────────────────────────

const LABELS_TIPO: Record<TipoConsentimento, string> = {
    TermosUso:         "Termos de Uso",
    PoliticaPrivacidade: "Politica de Privacidade",
    UsoIA:             "Uso de IA",
}

function fmtData(iso: string) {
    try {
        return new Date(iso).toLocaleString("pt-BR", {
            day: "2-digit", month: "2-digit", year: "numeric",
            hour: "2-digit", minute: "2-digit",
        })
    } catch {
        return iso
    }
}

onMounted(carregarConsentimentos)
</script>

<template>
    <main class="app-page app-page--narrow">
        <AppPageHeader
            titulo="Privacidade e LGPD"
            subtitulo="Gerencie seus dados pessoais e direitos previstos na Lei Geral de Protecao de Dados."
        />

        <!-- Exportar dados -->
        <AppCard title="Exportar meus dados" subtitle="Baixe um arquivo JSON com todas as informacoes associadas a sua conta.">
            <p class="info-texto">
                De acordo com o Art. 18 da LGPD, voce tem direito de acessar seus dados pessoais.
                O arquivo inclui dados de perfil, agendamentos, prontuarios e historico de atividade.
            </p>
            <p v-if="erroExportar" class="msg-erro" role="alert">{{ erroExportar }}</p>
            <template #footer>
                <AppButton
                    icon="fa-solid fa-download"
                    variant="secondary"
                    :loading="exportando"
                    @click="exportarDados"
                >
                    Exportar meus dados
                </AppButton>
            </template>
        </AppCard>

        <!-- Consentimentos -->
        <AppCard title="Meus consentimentos" subtitle="Documentos que voce aceitou ao usar a plataforma.">
            <div v-if="carregando" class="estado-msg">
                <i class="fa-solid fa-spinner fa-spin" aria-hidden="true"></i>
                Carregando...
            </div>
            <p v-else-if="erroCarregar" class="msg-erro" role="alert">{{ erroCarregar }}</p>
            <div v-else-if="consentimentos.length === 0" class="estado-msg">
                Nenhum consentimento registrado.
            </div>
            <table v-else class="tabela-consentimentos">
                <thead>
                    <tr>
                        <th>Documento</th>
                        <th>Versao</th>
                        <th>Aceito em</th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="c in consentimentos" :key="c.id">
                        <td>
                            <AppBadge variant="info" :label="LABELS_TIPO[c.tipo] ?? c.tipo" />
                        </td>
                        <td class="texto-muted">{{ c.versao }}</td>
                        <td class="texto-muted">{{ fmtData(c.aceitoEm) }}</td>
                    </tr>
                </tbody>
            </table>
        </AppCard>

        <!-- Excluir conta -->
        <AppCard title="Excluir minha conta (LGPD)" subtitle="Solicitar anonimizacao permanente dos seus dados pessoais.">
            <div class="aviso-excluir" role="note">
                <i class="fa-solid fa-triangle-exclamation" aria-hidden="true"></i>
                <div>
                    <strong>Esta acao e irreversivel.</strong>
                    <p>
                        Seus dados pessoais (nome, CPF, e-mail, telefone) serao anonimizados conforme o
                        direito ao esquecimento previsto no Art. 18, IV da LGPD. Dados clinicos anonimizados
                        sao retidos pelo prazo legal de 20 anos (CFM 1.821/07).
                    </p>
                </div>
            </div>
            <template #footer>
                <AppButton
                    variant="danger"
                    icon="fa-solid fa-trash"
                    @click="confirmandoExcluir = true"
                >
                    Excluir minha conta
                </AppButton>
            </template>
        </AppCard>

        <!-- Modal de confirmacao -->
        <AppModal
            :aberto="confirmandoExcluir"
            titulo="Confirmar exclusao de conta"
            largura="sm"
            @fechar="confirmandoExcluir = false; confirmacaoTexto = ''; erroExcluir = null"
        >
            <div class="modal-excluir-corpo">
                <p>
                    Esta acao anonimizara permanentemente seus dados pessoais e nao pode ser desfeita.
                </p>
                <p>
                    Para confirmar, digite exatamente: <strong>{{ TEXTO_CONFIRMACAO }}</strong>
                </p>
                <input
                    v-model="confirmacaoTexto"
                    class="input-confirmacao"
                    type="text"
                    :placeholder="TEXTO_CONFIRMACAO"
                    aria-label="Texto de confirmacao de exclusao"
                    autocomplete="off"
                />
                <p v-if="erroExcluir" class="msg-erro" role="alert">{{ erroExcluir }}</p>
            </div>
            <template #rodape>
                <AppButton
                    variant="secondary"
                    @click="confirmandoExcluir = false; confirmacaoTexto = ''; erroExcluir = null"
                >
                    Cancelar
                </AppButton>
                <AppButton
                    variant="danger"
                    :loading="excluindo"
                    :disabled="confirmacaoTexto !== TEXTO_CONFIRMACAO"
                    @click="excluirConta"
                >
                    Confirmar exclusao
                </AppButton>
            </template>
        </AppModal>
    </main>
</template>

<style scoped>
.info-texto {
    font-size: 0.875em;
    color: var(--text-muted);
    margin: 0 0 0.5rem;
    line-height: 1.6;
}

.estado-msg {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    color: var(--text-muted);
    font-size: 0.9em;
    padding: 1rem 0;
}

.msg-erro {
    color: hsl(var(--error));
    font-size: 0.875em;
    margin: 0.5rem 0 0;
}

.tabela-consentimentos {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.88em;
}
.tabela-consentimentos th {
    text-align: left;
    padding: 0.4rem 0.6rem;
    border-bottom: 2px solid hsl(var(--border));
    font-weight: 600;
    color: hsl(var(--muted-foreground));
    font-size: 0.8em;
    text-transform: uppercase;
    letter-spacing: 0.04em;
}
.tabela-consentimentos td {
    padding: 0.55rem 0.6rem;
    border-bottom: 1px solid hsl(var(--border));
}
.texto-muted { color: var(--text-muted); }

.aviso-excluir {
    display: flex;
    gap: 0.85rem;
    padding: 0.9rem 1rem;
    border-radius: var(--radius);
    background: hsl(var(--destructive) / 0.08);
    border: 1px solid hsl(var(--destructive) / 0.2);
    color: hsl(var(--destructive));
    font-size: 0.875em;
    line-height: 1.5;
}
.aviso-excluir i { margin-top: 0.1rem; flex-shrink: 0; }
.aviso-excluir p { margin: 0.3rem 0 0; color: hsl(var(--foreground)); }

.modal-excluir-corpo {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
    font-size: 0.9em;
}
.modal-excluir-corpo p { margin: 0; }

.input-confirmacao {
    padding: 0.5rem 0.75rem;
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius);
    font-family: inherit;
    font-size: 0.875em;
    width: 100%;
    background: hsl(var(--card));
    color: hsl(var(--foreground));
    transition: border-color 0.15s;
    box-sizing: border-box;
}
.input-confirmacao:focus {
    outline: none;
    border-color: hsl(var(--destructive));
    box-shadow: 0 0 0 2px hsl(var(--destructive) / 0.2);
}
</style>
