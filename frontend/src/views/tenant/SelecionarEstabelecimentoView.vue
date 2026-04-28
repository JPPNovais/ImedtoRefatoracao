<script setup lang="ts">
import { onMounted, ref } from "vue"
import { useRouter } from "vue-router"
import {
    estabelecimentoService,
    type Estabelecimento,
} from "@/services/estabelecimentoService"
import { useTenantStore } from "@/stores/tenantStore"
import PreAppShell from "@/components/PreAppShell.vue"

const router = useRouter()
const tenant = useTenantStore()

const estabelecimentos = ref<Estabelecimento[]>([])
const carregando = ref(true)
const erro = ref<string | null>(null)

onMounted(async () => {
    try {
        const lista = await estabelecimentoService.listarMeus()

        // 0 estabelecimentos: usuário ainda não criou nenhum nem foi convidado.
        // Manda direto para o fluxo de criação inicial (estilo legado).
        if (lista.length === 0) {
            router.replace({ name: "CriarPrimeiroEstabelecimento" })
            return
        }

        // 1 estabelecimento: não faz sentido escolher — auto-seleciona e segue.
        if (lista.length === 1) {
            const unico = lista[0]
            tenant.selecionar({
                id: unico.id,
                nomeFantasia: unico.nomeFantasia,
                papel: unico.papelDoUsuario,
            })
            router.replace({ name: "Home" })
            return
        }

        estabelecimentos.value = lista
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Não foi possível carregar."
    } finally {
        carregando.value = false
    }
})

function entrar(e: Estabelecimento) {
    tenant.selecionar({
        id: e.id,
        nomeFantasia: e.nomeFantasia,
        papel: e.papelDoUsuario,
    })
    router.push({ name: "Home" })
}

function inicial(nome: string) {
    return (nome.trim()[0] ?? "?").toUpperCase()
}
</script>

<template>
    <PreAppShell
        titulo="Escolha um estabelecimento"
        subtitulo="Você tem acesso a mais de um. Selecione qual deseja acessar agora."
    >
        <div v-if="carregando" class="estado-vazio">
            <span class="spinner" aria-hidden="true"></span>
            <span>Carregando estabelecimentos…</span>
        </div>

        <div v-else-if="erro" class="alerta">{{ erro }}</div>

        <ul v-else class="lista">
            <li v-for="e in estabelecimentos" :key="e.id">
                <button class="item" @click="entrar(e)">
                    <span class="avatar">
                        <img v-if="e.fotoUrl" :src="e.fotoUrl" :alt="e.nomeFantasia" />
                        <template v-else>{{ inicial(e.nomeFantasia) }}</template>
                    </span>
                    <span class="info">
                        <span class="nome">{{ e.nomeFantasia }}</span>
                        <span class="papel" :class="`papel--${e.papelDoUsuario.toLowerCase()}`">
                            {{ e.papelDoUsuario }}
                        </span>
                    </span>
                    <i class="fa-solid fa-chevron-right seta" aria-hidden="true"></i>
                </button>
            </li>
        </ul>
    </PreAppShell>
</template>

<style scoped>
.estado-vazio {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 0.5rem;
    padding: 1.5rem 0.5rem;
    color: hsl(var(--muted-foreground));
    font-size: 0.875rem;
}
.spinner {
    width: 14px; height: 14px;
    border: 2px solid hsl(var(--primary) / 0.25);
    border-top-color: hsl(var(--primary));
    border-radius: 50%;
    animation: spin 0.7s linear infinite;
}
@keyframes spin { to { transform: rotate(360deg); } }

.alerta {
    background: hsl(var(--error) / 0.1);
    color: hsl(var(--error));
    padding: 0.65rem 0.9rem;
    border-radius: var(--radius);
    font-size: 0.85rem;
}

.lista {
    list-style: none;
    padding: 0;
    margin: 0;
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}
.item {
    width: 100%;
    display: flex;
    align-items: center;
    gap: 0.85rem;
    padding: 0.75rem 0.85rem;
    border: 1px solid transparent;
    border-radius: var(--radius);
    background: transparent;
    cursor: pointer;
    text-align: left;
    font-family: inherit;
    transition: background 0.15s, border-color 0.15s, transform 0.05s;
}
.item:hover {
    background: hsl(var(--muted));
    border-color: hsl(var(--border));
}
.item:focus-visible {
    outline: 2px solid hsl(var(--primary) / 0.4);
    outline-offset: 2px;
}
.item:active { transform: scale(0.998); }

.avatar {
    width: 38px; height: 38px;
    border-radius: 50%;
    background: hsl(var(--primary) / 0.12);
    color: hsl(var(--primary-dark));
    display: inline-flex;
    align-items: center;
    justify-content: center;
    font-weight: 700;
    flex-shrink: 0;
    overflow: hidden;
}
.avatar img {
    width: 100%;
    height: 100%;
    object-fit: cover;
}
.info {
    flex: 1;
    min-width: 0;
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}
.nome {
    font-size: 0.95rem;
    font-weight: 600;
    color: hsl(var(--secondary));
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}
.papel {
    align-self: flex-start;
    font-size: 0.7rem;
    font-weight: 600;
    padding: 0.1rem 0.55rem;
    border-radius: 9999px;
}
.papel--dono         { background: hsl(var(--primary) / 0.12); color: hsl(var(--primary-dark)); }
.papel--profissional { background: hsl(var(--info) / 0.14);    color: hsl(var(--info)); }

.seta {
    color: hsl(var(--muted-foreground));
    font-size: 0.85rem;
}
</style>
