<script setup lang="ts">
import { computed } from "vue"
import { AppButton } from "@/components/ui"
import EstoqueStatusPill from "./EstoqueStatusPill.vue"
import type { ItemInventario } from "@/services/inventarioService"
import { formatarMoedaBrl } from "@/utils/format"

const props = defineProps<{
    itens: ItemInventario[]
}>()

const emit = defineEmits<{
    "abrir-item": [item: ItemInventario]
    "nova-movimentacao": [item: ItemInventario]
}>()

const itensBaixos = computed(() =>
    props.itens
        .filter(it => it.ativo && it.estoqueAbaixoMinimo)
        .sort((a, b) => {
            const ratioA = a.quantidadeMinima > 0 ? a.quantidadeAtual / a.quantidadeMinima : 0
            const ratioB = b.quantidadeMinima > 0 ? b.quantidadeAtual / b.quantidadeMinima : 0
            return ratioA - ratioB
        })
)

const itensSemEstoque = computed(() =>
    props.itens.filter(it => it.ativo && it.quantidadeAtual <= 0)
)

function formatarQtd(n: number) {
    return n % 1 === 0 ? n.toString() : n.toFixed(3).replace(/\.?0+$/, "")
}

function porcentagem(item: ItemInventario) {
    if (item.quantidadeMinima <= 0) return 0
    return Math.round((item.quantidadeAtual / item.quantidadeMinima) * 100)
}
</script>

<template>
    <div class="alertas-grid">
        <!-- Estoque baixo / esgotado -->
        <div class="alerta-card">
            <h3>
                <span class="alerta-icone vermelho">
                    <i class="fa-solid fa-arrow-trend-down"></i>
                </span>
                Estoque baixo
            </h3>
            <p class="alerta-desc">
                {{ itensBaixos.length }} {{ itensBaixos.length === 1 ? "item abaixo" : "itens abaixo" }} do mínimo configurado.
            </p>

            <div class="alerta-lista">
                <div v-if="itensBaixos.length === 0" class="alerta-ok">
                    <i class="fa-solid fa-circle-check"></i>
                    Todos os itens estão em níveis adequados.
                </div>

                <div
                    v-for="item in itensBaixos"
                    :key="item.id"
                    class="alerta-row"
                    @click="emit('abrir-item', item)"
                >
                    <div class="alerta-info">
                        <b>{{ item.nome }}</b>
                        <span>{{ item.codigo }} · {{ item.categoria }}</span>
                    </div>
                    <span
                        class="qtd"
                        :style="{ color: item.quantidadeAtual <= 0 ? 'hsl(0 70% 45%)' : 'hsl(40 90% 38%)' }"
                    >
                        {{ formatarQtd(item.quantidadeAtual) }} / {{ formatarQtd(item.quantidadeMinima) }} {{ item.unidadeMedida }}
                    </span>
                    <AppButton
                        variant="secondary"
                        size="sm"
                        icon="fa-solid fa-right-left"
                        @click.stop="emit('nova-movimentacao', item)"
                    >
                        Entrada
                    </AppButton>
                </div>
            </div>
        </div>

        <!-- Itens esgotados — destaque especial -->
        <div class="alerta-card">
            <h3>
                <span class="alerta-icone laranja">
                    <i class="fa-solid fa-triangle-exclamation"></i>
                </span>
                Itens esgotados
            </h3>
            <p class="alerta-desc">
                {{ itensSemEstoque.length }} {{ itensSemEstoque.length === 1 ? "item com" : "itens com" }} quantidade zero.
            </p>

            <div class="alerta-lista">
                <div v-if="itensSemEstoque.length === 0" class="alerta-ok">
                    <i class="fa-solid fa-circle-check"></i>
                    Nenhum item esgotado no momento.
                </div>

                <div
                    v-for="item in itensSemEstoque"
                    :key="item.id"
                    class="alerta-row"
                    @click="emit('abrir-item', item)"
                >
                    <div class="alerta-info">
                        <b>{{ item.nome }}</b>
                        <span>{{ item.codigo }} · {{ item.categoria }}</span>
                    </div>
                    <EstoqueStatusPill status="out" />
                    <AppButton
                        variant="secondary"
                        size="sm"
                        icon="fa-solid fa-plus"
                        @click.stop="emit('nova-movimentacao', item)"
                    >
                        Repor
                    </AppButton>
                </div>
            </div>
        </div>
    </div>
</template>

<style scoped>
.alertas-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 14px;
}
@media (max-width: 900px) {
    .alertas-grid { grid-template-columns: 1fr; }
}

.alerta-card {
    background: var(--bg-card);
    border: 1px solid hsl(var(--secondary) / 0.08);
    border-radius: var(--radius-lg);
    box-shadow: var(--shadow);
    padding: 18px 20px;
    display: flex;
    flex-direction: column;
    gap: 4px;
}

.alerta-card h3 {
    margin: 0 0 2px;
    font-size: var(--text-md);
    font-weight: var(--font-weight-extrabold);
    color: hsl(var(--primary-dark));
    display: flex;
    align-items: center;
    gap: 10px;
}

.alerta-icone {
    width: 28px;
    height: 28px;
    border-radius: 8px;
    display: grid;
    place-items: center;
    font-size: 13px;
    flex-shrink: 0;
}
.alerta-icone.vermelho { background: hsl(0 75% 95%); color: hsl(0 70% 45%); }
.alerta-icone.laranja  { background: hsl(40 95% 94%); color: hsl(35 95% 40%); }

.alerta-desc {
    font-size: 12px;
    color: hsl(var(--secondary) / 0.65);
    margin: 0 0 12px;
}

.alerta-lista { display: flex; flex-direction: column; }

.alerta-ok {
    padding: 30px 0;
    text-align: center;
    color: hsl(160 79% 39%);
    font-size: 13px;
    font-weight: 600;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
}

.alerta-row {
    display: grid;
    grid-template-columns: 1fr auto auto;
    gap: 12px;
    align-items: center;
    padding: 10px 0;
    border-top: 1px solid hsl(var(--secondary) / 0.06);
    cursor: pointer;
    transition: background 150ms;
}
.alerta-row:first-child { border-top: 0; }
.alerta-row:hover { background: hsl(var(--primary) / 0.025); margin: 0 -8px; padding-left: 8px; padding-right: 8px; border-radius: var(--radius-sm); }

.alerta-info b { font-size: 13px; color: hsl(var(--primary-dark)); font-weight: 700; display: block; }
.alerta-info span { font-size: 11px; color: hsl(var(--secondary) / 0.6); }

.qtd { font-size: 12px; font-weight: 800; white-space: nowrap; }
</style>
