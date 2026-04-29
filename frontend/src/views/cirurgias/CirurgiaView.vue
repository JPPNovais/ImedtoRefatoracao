<script setup lang="ts">
/**
 * CirurgiaView — detalhes de um procedimento cirúrgico.
 * Rota: /pacientes/:pacienteId/cirurgias/:id
 * Tabs: Resumo | Equipe | Descricao | Anestesia | Pos-op
 * Conforme ADR 03A: tabs, não steps (cirurgião volta nas seções em momentos diferentes).
 */
import { ref, computed, watch, onMounted } from "vue"
import { useRoute } from "vue-router"
import {
    cirurgiaService,
    PAPEIS_CIRURGIA,
    type ProcedimentoCirurgico,
    type StatusProcedimento,
} from "@/services/cirurgiaService"
import {
    AppPageHeader, AppButton, AppBadge, AppTabs, AppField,
    AppInput, AppTextarea, AppModal, AppCard,
} from "@/components/ui"

const route = useRoute()

const id = Number(route.params.id)
const cirurgia = ref<ProcedimentoCirurgico | null>(null)
const carregando = ref(false)
const erro = ref<string | null>(null)
const abaAtiva = ref("resumo")
const salvando = ref(false)
const confirmandoCancelar = ref(false)
const cancelando = ref(false)
const motivo = ref("")
const confirmandoRealizar = ref(false)
const realizando = ref(false)
const dataRealizada = ref("")

// Campos editáveis locais (sincronizados ao carregar)
const descricaoCirurgica = ref("")
const anestesiaTecnica = ref("")
const anestesiaIntercorrencias = ref("")
const evolucaoPosOp = ref("")

const abas = [
    { valor: "resumo",    label: "Resumo",    icone: "fa-solid fa-clipboard" },
    { valor: "equipe",    label: "Equipe",    icone: "fa-solid fa-users" },
    { valor: "descricao", label: "Descricao", icone: "fa-solid fa-file-pen" },
    { valor: "anestesia", label: "Anestesia", icone: "fa-solid fa-syringe" },
    { valor: "pos-op",    label: "Pos-op",    icone: "fa-solid fa-heart-pulse" },
]

const statusBadge: Record<StatusProcedimento, { variant: "warning" | "info" | "success" | "error" | "muted"; label: string }> = {
    Planejado:  { variant: "warning", label: "Planejado" },
    Confirmado: { variant: "info",    label: "Confirmado" },
    Realizado:  { variant: "success", label: "Realizado" },
    Cancelado:  { variant: "error",   label: "Cancelado" },
}

const podeCancelar = computed(() =>
    cirurgia.value?.status !== "Cancelado" && cirurgia.value?.status !== "Realizado",
)

const podeConfirmar = computed(() => cirurgia.value?.status === "Planejado")
const podeRegistrar = computed(() => cirurgia.value?.status === "Confirmado")
const isReadonly = computed(() =>
    cirurgia.value?.status === "Cancelado" || cirurgia.value?.status === "Realizado",
)

// Sincroniza campos locais ao carregar/recarregar
watch(cirurgia, (c) => {
    if (!c) return
    descricaoCirurgica.value = c.descricaoCirurgica ?? ""
    anestesiaTecnica.value = (c.fichaAnestesica?.tecnica as string) ?? ""
    anestesiaIntercorrencias.value = (c.fichaAnestesica?.intercorrencias as string) ?? ""
    evolucaoPosOp.value = c.evolucaoPosOp ?? ""
})

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        cirurgia.value = await cirurgiaService.obter(id)
    } catch {
        erro.value = "Nao foi possivel carregar o procedimento."
    } finally {
        carregando.value = false
    }
}

async function confirmar() {
    salvando.value = true
    try {
        await cirurgiaService.confirmar(id)
        await carregar()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao confirmar."
    } finally {
        salvando.value = false
    }
}

async function salvarDescricao() {
    salvando.value = true
    try {
        await cirurgiaService.atualizar(id, { descricaoCirurgica: descricaoCirurgica.value })
        if (cirurgia.value) cirurgia.value.descricaoCirurgica = descricaoCirurgica.value
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao salvar descricao."
    } finally {
        salvando.value = false
    }
}

async function salvarAnestesia() {
    salvando.value = true
    try {
        await cirurgiaService.atualizar(id, {
            fichaAnestesica: {
                tecnica: anestesiaTecnica.value,
                intercorrencias: anestesiaIntercorrencias.value,
            },
        })
        if (cirurgia.value) {
            cirurgia.value.fichaAnestesica = {
                tecnica: anestesiaTecnica.value,
                intercorrencias: anestesiaIntercorrencias.value,
            }
        }
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao salvar ficha anestesica."
    } finally {
        salvando.value = false
    }
}

async function salvarPosOp() {
    salvando.value = true
    try {
        await cirurgiaService.atualizar(id, { evolucaoPosOp: evolucaoPosOp.value })
        if (cirurgia.value) cirurgia.value.evolucaoPosOp = evolucaoPosOp.value
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao salvar evolucao."
    } finally {
        salvando.value = false
    }
}

async function registrarRealizacao() {
    realizando.value = true
    try {
        await cirurgiaService.registrarRealizacao(id, {
            dataRealizada: dataRealizada.value,
            descricaoCirurgica: descricaoCirurgica.value || undefined,
            fichaAnestesica: (anestesiaTecnica.value || anestesiaIntercorrencias.value)
                ? { tecnica: anestesiaTecnica.value, intercorrencias: anestesiaIntercorrencias.value }
                : undefined,
            evolucaoPosOp: evolucaoPosOp.value || undefined,
        })
        confirmandoRealizar.value = false
        await carregar()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao registrar realizacao."
    } finally {
        realizando.value = false
    }
}

async function cancelar() {
    cancelando.value = true
    try {
        await cirurgiaService.cancelar(id, motivo.value || "Motivo nao informado.")
        confirmandoCancelar.value = false
        await carregar()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao cancelar."
    } finally {
        cancelando.value = false
    }
}

function formatarData(iso: string | null) {
    if (!iso) return "—"
    return new Date(iso).toLocaleDateString("pt-BR", {
        day: "2-digit", month: "2-digit", year: "numeric",
    })
}

onMounted(carregar)
</script>

<template>
    <main class="app-page app-page--wide">
        <div v-if="carregando" class="carregando">
            <i class="fa-solid fa-spinner fa-spin" aria-hidden="true"></i>
            Carregando...
        </div>

        <div v-else-if="erro && !cirurgia" class="erro-banner" role="alert">
            <i class="fa-solid fa-triangle-exclamation" aria-hidden="true"></i>
            {{ erro }}
            <AppButton variant="ghost" size="sm" @click="carregar">Tentar novamente</AppButton>
        </div>

        <template v-else-if="cirurgia">
            <AppPageHeader
                :titulo="cirurgia.cirurgiaPrincipal"
                subtitulo="Procedimento cirurgico"
            >
                <template #acoes>
                    <AppBadge
                        :variant="statusBadge[cirurgia.status].variant"
                        :label="statusBadge[cirurgia.status].label"
                    />
                    <AppButton
                        v-if="podeConfirmar"
                        variant="secondary"
                        icon="fa-solid fa-check"
                        :loading="salvando"
                        @click="confirmar"
                    >
                        Confirmar
                    </AppButton>
                    <AppButton
                        v-if="podeRegistrar"
                        icon="fa-solid fa-check-double"
                        :loading="salvando"
                        @click="confirmandoRealizar = true"
                    >
                        Registrar realizacao
                    </AppButton>
                    <AppButton
                        v-if="podeCancelar"
                        variant="danger"
                        icon="fa-solid fa-ban"
                        @click="confirmandoCancelar = true"
                    >
                        Cancelar
                    </AppButton>
                </template>
            </AppPageHeader>

            <div v-if="erro" class="erro-banner" role="alert">
                {{ erro }}
            </div>

            <!-- Tabs -->
            <AppTabs v-model="abaAtiva" :abas="abas" class="tabs-cirurgia" />

            <!-- Conteúdo das tabs -->
            <div class="tab-conteudo">
                <!-- RESUMO -->
                <div v-if="abaAtiva === 'resumo'">
                    <AppCard title="Informacoes gerais">
                        <div class="resumo-grid">
                            <div class="resumo-item">
                                <span class="resumo-label">Cirurgia</span>
                                <strong>{{ cirurgia.cirurgiaPrincipal }}</strong>
                            </div>
                            <div v-if="cirurgia.cirurgiaCodigoTuss" class="resumo-item">
                                <span class="resumo-label">Codigo TUSS</span>
                                <span>{{ cirurgia.cirurgiaCodigoTuss }}</span>
                            </div>
                            <div class="resumo-item">
                                <span class="resumo-label">Status</span>
                                <AppBadge :variant="statusBadge[cirurgia.status].variant" :label="statusBadge[cirurgia.status].label" />
                            </div>
                            <div class="resumo-item">
                                <span class="resumo-label">Data agendada</span>
                                <span>{{ formatarData(cirurgia.dataAgendada) }}</span>
                            </div>
                            <div class="resumo-item">
                                <span class="resumo-label">Data realizada</span>
                                <span>{{ formatarData(cirurgia.dataRealizada) }}</span>
                            </div>
                            <div v-if="cirurgia.observacoes" class="resumo-item resumo-item--full">
                                <span class="resumo-label">Observacoes</span>
                                <span>{{ cirurgia.observacoes }}</span>
                            </div>
                        </div>
                    </AppCard>
                </div>

                <!-- EQUIPE -->
                <div v-else-if="abaAtiva === 'equipe'">
                    <AppCard title="Equipe cirurgica">
                        <div v-if="!cirurgia.equipe || cirurgia.equipe.length === 0" class="vazio">
                            <i class="fa-solid fa-users-slash" aria-hidden="true"></i>
                            Nenhum membro de equipe registrado.
                        </div>
                        <table v-else class="tabela">
                            <thead>
                                <tr>
                                    <th>Profissional</th>
                                    <th>Papel</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr v-for="m in cirurgia.equipe" :key="m.id ?? m.profissionalUsuarioId">
                                    <td>{{ m.profissionalNome ?? m.profissionalUsuarioId }}</td>
                                    <td>{{ PAPEIS_CIRURGIA.find(p => p.valor === m.papel)?.label ?? m.papel }}</td>
                                </tr>
                            </tbody>
                        </table>
                    </AppCard>
                </div>

                <!-- DESCRICAO -->
                <div v-else-if="abaAtiva === 'descricao'">
                    <AppCard title="Descricao cirurgica">
                        <AppField label="Descricao do procedimento" for="desc-cir">
                            <AppTextarea
                                id="desc-cir"
                                v-model="descricaoCirurgica"
                                :rows="10"
                                placeholder="Descreva o procedimento realizado..."
                                :disabled="isReadonly"
                            />
                        </AppField>
                        <template v-if="!isReadonly">
                            <AppButton icon="fa-solid fa-save" :loading="salvando" @click="salvarDescricao">
                                Salvar descricao
                            </AppButton>
                        </template>
                    </AppCard>
                </div>

                <!-- ANESTESIA -->
                <div v-else-if="abaAtiva === 'anestesia'">
                    <AppCard title="Ficha anestesica">
                        <AppField label="Tecnica anestesica" for="anest-tec">
                            <AppInput
                                id="anest-tec"
                                v-model="anestesiaTecnica"
                                placeholder="Ex: Anestesia geral balanceada"
                                :disabled="isReadonly"
                            />
                        </AppField>
                        <AppField label="Intercorrencias" for="anest-interc">
                            <AppTextarea
                                id="anest-interc"
                                v-model="anestesiaIntercorrencias"
                                :rows="4"
                                placeholder="Descreva intercorrencias, se houver..."
                                :disabled="isReadonly"
                            />
                        </AppField>
                        <template v-if="!isReadonly">
                            <AppButton icon="fa-solid fa-save" :loading="salvando" @click="salvarAnestesia">
                                Salvar ficha
                            </AppButton>
                        </template>
                    </AppCard>
                </div>

                <!-- POS-OP -->
                <div v-else-if="abaAtiva === 'pos-op'">
                    <AppCard title="Evolucao pos-operatoria">
                        <AppField label="Evolucao" for="pos-op-ev">
                            <AppTextarea
                                id="pos-op-ev"
                                v-model="evolucaoPosOp"
                                :rows="8"
                                placeholder="Descricao da evolucao pos-operatoria..."
                                :disabled="isReadonly"
                            />
                        </AppField>
                        <template v-if="!isReadonly">
                            <AppButton icon="fa-solid fa-save" :loading="salvando" @click="salvarPosOp">
                                Salvar evolucao
                            </AppButton>
                        </template>
                    </AppCard>
                </div>
            </div>
        </template>

        <!-- Modal registrar realizacao -->
        <AppModal
            :aberto="confirmandoRealizar"
            titulo="Registrar realizacao"
            largura="sm"
            @fechar="confirmandoRealizar = false"
        >
            <AppField label="Data de realizacao" for="data-realizada">
                <AppInput id="data-realizada" v-model="dataRealizada" type="date" />
            </AppField>
            <template #rodape>
                <AppButton variant="secondary" @click="confirmandoRealizar = false">Cancelar</AppButton>
                <AppButton
                    icon="fa-solid fa-check-double"
                    :loading="realizando"
                    :disabled="!dataRealizada"
                    @click="registrarRealizacao"
                >
                    Confirmar realizacao
                </AppButton>
            </template>
        </AppModal>

        <!-- Modal cancelar -->
        <AppModal
            :aberto="confirmandoCancelar"
            titulo="Cancelar procedimento?"
            largura="sm"
            @fechar="confirmandoCancelar = false"
        >
            <AppField label="Motivo do cancelamento" for="motivo-cancel">
                <AppTextarea id="motivo-cancel" v-model="motivo" :rows="3" placeholder="Informe o motivo..." />
            </AppField>
            <template #rodape>
                <AppButton variant="secondary" @click="confirmandoCancelar = false">Voltar</AppButton>
                <AppButton variant="danger" :loading="cancelando" @click="cancelar">
                    Confirmar cancelamento
                </AppButton>
            </template>
        </AppModal>
    </main>
</template>

<style scoped>
.carregando {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    color: hsl(var(--muted-foreground));
    padding: 2rem 0;
    font-size: 0.9em;
}

.erro-banner {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.85rem 1rem;
    background: hsl(var(--destructive) / 0.08);
    border: 1px solid hsl(var(--destructive) / 0.2);
    border-radius: var(--radius);
    color: hsl(var(--destructive));
    font-size: 0.9em;
    margin-bottom: 1rem;
}

.tabs-cirurgia { margin-bottom: 1.25rem; }

.tab-conteudo { display: flex; flex-direction: column; gap: 1rem; }

.resumo-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 0.75rem;
}
.resumo-item {
    display: flex;
    flex-direction: column;
    gap: 0.2rem;
    padding: 0.5rem 0;
    border-bottom: 1px solid hsl(var(--border));
}
.resumo-item:last-child { border-bottom: none; }
.resumo-item--full { grid-column: span 2; }
.resumo-label { font-size: 0.78em; color: hsl(var(--muted-foreground)); text-transform: uppercase; letter-spacing: 0.03em; }

.tabela {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.9em;
}
.tabela th, .tabela td {
    padding: 0.65rem 0.85rem;
    text-align: left;
    border-bottom: 1px solid hsl(var(--border));
}
.tabela th {
    font-weight: 600;
    color: hsl(var(--muted-foreground));
    font-size: 0.8em;
    text-transform: uppercase;
    letter-spacing: 0.04em;
}

.vazio {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    color: hsl(var(--muted-foreground));
    font-size: 0.9em;
    padding: 1.5rem;
    text-align: center;
    justify-content: center;
}

.info-ficha {
    font-size: 0.85em;
    color: hsl(var(--muted-foreground));
    background: hsl(var(--muted) / 0.4);
    padding: 0.65rem 0.85rem;
    border-radius: var(--radius-sm);
    margin: 0 0 1rem;
}

@media (max-width: 768px) {
    .resumo-grid { grid-template-columns: 1fr; }
    .resumo-item--full { grid-column: span 1; }
}
</style>
