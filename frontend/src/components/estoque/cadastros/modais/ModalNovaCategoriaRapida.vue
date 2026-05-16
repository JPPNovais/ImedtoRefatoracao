<script setup lang="ts">
/**
 * Cadastro rápido de categoria de estoque — usado pelo atalho "+ Nova"
 * dentro do drawer "Novo produto". Mantém só os campos mínimos (nome + cor
 * + ícone) e usa o primeiro valor das paletas como default para o usuário
 * não precisar configurar nada se não quiser.
 *
 * Emite `criada(opcao)` para a view orquestrar append + pré-seleção. Não
 * altera o catálogo principal — quem refaz o refetch é a view.
 */
import { ref, watch } from "vue"
import { AppModal, AppButton, AppField, AppInput } from "@/components/ui"
import {
    estoqueCadastrosService,
    type CadastroOpcao,
    type CategoriaPayload,
    ICONES_CATEGORIA, CORES_CATEGORIA,
} from "@/services/estoqueCadastrosService"

const props = defineProps<{ aberto: boolean }>()
const emit  = defineEmits<{ criada: [opcao: CadastroOpcao]; fechar: [] }>()

const form = ref<CategoriaPayload>({
    nome: "",
    cor: CORES_CATEGORIA[0].valor,
    icone: ICONES_CATEGORIA[0].valor,
})
const erro     = ref<string | null>(null)
const salvando = ref(false)

watch(() => props.aberto, (aberta) => {
    if (aberta) {
        form.value = { nome: "", cor: CORES_CATEGORIA[0].valor, icone: ICONES_CATEGORIA[0].valor }
        erro.value = null
    }
})

async function salvar() {
    erro.value = null
    const nome = form.value.nome.trim()
    if (!nome) { erro.value = "Nome é obrigatório."; return }

    salvando.value = true
    try {
        const { id } = await estoqueCadastrosService.categorias.criar({
            nome,
            cor: form.value.cor,
            icone: form.value.icone,
        })
        emit("criada", { id, nome })
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao criar categoria."
    } finally {
        salvando.value = false
    }
}
</script>

<template>
    <AppModal
        :aberto="aberto"
        titulo="Nova categoria"
        largura="sm"
        :acima-de-drawer="true"
        @fechar="emit('fechar')"
    >
        <AppField label="Nome" required>
            <AppInput v-model="form.nome" placeholder="Ex: Anestésicos" />
        </AppField>

        <AppField label="Cor" required>
            <div class="paleta">
                <button
                    v-for="c in CORES_CATEGORIA"
                    :key="c.valor"
                    type="button"
                    class="swatch"
                    :class="{ ativo: form.cor === c.valor }"
                    :style="{ background: c.valor }"
                    :title="c.rotulo"
                    @click="form.cor = c.valor"
                ></button>
            </div>
        </AppField>

        <AppField label="Ícone" required>
            <div class="icones">
                <button
                    v-for="i in ICONES_CATEGORIA"
                    :key="i.valor"
                    type="button"
                    class="icone-btn"
                    :class="{ ativo: form.icone === i.valor }"
                    :title="i.rotulo"
                    @click="form.icone = i.valor"
                >
                    <i :class="`fa-solid ${i.valor}`"></i>
                </button>
            </div>
        </AppField>

        <div class="preview">
            <span class="cat-icone" :style="{ background: form.cor, color: '#fff' }">
                <i :class="`fa-solid ${form.icone}`"></i>
            </span>
            <span class="preview-text">{{ form.nome || "Pré-visualização" }}</span>
        </div>

        <p v-if="erro" class="msg-erro">{{ erro }}</p>

        <template #rodape>
            <AppButton variant="secondary" @click="emit('fechar')">Cancelar</AppButton>
            <AppButton
                icon="fa-solid fa-plus"
                :loading="salvando"
                :disabled="salvando"
                @click="salvar"
            >Criar categoria</AppButton>
        </template>
    </AppModal>
</template>

<style scoped>
.paleta { display: flex; gap: 8px; flex-wrap: wrap; }
.swatch {
    width: 32px; height: 32px; border-radius: 50%;
    border: 2px solid hsl(var(--secondary) / 0.1);
    cursor: pointer; transition: transform 100ms, border-color 100ms;
}
.swatch:hover { transform: scale(1.1); }
.swatch.ativo { border-color: hsl(var(--secondary)); transform: scale(1.15); }

.icones { display: grid; grid-template-columns: repeat(6, 1fr); gap: 6px; }
.icone-btn {
    aspect-ratio: 1; border-radius: var(--radius-sm);
    border: 1px solid hsl(var(--secondary) / 0.1);
    background: var(--bg-card); color: hsl(var(--secondary) / 0.7);
    font-size: 14px; cursor: pointer;
    transition: all 100ms;
}
.icone-btn:hover { background: hsl(var(--primary) / 0.05); color: hsl(var(--primary)); }
.icone-btn.ativo {
    background: hsl(var(--primary) / 0.1);
    border-color: hsl(var(--primary) / 0.4);
    color: hsl(var(--primary));
}

.preview {
    display: flex; align-items: center; gap: 12px;
    padding: 12px;
    background: hsl(var(--secondary) / 0.03);
    border-radius: var(--radius-sm);
}
.preview-text { font-size: 14px; font-weight: 600; color: hsl(var(--primary-dark)); }
.cat-icone {
    display: inline-grid; place-items: center;
    width: 30px; height: 30px;
    border-radius: var(--radius-sm);
    font-size: 13px;
    flex-shrink: 0;
}

.msg-erro { color: hsl(var(--error)); font-size: 13px; margin: 0; }
</style>
