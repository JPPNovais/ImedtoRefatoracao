<!--
    Drawer lateral para cadastro rápido de paciente.
    Visual alinhado ao legado (modules/shared/components/PatientFormSidePanel.vue).
-->
<script setup lang="ts">
import { ref, reactive, watch } from "vue"
import { vMaska } from "maska/vue"
import { AppDrawer, AppDatePicker, AppButton, AppField, AppInput } from "@/components/ui"
import { pacienteService, type PacienteListaItem } from "@/services/pacienteService"
import { cpfValido } from "@/utils/cpf"

const props = defineProps<{
    aberto: boolean
    nomeInicial?: string
}>()

const emit = defineEmits<{
    fechar: []
    criado: [paciente: PacienteListaItem]
}>()

type TipoDocumento = "cpf" | "internacional"
type Sexo = "masculino" | "feminino" | "outro" | "nao_informar"

const form = reactive({
    nome: "",
    tipoDocumento: "cpf" as TipoDocumento,
    documento: "",
    celular: "",
    telefoneFixo: "",
    dataNascimento: "",
    sexo: "nao_informar" as Sexo,
    cep: "",
    endereco: "",
    numero: "",
    complemento: "",
    bairro: "",
    cidade: "",
    uf: "",
})

const salvando = ref(false)
const erro = ref<string | null>(null)

watch(() => props.aberto, (a) => {
    if (!a) return
    form.nome = props.nomeInicial ?? ""
    form.tipoDocumento = "cpf"
    form.documento = ""
    form.celular = ""
    form.telefoneFixo = ""
    form.dataNascimento = ""
    form.sexo = "nao_informar"
    form.cep = ""
    form.endereco = ""
    form.numero = ""
    form.complemento = ""
    form.bairro = ""
    form.cidade = ""
    form.uf = ""
    erro.value = null
})

function montarEndereco(): string | undefined {
    const partes: string[] = []
    if (form.endereco.trim()) partes.push(form.endereco.trim())
    if (form.numero.trim()) partes.push(form.numero.trim())
    if (form.complemento.trim()) partes.push(form.complemento.trim())
    if (form.bairro.trim()) partes.push(form.bairro.trim())
    const cidadeUf = [form.cidade.trim(), form.uf.trim()].filter(Boolean).join("/")
    if (cidadeUf) partes.push(cidadeUf)
    if (form.cep.trim()) partes.push("CEP " + form.cep.trim())
    return partes.length ? partes.join(", ") : undefined
}

async function salvar() {
    if (!form.nome.trim()) {
        erro.value = "Informe o nome do paciente."
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
        await pacienteService.criar({
            nomeCompleto: form.nome.trim(),
            cpf: form.tipoDocumento === "cpf" && docValor ? docValor : undefined,
            documentoInternacional: form.tipoDocumento === "internacional" && docValor ? docValor : undefined,
            telefone: form.celular.trim() || undefined,
            dataNascimento: form.dataNascimento || undefined,
            genero: form.sexo,
            endereco: montarEndereco(),
        })

        const pg = await pacienteService.listar(form.nome.trim(), 1, 5)
        const criado = pg.itens.find(p => p.nomeCompleto === form.nome.trim()) ?? pg.itens[0]
        if (criado) emit("criado", criado)
        emit("fechar")
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao cadastrar paciente."
    } finally {
        salvando.value = false
    }
}
</script>

<template>
    <AppDrawer :aberto="aberto" titulo="Criar novo paciente" @fechar="$emit('fechar')">
        <template #titulo>
            <div class="titulo-bloco">
                <span class="titulo">Criar novo paciente</span>
                <span class="subtitulo">Preencha os dados do paciente para criar um novo cadastro.</span>
            </div>
        </template>

        <AppField label="Nome completo" required>
            <AppInput
                v-model="form.nome"
                placeholder="Digite o nome do paciente"
                :disabled="salvando"
            />
        </AppField>

        <AppField
            label="Documento"
            :hint="form.tipoDocumento === 'cpf' ? 'Digite o CPF do paciente' : undefined"
        >
            <div class="tabs-doc" role="tablist">
                <button
                    type="button"
                    class="tab"
                    :class="{ ativa: form.tipoDocumento === 'cpf' }"
                    @click="form.tipoDocumento = 'cpf'"
                >CPF</button>
                <button
                    type="button"
                    class="tab"
                    :class="{ ativa: form.tipoDocumento === 'internacional' }"
                    @click="form.tipoDocumento = 'internacional'"
                >Documento internacional</button>
            </div>
            <AppInput
                v-if="form.tipoDocumento === 'cpf'"
                v-model="form.documento"
                v-maska="'###.###.###-##'"
                placeholder="000.000.000-00"
                :disabled="salvando"
            />
            <AppInput
                v-else
                v-model="form.documento"
                placeholder="Número do documento"
                :disabled="salvando"
            />
        </AppField>

        <AppField label="Celular (opcional)">
            <AppInput
                v-model="form.celular"
                v-maska="'(##) #####-####'"
                type="tel"
                placeholder="(00) 00000-0000"
                :disabled="salvando"
            />
        </AppField>

        <AppField label="Telefone fixo (opcional)">
            <AppInput
                v-model="form.telefoneFixo"
                v-maska="'(##) ####-####'"
                type="tel"
                placeholder="(00) 0000-0000"
                :disabled="salvando"
            />
        </AppField>

        <AppField label="Data de nascimento">
            <AppDatePicker
                v-model="form.dataNascimento"
                placeholder="Selecione a data de nascimento"
                :disabled="salvando"
                align="start"
            />
        </AppField>

        <AppField label="Sexo">
            <div class="tabs-doc" role="tablist">
                <button
                    type="button"
                    class="tab"
                    :class="{ ativa: form.sexo === 'masculino' }"
                    @click="form.sexo = 'masculino'"
                >Masculino</button>
                <button
                    type="button"
                    class="tab"
                    :class="{ ativa: form.sexo === 'feminino' }"
                    @click="form.sexo = 'feminino'"
                >Feminino</button>
                <button
                    type="button"
                    class="tab"
                    :class="{ ativa: form.sexo === 'outro' }"
                    @click="form.sexo = 'outro'"
                >Outro</button>
                <button
                    type="button"
                    class="tab"
                    :class="{ ativa: form.sexo === 'nao_informar' }"
                    @click="form.sexo = 'nao_informar'"
                >Não informar</button>
            </div>
        </AppField>

        <AppField label="CEP">
            <AppInput
                v-model="form.cep"
                v-maska="'#####-###'"
                placeholder="_____-___"
                :disabled="salvando"
            />
        </AppField>

        <AppField label="Endereço">
            <AppInput
                v-model="form.endereco"
                placeholder="Rua, avenida..."
                :disabled="salvando"
            />
        </AppField>

        <div class="grid-3">
            <AppField label="Número">
                <AppInput v-model="form.numero" placeholder="Número" :disabled="salvando" />
            </AppField>
            <AppField label="Complemento">
                <AppInput v-model="form.complemento" placeholder="Compl." :disabled="salvando" />
            </AppField>
            <AppField label="Bairro">
                <AppInput v-model="form.bairro" placeholder="Bairro" :disabled="salvando" />
            </AppField>
        </div>

        <div class="grid-cidade-uf">
            <AppField label="Cidade">
                <AppInput v-model="form.cidade" placeholder="Cidade" :disabled="salvando" />
            </AppField>
            <AppField label="UF">
                <AppInput v-model="form.uf" placeholder="UF" maxlength="2" :disabled="salvando" />
            </AppField>
        </div>

        <p v-if="erro" class="msg-erro">{{ erro }}</p>

        <template #rodape>
            <AppButton variant="ghost" :disabled="salvando" @click="$emit('fechar')">
                Cancelar
            </AppButton>
            <AppButton
                :disabled="salvando || !form.nome.trim()"
                :loading="salvando"
                @click="salvar"
            >{{ salvando ? "Salvando..." : "Criar paciente" }}</AppButton>
        </template>
    </AppDrawer>
</template>

<style scoped>
.titulo-bloco { display: flex; flex-direction: column; gap: 0.2rem; }
.titulo { font-weight: 700; font-size: 1rem; }
.subtitulo { font-size: 0.78em; color: var(--text-muted); font-weight: 500; }

.tabs-doc {
    display: flex; gap: 0.35rem; flex-wrap: wrap;
    background: hsl(var(--primary-light)); border-radius: var(--radius);
    padding: 0.25rem;
}
.tab {
    flex: 1; min-width: 0; padding: 0.45rem 0.75rem;
    border: none; background: transparent; cursor: pointer;
    font-family: inherit; font-size: 0.82em; font-weight: 600;
    color: var(--text-muted); border-radius: calc(var(--radius) - 2px);
    transition: all 0.15s; white-space: nowrap;
}
.tab:hover:not(.ativa) { color: var(--text); }
.tab.ativa {
    background: var(--bg-card); color: hsl(var(--primary-dark));
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
}

.grid-3 {
    display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 0.75rem;
}
.grid-cidade-uf {
    display: grid; grid-template-columns: 1fr 80px; gap: 0.75rem;
}

.msg-erro { color: var(--danger); font-size: 0.85em; margin: 0; }
</style>
