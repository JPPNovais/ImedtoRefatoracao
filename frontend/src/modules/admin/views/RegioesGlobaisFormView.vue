<script setup lang="ts">
/**
 * RegioesGlobaisFormView — criação/edição de região anatômica (Wave 4 live-link).
 * Criação: codigo, nome, vista, paiCodigo, nivel, templateTexto, ordem, lateralidade, motivo.
 * Edição: apenas nome, templateTexto, motivo (estrutura não muda).
 * B3 (2026-06-08_007): OPCOES_VISTA corrigido; guard de pai circunferencial (R7/CA6).
 */
import { ref, onMounted, computed, watch } from "vue"
import { useRouter } from "vue-router"
import { AppPageHeader, AppCard, AppField, AppInput, AppTextarea, AppButton, AppSelect } from "@/components/ui"
import { useRegioesGlobaisStore } from "../stores/regioesGlobaisStore"
import { regioesGlobaisService } from "../services/catalogosService"

const MSG_CIRCUNFERENCIAL = "Nós circunferenciais são agregadores e não aceitam sub-regiões."

const props = defineProps<{ id?: string }>()
const router = useRouter()
const store = useRegioesGlobaisStore()

const idNumerico = computed(() => props.id ? Number(props.id) : null)
const editando = computed(() => idNumerico.value !== null && !isNaN(idNumerico.value))

// CA2: apenas os 3 valores reais do catálogo (+ sem vista para nós raiz/neutros)
const OPCOES_VISTA = [
    { value: "", label: "Sem vista (raiz ou neutra)" },
    { value: "anterior", label: "Anterior" },
    { value: "posterior", label: "Posterior" },
    { value: "circunferencial", label: "Circunferencial" },
]

// campos de criação (imutáveis após criar)
const codigo = ref("")
const vista = ref("")
const paiCodigo = ref("")
const nivel = ref<number>(1)
const ordem = ref<number>(1)
const lateralidade = ref(false)

// estado derivado do pai (R2/R3/R7)
const erroCircunferencial = ref("")

// campos sempre editáveis
const nome = ref("")
const templateTexto = ref("")
const motivo = ref("")
const erros = ref<Record<string, string>>({})
const salvando = ref(false)
const erroGeral = ref("")

onMounted(async () => {
    if (editando.value && idNumerico.value !== null) {
        await store.carregarItem(idNumerico.value)
        if (store.itemAtual) {
            nome.value = store.itemAtual.nome
            templateTexto.value = store.itemAtual.templateTexto ?? ""
            // somente leitura no modo edição:
            codigo.value = store.itemAtual.codigo
            vista.value = store.itemAtual.vista ?? ""
            paiCodigo.value = store.itemAtual.paiCodigo ?? ""
            nivel.value = store.itemAtual.nivel
            ordem.value = store.itemAtual.ordem
            lateralidade.value = store.itemAtual.lateralidade
        }
    }
})

// CA6 (R7/R2/R3): ao digitar código do pai, verificar circunferencial e derivar vista/nível
watch(paiCodigo, async (codigoPai) => {
    erroCircunferencial.value = ""
    if (!codigoPai.trim()) return

    try {
        // Reutiliza árvore já carregada quando possível
        const arvore = store.arvore.length > 0 ? store.arvore : await regioesGlobaisService.listarArvore(true)
        const encontrado = encontrarNaArvore(arvore, codigoPai.trim())
        if (!encontrado) return

        // R7: bloquear pai circunferencial (CA6)
        if (encontrado.vista === "circunferencial") {
            erroCircunferencial.value = MSG_CIRCUNFERENCIAL
            return
        }

        // R2/R3: derivar vista e nível do pai (CA3/CA5)
        vista.value = encontrado.vista ?? ""
        nivel.value = encontrado.nivel + 1
    } catch {
        // silencia: a validação real está no backend (CA6b)
    }
})

type NoArvore = { codigo: string; vista: string | null; nivel: number; filhos: NoArvore[] }

function encontrarNaArvore(nos: NoArvore[], codigo: string): NoArvore | null {
    for (const no of nos) {
        if (no.codigo.toLowerCase() === codigo.toLowerCase()) return no
        const filho = encontrarNaArvore(no.filhos, codigo)
        if (filho) return filho
    }
    return null
}

function validar(): boolean {
    const e: Record<string, string> = {}
    if (!nome.value.trim()) e.nome = "Nome é obrigatório."
    if (!editando.value) {
        if (!codigo.value.trim()) e.codigo = "Código é obrigatório."
        else if (codigo.value.trim().length > 60) e.codigo = "Código deve ter no máximo 60 caracteres."
        if (nivel.value < 1 || nivel.value > 3) e.nivel = "Nível deve ser entre 1 e 3."
        if (erroCircunferencial.value) e.paiCodigo = erroCircunferencial.value
    }
    if (motivo.value.trim().length < 10) e.motivo = "Motivo deve ter ao menos 10 caracteres."
    erros.value = e
    return Object.keys(e).length === 0
}

const submitBloqueado = computed(() =>
    motivo.value.trim().length < 10 || !!erroCircunferencial.value
)

async function salvar() {
    if (!validar()) return
    salvando.value = true
    erroGeral.value = ""
    try {
        if (editando.value && idNumerico.value !== null) {
            await store.atualizar(idNumerico.value, {
                nome: nome.value.trim(),
                templateTexto: templateTexto.value.trim() || null,
                motivo: motivo.value.trim(),
            })
        } else {
            await store.criar({
                codigo: codigo.value.trim(),
                nome: nome.value.trim(),
                paiCodigo: paiCodigo.value.trim() || null,
                nivel: nivel.value,
                vista: vista.value || null,
                templateTexto: templateTexto.value.trim() || null,
                ordem: ordem.value,
                lateralidade: lateralidade.value,
                motivo: motivo.value.trim(),
            })
        }
        router.push({ name: "AdminRegioesGlobais" })
    } catch (err: unknown) {
        const msg = (err as { response?: { data?: { mensagem?: string } } })?.response?.data?.mensagem
        erroGeral.value = msg ?? "Não foi possível salvar a região."
    } finally {
        salvando.value = false
    }
}
</script>

<template>
    <main class="app-page app-page--narrow">
        <AppPageHeader
            :titulo="editando ? 'Editar região anatômica' : 'Nova região anatômica'"
        />

        <div v-if="store.carregando && editando" class="estado-info">
            <i class="fa-solid fa-spinner fa-spin"></i> Carregando...
        </div>
        <p v-else-if="store.erro && editando" class="estado-erro">{{ store.erro }}</p>

        <AppCard v-else>
            <form @submit.prevent="salvar" class="form-campos">

                <!-- Campos somente criação -->
                <template v-if="!editando">
                    <AppField label="Código" required :hint="erros.codigo || 'Identificador único da região (ex.: ABD-SUP-D). Imutável após criação.'">
                        <AppInput
                            v-model="codigo"
                            placeholder="Ex.: ABD-SUP-D"
                            maxlength="60"
                            :disabled="salvando"
                        />
                    </AppField>

                    <AppField label="Vista" hint="Vista corporal onde a região aparece no mapa.">
                        <AppSelect v-model="vista" :options="OPCOES_VISTA" :disabled="salvando || !!paiCodigo.trim()" />
                        <p v-if="paiCodigo.trim()" class="hint-derivado">Vista derivada do pai.</p>
                    </AppField>

                    <AppField label="Código do pai" :hint="erros.paiCodigo || 'Código da região pai (deixe em branco para nó raiz). Nós circunferenciais não aceitam filhos.'">
                        <AppInput
                            v-model="paiCodigo"
                            placeholder="Ex.: ABD"
                            maxlength="60"
                            :disabled="salvando"
                        />
                        <p v-if="erroCircunferencial" class="campo-erro-inline" role="alert">{{ erroCircunferencial }}</p>
                    </AppField>

                    <div class="row-2col">
                        <AppField label="Nível" required :hint="erros.nivel || '1=Raiz, 2=Sub-região, 3=Detalhe'">
                            <AppInput v-model.number="nivel" type="number" min="1" max="3" :disabled="salvando || !!paiCodigo.trim()" />
                        </AppField>
                        <AppField label="Ordem" hint="Ordem de exibição dentro do mesmo pai.">
                            <AppInput v-model.number="ordem" type="number" min="1" max="9999" :disabled="salvando" />
                        </AppField>
                    </div>

                    <AppField label="Lateralidade">
                        <label class="label-check">
                            <input type="checkbox" v-model="lateralidade" :disabled="salvando" />
                            Esta região possui distinção esquerda/direita
                        </label>
                    </AppField>
                </template>

                <!-- Campos somente leitura no modo edição -->
                <template v-else>
                    <div class="info-somente-leitura">
                        <span class="info-label">Código</span>
                        <code class="info-valor">{{ codigo }}</code>
                        <span class="info-label">Nível</span>
                        <span class="info-valor">{{ nivel }}</span>
                        <span class="info-label">Vista</span>
                        <span class="info-valor">{{ vista || "—" }}</span>
                    </div>
                </template>

                <!-- Campos sempre editáveis -->
                <AppField label="Nome" required :hint="erros.nome">
                    <AppInput
                        v-model="nome"
                        placeholder="Ex.: Quadrante superior direito"
                        maxlength="120"
                        :disabled="salvando"
                    />
                </AppField>

                <AppField label="Template de texto" hint="Texto padrão sugerido ao registrar achados nesta região.">
                    <AppTextarea
                        v-model="templateTexto"
                        :rows="4"
                        placeholder="Ex.: Sem alterações..."
                        :disabled="salvando"
                    />
                </AppField>

                <AppField label="Motivo da alteração" required :hint="erros.motivo || 'Mínimo 10 caracteres.'">
                    <AppInput
                        v-model="motivo"
                        placeholder="Descreva o motivo..."
                        :disabled="salvando"
                    />
                </AppField>

                <p v-if="erroGeral" class="campo-erro" role="alert">{{ erroGeral }}</p>

                <div class="form-acoes">
                    <AppButton variant="ghost" type="button" @click="router.push({ name: 'AdminRegioesGlobais' })">Cancelar</AppButton>
                    <AppButton
                        type="submit"
                        :loading="salvando"
                        :disabled="submitBloqueado"
                    >
                        {{ editando ? "Salvar alterações" : "Criar região" }}
                    </AppButton>
                </div>
            </form>
        </AppCard>
    </main>
</template>

<style scoped>
.estado-info {
    text-align: center;
    padding: 2rem 0;
    color: hsl(var(--muted-foreground));
    font-size: var(--text-sm);
}

.estado-erro {
    padding: 0.75rem 1rem;
    background: hsl(var(--destructive) / 0.1);
    color: hsl(var(--destructive));
    border: 1px solid hsl(var(--destructive) / 0.3);
    border-radius: calc(var(--radius) - 2px);
    font-size: var(--text-sm);
}

.form-campos {
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
}

.row-2col {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1rem;
}

.label-check {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: var(--text-sm);
    cursor: pointer;
    color: hsl(var(--foreground));
}

.hint-derivado {
    font-size: var(--text-xs);
    color: hsl(var(--muted-foreground));
    margin: 0.25rem 0 0;
}

.info-somente-leitura {
    display: grid;
    grid-template-columns: auto 1fr;
    gap: 0.375rem 0.75rem;
    align-items: center;
    padding: 0.75rem;
    background: hsl(var(--muted) / 0.4);
    border-radius: var(--radius);
    font-size: var(--text-xs);
}

.info-label {
    color: hsl(var(--muted-foreground));
    font-weight: var(--font-weight-semibold);
    font-size: var(--text-xs);
    text-transform: uppercase;
    letter-spacing: 0.03em;
}

.info-valor {
    color: hsl(var(--foreground));
}

.campo-erro {
    padding: 0.75rem 1rem;
    background: hsl(var(--destructive) / 0.1);
    color: hsl(var(--destructive));
    border: 1px solid hsl(var(--destructive) / 0.3);
    border-radius: calc(var(--radius) - 2px);
    font-size: var(--text-sm);
    margin: 0;
}

.campo-erro-inline {
    color: hsl(var(--destructive));
    font-size: var(--text-xs);
    margin: 0.25rem 0 0;
}

.form-acoes {
    display: flex;
    gap: 0.75rem;
    justify-content: flex-end;
    padding-top: 0.5rem;
}
</style>
