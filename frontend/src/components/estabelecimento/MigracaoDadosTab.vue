<script setup lang="ts">
/**
 * MigracaoDadosTab — seção "Migrar meus dados" nas Configurações do Estabelecimento.
 *
 * Fluxo:
 * 1. Cliente seleciona um arquivo ZIP (máx 50MB — CA19, R11).
 * 2. Marca o checkbox de termo de responsabilidade (R12).
 * 3. Clica "Enviar arquivo" → upload para o backend → job criado.
 * 4. Exibe o status do job retornado.
 *
 * Validação 50MB: feita no front (CA19) E no back (422) — R11.
 * Nenhum PII logado aqui — apenas o status/erros do job (CA4).
 */
import { ref } from "vue"
import { AppButton } from "@/components/ui"
import migracaoService, { LIMITE_UPLOAD_BYTES, MENSAGEM_LIMITE } from "@/services/migracaoService"
import type { MigracaoJobStatus } from "@/services/migracaoService"

const props = defineProps<{
    estabelecimentoId: number
}>()

// ─── Estado ──────────────────────────────────────────────────────────────────
const arquivoSelecionado = ref<File | null>(null)
const termoAceito        = ref(false)
const enviando           = ref(false)
const erroUpload         = ref<string | null>(null)
const jobCriado          = ref<MigracaoJobStatus | null>(null)
const inputRef           = ref<HTMLInputElement | null>(null)

// Onda — Onda 1 (pacientes) é o padrão; "prontuario" = Onda 2 (CA13).
const ondaSelecionada    = ref<"" | "prontuario">("")

const OPCOES_ONDA = [
    { valor: "",           rotulo: "Onda 1 — Pacientes e agenda",     descricao: "Importa pacientes, agendamentos e dados cadastrais." },
    { valor: "prontuario", rotulo: "Onda 2 — Prontuários históricos", descricao: "Importa evoluções e documentos de prontuário. Requer a Onda 1 concluída." },
] as const

// ─── Label de status ─────────────────────────────────────────────────────────
const STATUS_LABELS: Record<string, string> = {
    aguardando_arquivo:   "Aguardando arquivo",
    aguardando_aprovacao: "Recebemos seus arquivos. Aguardando aprovação da equipe Imedto para iniciar a análise.",
    aguardando_mapa:      "Arquivo recebido — aguardando mapeamento",
    mapa_em_revisao:      "Mapa em revisão pelo administrador",
    preview_pronto:       "Preview pronto — aguardando aprovação",
    migrando:             "Importação em andamento...",
    concluido:            "Importação concluída",
    concluido_com_erros:  "Importação concluída com avisos",
    desfeito:             "Importação desfeita",
    rejeitado:            "Rejeitado",
}

function labelStatus(status: string) {
    return STATUS_LABELS[status] ?? status
}

// ─── Seleção de arquivo ───────────────────────────────────────────────────────
function abrirSeletor() {
    inputRef.value?.click()
}

function aoSelecionarArquivo(evento: Event) {
    erroUpload.value = null
    const input = evento.target as HTMLInputElement
    const arquivo = input.files?.[0] ?? null
    if (!arquivo) return

    // Validação de formato — só ZIP aceito.
    if (!arquivo.name.toLowerCase().endsWith(".zip")) {
        erroUpload.value = "Apenas arquivos ZIP são aceitos."
        return
    }

    // Validação de tamanho (CA19) — trava no front antes de enviar.
    if (arquivo.size > LIMITE_UPLOAD_BYTES) {
        erroUpload.value = MENSAGEM_LIMITE
        return
    }

    arquivoSelecionado.value = arquivo
}

function formatarTamanho(bytes: number): string {
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

// ─── Upload ───────────────────────────────────────────────────────────────────
async function enviar() {
    if (!arquivoSelecionado.value || !termoAceito.value) return
    enviando.value = true
    erroUpload.value = null
    try {
        const resultado = await migracaoService.iniciarUpload(
            props.estabelecimentoId,
            arquivoSelecionado.value,
            undefined,
            ondaSelecionada.value || undefined
        )
        jobCriado.value = resultado
        arquivoSelecionado.value = null
        termoAceito.value = false
    } catch (e: any) {
        erroUpload.value = e?.response?.data?.mensagem ?? e?.message ?? "Erro ao enviar o arquivo."
    } finally {
        enviando.value = false
    }
}

function iniciarNovaMigracao() {
    jobCriado.value = null
    erroUpload.value = null
}
</script>

<template>
    <div class="migracao-tab">
        <!-- ── Sucesso: job criado ───────────────────────────────────────── -->
        <div v-if="jobCriado" class="job-criado">
            <div class="job-status-icon">
                <i class="fa-solid fa-circle-check" />
            </div>
            <h3 class="ds-card-title">Arquivo recebido com sucesso</h3>
            <p class="job-status-desc">
                Seu arquivo foi enviado e está na fila de processamento.
                Nossa equipe irá revisar o mapeamento e iniciar a importação em breve.
            </p>
            <div class="job-info">
                <span class="job-info-label">Status</span>
                <span class="job-info-valor">{{ labelStatus(jobCriado.status) }}</span>
            </div>
            <div class="job-info">
                <span class="job-info-label">ID do job</span>
                <span class="job-info-valor">#{{ jobCriado.jobId }}</span>
            </div>
            <p class="job-aviso">
                <i class="fa-solid fa-info-circle" />
                O arquivo original será mantido por 30 dias por motivo de auditoria e, após esse período, apagado automaticamente.
            </p>
            <AppButton variante="secondary" @click="iniciarNovaMigracao">
                Enviar outro arquivo
            </AppButton>
        </div>

        <!-- ── Formulário de upload ──────────────────────────────────────── -->
        <div v-else>
            <header class="secao-head">
                <h2 class="ds-section-title">Migrar meus dados</h2>
                <p class="secao-head-sub">
                    Importe seus dados de outros sistemas (iClinic, Feegow, Clinicorp, planilhas…)
                    para o Imedto. Envie um arquivo ZIP com seus dados exportados e nossa equipe
                    cuidará do mapeamento e da importação.
                </p>
            </header>

            <!-- Instruções -->
            <div class="card info-card">
                <h3 class="ds-card-title">Como funciona</h3>
                <ol class="instrucoes-lista">
                    <li>Exporte seus dados do sistema atual em formato CSV, XLSX ou JSON</li>
                    <li>Compacte todos os arquivos em um único ZIP (máximo 50MB)</li>
                    <li>Envie o arquivo abaixo — nosso time revisará o mapeamento</li>
                    <li>Após aprovação, os dados serão importados automaticamente</li>
                </ol>
                <p class="instrucoes-obs">
                    <i class="fa-solid fa-shield-halved" />
                    Seus dados são criptografados no envio e mantidos por 30 dias para fins de auditoria.
                </p>
            </div>

            <!-- Seleção de onda (CA13) -->
            <div class="card upload-card">
                <h3 class="ds-card-title">Tipo de importação</h3>
                <div class="onda-opcoes" role="radiogroup" aria-label="Tipo de importação">
                    <label
                        v-for="op in OPCOES_ONDA"
                        :key="op.valor"
                        class="onda-opcao"
                        :class="{ 'onda-opcao--ativa': ondaSelecionada === op.valor }"
                    >
                        <input
                            v-model="ondaSelecionada"
                            type="radio"
                            :value="op.valor"
                            :disabled="enviando"
                            class="visually-hidden"
                        />
                        <span class="onda-opcao-rotulo">{{ op.rotulo }}</span>
                        <span class="onda-opcao-desc">{{ op.descricao }}</span>
                    </label>
                </div>
            </div>

            <!-- Área de upload -->
            <div class="card upload-card">
                <input
                    ref="inputRef"
                    type="file"
                    accept=".zip,application/zip,application/x-zip-compressed"
                    class="visually-hidden"
                    @change="aoSelecionarArquivo"
                />

                <!-- Dropzone / botão de seleção -->
                <div
                    class="dropzone"
                    :class="{ 'dropzone--selecionado': !!arquivoSelecionado }"
                    role="button"
                    tabindex="0"
                    @click="abrirSeletor"
                    @keydown.enter.prevent="abrirSeletor"
                    @keydown.space.prevent="abrirSeletor"
                >
                    <template v-if="!arquivoSelecionado">
                        <i class="fa-solid fa-file-zipper dropzone-icone" />
                        <p class="dropzone-titulo">Clique para selecionar o arquivo ZIP</p>
                        <p class="dropzone-sub">Máximo 50MB &bull; CSV, XLSX ou JSON dentro do ZIP</p>
                    </template>
                    <template v-else>
                        <i class="fa-solid fa-file-check dropzone-icone dropzone-icone--ok" />
                        <p class="dropzone-titulo">{{ arquivoSelecionado.name }}</p>
                        <p class="dropzone-sub">{{ formatarTamanho(arquivoSelecionado.size) }} &bull; Clique para trocar</p>
                    </template>
                </div>

                <p v-if="erroUpload" class="erro-upload">
                    <i class="fa-solid fa-triangle-exclamation" />
                    {{ erroUpload }}
                </p>

                <!-- Termo de responsabilidade (R12) -->
                <label class="termo-label">
                    <input
                        v-model="termoAceito"
                        type="checkbox"
                        class="termo-checkbox"
                        :disabled="enviando"
                    />
                    <span>
                        Confirmo que tenho autorização para transferir os dados contidos neste arquivo
                        e que eles foram obtidos de forma lícita, em conformidade com a LGPD
                        (Lei 13.709/2018) e o CFM 1.821/07.
                    </span>
                </label>

                <!-- Botão de envio -->
                <div class="upload-footer">
                    <AppButton
                        :disabled="!arquivoSelecionado || !termoAceito || enviando"
                        :loading="enviando"
                        @click="enviar"
                    >
                        {{ enviando ? "Enviando arquivo..." : "Enviar arquivo para migração" }}
                    </AppButton>
                    <p v-if="enviando" class="upload-progresso-hint">
                        Aguarde — arquivos grandes podem levar alguns segundos.
                    </p>
                </div>
            </div>
        </div>
    </div>
</template>

<style scoped>
.migracao-tab {
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
}

/* ── Estado de sucesso ────────────────────────────────────────────────────── */
.job-criado {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
    background: hsl(var(--card));
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius);
    padding: 2rem 1.5rem;
    align-items: flex-start;
}

.job-status-icon {
    font-size: var(--text-3xl);
    color: hsl(var(--success, 142 76% 36%));
}

.job-status-desc {
    font-size: var(--text-sm);
    color: hsl(var(--muted-foreground));
    margin: 0;
}

.job-info {
    display: flex;
    gap: 0.5rem;
    font-size: var(--text-sm);
}

.job-info-label {
    font-weight: var(--font-weight-semibold);
    color: hsl(var(--muted-foreground));
    min-width: 80px;
}

.job-info-valor {
    color: hsl(var(--foreground));
}

.job-aviso {
    font-size: var(--text-xs);
    color: hsl(var(--muted-foreground));
    display: flex;
    align-items: center;
    gap: 0.375rem;
    margin: 0;
    border-top: 1px solid hsl(var(--border));
    padding-top: 0.75rem;
}

/* ── Card de instruções ───────────────────────────────────────────────────── */
.info-card {
    padding: 1.25rem 1.5rem;
}

.instrucoes-lista {
    margin: 0.75rem 0;
    padding-left: 1.25rem;
    display: flex;
    flex-direction: column;
    gap: 0.375rem;
}

.instrucoes-lista li {
    font-size: var(--text-sm);
    color: hsl(var(--foreground) / 0.85);
}

.instrucoes-obs {
    font-size: var(--text-xs);
    color: hsl(var(--muted-foreground));
    display: flex;
    align-items: center;
    gap: 0.375rem;
    margin: 0.75rem 0 0;
}

/* ── Card de upload ───────────────────────────────────────────────────────── */
.upload-card {
    padding: 1.25rem 1.5rem;
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.visually-hidden {
    position: absolute;
    width: 1px;
    height: 1px;
    overflow: hidden;
    clip: rect(0 0 0 0);
    white-space: nowrap;
}

.dropzone {
    border: 2px dashed hsl(var(--border));
    border-radius: calc(var(--radius) - 2px);
    padding: 2rem 1.5rem;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.5rem;
    cursor: pointer;
    transition: border-color 0.15s, background 0.15s;
    text-align: center;
}

.dropzone:hover,
.dropzone:focus-visible {
    border-color: hsl(var(--primary));
    background: hsl(var(--primary) / 0.04);
    outline: none;
}

.dropzone--selecionado {
    border-color: hsl(var(--primary) / 0.6);
    background: hsl(var(--primary) / 0.03);
}

.dropzone-icone {
    font-size: var(--text-3xl);
    color: hsl(var(--muted-foreground));
}

.dropzone-icone--ok {
    color: hsl(var(--primary));
}

.dropzone-titulo {
    font-size: var(--text-sm);
    font-weight: var(--font-weight-semibold);
    color: hsl(var(--foreground));
    margin: 0;
}

.dropzone-sub {
    font-size: var(--text-xs);
    color: hsl(var(--muted-foreground));
    margin: 0;
}

.erro-upload {
    font-size: var(--text-sm);
    color: hsl(var(--destructive));
    display: flex;
    align-items: center;
    gap: 0.375rem;
    margin: 0;
}

/* ── Termo de responsabilidade ────────────────────────────────────────────── */
.termo-label {
    display: flex;
    gap: 0.625rem;
    align-items: flex-start;
    font-size: var(--text-sm);
    color: hsl(var(--foreground) / 0.85);
    cursor: pointer;
    line-height: 1.5;
}

.termo-checkbox {
    margin-top: 3px;
    flex-shrink: 0;
    accent-color: hsl(var(--primary));
}

/* ── Rodapé do upload ─────────────────────────────────────────────────────── */
.upload-footer {
    display: flex;
    flex-direction: column;
    gap: 0.375rem;
}

.upload-progresso-hint {
    font-size: var(--text-xs);
    color: hsl(var(--muted-foreground));
    margin: 0;
}

/* ── Seletor de onda (CA13) ───────────────────────────────────────────────── */
.onda-opcoes {
    display: flex;
    flex-direction: column;
    gap: 0.625rem;
    margin-top: 0.75rem;
}

.onda-opcao {
    display: flex;
    flex-direction: column;
    gap: 0.125rem;
    padding: 0.875rem 1rem;
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius);
    cursor: pointer;
    transition: border-color 0.15s, background 0.15s;
}

.onda-opcao:hover {
    border-color: hsl(var(--primary) / 0.5);
    background: hsl(var(--primary) / 0.03);
}

.onda-opcao--ativa {
    border-color: hsl(var(--primary));
    background: hsl(var(--primary) / 0.04);
}

.onda-opcao-rotulo {
    font-size: var(--text-sm);
    font-weight: var(--font-weight-semibold);
    color: hsl(var(--foreground));
}

.onda-opcao-desc {
    font-size: var(--text-xs);
    color: hsl(var(--muted-foreground));
}
</style>
