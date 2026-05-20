<script setup lang="ts">
import { computed, ref, watch } from "vue"
import {
    AppModal, AppButton, AppBadge, AppSearchInput, AppFilterPills,
} from "@/components/ui"
import { useTenantStore } from "@/stores/tenantStore"
import { useAuthStore } from "@/stores/authStore"
import { useProfissionalStore } from "@/stores/profissionalStore"
import { useDebouncedRef } from "@/composables/useDebouncedRef"
import { termoModeloService, type TermoModeloDto } from "@/services/termoModeloService"
import { pacienteTermoService } from "@/services/pacienteTermoService"
import { profissionalService } from "@/services/profissionalService"
import { estabelecimentoService, type Estabelecimento } from "@/services/estabelecimentoService"
import { resolverVariaveis, type ContextoResolucaoTermo } from "@/utils/termoResolverVariaveis"
import { CATEGORIAS_TERMO } from "@/constants/termoVariaveis"
import type { Paciente } from "@/services/pacienteService"
import type { CategoriaTermo } from "@/services/termoModeloService"

/**
 * Wizard de 3 passos pra emitir um termo de consentimento ao paciente:
 *   1. Selecionar modelo (cards filtráveis por categoria + busca)
 *   2. Preview com variáveis resolvidas client-side
 *   3. Forma de assinatura (PDF físico recomendado; aceite por link na Fase 4)
 *
 * Após confirmação, chama `POST /api/pacientes/{id}/termos` e emite evento
 * `emitido` para o pai (que tipicamente baixa o PDF para impressão e atualiza
 * a lista).
 *
 * Carregamento de modelos é lazy: só dispara ao abrir o modal. Lista de
 * estabelecimento/profissional usa stores já hidratadas (sem custo extra).
 */
const props = defineProps<{
    aberto:   boolean
    paciente: Paciente | null
}>()

const emit = defineEmits<{
    (e: "update:aberto", v: boolean): void
    (e: "fechar"): void
    /** Disparado depois da emissão bem-sucedida. Recebe o id do termo. */
    (e: "emitido", payload: { termoEmitidoId: number; modeloTitulo: string }): void
}>()

// ─── Estado do wizard ──────────────────────────────────────────────────────
const passo = ref<1 | 2 | 3>(1)
const emitindo = ref(false)
const erro = ref<string | null>(null)

// Passo 1: lista de modelos
const carregandoModelos = ref(false)
const modelos = ref<TermoModeloDto[]>([])
const filtroBuscaInput = ref("")
const filtroBusca = useDebouncedRef(filtroBuscaInput, 250)
const filtroCategoria = ref<CategoriaTermo | "todas">("todas")
const modeloSelecionado = ref<TermoModeloDto | null>(null)

// Passo 2: contexto pro preview
const carregandoContexto = ref(false)
const estabelecimentoAtivo = ref<Estabelecimento | null>(null)
const profissionalAtivo = ref<{ nome: string | null; conselho: string | null; uf: string | null; numeroRegistro: string | null; especialidade: string | null } | null>(null)

const tipoAssinatura = ref<"pdf_anexado" | "aceite_link">("pdf_anexado")

// ─── Reset ao abrir/fechar ────────────────────────────────────────────────
watch(() => props.aberto, (aberto) => {
    if (aberto) {
        passo.value = 1
        erro.value = null
        modeloSelecionado.value = null
        tipoAssinatura.value = "pdf_anexado"
        filtroBuscaInput.value = ""
        filtroCategoria.value = "todas"
        carregarModelos()
    }
}, { immediate: true })

// ─── Passo 1 — modelos ─────────────────────────────────────────────────────
async function carregarModelos() {
    carregandoModelos.value = true
    try {
        const r = await termoModeloService.listarModelos({
            somenteAtivos: true,
            incluirPadroes: true,
            tamanho: 100, // a lista é pequena no MVP; 1 página
        })
        modelos.value = r.itens
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Não foi possível carregar os modelos."
    } finally {
        carregandoModelos.value = false
    }
}

const modelosFiltrados = computed(() => {
    const busca = filtroBusca.value.trim().toLowerCase()
    return modelos.value.filter(m => {
        if (filtroCategoria.value !== "todas" && m.categoria !== filtroCategoria.value) return false
        if (busca && !m.titulo.toLowerCase().includes(busca)) return false
        return true
    })
})

const opcoesCategoria = computed(() => [
    { valor: "todas" as const, label: "Todas" },
    ...CATEGORIAS_TERMO.map(c => ({ valor: c.chave, label: c.label })),
])

function selecionar(m: TermoModeloDto) {
    modeloSelecionado.value = m
}

function previewLabel(m: TermoModeloDto): string {
    const tmp = document.createElement("div")
    tmp.innerHTML = m.conteudoHtml || ""
    return (tmp.textContent || "").trim().slice(0, 180)
}

function corCategoria(cat: string): "default" | "success" | "warning" | "error" | "info" | "muted" {
    return CATEGORIAS_TERMO.find(c => c.chave === cat)?.cor ?? "muted"
}

function labelCategoria(cat: string): string {
    return CATEGORIAS_TERMO.find(c => c.chave === cat)?.label ?? cat
}

// ─── Passo 2 — Preview com variáveis resolvidas ────────────────────────────
const contexto = computed<ContextoResolucaoTermo | null>(() => {
    if (!props.paciente) return null
    return {
        paciente: {
            nomeCompleto: props.paciente.nomeCompleto,
            cpf: props.paciente.cpf,
            documentoInternacional: props.paciente.documentoInternacional,
            dataNascimento: props.paciente.dataNascimento,
            telefone: props.paciente.telefone,
            email: props.paciente.email,
            endereco: props.paciente.endereco,
            genero: props.paciente.genero,
        },
        estabelecimento: estabelecimentoAtivo.value ? {
            nomeFantasia: estabelecimentoAtivo.value.nomeFantasia,
            razaoSocial: estabelecimentoAtivo.value.razaoSocial,
            cnpj: estabelecimentoAtivo.value.cnpj,
            telefone: estabelecimentoAtivo.value.telefone,
            endereco: estabelecimentoAtivo.value.endereco,
            cidade: estabelecimentoAtivo.value.cidade,
        } : null,
        profissional: profissionalAtivo.value,
    }
})

const previewResolvido = computed(() => {
    if (!modeloSelecionado.value || !contexto.value) return null
    return resolverVariaveis(modeloSelecionado.value.conteudoHtml || "", contexto.value)
})

const nomePacienteVazio = computed(() => {
    const nome = props.paciente?.nomeCompleto?.trim() ?? ""
    return !nome
})

// ─── Carregamento de contexto ao avançar do passo 1 → 2 ────────────────────
async function carregarContexto() {
    carregandoContexto.value = true
    try {
        const [perfil, ests] = await Promise.all([
            profissionalService.obterMeu().catch(() => null),
            estabelecimentoService.listarMeus().catch(() => [] as Estabelecimento[]),
        ])
        const tenant = useTenantStore()
        estabelecimentoAtivo.value = ests.find(e => e.id === tenant.estabelecimentoAtivoId) ?? null

        if (perfil) {
            const auth = useAuthStore()
            profissionalAtivo.value = {
                nome: auth.usuario?.nomeCompleto ?? null,
                conselho: perfil.conselho,
                uf: perfil.uf,
                numeroRegistro: perfil.numeroRegistro,
                especialidade: perfil.especialidade,
            }
        } else {
            const auth = useAuthStore()
            profissionalAtivo.value = {
                nome: auth.usuario?.nomeCompleto ?? null,
                conselho: null, uf: null, numeroRegistro: null, especialidade: null,
            }
        }
    } finally {
        carregandoContexto.value = false
    }
}

// ─── Navegação ─────────────────────────────────────────────────────────────
async function avancar() {
    erro.value = null
    if (passo.value === 1) {
        if (!modeloSelecionado.value) return
        await carregarContexto()
        passo.value = 2
    } else if (passo.value === 2) {
        passo.value = 3
    }
}

function voltar() {
    erro.value = null
    if (passo.value > 1) passo.value = (passo.value - 1) as 1 | 2 | 3
}

function fechar() {
    if (emitindo.value) return
    emit("update:aberto", false)
    emit("fechar")
}

// ─── Confirmar emissão ─────────────────────────────────────────────────────
async function emitir() {
    if (!props.paciente || !modeloSelecionado.value) return
    if (nomePacienteVazio.value) {
        erro.value = "Paciente sem nome — não é possível emitir o termo."
        return
    }
    emitindo.value = true
    erro.value = null
    try {
        const r = await pacienteTermoService.emitir(props.paciente.id, {
            modeloId: modeloSelecionado.value.id,
            assinaturaTipo: tipoAssinatura.value,
        })
        emit("emitido", {
            termoEmitidoId: r.termoEmitidoId,
            modeloTitulo: modeloSelecionado.value.titulo,
        })
        emit("update:aberto", false)
        emit("fechar")
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao emitir termo."
    } finally {
        emitindo.value = false
    }
}

// ─── Ferramentas auxiliares (necessárias para o profissionalStore) ─────────
// Garante que o store esteja populado se a tela for aberta antes do bootstrap.
const _profStore = useProfissionalStore()
if (!_profStore.carregado) {
    void _profStore.init()
}
</script>

<template>
    <AppModal :aberto="aberto" titulo="Emitir termo de consentimento" largura="lg" @fechar="fechar">
        <!-- Stepper -->
        <div class="et-stepper">
            <div class="et-step" :class="{ ativo: passo === 1, concluido: passo > 1 }">
                <span class="num">1</span>Modelo
            </div>
            <div class="et-step" :class="{ ativo: passo === 2, concluido: passo > 2 }">
                <span class="num">2</span>Preview
            </div>
            <div class="et-step" :class="{ ativo: passo === 3 }">
                <span class="num">3</span>Confirmar
            </div>
        </div>

        <!-- Passo 1: Modelos -->
        <section v-if="passo === 1" class="et-pane">
            <div class="et-toolbar">
                <AppSearchInput v-model="filtroBuscaInput" placeholder="Buscar modelo pelo título…" />
                <AppFilterPills v-model="filtroCategoria" :opcoes="opcoesCategoria" />
            </div>

            <p v-if="carregandoModelos" class="msg">Carregando modelos…</p>

            <p v-else-if="modelosFiltrados.length === 0" class="msg">
                Nenhum modelo encontrado.
                <span v-if="modelos.length === 0">
                    Vá em <b>Configurações &rsaquo; Termos de Consentimento</b> para criar um modelo.
                </span>
            </p>

            <ul v-else class="et-modelos">
                <li
                    v-for="m in modelosFiltrados"
                    :key="m.id"
                    class="et-modelo"
                    :class="{ ativo: modeloSelecionado?.id === m.id }"
                    @click="selecionar(m)"
                >
                    <div class="et-modelo-head">
                        <h4>{{ m.titulo }}</h4>
                        <AppBadge :variant="corCategoria(m.categoria)" :label="labelCategoria(m.categoria)" />
                        <span v-if="m.ehPadraoDoSistema" class="et-tag-padrao">Padrão Imedto</span>
                    </div>
                    <p class="et-modelo-preview">{{ previewLabel(m) }}</p>
                </li>
            </ul>
        </section>

        <!-- Passo 2: Preview -->
        <section v-else-if="passo === 2" class="et-pane et-preview">
            <p v-if="carregandoContexto" class="msg">Preparando preview…</p>
            <template v-else>
                <div v-if="nomePacienteVazio" class="et-warn">
                    <i class="fa-solid fa-triangle-exclamation"></i>
                    Paciente sem nome — preencha antes de continuar.
                </div>
                <div class="et-preview-grid">
                    <article class="et-html" v-html="previewResolvido?.htmlResolvido"></article>
                    <aside class="et-sidebar">
                        <h5>Variáveis aplicadas</h5>
                        <ul v-if="previewResolvido?.variaveisAplicadas.length">
                            <li
                                v-for="v in previewResolvido!.variaveisAplicadas"
                                :key="v.chave"
                                :class="{ fallback: v.fallback }"
                                :title="v.fallback ? 'Dado ausente no cadastro — preencher antes da assinatura' : ''"
                            >
                                <code>{{ v.chave }}</code>
                                <span>{{ v.valor || "(vazio)" }}</span>
                            </li>
                        </ul>
                        <p v-else class="msg-mini">Este modelo não usa variáveis.</p>
                    </aside>
                </div>
            </template>
        </section>

        <!-- Passo 3: Forma de assinatura -->
        <section v-else class="et-pane et-confirmar">
            <h4>Como o paciente vai assinar?</h4>
            <div class="et-tipos">
                <label
                    class="et-tipo"
                    :class="{ ativo: tipoAssinatura === 'pdf_anexado' }"
                >
                    <input v-model="tipoAssinatura" type="radio" value="pdf_anexado" />
                    <div class="et-tipo-body">
                        <div class="et-tipo-head">
                            <i class="fa-solid fa-print"></i>
                            <b>Gerar PDF para assinatura física</b>
                            <span class="et-rec">Recomendado</span>
                        </div>
                        <p>Você imprime, o paciente assina e depois você anexa o PDF assinado pelo botão na lista.</p>
                    </div>
                </label>

                <label class="et-tipo desabilitado" title="Disponível na próxima atualização">
                    <input type="radio" disabled />
                    <div class="et-tipo-body">
                        <div class="et-tipo-head">
                            <i class="fa-solid fa-link"></i>
                            <b>Enviar link de aceite por e-mail</b>
                            <span class="et-rec em-breve">Em breve</span>
                        </div>
                        <p>O paciente recebe um link e aceita digitalmente. Disponível na próxima atualização.</p>
                    </div>
                </label>
            </div>

            <div v-if="modeloSelecionado" class="et-resumo">
                <h5>Resumo da emissão</h5>
                <dl>
                    <div>
                        <dt>Paciente</dt>
                        <dd>{{ paciente?.nomeCompleto }}</dd>
                    </div>
                    <div>
                        <dt>Modelo</dt>
                        <dd>{{ modeloSelecionado.titulo }} (v{{ modeloSelecionado.versaoAtual }})</dd>
                    </div>
                    <div>
                        <dt>Categoria</dt>
                        <dd>{{ labelCategoria(modeloSelecionado.categoria) }}</dd>
                    </div>
                </dl>
            </div>
        </section>

        <p v-if="erro" class="msg-erro">{{ erro }}</p>

        <template #rodape>
            <AppButton variant="secondary" :disabled="emitindo" @click="fechar">Cancelar</AppButton>
            <AppButton v-if="passo > 1" variant="secondary" :disabled="emitindo" @click="voltar">Voltar</AppButton>
            <AppButton
                v-if="passo < 3"
                :disabled="(passo === 1 && !modeloSelecionado) || carregandoModelos || carregandoContexto || nomePacienteVazio"
                @click="avancar"
            >
                {{ passo === 1 ? "Avançar para preview" : "Avançar" }}
            </AppButton>
            <AppButton
                v-else
                icon="fa-solid fa-file-signature"
                :loading="emitindo"
                :disabled="nomePacienteVazio"
                @click="emitir"
            >
                Emitir termo
            </AppButton>
        </template>
    </AppModal>
</template>

<style scoped>
.et-stepper {
    display: flex; gap: 8px; align-items: center;
    margin-bottom: 16px; padding-bottom: 12px;
    border-bottom: 1px solid hsl(var(--secondary) / 0.08);
}
.et-step {
    flex: 1;
    display: flex; align-items: center; gap: 6px;
    font-size: 12px; font-weight: 600;
    color: hsl(var(--secondary) / 0.5);
}
.et-step .num {
    display: inline-flex; align-items: center; justify-content: center;
    width: 22px; height: 22px; border-radius: 50%;
    background: hsl(var(--secondary) / 0.12);
    color: hsl(var(--secondary) / 0.55);
    font-size: 11px; font-weight: 700;
}
.et-step.ativo { color: hsl(var(--primary)); }
.et-step.ativo .num { background: hsl(var(--primary)); color: white; }
.et-step.concluido { color: hsl(var(--success)); }
.et-step.concluido .num { background: hsl(var(--success) / 0.18); color: hsl(var(--success)); }

.et-pane { display: flex; flex-direction: column; gap: 12px; }

.et-toolbar { display: flex; gap: 12px; flex-wrap: wrap; align-items: center; }

.et-modelos {
    list-style: none; padding: 0; margin: 0;
    display: grid; grid-template-columns: 1fr; gap: 8px;
    max-height: 380px; overflow-y: auto;
}
.et-modelo {
    border: 1px solid hsl(var(--secondary) / 0.12);
    border-radius: 8px;
    padding: 12px 14px;
    cursor: pointer;
    transition: border 120ms, background 120ms;
}
.et-modelo:hover { border-color: hsl(var(--primary) / 0.45); background: hsl(var(--primary) / 0.03); }
.et-modelo.ativo { border-color: hsl(var(--primary)); background: hsl(var(--primary) / 0.06); }
.et-modelo-head {
    display: flex; align-items: center; gap: 8px; margin-bottom: 4px; flex-wrap: wrap;
}
.et-modelo-head h4 { margin: 0; font-size: 14px; color: hsl(var(--primary-dark)); flex: 1; min-width: 200px; }
.et-modelo-preview {
    margin: 0; font-size: 12.5px; color: hsl(var(--secondary) / 0.7); line-height: 1.5;
    display: -webkit-box; -webkit-box-orient: vertical; -webkit-line-clamp: 2; overflow: hidden;
}
.et-tag-padrao {
    font-size: 10.5px; font-weight: 600;
    background: hsl(var(--info) / 0.12); color: hsl(var(--info));
    padding: 2px 7px; border-radius: 999px;
}

.et-preview-grid {
    display: grid; grid-template-columns: 1.6fr 1fr; gap: 16px;
    max-height: 460px; min-height: 320px;
}
.et-html {
    overflow-y: auto;
    padding: 16px;
    border: 1px solid hsl(var(--secondary) / 0.12);
    border-radius: 8px;
    background: white;
    font-size: 13.5px; line-height: 1.6;
    color: hsl(var(--foreground));
}
.et-html :deep(p) { margin: 0 0 8px; }
.et-html :deep(h1) { font-size: 17px; color: hsl(var(--primary-dark)); margin: 12px 0 6px; }
.et-html :deep(h2) { font-size: 15px; color: hsl(var(--primary-dark)); margin: 12px 0 6px; }
.et-html :deep(h3) { font-size: 13px; color: hsl(var(--primary-dark)); margin: 10px 0 6px; }
.et-html :deep(ul), .et-html :deep(ol) { margin: 0 0 8px 20px; }
.et-html :deep(strong) { font-weight: 700; }
.et-html :deep(em) { font-style: italic; }

.et-sidebar {
    overflow-y: auto;
    padding: 12px 14px;
    background: hsl(var(--muted) / 0.4);
    border: 1px solid hsl(var(--secondary) / 0.08);
    border-radius: 8px;
}
.et-sidebar h5 {
    margin: 0 0 8px;
    font-size: 11.5px; text-transform: uppercase; letter-spacing: 0.04em;
    color: hsl(var(--secondary)); font-weight: 700;
}
.et-sidebar ul { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: 6px; }
.et-sidebar li {
    font-size: 12px;
    display: flex; flex-direction: column; gap: 1px;
    padding: 6px 8px; border-radius: 6px;
    background: white; border: 1px solid hsl(var(--secondary) / 0.08);
}
.et-sidebar li code {
    font-size: 10.5px; color: hsl(var(--secondary) / 0.75);
    font-family: ui-monospace, monospace;
}
.et-sidebar li span { font-weight: 600; color: hsl(var(--foreground)); word-break: break-word; }
.et-sidebar li.fallback {
    background: hsl(var(--warning) / 0.08);
    border-color: hsl(var(--warning) / 0.4);
}
.et-sidebar li.fallback::before {
    content: "⚠";
    font-size: 11px;
    color: hsl(var(--warning));
    align-self: flex-end;
    margin-bottom: -16px;
}

.et-confirmar h4 { margin: 0 0 8px; font-size: 14px; font-weight: 700; }
.et-tipos { display: flex; flex-direction: column; gap: 10px; margin-bottom: 16px; }
.et-tipo {
    display: flex; align-items: flex-start; gap: 10px;
    padding: 12px 14px;
    border: 1px solid hsl(var(--secondary) / 0.16);
    border-radius: 8px;
    cursor: pointer;
    transition: border 120ms, background 120ms;
}
.et-tipo:hover { border-color: hsl(var(--primary) / 0.45); }
.et-tipo.ativo { border-color: hsl(var(--primary)); background: hsl(var(--primary) / 0.04); }
.et-tipo.desabilitado { opacity: 0.55; cursor: not-allowed; }
.et-tipo input[type="radio"] { margin-top: 3px; }
.et-tipo-body { flex: 1; }
.et-tipo-head { display: flex; align-items: center; gap: 8px; margin-bottom: 2px; flex-wrap: wrap; }
.et-tipo-head i { color: hsl(var(--primary)); }
.et-tipo-head b { font-size: 14px; }
.et-tipo p { margin: 0; font-size: 12.5px; color: hsl(var(--secondary) / 0.75); }
.et-rec {
    background: hsl(var(--success) / 0.15); color: hsl(var(--success));
    font-size: 10.5px; font-weight: 700; padding: 2px 7px; border-radius: 999px;
}
.et-rec.em-breve { background: hsl(var(--secondary) / 0.15); color: hsl(var(--secondary)); }

.et-resumo {
    background: hsl(var(--muted) / 0.5);
    border: 1px solid hsl(var(--secondary) / 0.08);
    border-radius: 8px;
    padding: 12px 14px;
}
.et-resumo h5 {
    margin: 0 0 8px; font-size: 11.5px; text-transform: uppercase;
    letter-spacing: 0.04em; color: hsl(var(--secondary)); font-weight: 700;
}
.et-resumo dl { margin: 0; display: grid; grid-template-columns: repeat(2, 1fr); gap: 8px 14px; }
.et-resumo dt { font-size: 10.5px; color: hsl(var(--secondary) / 0.65); text-transform: uppercase; font-weight: 700; }
.et-resumo dd { margin: 0; font-size: 13px; }

.et-warn {
    background: hsl(var(--warning) / 0.1);
    border: 1px solid hsl(var(--warning) / 0.35);
    color: hsl(var(--warning));
    border-radius: 8px;
    padding: 10px 14px;
    font-size: 13px; font-weight: 600;
    display: flex; align-items: center; gap: 8px;
}

.msg { font-size: 13.5px; color: hsl(var(--secondary) / 0.7); margin: 0; }
.msg-mini { font-size: 12px; color: hsl(var(--secondary) / 0.55); margin: 0; }
.msg-erro {
    color: hsl(var(--error));
    background: hsl(var(--error) / 0.08);
    border: 1px solid hsl(var(--error) / 0.25);
    padding: 8px 12px; border-radius: 6px;
    font-size: 13px; font-weight: 600;
    margin-top: 8px;
}

@media (max-width: 720px) {
    .et-preview-grid { grid-template-columns: 1fr; min-height: unset; max-height: 60vh; }
}
</style>
