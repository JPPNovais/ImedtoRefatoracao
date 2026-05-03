<script setup lang="ts">
/**
 * AppTopBar — header global com gradient roxo (estilo design Anthropic).
 *
 * Estrutura:
 *   - slot "brand" (logo / título — esquerda)
 *   - slot "context" (info do tenant — centro/esquerda)
 *   - slot default (ações específicas)
 *   - dropdown de notificações (auto)
 *   - dropdown de perfil do usuário (auto)
 *
 * Notificações e perfil são opcionais via props/slots.
 */
import { ref, onMounted, onBeforeUnmount } from "vue"

defineProps<{
    nomeUsuario?: string
    subtituloUsuario?: string
    inicialUsuario?: string
    fotoUrl?: string | null
    contadorNotificacoes?: number
}>()

defineEmits<{
    (e: "abrir-notificacoes"): void
    (e: "abrir-perfil"): void
    (e: "logout"): void
}>()

const aberto = ref<"notif" | "perfil" | null>(null)
const wrapNotif = ref<HTMLElement | null>(null)
const wrapPerfil = ref<HTMLElement | null>(null)

function fecharFora(ev: MouseEvent) {
    const t = ev.target as Node
    if (!wrapNotif.value?.contains(t) && !wrapPerfil.value?.contains(t)) {
        aberto.value = null
    }
}

onMounted(() => document.addEventListener("click", fecharFora))
onBeforeUnmount(() => document.removeEventListener("click", fecharFora))
</script>

<template>
    <header class="topbar">
        <div class="brand">
            <slot name="brand" />
        </div>

        <div class="context">
            <slot name="context" />
        </div>

        <div class="actions">
            <slot />

            <div ref="wrapNotif" class="pop-wrap">
                <button
                    type="button"
                    class="tb-btn"
                    :class="{ active: aberto === 'notif' }"
                    title="Notificações"
                    @click.stop="aberto = aberto === 'notif' ? null : 'notif'"
                >
                    <i class="fa-solid fa-bell" aria-hidden="true"></i>
                    <span v-if="contadorNotificacoes && contadorNotificacoes > 0" class="tb-badge">
                        {{ contadorNotificacoes > 9 ? '9+' : contadorNotificacoes }}
                    </span>
                </button>
                <div v-if="aberto === 'notif'" class="pop">
                    <slot name="notificacoes" :fechar="() => aberto = null">
                        <div class="pop-vazio">Sem novas notificações.</div>
                    </slot>
                </div>
            </div>

            <div class="divider"></div>

            <div ref="wrapPerfil" class="pop-wrap">
                <button
                    type="button"
                    class="tb-profile"
                    :class="{ active: aberto === 'perfil' }"
                    @click.stop="aberto = aberto === 'perfil' ? null : 'perfil'"
                >
                    <div class="av">
                        <img v-if="fotoUrl" :src="fotoUrl" :alt="nomeUsuario || 'Avatar'" />
                        <template v-else>{{ inicialUsuario || '?' }}</template>
                    </div>
                    <div class="who">
                        <b>{{ nomeUsuario || 'Usuário' }}</b>
                        <span v-if="subtituloUsuario">{{ subtituloUsuario }}</span>
                    </div>
                    <i class="fa-solid fa-chevron-down chev" aria-hidden="true"></i>
                </button>
                <div v-if="aberto === 'perfil'" class="pop pop-profile">
                    <slot name="perfil" :fechar="() => aberto = null" />
                </div>
            </div>
        </div>
    </header>
</template>

<style scoped>
.topbar {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    z-index: 50;
    display: flex;
    align-items: center;
    gap: 14px;
    height: var(--topbar-h, 64px);
    padding: 0 28px;
    background: linear-gradient(
        90deg,
        hsl(var(--primary-dark, 254 56% 21%)) 0%,
        hsl(var(--primary, 254 56% 38%)) 100%
    );
    border-bottom: 1px solid hsl(var(--primary-dark, 254 56% 21%) / 0.6);
    box-shadow: 0 4px 14px hsl(var(--primary-dark, 254 56% 21%) / 0.18);
    color: white;
}

.brand {
    display: flex;
    align-items: center;
    flex-shrink: 0;
}

.context {
    flex: 0 1 auto;
    color: white;
    font-size: 13px;
    font-weight: 600;
    opacity: 0.9;
    display: flex;
    align-items: center;
    gap: 8px;
}

.actions {
    display: flex;
    align-items: center;
    gap: 6px;
    margin-left: auto;
}

.tb-btn {
    position: relative;
    width: 38px;
    height: 38px;
    border: none;
    background: transparent;
    border-radius: 6px;
    color: hsl(0 0% 100% / 0.85);
    font-size: 15px;
    cursor: pointer;
    transition: background 0.15s, color 0.15s;
    display: flex;
    align-items: center;
    justify-content: center;
    font-family: inherit;
}
.tb-btn:hover, .tb-btn.active {
    background: hsl(0 0% 100% / 0.14);
    color: white;
}
.tb-badge {
    position: absolute;
    top: 6px;
    right: 6px;
    min-width: 16px;
    height: 16px;
    padding: 0 4px;
    background: hsl(45 96% 47%);
    color: white;
    border: 2px solid hsl(var(--primary, 254 56% 38%));
    border-radius: 8px;
    font-size: 9px;
    font-weight: 700;
    line-height: 12px;
    display: flex;
    align-items: center;
    justify-content: center;
}

.divider {
    width: 1px;
    height: 24px;
    background: hsl(0 0% 100% / 0.2);
    margin: 0 4px;
}

.tb-profile {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 4px 10px 4px 4px;
    height: 42px;
    background: transparent;
    border: 1px solid transparent;
    border-radius: 6px;
    cursor: pointer;
    transition: background 0.15s, border 0.15s;
    font-family: inherit;
    color: white;
}
.tb-profile:hover, .tb-profile.active {
    background: hsl(0 0% 100% / 0.12);
    border-color: hsl(0 0% 100% / 0.18);
}
.tb-profile .av {
    width: 32px;
    height: 32px;
    border-radius: 50%;
    background: white;
    color: hsl(var(--primary-dark));
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 12px;
    font-weight: 700;
    overflow: hidden;
}
.tb-profile .av img { width: 100%; height: 100%; object-fit: cover; }
.tb-profile .who { line-height: 1.2; text-align: left; }
.tb-profile .who b {
    display: block;
    font-size: 12px;
    font-weight: 600;
    color: white;
    max-width: 160px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}
.tb-profile .who span {
    display: block;
    font-size: 10px;
    color: hsl(0 0% 100% / 0.7);
    max-width: 160px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}
.chev { font-size: 10px; color: hsl(0 0% 100% / 0.6); }

.pop-wrap { position: relative; }
.pop {
    position: absolute;
    top: calc(100% + 6px);
    right: 0;
    width: 320px;
    background: hsl(var(--popover));
    border: 1px solid hsl(var(--border));
    border-radius: 8px;
    box-shadow: 0 10px 40px hsl(0 0% 0% / 0.25);
    overflow: hidden;
    z-index: 50;
    color: hsl(var(--popover-foreground));
    animation: pop-in 0.15s ease-out;
}
.pop-profile { width: 280px; }
.pop-vazio {
    padding: 1.5rem;
    text-align: center;
    color: hsl(var(--muted-foreground));
    font-size: 13px;
}

@keyframes pop-in {
    from { opacity: 0; transform: translateY(-4px); }
    to { opacity: 1; transform: translateY(0); }
}

@media (max-width: 768px) {
    .topbar { padding: 0 14px; }
    .context { display: none; }
    .tb-profile .who { display: none; }
    .chev { display: none; }
}
</style>
