<script setup lang="ts">
/**
 * MigracoesListView — lista paginada de jobs de migração para o admin.
 */
import { ref, onMounted } from "vue"
import { useRouter } from "vue-router"
import {
    AppPageHeader,
    AppCard,
    AppEmptyState,
    AppButton,
    AppBadge,
    AppPagination,
} from "@/components/ui"
import { useMigracaoAdminStore } from "../stores/migracaoAdminStore"

const router = useRouter()
const store = useMigracaoAdminStore()

const filtroStatus = ref<string | null>(null)

const STATUS_LABELS: Record<string, string> = {
    aguardando_arquivo: "Aguardando arquivo",
    aguardando_mapa:    "Aguardando mapa",
    mapa_em_revisao:    "Mapa em revisão",
    preview_pronto:     "Preview pronto",
    migrando:           "Migrando",
    concluido:          "Concluído",
    concluido_com_erros:"Concluído c/ erros",
    desfeito:           "Desfeito",
    rejeitado:          "Rejeitado",
    falhou:             "Falhou",
}

const STATUS_VARIANT: Record<string, "default" | "success" | "warning" | "error" | "info"> = {
    aguardando_arquivo: "default",
    aguardando_mapa:    "warning",
    mapa_em_revisao:    "info",
    preview_pronto:     "info",
    migrando:           "warning",
    concluido:          "success",
    concluido_com_erros:"error",
    desfeito:           "default",
    rejeitado:          "error",
    falhou:             "error",
}

onMounted(() => carregar())

async function carregar(p = 1) {
    await store.carregar({ status: filtroStatus.value, page: p })
}

function verJob(id: number) {
    router.push({ name: "AdminMigracaoRevisao", params: { jobId: String(id) } })
}

function formatarData(iso: string) {
    return new Date(iso).toLocaleString("pt-BR", {
        day: "2-digit", month: "2-digit", year: "numeric",
        hour: "2-digit", minute: "2-digit",
    })
}
</script>

<template>
    <div class="app-page">
        <AppPageHeader titulo="Central de Migração" />

        <AppCard>
            <!-- Filtro de status -->
            <div class="filtros">
                <select
                    v-model="filtroStatus"
                    class="form-input"
                    style="width: 220px;"
                    @change="carregar(1)"
                >
                    <option :value="null">Todos os status</option>
                    <option v-for="(label, key) in STATUS_LABELS" :key="key" :value="key">{{ label }}</option>
                </select>
            </div>

            <!-- Estado de carregamento -->
            <div v-if="store.carregando" class="loading-msg">Carregando...</div>

            <!-- Erro -->
            <div v-else-if="store.erro" class="erro-msg">{{ store.erro }}</div>

            <!-- Lista vazia -->
            <AppEmptyState
                v-else-if="store.jobs.length === 0"
                titulo="Nenhum job de migração encontrado"
                descricao="Os jobs de migração aparecem aqui após o cliente fazer upload do arquivo."
            />

            <!-- Tabela -->
            <table v-else class="migracao-table">
                <thead>
                    <tr>
                        <th>#</th>
                        <th>Estabelecimento</th>
                        <th>Origem</th>
                        <th>Status</th>
                        <th>Criado em</th>
                        <th></th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="job in store.jobs" :key="job.id">
                        <td>{{ job.id }}</td>
                        <td>{{ job.estabelecimentoId }}</td>
                        <td>{{ job.origem ?? "—" }}</td>
                        <td>
                            <AppBadge :variant="STATUS_VARIANT[job.status] ?? 'default'">
                                {{ STATUS_LABELS[job.status] ?? job.status }}
                            </AppBadge>
                        </td>
                        <td>{{ formatarData(job.criadoEm) }}</td>
                        <td>
                            <AppButton variant="secondary" size="sm" @click="verJob(job.id)">
                                Ver
                            </AppButton>
                        </td>
                    </tr>
                </tbody>
            </table>

            <AppPagination
                v-if="store.total > store.tamanho"
                :total="store.total"
                :pagina="store.pagina"
                :tamanho="store.tamanho"
                @mudar="carregar"
            />
        </AppCard>
    </div>
</template>

<style scoped>
.filtros {
    margin-bottom: 1rem;
}

.loading-msg,
.erro-msg {
    padding: 1rem 0;
    font-size: var(--text-sm);
    color: hsl(var(--muted-foreground));
}

.erro-msg {
    color: hsl(var(--destructive));
}

.migracao-table {
    width: 100%;
    border-collapse: collapse;
    font-size: var(--text-sm);
}

.migracao-table th,
.migracao-table td {
    padding: 0.625rem 0.75rem;
    text-align: left;
    border-bottom: 1px solid hsl(var(--border));
}

.migracao-table th {
    font-weight: var(--font-weight-semibold);
    color: hsl(var(--muted-foreground));
}

.migracao-table tr:last-child td {
    border-bottom: none;
}
</style>
