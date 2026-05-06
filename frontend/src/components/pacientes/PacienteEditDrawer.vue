<!--
    PacienteEditDrawer — drawer lateral de edição (ou criação) de paciente.

    Uso:
        <PacienteEditDrawer
            :aberto="abrir"
            :paciente="pacienteAtual"
            @fechar="abrir = false"
            @salvo="onPacienteSalvo"
        />

    `paciente` é opcional — quando null/ausente, o drawer opera em modo "novo paciente".
-->
<script setup lang="ts">
import { reactive, ref, watch } from "vue"
import { vMaska } from "maska/vue"
import { AppDrawer, AppButton, AppField, AppInput, AppSelect, AppTextarea, AppDatePicker } from "@/components/ui"
import {
    pacienteService,
    type Paciente,
    type PacientePayload,
} from "@/services/pacienteService"
import { cpfValido } from "@/utils/cpf"
import { PACIENTE_TAGS, resolverTag } from "@/constants/pacienteTags"

type TipoDocumento = "cpf" | "internacional"

const props = defineProps<{
    aberto: boolean
    paciente?: Paciente | null
}>()

const emit = defineEmits<{
    fechar: []
    salvo: [paciente: Paciente]
}>()

// ─── Estado do formulário ─────────────────────────────────────────────────────
const form = reactive({
    nomeCompleto:   "",
    tipoDocumento:  "cpf" as TipoDocumento,
    documento:      "",
    rg:             "",
    dataNascimento: "",
    genero:         "NaoInformado",
    celular:        "",
    telefoneFixo:   "",
    email:          "",
    cep:            "",
    logradouro:     "",
    numero:         "",
    complemento:    "",
    bairro:         "",
    cidade:         "",
    uf:             "",
    observacoes:    "",
    tags:           [] as string[],
    alertas:        [] as string[],
})

const novoAlerta = ref("")

const salvando = ref(false)
const erro     = ref<string | null>(null)

const UFS = [
    "AC","AL","AP","AM","BA","CE","DF","ES","GO","MA","MT","MS","MG",
    "PA","PB","PR","PE","PI","RJ","RN","RS","RO","RR","SC","SP","SE","TO",
]

const GENEROS = [
    { value: "NaoInformado", label: "Não informado" },
    { value: "Masculino",    label: "Masculino" },
    { value: "Feminino",     label: "Feminino" },
    { value: "Outro",        label: "Outro" },
]

function resetForm() {
    form.nomeCompleto   = ""
    form.tipoDocumento  = "cpf"
    form.documento      = ""
    form.rg             = ""
    form.dataNascimento = ""
    form.genero         = "NaoInformado"
    form.celular        = ""
    form.telefoneFixo   = ""
    form.email          = ""
    form.cep            = ""
    form.logradouro     = ""
    form.numero         = ""
    form.complemento    = ""
    form.bairro         = ""
    form.cidade         = ""
    form.uf             = ""
    form.observacoes    = ""
    form.tags           = []
    form.alertas        = []
    novoAlerta.value    = ""
    erro.value = null
}

function toggleTag(chave: string) {
    const idx = form.tags.indexOf(chave)
    if (idx === -1) form.tags.push(chave)
    else form.tags.splice(idx, 1)
}

function adicionarAlerta() {
    const v = novoAlerta.value.trim()
    if (!v) return
    if (v.length > 200) { erro.value = "Cada alerta deve ter no máximo 200 caracteres."; return }
    if (form.alertas.length >= 10) { erro.value = "Máximo de 10 alertas por paciente."; return }
    if (form.alertas.some(a => a.toLowerCase() === v.toLowerCase())) { novoAlerta.value = ""; return }
    form.alertas.push(v)
    novoAlerta.value = ""
}

function removerAlerta(i: number) {
    form.alertas.splice(i, 1)
}

function parseEndereco(end: string | null | undefined) {
    if (!end) return
    const matchCep = end.match(/(\d{5}-?\d{3})/)
    if (matchCep) form.cep = matchCep[1]

    const semCep = end.replace(/(\d{5}-?\d{3})/, "").replace(/CEP\s*/i, "").trim()
    const partes = semCep.split(",").map(s => s.trim()).filter(Boolean)

    if (partes.length >= 1) form.logradouro = partes[0]
    if (partes.length >= 2) form.numero     = partes[1]
    if (partes.length >= 3) form.bairro     = partes[2]
    if (partes.length >= 4) {
        const ultima = partes[partes.length - 1]
        const mUf = ultima.match(/(.+?)\s*-\s*([A-Z]{2})\s*$/)
        if (mUf) {
            form.cidade = mUf[1].trim()
            form.uf     = mUf[2]
        } else {
            form.cidade = ultima
        }
    }
}

function montarEndereco(): string {
    const partes: string[] = []
    if (form.logradouro) partes.push(form.logradouro + (form.numero ? `, ${form.numero}` : ""))
    if (form.complemento) partes.push(form.complemento)
    if (form.bairro)      partes.push(form.bairro)
    if (form.cidade)      partes.push(form.cidade + (form.uf ? ` - ${form.uf}` : ""))
    if (form.cep)         partes.push(`CEP ${form.cep}`)
    return partes.join(", ")
}

async function buscarCep() {
    const digits = form.cep.replace(/\D/g, "")
    if (digits.length !== 8) return
    try {
        const r = await fetch(`https://viacep.com.br/ws/${digits}/json/`)
        const d = await r.json()
        if (d.erro) return
        if (d.logradouro) form.logradouro = d.logradouro
        if (d.bairro)     form.bairro     = d.bairro
        if (d.localidade) form.cidade     = d.localidade
        if (d.uf)         form.uf         = d.uf
    } catch { /* offline — silencioso */ }
}

watch(() => form.cep, (v) => {
    const digits = (v ?? "").replace(/\D/g, "")
    if (digits.length === 8) void buscarCep()
})

// Popular o form quando o drawer abrir / paciente mudar.
watch(
    () => [props.aberto, props.paciente] as const,
    ([aberto, p]) => {
        if (!aberto) return
        resetForm()
        if (p) {
            form.nomeCompleto   = p.nomeCompleto
            if (p.documentoInternacional) {
                form.tipoDocumento = "internacional"
                form.documento     = p.documentoInternacional
            } else {
                form.tipoDocumento = "cpf"
                form.documento     = p.cpf ?? ""
            }
            form.dataNascimento = p.dataNascimento ?? ""
            form.genero         = p.genero || "NaoInformado"
            form.celular        = p.telefone ?? ""
            form.email          = p.email ?? ""
            form.observacoes    = p.observacoes ?? ""
            form.tags           = [...(p.tags ?? [])]
            form.alertas        = [...(p.alertas ?? [])]
            parseEndereco(p.endereco)
        }
    },
    { immediate: true },
)

async function salvar() {
    if (!form.nomeCompleto.trim()) {
        erro.value = "Nome completo é obrigatório."
        return
    }
    const docValor = form.documento.trim()
    if (docValor && form.tipoDocumento === "cpf" && !cpfValido(docValor)) {
        erro.value = "CPF inválido."
        return
    }

    salvando.value = true
    erro.value = null
    try {
        const payload: PacientePayload = {
            nomeCompleto:   form.nomeCompleto.trim(),
            cpf:                    form.tipoDocumento === "cpf" && docValor ? docValor : undefined,
            documentoInternacional: form.tipoDocumento === "internacional" && docValor ? docValor : undefined,
            dataNascimento: form.dataNascimento || undefined,
            genero:         form.genero || undefined,
            telefone:       form.celular || form.telefoneFixo || undefined,
            email:          form.email || undefined,
            endereco:       montarEndereco() || undefined,
            observacoes:    form.observacoes || undefined,
            tags:           form.tags.length    ? [...form.tags]    : [],
            alertas:        form.alertas.length ? [...form.alertas] : [],
        }

        if (props.paciente?.id) {
            await pacienteService.atualizar(props.paciente.id, payload)
            const atualizado = await pacienteService.obter(props.paciente.id)
            emit("salvo", atualizado)
        } else {
            await pacienteService.criar(payload)
            // Busca pelo nome recém-criado (POST não retorna ID).
            const pg = await pacienteService.listar(form.nomeCompleto, 1, 5)
            const criado = pg.itens.find(i => i.nomeCompleto === form.nomeCompleto.trim()) ?? pg.itens[0]
            if (criado) {
                const completo = await pacienteService.obter(criado.id)
                emit("salvo", completo)
            }
        }
        emit("fechar")
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao salvar paciente."
    } finally {
        salvando.value = false
    }
}
</script>

<template>
    <AppDrawer
        :aberto="aberto"
        :titulo="paciente ? 'Editar paciente' : 'Novo paciente'"
        :largura="700"
        @fechar="$emit('fechar')"
    >
        <section class="secao">
            <h3 class="secao-titulo">Dados pessoais</h3>

            <AppField label="Nome completo" required>
                <AppInput v-model="form.nomeCompleto" :disabled="salvando" />
            </AppField>

            <div class="grid-2">
                <AppField label="Documento">
                    <div class="tabs-doc" role="tablist">
                        <button
                            type="button"
                            class="tab"
                            :class="{ ativa: form.tipoDocumento === 'cpf' }"
                            @click="form.tipoDocumento = 'cpf'; form.documento = ''"
                        >CPF</button>
                        <button
                            type="button"
                            class="tab"
                            :class="{ ativa: form.tipoDocumento === 'internacional' }"
                            @click="form.tipoDocumento = 'internacional'; form.documento = ''"
                        >Internacional</button>
                    </div>
                    <AppInput
                        v-if="form.tipoDocumento === 'cpf'"
                        v-model="form.documento" v-maska="'###.###.###-##'"
                        placeholder="000.000.000-00"
                        :disabled="salvando"
                    />
                    <AppInput
                        v-else
                        v-model="form.documento"
                        placeholder="Nº do documento (passaporte, RNE...)"
                        maxlength="30"
                        :disabled="salvando"
                    />
                </AppField>
                <AppField label="Data de nascimento">
                    <AppDatePicker v-model="form.dataNascimento" :disabled="salvando" />
                </AppField>
            </div>

            <div class="grid-2">
                <AppField label="RG">
                    <AppInput v-model="form.rg" :disabled="salvando" />
                </AppField>
                <AppField label="Sexo">
                    <AppSelect v-model="form.genero" :disabled="salvando">
                        <option v-for="g in GENEROS" :key="g.value" :value="g.value">{{ g.label }}</option>
                    </AppSelect>
                </AppField>
            </div>
        </section>

        <section class="secao">
            <h3 class="secao-titulo">Contato</h3>

            <div class="grid-2">
                <AppField label="Celular">
                    <AppInput
                        v-model="form.celular" v-maska="'(##) #####-####'" type="tel"
                        placeholder="(00) 00000-0000"
                        :disabled="salvando"
                    />
                </AppField>
                <AppField label="Telefone fixo">
                    <AppInput
                        v-model="form.telefoneFixo" v-maska="'(##) ####-####'" type="tel"
                        placeholder="(00) 0000-0000"
                        :disabled="salvando"
                    />
                </AppField>
            </div>

            <AppField label="E-mail">
                <AppInput
                    v-model="form.email" type="email"
                    placeholder="paciente@exemplo.com"
                    :disabled="salvando"
                />
            </AppField>
        </section>

        <section class="secao">
            <h3 class="secao-titulo">Endereço</h3>

            <div class="grid-2">
                <AppField label="CEP">
                    <AppInput
                        v-model="form.cep" v-maska="'#####-###'"
                        placeholder="00000-000"
                        :disabled="salvando"
                        @blur="buscarCep"
                    />
                </AppField>
                <AppField label="UF">
                    <AppSelect v-model="form.uf" :disabled="salvando">
                        <option value="">—</option>
                        <option v-for="u in UFS" :key="u" :value="u">{{ u }}</option>
                    </AppSelect>
                </AppField>
            </div>

            <AppField label="Logradouro">
                <AppInput v-model="form.logradouro" placeholder="Rua, avenida..." :disabled="salvando" />
            </AppField>

            <div class="grid-3">
                <AppField label="Número">
                    <AppInput v-model="form.numero" :disabled="salvando" />
                </AppField>
                <AppField label="Complemento">
                    <AppInput v-model="form.complemento" placeholder="Apto, bloco..." :disabled="salvando" />
                </AppField>
                <AppField label="Bairro">
                    <AppInput v-model="form.bairro" :disabled="salvando" />
                </AppField>
            </div>

            <AppField label="Cidade">
                <AppInput v-model="form.cidade" :disabled="salvando" />
            </AppField>
        </section>

        <section class="secao">
            <h3 class="secao-titulo">Tags clínicas</h3>
            <p class="secao-hint">Use tags para classificar e filtrar o paciente na lista (ex: VIP, Gestante, Crônico).</p>
            <div class="tag-grid">
                <button
                    v-for="t in PACIENTE_TAGS" :key="t.chave"
                    type="button"
                    class="tag-pick"
                    :class="{ ativa: form.tags.includes(t.chave) }"
                    :style="form.tags.includes(t.chave) ? { background: `color-mix(in srgb, ${t.cor} 15%, white)`, color: t.cor, borderColor: `color-mix(in srgb, ${t.cor} 35%, white)` } : null"
                    :disabled="salvando"
                    @click="toggleTag(t.chave)"
                >
                    <i class="fa-solid" :class="t.icone"></i>
                    {{ t.label }}
                </button>
            </div>
        </section>

        <section class="secao">
            <h3 class="secao-titulo">Alertas clínicos</h3>
            <p class="secao-hint">
                Avisos críticos que aparecerão em destaque no detalhe do paciente
                (ex: "Alergia grave a penicilina", "Diabético tipo 1"). Máximo de 10.
            </p>
            <div v-if="form.alertas.length" class="alertas-lista">
                <div v-for="(a, i) in form.alertas" :key="i" class="alerta-item">
                    <i class="fa-solid fa-triangle-exclamation"></i>
                    <span>{{ a }}</span>
                    <button
                        type="button"
                        class="alerta-remover"
                        :disabled="salvando"
                        aria-label="Remover alerta"
                        @click="removerAlerta(i)"
                    >
                        <i class="fa-solid fa-xmark"></i>
                    </button>
                </div>
            </div>
            <div class="alerta-novo">
                <AppInput
                    v-model="novoAlerta"
                    placeholder="Descreva o alerta e pressione Enter..."
                    :disabled="salvando || form.alertas.length >= 10"
                    @keyup.enter="adicionarAlerta"
                />
                <AppButton
                    variant="secondary"
                    icon="fa-solid fa-plus"
                    :disabled="salvando || !novoAlerta.trim() || form.alertas.length >= 10"
                    @click="adicionarAlerta"
                >
                    Adicionar
                </AppButton>
            </div>
        </section>

        <section class="secao">
            <h3 class="secao-titulo">Observações</h3>
            <AppTextarea
                v-model="form.observacoes"
                :rows="4"
                placeholder="Notas internas sobre o paciente..."
                :disabled="salvando"
            />
        </section>

        <p v-if="erro" class="msg-erro">{{ erro }}</p>

        <template #rodape>
            <AppButton variant="secondary" :disabled="salvando" @click="$emit('fechar')">
                Cancelar
            </AppButton>
            <AppButton :loading="salvando" :disabled="!form.nomeCompleto.trim()" @click="salvar">
                {{ paciente ? "Salvar alterações" : "Cadastrar paciente" }}
            </AppButton>
        </template>
    </AppDrawer>
</template>

<style scoped>
.secao {
    display: flex;
    flex-direction: column;
    gap: 0.85rem;
}
.secao-titulo {
    font-size: 0.82rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    color: hsl(var(--primary));
    margin: 0;
}

.grid-2 {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 0.85rem;
}
.grid-3 {
    display: grid;
    grid-template-columns: 1fr 1fr 1fr;
    gap: 0.85rem;
}

.msg-erro {
    color: hsl(var(--error));
    font-size: 0.875rem;
    margin: 0;
}

.tabs-doc {
    display: flex;
    gap: 0.25rem;
    background: hsl(var(--primary-light));
    border-radius: var(--radius);
    padding: 0.2rem;
    margin-bottom: 0.4rem;
}
.tab {
    flex: 1;
    padding: 0.4rem 0.6rem;
    border: 0;
    background: transparent;
    cursor: pointer;
    font-family: inherit;
    font-size: 0.78em;
    font-weight: 600;
    color: var(--text-muted);
    border-radius: calc(var(--radius) - 2px);
    transition: all 0.15s;
}
.tab:hover:not(.ativa) { color: var(--text); }
.tab.ativa {
    background: var(--bg-card);
    color: hsl(var(--primary-dark));
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
}

.secao-hint {
    font-size: 0.78rem;
    color: hsl(var(--secondary) / 0.65);
    margin: -0.4rem 0 0.2rem;
    line-height: 1.4;
}

/* Tags clínicas */
.tag-grid { display: flex; flex-wrap: wrap; gap: 0.4rem; }
.tag-pick {
    display: inline-flex; align-items: center; gap: 0.4rem;
    background: white;
    border: 1.5px solid hsl(var(--secondary) / 0.15);
    padding: 6px 12px; border-radius: 999px;
    font-family: inherit; font-size: 0.8rem; font-weight: 600;
    color: hsl(var(--secondary) / 0.7);
    cursor: pointer;
    transition: all 150ms;
}
.tag-pick:hover { border-color: hsl(var(--primary) / 0.4); color: hsl(var(--primary-dark)); }
.tag-pick:disabled { opacity: 0.5; cursor: not-allowed; }
.tag-pick i { font-size: 11px; }

/* Alertas */
.alertas-lista { display: flex; flex-direction: column; gap: 0.4rem; }
.alerta-item {
    display: flex; align-items: center; gap: 0.5rem;
    background: hsl(var(--error) / 0.06);
    border: 1px solid hsl(var(--error) / 0.2);
    border-left-width: 3px;
    padding: 0.5rem 0.75rem;
    border-radius: 0.5rem;
    font-size: 0.85rem;
    color: hsl(0 70% 30%);
}
.alerta-item > i:first-child { color: hsl(var(--error)); flex-shrink: 0; }
.alerta-item > span { flex: 1; }
.alerta-remover {
    background: none; border: none; cursor: pointer;
    color: hsl(var(--error) / 0.7); font-size: 0.85rem;
    padding: 4px 6px; border-radius: 4px;
}
.alerta-remover:hover:not(:disabled) {
    background: hsl(var(--error) / 0.12);
    color: hsl(var(--error));
}

.alerta-novo { display: flex; gap: 0.5rem; align-items: stretch; }
.alerta-novo > :first-child { flex: 1; }

@media (max-width: 720px) {
    .grid-2, .grid-3 { grid-template-columns: 1fr; }
    .alerta-novo { flex-direction: column; }
}
</style>
