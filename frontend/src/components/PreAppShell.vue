<!--
    PreAppShell — layout padronizado para telas pré-tenant (login, onboarding,
    criação/seleção de estabelecimento). Replica a identidade do LoginView:
    coluna esquerda com card e logo, coluna direita com hero gradiente.

    Props:
      titulo / subtitulo  — texto do cabeçalho (centralizado dentro do card)
      heroVariant         — gradiente da coluna direita
      mostrarSair         — exibe botão "Sair" no canto superior direito do card

    Slots:
      default  — corpo do card (form / lista / etc.)
      footer   — texto opcional abaixo do card (ex.: links auxiliares)
-->
<script setup lang="ts">
import { useRouter } from "vue-router"
import { useAuthStore } from "@/stores/authStore"
import logo from "@/assets/imedto-logo.png"
import { AppButton } from "@/components/ui"

withDefaults(defineProps<{
    titulo:       string
    subtitulo?:   string
    heroVariant?: "primary" | "verde"
    mostrarSair?: boolean
}>(), {
    heroVariant: "primary",
    mostrarSair: true,
})

const router = useRouter()
const auth = useAuthStore()

async function sair() {
    await auth.logout()
    router.push({ name: "Login" })
}
</script>

<template>
    <main class="pre-app">
        <section class="form-col">
            <div class="card">
                <header class="topo">
                    <img :src="logo" alt="Imedto" class="logo" />
                    <AppButton v-if="mostrarSair" variant="ghost" size="sm" icon="fa-solid fa-arrow-right-from-bracket" @click="sair">
                        Sair
                    </AppButton>
                </header>

                <div class="cabecalho">
                    <h1>{{ titulo }}</h1>
                    <p v-if="subtitulo">{{ subtitulo }}</p>
                </div>

                <div class="conteudo">
                    <slot />
                </div>

                <p v-if="$slots.footer" class="rodape">
                    <slot name="footer" />
                </p>
            </div>
        </section>

        <section class="hero-col" :class="`hero-col--${heroVariant}`"></section>
    </main>
</template>

<style scoped>
.pre-app {
    display: grid;
    grid-template-columns: 1fr 1fr;
    min-height: 100vh;
    background: hsl(var(--primary-light));
}

.form-col {
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 2.5rem 1.5rem;
    overflow-y: auto;
}

.card {
    width: 100%;
    max-width: 460px;
    background: hsl(var(--card));
    border: 1px solid hsl(var(--border));
    border-radius: 0.75rem;
    box-shadow: var(--shadow-md);
    padding: 1.75rem 1.75rem 1.5rem;
}

.topo {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 1.25rem;
}
.logo { height: 32px; }

.cabecalho {
    text-align: center;
    margin-bottom: 1.5rem;
}
.cabecalho h1 {
    font-size: 1.4rem;
    font-weight: 700;
    color: hsl(var(--primary-dark));
    margin: 0 0 0.4rem;
}
.cabecalho p {
    font-size: 0.875rem;
    color: hsl(var(--muted-foreground));
    margin: 0;
    line-height: 1.45;
}

.conteudo {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.rodape {
    margin: 1.25rem 0 0;
    text-align: center;
    font-size: 0.85rem;
    color: hsl(var(--muted-foreground));
}

/* Hero (coluna direita) — replicada do LoginView para manter identidade */
.hero-col { display: none; }
.hero-col--primary {
    background: linear-gradient(135deg,
        rgba(69, 43, 151, 0.92) 0%,
        rgba(36, 21, 84, 0.88) 50%,
        rgba(69, 43, 151, 0.92) 100%
    );
}
.hero-col--verde {
    background: linear-gradient(135deg,
        rgba(16, 185, 129, 0.88) 0%,
        rgba(14, 165, 233, 0.85) 50%,
        rgba(69, 43, 151, 0.88) 100%
    );
}

@media (min-width: 768px) {
    .hero-col { display: block; }
}
@media (max-width: 767px) {
    .pre-app { grid-template-columns: 1fr; }
    .card { box-shadow: none; border: none; }
}
</style>
