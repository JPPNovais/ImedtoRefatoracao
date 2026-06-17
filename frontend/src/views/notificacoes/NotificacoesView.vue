<script setup lang="ts">
import { ref, watch, onMounted } from "vue"
import { useRouter } from "vue-router"
import { notificacaoService, type Notificacao } from "@/services/notificacaoService"
import { useNotificacoesStore } from "@/stores/notificacoesStore"
import {
    AppPageHeader, AppButton, AppPagination, AppEmptyState,
} from "@/components/ui"

const router = useRouter()
const store = useNotificacoesStore()

const pagina = ref(1)
const tamanho = ref(10)
const total = ref(0)
const itens = ref<Notificacao[]>([])
const carregando = ref(false)
const filtraSoNaoLidas = ref(false)

async function carregar() {
    carregando.value = true
    try {
        const resultado = await notificacaoService.listar({
            lidas: filtraSoNaoLidas.value ? false : undefined,
            pagina: pagina.value,
            tamanho: tamanho.value,
        })
        itens.value = resultado.itens
        total.value = resultado.total
    } finally {
        carregando.value = false
    }
}

watch(filtraSoNaoLidas, () => { pagina.value = 1 })
watch([filtraSoNaoLidas, pagina, tamanho], carregar)

async function marcarLida(id: number) {
    await store.marcarComoLida(id)
    const item = itens.value.find((n) => n.id === id)
    if (item) { item.lida = true }
}

async function marcarTodas() {
    await store.marcarTodasLidas()
    itens.value.forEach((n) => { n.lida = true })
}

async function abrirNotificacao(n: Notificacao) {
    if (!n.lida) await marcarLida(n.id)
    if (n.linkAcao) router.push(n.linkAcao)
}

function formatarData(iso: string) {
    return new Date(iso).toLocaleString("pt-BR", {
        day: "2-digit", month: "2-digit", year: "numeric",
        hour: "2-digit", minute: "2-digit",
    })
}

function iconeCategoria(categoria: string) {
    switch (categoria) {
        case "Convite":    return "fa-solid fa-envelope-open-text"
        case "Agenda":     return "fa-solid fa-calendar-days"
        case "Financeiro": return "fa-solid fa-coins"
        case "Automacao":  return "fa-solid fa-bolt"
        case "Estoque":    return "fa-solid fa-boxes-stacked"
        default:           return "fa-solid fa-bell"
    }
}

onMounted(carregar)
</script>

<template>
    <main class="app-page">
        <AppPageHeader titulo="Notificações" subtitulo="Acompanhe avisos do sistema e do seu estabelecimento.">
            <template #acoes>
                <AppButton
                    v-if="store.naoLidas > 0"
                    variant="secondary"
                    icon="fa-solid fa-check-double"
                    @click="marcarTodas"
                >
                    Marcar todas como lidas
                </AppButton>
            </template>
        </AppPageHeader>

        <!-- Filtro -->
        <div class="filtro-bar">
            <label class="filtro-toggle">
                <input
                    v-model="filtraSoNaoLidas"
                    type="checkbox"
                    role="switch"
                    :aria-checked="filtraSoNaoLidas"
                />
                <span>Mostrar apenas não lidas</span>
                <span v-if="store.naoLidas > 0" class="badge-nao-lidas">{{ store.naoLidas }}</span>
            </label>
        </div>

        <div v-if="carregando" class="carregando" aria-busy="true" aria-live="polite">
            <i class="fa-solid fa-spinner fa-spin" aria-hidden="true"></i>
            Carregando...
        </div>

        <AppEmptyState
            v-else-if="itens.length === 0"
            icone="fa-solid fa-bell-slash"
            titulo="Nenhuma notificação"
            :descricao="filtraSoNaoLidas ? 'Voce nao tem notificacoes nao lidas.' : 'Nenhum aviso por enquanto.'"
        />

        <div v-else class="lista">
            <div
                v-for="n in itens"
                :key="n.id"
                class="item"
                :class="{ 'item--nao-lida': !n.lida, 'item--clicavel': !!n.linkAcao }"
                role="article"
                @click="abrirNotificacao(n)"
            >
                <div class="item-icone">
                    <i :class="iconeCategoria(String(n.categoria))" aria-hidden="true"></i>
                </div>

                <div class="item-corpo">
                    <div class="item-topo">
                        <strong class="item-titulo">{{ n.titulo }}</strong>
                        <span class="item-data">{{ formatarData(n.criadaEm) }}</span>
                    </div>
                    <p class="item-mensagem">{{ n.mensagem }}</p>
                    <div class="item-meta">
                        <span class="tag-categoria">{{ n.categoria }}</span>
                        <span v-if="n.lida" class="tag-lida">Lida</span>
                    </div>
                </div>

                <div class="item-acoes">
                    <button
                        v-if="!n.lida"
                        class="btn-marcar"
                        title="Marcar como lida"
                        @click.stop="marcarLida(n.id)"
                        aria-label="Marcar como lida"
                    >
                        <i class="fa-solid fa-check" aria-hidden="true"></i>
                    </button>
                    <i v-if="n.linkAcao" class="fa-solid fa-arrow-right seta" aria-hidden="true"></i>
                </div>
            </div>
        </div>

        <AppPagination
            v-if="total > 0"
            v-model:pagina="pagina"
            v-model:tamanho="tamanho"
            :total="total"
            rotulo-itens="notificações"
        />
    </main>
</template>

<style scoped>
.filtro-bar {
    margin-bottom: 1rem;
}

.filtro-toggle {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.88em;
    cursor: pointer;
    user-select: none;
}
.filtro-toggle input { cursor: pointer; }

.badge-nao-lidas {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    min-width: 20px;
    height: 20px;
    padding: 0 5px;
    border-radius: 999px;
    background: hsl(0 70% 45%);
    color: #fff;
    font-size: 0.7em;
    font-weight: 700;
}

.carregando {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    color: hsl(var(--muted-foreground));
    padding: 2rem 0;
    font-size: 0.9em;
}

.lista {
    display: flex;
    flex-direction: column;
    gap: 0;
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius);
    overflow: hidden;
    margin-bottom: 1rem;
}

.item {
    display: flex;
    align-items: flex-start;
    gap: 1rem;
    padding: 1rem 1.25rem;
    border-bottom: 1px solid hsl(var(--border));
    background: hsl(var(--card));
    transition: background 0.12s;
}
.item:last-child { border-bottom: none; }
.item--nao-lida { background: hsl(var(--primary) / 0.04); }
.item--clicavel { cursor: pointer; }
.item--clicavel:hover { background: hsl(var(--muted) / 0.5); }

.item-icone {
    width: 36px;
    height: 36px;
    border-radius: 50%;
    background: hsl(var(--accent));
    color: hsl(var(--primary));
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
    font-size: 0.9em;
}

.item-corpo {
    flex: 1;
    min-width: 0;
    display: flex;
    flex-direction: column;
    gap: 0.3rem;
}

.item-topo {
    display: flex;
    align-items: baseline;
    gap: 0.75rem;
    flex-wrap: wrap;
}

.item-titulo {
    font-size: 0.9em;
    font-weight: 600;
    color: hsl(var(--foreground));
}

.item-data {
    font-size: 0.75em;
    color: hsl(var(--muted-foreground));
    margin-left: auto;
    flex-shrink: 0;
}

.item-mensagem {
    margin: 0;
    font-size: 0.85em;
    color: hsl(var(--muted-foreground));
    line-height: 1.5;
}

.item-meta {
    display: flex;
    gap: 0.4rem;
    flex-wrap: wrap;
}

.tag-categoria, .tag-lida {
    display: inline-block;
    padding: 0.1rem 0.5rem;
    border-radius: 999px;
    font-size: 0.7em;
    font-weight: 600;
}
.tag-categoria {
    background: hsl(var(--muted));
    color: hsl(var(--muted-foreground));
}
.tag-lida {
    background: hsl(var(--success) / 0.15);
    color: hsl(var(--success));
}

.item-acoes {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    flex-shrink: 0;
}

.btn-marcar {
    width: 30px;
    height: 30px;
    border: 1px solid hsl(var(--border));
    background: transparent;
    border-radius: var(--radius-sm);
    color: hsl(var(--muted-foreground));
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 0.85em;
    transition: all 0.12s;
}
.btn-marcar:hover {
    background: hsl(var(--success) / 0.1);
    color: hsl(var(--success));
    border-color: hsl(var(--success) / 0.3);
}

.seta {
    color: hsl(var(--muted-foreground));
    font-size: 0.8em;
}
</style>
