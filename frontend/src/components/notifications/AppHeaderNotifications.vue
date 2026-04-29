<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from "vue"
import { useRouter } from "vue-router"
import { useNotificacoesStore } from "@/stores/notificacoesStore"
import { AppButton, AppEmptyState } from "@/components/ui"

/**
 * Sino de notificações no header. Apresenta:
 *  - Badge com contador de não-lidas (limitado a "9+" por estética).
 *  - Dropdown com últimas 10 notificações.
 *  - Botão "marcar todas como lidas".
 *
 * Estado vem do Pinia (`notificacoesStore`) — duas fontes:
 *   1. Carga inicial via REST quando o sino monta.
 *   2. Push em tempo real via realtimeService (registrado pelo authStore após login).
 */
const router = useRouter()
const store = useNotificacoesStore()
const aberto = ref(false)
const sinoEl = ref<HTMLElement | null>(null)

const contadorTexto = computed(() => (store.naoLidas > 9 ? "9+" : String(store.naoLidas)))
const ultimas = computed(() => store.notificacoes.slice(0, 10))

function alternar() {
    aberto.value = !aberto.value
    if (aberto.value) {
        // Toda abertura recarrega leve para refletir o estado mais recente.
        store.carregar({ pagina: 1, tamanho: 20 })
    }
}

function fecharFora(ev: MouseEvent) {
    if (!sinoEl.value) return
    if (!sinoEl.value.contains(ev.target as Node)) aberto.value = false
}

async function abrirNotificacao(id: number, link: string | null) {
    await store.marcarComoLida(id)
    aberto.value = false
    if (link) router.push(link)
}

async function marcarTodas() {
    await store.marcarTodasLidas()
}

function formatarData(iso: string) {
    const d = new Date(iso)
    return d.toLocaleString("pt-BR", {
        day: "2-digit",
        month: "2-digit",
        hour: "2-digit",
        minute: "2-digit",
    })
}

function iconeCategoria(categoria: string) {
    switch (categoria) {
        case "Convite":    return "fa-solid fa-envelope-open-text"
        case "Agenda":     return "fa-solid fa-calendar-days"
        case "Financeiro": return "fa-solid fa-coins"
        case "Automacao":  return "fa-solid fa-bolt"
        default:           return "fa-solid fa-bell"
    }
}

onMounted(() => {
    store.atualizarContador()
    document.addEventListener("click", fecharFora)
})

onBeforeUnmount(() => {
    document.removeEventListener("click", fecharFora)
})
</script>

<template>
    <div ref="sinoEl" class="sino">
        <button
            type="button"
            class="sino-trigger"
            :aria-label="`Notificações${store.naoLidas ? ', ' + store.naoLidas + ' não lidas' : ''}`"
            :aria-expanded="aberto"
            @click="alternar"
        >
            <i class="fa-solid fa-bell" aria-hidden="true"></i>
            <span v-if="store.naoLidas > 0" class="badge">{{ contadorTexto }}</span>
        </button>

        <div v-if="aberto" class="dropdown" role="menu">
            <header class="dropdown-header">
                <span class="dropdown-titulo">Notificações</span>
                <div class="dropdown-header-acoes">
                    <AppButton
                        v-if="store.naoLidas > 0"
                        type="button"
                        variant="ghost"
                        size="sm"
                        @click="marcarTodas"
                    >
                        Marcar todas
                    </AppButton>
                    <router-link
                        :to="{ name: 'Notificacoes' }"
                        class="ver-todas"
                        @click="aberto = false"
                    >
                        Ver todas
                    </router-link>
                </div>
            </header>

            <div v-if="store.carregando && ultimas.length === 0" class="dropdown-loading">
                Carregando...
            </div>

            <ul v-else-if="ultimas.length > 0" class="dropdown-lista">
                <li
                    v-for="n in ultimas"
                    :key="n.id"
                    class="item"
                    :class="{ 'item--nao-lida': !n.lida }"
                    @click="abrirNotificacao(n.id, n.linkAcao)"
                >
                    <span class="item-icone">
                        <i :class="iconeCategoria(String(n.categoria))" aria-hidden="true"></i>
                    </span>
                    <div class="item-corpo">
                        <strong class="item-titulo">{{ n.titulo }}</strong>
                        <p class="item-mensagem">{{ n.mensagem }}</p>
                        <span class="item-data">{{ formatarData(n.criadaEm) }}</span>
                    </div>
                </li>
            </ul>

            <div v-else class="dropdown-vazio">
                <AppEmptyState
                    icone="bell"
                    titulo="Nenhuma notificação"
                    descricao="Você está em dia. Novos avisos aparecerão aqui."
                    compacto
                />
            </div>
        </div>
    </div>
</template>

<style scoped>
.sino { position: relative; }

.sino-trigger {
    position: relative;
    width: 36px;
    height: 36px;
    border-radius: 50%;
    border: none;
    background: transparent;
    color: var(--text);
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    transition: background 0.15s;
}
.sino-trigger:hover { background: var(--bg-muted); }

.badge {
    position: absolute;
    top: 2px;
    right: 2px;
    min-width: 16px;
    height: 16px;
    padding: 0 4px;
    border-radius: 999px;
    background: hsl(0 70% 45%);
    color: #fff;
    font-size: 0.65em;
    font-weight: 700;
    line-height: 16px;
    text-align: center;
}

.dropdown {
    position: absolute;
    top: calc(100% + 8px);
    right: 0;
    width: 360px;
    max-width: 90vw;
    max-height: 480px;
    overflow: hidden;
    display: flex;
    flex-direction: column;
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: 12px;
    box-shadow: 0 12px 32px rgba(0, 0, 0, 0.18);
    z-index: 200;
}

.dropdown-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 0.65rem 0.85rem;
    border-bottom: 1px solid var(--border);
}
.dropdown-titulo { font-weight: 600; font-size: 0.9em; }
.dropdown-header-acoes { display: flex; align-items: center; gap: 0.5rem; }
.ver-todas { font-size: 0.78em; color: hsl(var(--primary)); text-decoration: none; font-weight: 600; }
.ver-todas:hover { text-decoration: underline; }

.dropdown-lista {
    list-style: none;
    margin: 0;
    padding: 0;
    overflow-y: auto;
    flex: 1;
}
.dropdown-loading,
.dropdown-vazio {
    padding: 1rem;
    text-align: center;
    font-size: 0.85em;
    color: var(--text-muted);
}

.item {
    display: flex;
    gap: 0.6rem;
    padding: 0.65rem 0.85rem;
    border-bottom: 1px solid var(--border);
    cursor: pointer;
    transition: background 0.12s;
}
.item:last-child { border-bottom: none; }
.item:hover { background: var(--bg-muted); }
.item--nao-lida { background: hsl(220 90% 97%); }

.item-icone {
    width: 32px;
    height: 32px;
    border-radius: 50%;
    background: var(--bg-muted);
    color: var(--primary);
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
    font-size: 0.85em;
}

.item-corpo { min-width: 0; flex: 1; display: flex; flex-direction: column; gap: 2px; }
.item-titulo {
    font-size: 0.85em;
    font-weight: 600;
    color: var(--text);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}
.item-mensagem {
    margin: 0;
    font-size: 0.78em;
    color: var(--text-muted);
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
}
.item-data {
    font-size: 0.7em;
    color: var(--text-muted);
}
</style>
