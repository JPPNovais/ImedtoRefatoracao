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
    cpf:            "",
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
})

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
    form.cpf            = ""
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
    erro.value = null
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
            form.cpf            = p.cpf ?? ""
            form.dataNascimento = p.dataNascimento ?? ""
            form.genero         = p.genero || "NaoInformado"
            form.celular        = p.telefone ?? ""
            form.email          = p.email ?? ""
            form.observacoes    = p.observacoes ?? ""
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
    salvando.value = true
    erro.value = null
    try {
        const payload: PacientePayload = {
            nomeCompleto:   form.nomeCompleto.trim(),
            cpf:            form.cpf || undefined,
            dataNascimento: form.dataNascimento || undefined,
            genero:         form.genero || undefined,
            telefone:       form.celular || form.telefoneFixo || undefined,
            email:          form.email || undefined,
            endereco:       montarEndereco() || undefined,
            observacoes:    form.observacoes || undefined,
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
                <AppField label="CPF">
                    <AppInput
                        v-model="form.cpf" v-maska="'###.###.###-##'"
                        placeholder="000.000.000-00"
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

@media (max-width: 720px) {
    .grid-2, .grid-3 { grid-template-columns: 1fr; }
}
</style>
