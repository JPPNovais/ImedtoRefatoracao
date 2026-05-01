<script setup lang="ts">
/**
 * AppSidebar — sidebar colapsável global (estilo design Anthropic).
 *
 * Comportamento:
 *   - Default: collapsed (64px) com só ícones.
 *   - Hover: expande temporariamente (240px) com labels.
 *   - Pin: fixa expandida; deixa o conteúdo principal ajustado.
 *
 * Items são roteáveis (via `to`) ou clicáveis (via `@click`); active é detectado
 * automaticamente pelo route.name. Suporta footer via slot `footer`.
 */
import { ref, computed, watch, onMounted, onBeforeUnmount } from "vue"
import { useRoute } from "vue-router"

interface SidebarItem {
    name?: string                  // route.name para detectar active
    label: string
    icon: string                   // FontAwesome class (ex: "fa-solid fa-house")
    to?: { name: string } | string // route target
    badge?: number | string
    onClick?: () => void
    active?: boolean               // override manual
    danger?: boolean               // ex: "Sair"
}

const props = defineProps<{
    items: SidebarItem[]
    /** Mapa route.name → item.name para active virtual (ex: PacienteDetalhe → Pacientes) */
    activeMap?: Record<string, string>
    /** Callback quando o pin muda — útil pro AppLayout reservar espaço. */
    onPinnedChange?: (pinned: boolean) => void
}>()

const route = useRoute()
const pinned = ref(false)
const hovered = ref(false)

const expanded = computed(() => pinned.value || hovered.value)

const activeName = computed(() => {
    const r = route.name as string | undefined
    if (!r) return null
    return props.activeMap?.[r] ?? r
})

function isActive(item: SidebarItem) {
    if (item.active !== undefined) return item.active
    if (!item.name) return false
    return activeName.value === item.name
}

watch(pinned, (v) => {
    document.body.classList.toggle("has-pinned-sidebar", v)
    props.onPinnedChange?.(v)
})

onMounted(() => {
    document.body.classList.toggle("has-pinned-sidebar", pinned.value)
})
onBeforeUnmount(() => {
    document.body.classList.remove("has-pinned-sidebar")
})
</script>

<template>
    <aside
        class="side"
        :class="{ expanded, collapsed: !expanded, pinned }"
        @mouseenter="hovered = true"
        @mouseleave="hovered = false"
    >
        <button
            class="pin-btn"
            :title="pinned ? 'Desafixar menu' : 'Fixar menu'"
            @click="pinned = !pinned"
        >
            <i :class="['fa-solid', pinned ? 'fa-angles-left' : 'fa-angles-right']" aria-hidden="true"></i>
        </button>

        <nav class="nav">
            <template v-for="(item, idx) in items" :key="idx">
                <router-link
                    v-if="item.to"
                    :to="item.to"
                    :class="['item', { active: isActive(item), danger: item.danger }]"
                    :title="!expanded ? item.label : ''"
                >
                    <i :class="item.icon" aria-hidden="true"></i>
                    <span class="lbl">{{ item.label }}</span>
                    <span v-if="item.badge" class="nav-badge">{{ item.badge }}</span>
                </router-link>
                <button
                    v-else
                    type="button"
                    :class="['item', { active: isActive(item), danger: item.danger }]"
                    :title="!expanded ? item.label : ''"
                    @click="item.onClick?.()"
                >
                    <i :class="item.icon" aria-hidden="true"></i>
                    <span class="lbl">{{ item.label }}</span>
                    <span v-if="item.badge" class="nav-badge">{{ item.badge }}</span>
                </button>
            </template>
        </nav>

        <div v-if="$slots.footer" class="foot">
            <slot name="footer" :expanded="expanded" />
        </div>
    </aside>
</template>

<style scoped>
.side {
    position: fixed;
    top: var(--topbar-h, 64px);
    left: 0;
    height: calc(100vh - var(--topbar-h, 64px));
    background: hsl(var(--primary-light, 240 33% 99%));
    color: hsl(var(--primary-dark, 254 56% 21%));
    padding: 8px 10px;
    display: flex;
    flex-direction: column;
    gap: 8px;
    transition: width 220ms cubic-bezier(.2,.8,.2,1), box-shadow 220ms;
    z-index: 35;
    overflow: visible;
    border-right: 1px solid hsl(0 0% 0% / 0.08);
}
.side.collapsed { width: 64px; }
.side.expanded { width: 240px; }
.side.expanded:not(.pinned) {
    box-shadow: 6px 0 30px hsl(var(--primary-dark, 254 56% 21%) / 0.18);
}

.pin-btn {
    position: absolute;
    top: 14px;
    right: -14px;
    width: 28px;
    height: 28px;
    background: white;
    border: 1px solid hsl(0 0% 0% / 0.12);
    border-radius: 50%;
    color: hsl(0 0% 0% / 0.6);
    cursor: pointer;
    font-size: 11px;
    display: flex;
    align-items: center;
    justify-content: center;
    transition: background 0.15s, color 0.15s, opacity 0.15s, transform 0.15s;
    opacity: 0;
    box-shadow: 0 2px 8px hsl(var(--primary-dark, 254 56% 21%) / 0.12);
    z-index: 5;
    font-family: inherit;
}
.side:hover .pin-btn,
.side.expanded .pin-btn { opacity: 1; }
.pin-btn:hover {
    background: hsl(var(--primary, 254 56% 38%));
    color: white;
    transform: scale(1.08);
    border-color: hsl(var(--primary, 254 56% 38%));
}
.side.pinned .pin-btn {
    background: hsl(var(--primary, 254 56% 38%));
    color: white;
    border-color: hsl(var(--primary, 254 56% 38%));
}

.nav {
    display: flex;
    flex-direction: column;
    gap: 2px;
    flex: 1;
    margin-top: 30px;
    overflow-y: auto;
    overflow-x: hidden;
}
.nav::-webkit-scrollbar { width: 0; }

.item {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 9px 12px;
    border-radius: 8px;
    font-size: 13px;
    font-weight: 500;
    color: hsl(0 0% 0% / 0.78);
    cursor: pointer;
    user-select: none;
    text-decoration: none;
    transition: background 0.15s, color 0.15s;
    position: relative;
    white-space: nowrap;
    background: transparent;
    border: none;
    text-align: left;
    width: 100%;
    font-family: inherit;
}
.item i {
    width: 20px;
    text-align: center;
    font-size: 15px;
    flex-shrink: 0;
}
.item .lbl {
    flex: 1;
    opacity: 0;
    transition: opacity 160ms;
    pointer-events: none;
}
.side.expanded .item .lbl {
    opacity: 1;
    pointer-events: auto;
}
.item:hover {
    color: hsl(var(--primary-dark, 254 56% 21%));
    background: hsl(0 0% 0% / 0.06);
}
.item.active {
    background: hsl(var(--primary, 254 56% 38%) / 0.12);
    color: hsl(var(--primary-dark, 254 56% 21%));
    font-weight: 600;
}
.item.active i {
    color: hsl(var(--primary, 254 56% 38%));
}
.item.active::before {
    content: '';
    position: absolute;
    left: 0;
    top: 8px;
    bottom: 8px;
    width: 3px;
    background: hsl(var(--primary, 254 56% 38%));
    border-radius: 0 3px 3px 0;
}

.item.danger { color: hsl(0 70% 50%); }
.item.danger:hover { background: hsl(0 70% 50% / 0.1); color: hsl(0 70% 45%); }

.nav-badge {
    background: hsl(45 96% 47%);
    color: white;
    font-size: 10px;
    font-weight: 800;
    padding: 1px 6px;
    border-radius: 99px;
    min-width: 16px;
    text-align: center;
    opacity: 0;
    transition: opacity 160ms;
}
.side.expanded .nav-badge { opacity: 1; }

/* Mini bolha quando colapsada e há badge */
.side.collapsed .item:has(.nav-badge)::after {
    content: '';
    position: absolute;
    top: 6px;
    right: 8px;
    width: 7px;
    height: 7px;
    border-radius: 50%;
    background: hsl(45 96% 47%);
    border: 2px solid hsl(var(--primary-light, 240 33% 99%));
}

.foot {
    font-size: 12px;
    color: hsl(0 0% 0% / 0.7);
    border-top: 1px solid hsl(0 0% 0% / 0.1);
    padding-top: 10px;
    display: flex;
    flex-direction: column;
    gap: 2px;
}
</style>
