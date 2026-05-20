<!--
    Aba "Consulta atual" — visual modular do design Imedto care:
      - Esquerda: navegação por módulos com ponto de status (preenchido/vazio).
      - Centro: cada seção renderizada como um "module card" (header com ícone,
        título, status pill + body com o componente da seção).
      - Direita: painel com modelo atual + atalhos para trocar de modelo +
        botão CTA "Salvar evolução".

    Não muda o backend nem a estrutura de dados (chave, tipo, valor) — apenas
    reorganiza visualmente o que já existia.
-->
<script setup lang="ts">
import { computed } from "vue"
import { AppButton } from "@/components/ui"
import SecaoProntuario from "@/components/prontuario/SecaoProntuario.vue"
import type { ModeloProntuario, SecaoModelo } from "@/services/prontuarioService"

const props = defineProps<{
    modeloId: number | null
    modelos: ModeloProntuario[]
    secoes: SecaoModelo[]
    novaEvolucao: Record<string, any>
    salvando: boolean
    /** Sexo do paciente — propagado ao SecaoExameFisico → BodyMap. */
    pacienteSexo?: string | null
}>()

const emit = defineEmits<{
    salvar: []
    "update:modeloId": [id: number]
}>()

// Heurística simples para mapear chaves conhecidas a ícones FontAwesome.
function iconePara(chave: string): string {
    if (chave === "qp" || chave === "queixa-principal") return "fa-circle-info"
    if (chave === "hda" || chave === "h-doenca-atual") return "fa-stethoscope"
    if (chave === "hpp") return "fa-clock-rotate-left"
    if (chave === "h-familiar") return "fa-people-group"
    if (chave === "h-social") return "fa-user-tag"
    if (chave === "exame-fisico" || chave === "exame") return "fa-person"
    if (chave === "exames-realizados") return "fa-flask"
    if (chave === "exames-solicitados") return "fa-vial"
    if (chave === "procedimentos-indicados") return "fa-syringe"
    if (chave === "cid" || chave === "cid-10") return "fa-tag"
    if (chave === "conduta" || chave === "plano") return "fa-clipboard-check"
    if (chave === "prescricao") return "fa-prescription"
    if (chave === "atestado") return "fa-file-signature"
    if (chave === "evolucao" || chave === "soap") return "fa-pen-to-square"
    return "fa-file-lines"
}

function temConteudo(valor: any): boolean {
    if (valor === null || valor === undefined) return false
    if (typeof valor === "string") return valor.trim().length > 0
    if (Array.isArray(valor)) return valor.length > 0
    if (typeof valor === "object") return Object.values(valor).some(v => temConteudo(v))
    if (typeof valor === "boolean") return valor === true
    return false
}

const statusSecao = computed(() => {
    const map: Record<string, "filled" | "empty"> = {}
    for (const s of props.secoes) {
        map[s.chave] = temConteudo(props.novaEvolucao[s.chave]) ? "filled" : "empty"
    }
    return map
})

const totalPreenchidos = computed(
    () => Object.values(statusSecao.value).filter(v => v === "filled").length,
)

function scrollToSecao(chave: string) {
    const el = document.getElementById(`mod-${chave}`)
    if (!el) return
    el.scrollIntoView({ behavior: "smooth", block: "start" })
    el.classList.add("mod-pulse")
    setTimeout(() => el.classList.remove("mod-pulse"), 1100)
}

const modeloAtual = computed(() => props.modelos.find(m => m.id === props.modeloId) ?? null)

const modelosAlternativos = computed(
    () => props.modelos.filter(m => m.id !== props.modeloId).slice(0, 6),
)
</script>

<template>
    <div class="pront-grid">
        <!-- ──── Sidebar esquerda: navegação por módulos ──── -->
        <aside class="pront-nav" aria-label="Módulos do prontuário">
            <div class="pn-head">
                <h4>Módulos</h4>
                <span>{{ totalPreenchidos }}/{{ secoes.length }} ok</span>
            </div>

            <div class="pn-group">
                <button
                    v-for="(secao, i) in secoes"
                    :key="secao.chave"
                    type="button"
                    class="pn-item"
                    :class="{ current: i === 0 }"
                    @click="scrollToSecao(secao.chave)"
                >
                    <i class="fa-solid pn-ic" :class="iconePara(secao.chave)"></i>
                    <span class="pn-lbl">{{ secao.titulo }}</span>
                    <span class="pn-dot" :class="`status-${statusSecao[secao.chave]}`"></span>
                </button>
            </div>
        </aside>

        <!-- ──── Main: módulos editáveis ──── -->
        <div class="pront-main">
            <div v-if="modeloAtual" class="pront-toolbar">
                <div class="tpl-current" :title="modeloAtual.descricao || ''">
                    <i class="fa-solid fa-stethoscope"></i>
                    <div>
                        <span class="tpl-lbl">Tipo de prontuário</span>
                        <strong>{{ modeloAtual.nome }}</strong>
                    </div>
                </div>
                <div class="pt-info">
                    <span>{{ secoes.length }} módulos</span>
                </div>
            </div>

            <div class="modules-list">
                <section
                    v-for="secao in secoes"
                    :key="secao.chave"
                    :id="`mod-${secao.chave}`"
                    class="module"
                >
                    <header class="module-head">
                        <div class="module-ic">
                            <i class="fa-solid" :class="iconePara(secao.chave)"></i>
                        </div>
                        <div class="module-title">
                            <h3>{{ secao.titulo }}</h3>
                        </div>
                        <div class="module-status">
                            <span
                                class="ms-pill"
                                :class="statusSecao[secao.chave] === 'filled' ? 'success' : 'neutral'"
                            >
                                <i
                                    class="fa-solid"
                                    :class="statusSecao[secao.chave] === 'filled' ? 'fa-check' : 'fa-circle'"
                                ></i>
                                {{ statusSecao[secao.chave] === 'filled' ? 'Preenchido' : 'Vazio' }}
                            </span>
                        </div>
                    </header>
                    <div class="module-body">
                        <SecaoProntuario
                            v-model="novaEvolucao[secao.chave]"
                            :chave="secao.chave"
                            :titulo="secao.titulo"
                            :tipo="secao.tipo"
                            :paciente-sexo="pacienteSexo"
                        />
                    </div>
                </section>

                <div class="acoes-rodape">
                    <AppButton
                        type="button"
                        size="lg"
                        icon="fa-solid fa-save"
                        :loading="salvando"
                        :disabled="salvando"
                        @click="emit('salvar')"
                    >
                        {{ salvando ? "Salvando..." : "Salvar evolução" }}
                    </AppButton>
                </div>
            </div>
        </div>

        <!-- ──── Sidebar direita: Modelos alternativos ──── -->
        <aside class="mod-lib" aria-label="Modelos disponíveis">
            <div class="mod-lib-head">
                <h4>Trocar modelo</h4>
                <span>{{ modelosAlternativos.length }} disponíveis</span>
            </div>

            <p v-if="modelosAlternativos.length === 0" class="mod-lib-empty">
                Nenhum outro modelo cadastrado.
            </p>
            <div class="mod-lib-list">
                <button
                    v-for="m in modelosAlternativos"
                    :key="m.id"
                    type="button"
                    class="mod-lib-item"
                    @click="emit('update:modeloId', m.id)"
                >
                    <i class="fa-solid fa-stethoscope"></i>
                    <div>
                        <b>{{ m.nome }}</b>
                        <span v-if="m.descricao">{{ m.descricao }}</span>
                        <span v-else-if="m.ehPadraoSistema">Modelo do sistema</span>
                    </div>
                    <i class="fa-solid fa-arrow-right-arrow-left"></i>
                </button>
            </div>
        </aside>
    </div>
</template>

<style scoped>
/* ──── Layout 3 colunas ──── */
.pront-grid {
    display: grid;
    grid-template-columns: 240px 1fr 280px;
    gap: 16px;
    align-items: start;
}
@media (max-width: 1200px) {
    .pront-grid { grid-template-columns: 220px 1fr; }
    .mod-lib { display: none; }
}
@media (max-width: 900px) {
    .pront-grid { grid-template-columns: 1fr; }
    .pront-nav { display: none; }
}

/* ──── Sidebar esquerda ──── */
.pront-nav {
    background: white;
    padding: 14px 8px;
    border: 1px solid hsl(var(--secondary) / 0.08);
    border-radius: var(--radius-lg);
    display: flex; flex-direction: column; gap: 14px;
    position: sticky;
    top: calc(var(--topbar-h, var(--top-h)) + 160px);
    max-height: calc(100vh - var(--topbar-h, 64px) - 180px);
    overflow-y: auto;
}
.pn-head { display: flex; align-items: baseline; justify-content: space-between; padding: 0 8px 4px; }
.pn-head h4 {
    font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.06em;
    color: hsl(var(--secondary) / 0.6); margin: 0;
}
.pn-head span { font-size: 11px; color: hsl(var(--secondary) / 0.5); }
.pn-group { display: flex; flex-direction: column; gap: 2px; }
.pn-item {
    display: flex; align-items: center; gap: 10px;
    width: 100%; padding: 8px 10px;
    border-radius: 8px;
    background: transparent; border: 0; cursor: pointer;
    text-align: left; font: inherit; font-size: 13px;
    color: hsl(var(--secondary) / 0.78);
    transition: all 150ms;
    border-left: 2px solid transparent;
}
.pn-item:hover { background: hsl(var(--primary) / 0.05); color: hsl(var(--primary-dark)); }
.pn-item.current {
    background: hsl(var(--primary) / 0.08);
    color: hsl(var(--primary));
    border-left-color: hsl(var(--primary));
    font-weight: 600;
}
.pn-ic { width: 16px; text-align: center; font-size: 12px; flex-shrink: 0; opacity: 0.8; }
.pn-item.current .pn-ic { opacity: 1; }
.pn-lbl { flex: 1; min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.pn-dot {
    width: 8px; height: 8px; border-radius: 50%;
    background: hsl(var(--secondary) / 0.18); flex-shrink: 0;
}
.pn-dot.status-filled { background: hsl(155 60% 50%); }

/* ──── Main: lista de módulos ──── */
.pront-main { width: 100%; min-width: 0; }

.pront-toolbar {
    display: flex; align-items: center; justify-content: space-between; gap: 16px;
    margin-bottom: 16px; flex-wrap: wrap;
}
.tpl-current {
    display: inline-flex; align-items: center; gap: 12px;
    padding: 10px 16px;
    background: white;
    border: 1px solid hsl(var(--secondary) / 0.14);
    border-radius: var(--radius-md);
    font: inherit;
}
.tpl-current > i:first-child {
    width: 32px; height: 32px; border-radius: 8px;
    background: hsl(var(--primary) / 0.12);
    color: hsl(var(--primary));
    display: inline-flex; align-items: center; justify-content: center;
}
.tpl-current > div { text-align: left; display: flex; flex-direction: column; }
.tpl-lbl {
    font-size: 10px;
    color: hsl(var(--secondary) / 0.5);
    text-transform: uppercase;
    letter-spacing: 0.05em;
}
.tpl-current strong {
    font-size: 14px;
    color: hsl(var(--primary-dark));
}
.pt-info { font-size: 12px; color: hsl(var(--secondary) / 0.65); }

.modules-list { display: flex; flex-direction: column; gap: 12px; }
.module {
    background: white;
    border-radius: var(--radius-lg);
    border: 1px solid hsl(var(--secondary) / 0.08);
    transition: box-shadow 150ms;
    scroll-margin-top: calc(var(--topbar-h, var(--top-h)) + 160px);
}
.module:hover { box-shadow: var(--shadow); }
@keyframes modPulse {
    0%   { box-shadow: 0 0 0 0 hsl(var(--primary) / 0.4); }
    60%  { box-shadow: 0 0 0 8px hsl(var(--primary) / 0); }
    100% { box-shadow: 0 0 0 0 hsl(var(--primary) / 0); }
}
.module.mod-pulse { animation: modPulse 1s ease-out; }
.module-head {
    display: flex; align-items: center; gap: 10px;
    padding: 12px 16px;
    border-bottom: 1px solid hsl(var(--secondary) / 0.06);
}
.module-ic {
    width: 32px; height: 32px; border-radius: 8px;
    background: hsl(var(--primary) / 0.1);
    color: hsl(var(--primary));
    display: inline-flex; align-items: center; justify-content: center;
    flex-shrink: 0;
}
.module-title { flex: 1; min-width: 0; }
.module-title h3 {
    margin: 0;
    font-size: 15px; font-weight: 700;
    color: hsl(var(--primary-dark));
}
.module-status .ms-pill {
    font-size: 11px; padding: 3px 8px; border-radius: 99px; font-weight: 600;
    display: inline-flex; align-items: center; gap: 4px;
}
.ms-pill.success { background: hsl(155 60% 50% / 0.14); color: hsl(155 60% 30%); }
.ms-pill.neutral { background: hsl(var(--secondary) / 0.08); color: hsl(var(--secondary) / 0.6); }
.ms-pill i { font-size: 8px; }
.module-body { padding: 16px; }

.acoes-rodape {
    display: flex; justify-content: flex-end;
    padding-top: 4px;
}

/* ──── Sidebar direita: modelos alternativos ──── */
.mod-lib {
    background: hsl(220 20% 98%);
    padding: 16px;
    border: 1px solid hsl(var(--secondary) / 0.06);
    border-radius: var(--radius-lg);
    display: flex; flex-direction: column; gap: 12px;
    position: sticky;
    top: calc(var(--topbar-h, var(--top-h)) + 160px);
    max-height: calc(100vh - var(--topbar-h, 64px) - 180px);
    overflow-y: auto;
}
.mod-lib-head { display: flex; align-items: baseline; justify-content: space-between; }
.mod-lib-head h4 {
    font-size: 12px; font-weight: 700;
    text-transform: uppercase; letter-spacing: 0.06em;
    color: hsl(var(--secondary) / 0.6);
    margin: 0;
}
.mod-lib-head span { font-size: 11px; color: hsl(var(--secondary) / 0.5); }
.mod-lib-empty { font-size: 12px; color: hsl(var(--secondary) / 0.5); margin: 6px 4px; }
.mod-lib-list { display: flex; flex-direction: column; gap: 8px; }
.mod-lib-item {
    display: flex; align-items: center; gap: 10px;
    padding: 10px 12px;
    background: white;
    border: 1px solid hsl(var(--secondary) / 0.08);
    border-radius: var(--radius-md);
    cursor: pointer;
    text-align: left; font: inherit;
    transition: all 150ms;
}
.mod-lib-item:hover {
    border-color: hsl(var(--primary));
    transform: translateX(-2px);
    box-shadow: -4px 0 12px hsl(var(--primary) / 0.1);
}
.mod-lib-item > i:first-child {
    width: 28px; height: 28px; border-radius: 6px;
    background: hsl(var(--primary) / 0.1);
    color: hsl(var(--primary));
    display: inline-flex; align-items: center; justify-content: center;
    flex-shrink: 0; font-size: 12px;
}
.mod-lib-item > div { flex: 1; min-width: 0; }
.mod-lib-item b {
    display: block; font-size: 12px;
    color: hsl(var(--primary-dark));
    font-weight: 600;
}
.mod-lib-item span {
    display: block; font-size: 11px;
    color: hsl(var(--secondary) / 0.6);
    line-height: 1.3;
    overflow: hidden; text-overflow: ellipsis;
    display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical;
}
.mod-lib-item > i:last-child { color: hsl(var(--primary) / 0.5); font-size: 11px; }
</style>
