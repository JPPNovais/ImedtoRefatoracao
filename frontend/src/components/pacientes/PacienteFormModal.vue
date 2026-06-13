<script setup lang="ts">
import { computed, reactive, ref, toRef, watch } from "vue"
import { vMaska } from "maska/vue"
import {
    AppButton, AppDatePicker, AppField, AppInput, AppModal, AppSelect, AppTextarea,
} from "@/components/ui"
import { pacienteService, type Paciente, type PacientePayload } from "@/services/pacienteService"
import { cpfValido } from "@/utils/cpf"
import { PACIENTE_TAGS } from "@/constants/pacienteTags"
import { useCepAutofill } from "@/composables/useCepAutofill"

/**
 * Modal de cadastro/edição de paciente — alinhado ao estilo do design system
 * (modal centralizado, header título+sub+X, form-grid + field-group, footer
 * Cancelar/Salvar). Substitui o drawer antigo.
 *
 * Dois modos:
 *  - `criar` (default quando `paciente` é null): cadastro rápido com apenas os
 *     5 campos essenciais (nome/CPF/telefone/nascimento/sexo). Botão "Cadastrar
 *     com mais detalhes" expande para o cadastro completo na hora.
 *  - `editar` (quando `paciente` está presente): cadastro completo direto, com
 *     todas as seções (Dados, Contato, Endereço, Tags, Alertas, Observações).
 *
 * Quando o backend permite, o cadastro rápido pode salvar com só nome — CPF
 * e telefone permanecem obrigatórios para reduzir duplicidade na clínica.
 */
const props = defineProps<{
    aberto: boolean
    paciente?: Paciente | null
}>()

const emit = defineEmits<{
    (e: "fechar"): void
    (e: "salvo", p: Paciente): void
}>()

type TipoDocumento = "cpf" | "internacional"

interface Form {
    nomeCompleto: string
    tipoDocumento: TipoDocumento
    documento: string
    dataNascimento: string
    genero: string
    celular: string
    telefoneFixo: string
    email: string
    cep: string
    logradouro: string
    numero: string
    complemento: string
    bairro: string
    cidade: string
    uf: string
    observacoes: string
    tags: string[]
    alertas: string[]
}

function novaForm(): Form {
    return {
        nomeCompleto: "",
        tipoDocumento: "cpf",
        documento: "",
        dataNascimento: "",
        genero: "NaoInformado",
        celular: "",
        telefoneFixo: "",
        email: "",
        cep: "",
        logradouro: "",
        numero: "",
        complemento: "",
        bairro: "",
        cidade: "",
        uf: "",
        observacoes: "",
        tags: [],
        alertas: [],
    }
}

const form = reactive<Form>(novaForm())
const novoAlerta = ref("")
const erro = ref<string | null>(null)
const salvando = ref(false)

// Modo é derivado: paciente presente = edição (form completo); ausente = criação rápida.
const modo = computed<"criar" | "editar">(() => (props.paciente ? "editar" : "criar"))

// No criar, o usuário pode optar por ver mais campos (cadastro completo de uma vez).
const expandirCadastro = ref(false)
const formCompleto = computed(() => modo.value === "editar" || expandirCadastro.value)

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

function reset() {
    Object.assign(form, novaForm())
    expandirCadastro.value = false
    novoAlerta.value = ""
    erro.value = null
}

function popularComPaciente(p: Paciente) {
    Object.assign(form, novaForm())
    form.nomeCompleto = p.nomeCompleto
    if (p.documentoInternacional) {
        form.tipoDocumento = "internacional"
        form.documento = p.documentoInternacional
    } else {
        form.tipoDocumento = "cpf"
        form.documento = p.cpf ?? ""
    }
    form.dataNascimento = p.dataNascimento ?? ""
    form.genero = p.genero || "NaoInformado"
    form.celular = p.telefone ?? ""
    form.email = p.email ?? ""
    form.observacoes = p.observacoes ?? ""
    form.tags = [...(p.tags ?? [])]
    form.alertas = [...(p.alertas ?? [])]
    parseEndereco(p.endereco)
    // Previne disparo automático de busca ao montar em modo edição (CA12):
    // o composable ignora o próximo disparo debounced com este valor.
    if (form.cep) marcarCargaCep(form.cep)
}

watch(() => [props.aberto, props.paciente] as const, ([aberto, p]) => {
    if (!aberto) return
    if (p) popularComPaciente(p)
    else reset()
}, { immediate: true })

// ─── Endereço (CEP autocompletar) ────────────────────────────────────────────
const { buscando: buscandoCep, marcarCarga: marcarCargaCep } = useCepAutofill(
    toRef(form, "cep"),
    (e) => {
        // R5: preserva o que o usuário já digitou manualmente
        if (e.logradouro) form.logradouro = e.logradouro || form.logradouro
        if (e.bairro)     form.bairro     = e.bairro     || form.bairro
        if (e.cidade)     form.cidade     = e.cidade     || form.cidade
        if (e.uf)         form.uf         = e.uf         || form.uf
        if (!form.complemento && e.complemento) form.complemento = e.complemento
    },
    {
        onLimpar: () => {
            form.logradouro = ""
            form.bairro     = ""
            form.cidade     = ""
            form.uf         = ""
        },
    },
)

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
        if (mUf) { form.cidade = mUf[1].trim(); form.uf = mUf[2] }
        else { form.cidade = ultima }
    }
}

function montarEndereco(): string {
    const partes: string[] = []
    if (form.logradouro)  partes.push(form.logradouro + (form.numero ? `, ${form.numero}` : ""))
    if (form.complemento) partes.push(form.complemento)
    if (form.bairro)      partes.push(form.bairro)
    if (form.cidade)      partes.push(form.cidade + (form.uf ? ` - ${form.uf}` : ""))
    if (form.cep)         partes.push(`CEP ${form.cep}`)
    return partes.join(", ")
}

// ─── Tags + alertas ────────────────────────────────────────────────────────
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

// ─── Validação + persist ───────────────────────────────────────────────────
const valido = computed(() => {
    if (!form.nomeCompleto.trim()) return false
    if (modo.value === "criar" && !expandirCadastro.value) {
        // Cadastro rápido: nome obrigatório; CPF e telefone fortemente recomendados.
        // Aceitamos só o nome para reduzir atrito (igual ao design "preencha o resto depois").
        return true
    }
    if (form.tipoDocumento === "cpf" && form.documento.trim() && !cpfValido(form.documento)) return false
    return true
})

async function salvar() {
    if (!valido.value || salvando.value) return
    salvando.value = true
    erro.value = null

    try {
        const docValor = form.documento.trim()
        if (docValor && form.tipoDocumento === "cpf" && !cpfValido(docValor)) {
            erro.value = "CPF inválido."
            return
        }

        const payload: PacientePayload = {
            nomeCompleto:           form.nomeCompleto.trim(),
            cpf:                    form.tipoDocumento === "cpf" && docValor ? docValor : undefined,
            documentoInternacional: form.tipoDocumento === "internacional" && docValor ? docValor : undefined,
            dataNascimento:         form.dataNascimento || undefined,
            genero:                 form.genero || undefined,
            telefone:               form.celular || form.telefoneFixo || undefined,
            email:                  form.email || undefined,
            endereco:               montarEndereco() || undefined,
            observacoes:            form.observacoes || undefined,
            tags:                   form.tags.length    ? [...form.tags]    : [],
            alertas:                form.alertas.length ? [...form.alertas] : [],
        }

        if (props.paciente?.id) {
            await pacienteService.atualizar(props.paciente.id, payload)
            const atualizado = await pacienteService.obter(props.paciente.id)
            emit("salvo", atualizado)
        } else {
            await pacienteService.criar(payload)
            // POST não retorna ID; busca pelo nome recém-criado.
            const pg = await pacienteService.listar(form.nomeCompleto, 1, 5)
            const criado = pg.itens.find(i => i.nomeCompleto === form.nomeCompleto.trim()) ?? pg.itens[0]
            if (criado) {
                const completo = await pacienteService.obter(criado.id)
                emit("salvo", completo)
            } else {
                emit("fechar")
            }
        }
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao salvar paciente."
    } finally {
        salvando.value = false
    }
}

function fechar() {
    if (salvando.value) return
    emit("fechar")
}

// Largura do modal: cadastro rápido cabe em md; completo precisa de lg.
const largura = computed<"md" | "lg">(() => (formCompleto.value ? "lg" : "md"))

const titulo = computed(() => (modo.value === "editar" ? "Editar paciente" : "Cadastrar paciente"))
const subtitulo = computed(() =>
    modo.value === "editar"
        ? "Atualize os dados do paciente."
        : (expandirCadastro.value
            ? "Cadastro completo — preencha o que tiver disponível."
            : "Cadastro rápido — só o essencial. Detalhes podem ser preenchidos depois."),
)
</script>

<template>
    <AppModal :aberto="aberto" :largura="largura" @fechar="fechar">
        <template #titulo>
            <div class="modal-titulo">
                <h2>{{ titulo }}</h2>
                <span>{{ subtitulo }}</span>
            </div>
        </template>

        <!-- ── Cadastro rápido (modo criar, não-expandido) ── -->
        <div v-if="!formCompleto" class="form-rapido">
            <div class="step-info">
                <i class="fa-solid fa-user-plus"></i>
                <div>
                    <b>Cadastro rápido</b>
                    <span>Só o essencial. Endereço, alergias e mais podem ser completados depois.</span>
                </div>
            </div>

            <div class="form-grid">
                <AppField label="Nome completo" required class="full">
                    <AppInput v-model="form.nomeCompleto" placeholder="Ex: Carla Mendes Souza" :disabled="salvando" />
                </AppField>

                <AppField label="CPF (recomendado)">
                    <AppInput
                        v-model="form.documento"
                        v-maska="'###.###.###-##'"
                        placeholder="000.000.000-00"
                        :disabled="salvando"
                    />
                </AppField>

                <AppField label="Telefone (recomendado)">
                    <AppInput
                        v-model="form.celular"
                        v-maska="'(##) #####-####'"
                        type="tel"
                        placeholder="(00) 00000-0000"
                        :disabled="salvando"
                    />
                </AppField>

                <AppField label="Data de nascimento">
                    <AppDatePicker v-model="form.dataNascimento" :disabled="salvando" />
                </AppField>

                <AppField label="Sexo">
                    <AppSelect v-model="form.genero" :disabled="salvando">
                        <option v-for="g in GENEROS" :key="g.value" :value="g.value">{{ g.label }}</option>
                    </AppSelect>
                </AppField>
            </div>

            <button type="button" class="link-completo" :disabled="salvando" @click="expandirCadastro = true">
                <i class="fa-solid fa-plus-circle"></i>
                Cadastrar com mais detalhes (endereço, tags, alertas, observações)
            </button>
        </div>

        <!-- ── Cadastro completo (modo editar OU criar-expandido) ── -->
        <div v-else class="form-completo">
            <button
                v-if="expandirCadastro"
                type="button"
                class="link-voltar"
                :disabled="salvando"
                @click="expandirCadastro = false"
            >
                <i class="fa-solid fa-arrow-left"></i>
                Voltar para cadastro rápido
            </button>

            <section class="secao">
                <h3 class="ds-section-title">Dados pessoais</h3>
                <div class="form-grid">
                    <AppField label="Nome completo" required class="full">
                        <AppInput v-model="form.nomeCompleto" :disabled="salvando" />
                    </AppField>

                    <AppField label="Documento">
                        <div class="tabs-doc">
                            <button
                                type="button"
                                class="tab-doc"
                                :class="{ ativa: form.tipoDocumento === 'cpf' }"
                                @click="form.tipoDocumento = 'cpf'; form.documento = ''"
                            >CPF</button>
                            <button
                                type="button"
                                class="tab-doc"
                                :class="{ ativa: form.tipoDocumento === 'internacional' }"
                                @click="form.tipoDocumento = 'internacional'; form.documento = ''"
                            >Internacional</button>
                        </div>
                        <AppInput
                            v-if="form.tipoDocumento === 'cpf'"
                            v-model="form.documento" v-maska="'###.###.###-##'"
                            placeholder="000.000.000-00" :disabled="salvando"
                        />
                        <AppInput
                            v-else
                            v-model="form.documento"
                            placeholder="Nº do documento (passaporte, RNE...)"
                            maxlength="30" :disabled="salvando"
                        />
                    </AppField>

                    <AppField label="Data de nascimento">
                        <AppDatePicker v-model="form.dataNascimento" :disabled="salvando" />
                    </AppField>

                    <AppField label="Sexo">
                        <AppSelect v-model="form.genero" :disabled="salvando">
                            <option v-for="g in GENEROS" :key="g.value" :value="g.value">{{ g.label }}</option>
                        </AppSelect>
                    </AppField>
                </div>
            </section>

            <section class="secao">
                <h3 class="ds-section-title">Contato</h3>
                <div class="form-grid">
                    <AppField label="Celular">
                        <AppInput
                            v-model="form.celular" v-maska="'(##) #####-####'" type="tel"
                            placeholder="(00) 00000-0000" :disabled="salvando"
                        />
                    </AppField>

                    <AppField label="Telefone fixo">
                        <AppInput
                            v-model="form.telefoneFixo" v-maska="'(##) ####-####'" type="tel"
                            placeholder="(00) 0000-0000" :disabled="salvando"
                        />
                    </AppField>

                    <AppField label="E-mail" class="full">
                        <AppInput
                            v-model="form.email" type="email"
                            placeholder="paciente@exemplo.com" :disabled="salvando"
                        />
                    </AppField>
                </div>
            </section>

            <section class="secao">
                <h3 class="ds-section-title">Endereço</h3>
                <div class="form-grid">
                    <AppField :label="buscandoCep ? 'CEP (buscando...)' : 'CEP'">
                        <AppInput
                            v-model="form.cep" v-maska="'#####-###'"
                            placeholder="00000-000" :disabled="salvando"
                        />
                    </AppField>

                    <AppField label="UF">
                        <AppSelect v-model="form.uf" :disabled="salvando">
                            <option value="">—</option>
                            <option v-for="u in UFS" :key="u" :value="u">{{ u }}</option>
                        </AppSelect>
                    </AppField>

                    <AppField label="Logradouro" class="full">
                        <AppInput v-model="form.logradouro" placeholder="Rua, avenida..." :disabled="salvando" />
                    </AppField>

                    <AppField label="Número">
                        <AppInput v-model="form.numero" :disabled="salvando" />
                    </AppField>

                    <AppField label="Complemento">
                        <AppInput v-model="form.complemento" placeholder="Apto, bloco..." :disabled="salvando" />
                    </AppField>

                    <AppField label="Bairro">
                        <AppInput v-model="form.bairro" :disabled="salvando" />
                    </AppField>

                    <AppField label="Cidade">
                        <AppInput v-model="form.cidade" :disabled="salvando" />
                    </AppField>
                </div>
            </section>

            <section class="secao">
                <h3 class="ds-section-title">Tags clínicas</h3>
                <p class="secao-hint">Use tags para classificar o paciente nos filtros da lista.</p>
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
                <h3 class="ds-section-title">Alertas clínicos</h3>
                <p class="secao-hint">
                    Avisos críticos que aparecem no detalhe (ex: "Alergia a penicilina"). Máximo 10.
                </p>
                <div v-if="form.alertas.length" class="alertas-lista">
                    <div v-for="(a, i) in form.alertas" :key="i" class="alerta-item">
                        <i class="fa-solid fa-triangle-exclamation"></i>
                        <span>{{ a }}</span>
                        <button
                            type="button" class="alerta-remover"
                            :disabled="salvando" aria-label="Remover alerta"
                            @click="removerAlerta(i)"
                        ><i class="fa-solid fa-xmark"></i></button>
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
                    >Adicionar</AppButton>
                </div>
            </section>

            <section class="secao">
                <h3 class="ds-section-title">Observações</h3>
                <AppTextarea
                    v-model="form.observacoes"
                    :rows="3"
                    placeholder="Notas internas sobre o paciente..."
                    :disabled="salvando"
                />
            </section>
        </div>

        <p v-if="erro" class="msg-erro">{{ erro }}</p>

        <template #rodape>
            <AppButton variant="ghost" :disabled="salvando" @click="fechar">Cancelar</AppButton>
            <AppButton
                icon="fa-solid fa-floppy-disk"
                :loading="salvando"
                :disabled="!valido || salvando"
                @click="salvar"
            >
                {{ paciente ? "Salvar alterações" : "Cadastrar paciente" }}
            </AppButton>
        </template>
    </AppModal>
</template>

<style scoped>
.modal-titulo h2 {
    font-size: var(--text-lg); font-weight: var(--font-weight-bold);
    color: hsl(var(--primary-dark)); margin: 0 0 2px;
}
.modal-titulo span {
    font-size: 13px; color: hsl(var(--secondary) / 0.65);
}

/* Step-info do design */
.step-info {
    display: flex; align-items: flex-start; gap: 12px;
    background: hsl(var(--primary) / 0.05);
    border: 1px solid hsl(var(--primary) / 0.15);
    border-radius: 8px; padding: 12px 14px; margin-bottom: 18px;
}
.step-info > i {
    width: 32px; height: 32px; border-radius: 50%;
    background: hsl(var(--primary) / 0.12); color: hsl(var(--primary));
    display: inline-flex; align-items: center; justify-content: center;
    font-size: 14px; flex-shrink: 0;
}
.step-info b { display: block; font-size: 14px; color: hsl(var(--primary-dark)); }
.step-info span { font-size: 12px; color: hsl(var(--secondary) / 0.7); }

/* Form grid (2 colunas, full quebra para 100%) */
.form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; }
.form-grid :deep(.full) { grid-column: 1 / -1; }

/* Link "expandir cadastro" */
.link-completo {
    width: 100%; margin-top: 14px;
    background: transparent;
    border: 1px dashed hsl(var(--secondary) / 0.25);
    border-radius: 8px;
    padding: 10px 12px;
    display: inline-flex; align-items: center; justify-content: center; gap: 8px;
    font-family: inherit; font-size: 12px; font-weight: 600;
    color: hsl(var(--primary)); cursor: pointer;
    transition: all 150ms;
}
.link-completo:hover:not(:disabled) {
    border-color: hsl(var(--primary) / 0.5);
    background: hsl(var(--primary) / 0.04);
}

/* Botão "voltar para cadastro rápido" */
.link-voltar {
    margin-bottom: 18px;
    background: transparent; border: none;
    display: inline-flex; align-items: center; gap: 6px;
    padding: 4px 0;
    font-family: inherit; font-size: 13px; font-weight: 600;
    color: hsl(var(--primary)); cursor: pointer;
    transition: color 150ms;
}
.link-voltar:hover:not(:disabled) { color: hsl(var(--primary-dark)); }
.link-voltar:disabled { opacity: 0.5; cursor: not-allowed; }

/* Form completo: seções */
.form-completo .secao + .secao { margin-top: 22px; }
.secao-hint {
    font-size: 12px; color: hsl(var(--secondary) / 0.65);
    margin: -6px 0 8px; line-height: 1.4;
}

/* Tabs Documento */
.tabs-doc {
    display: flex; gap: 4px;
    background: hsl(var(--secondary) / 0.06);
    border-radius: 8px; padding: 3px;
    margin-bottom: 6px;
}
.tab-doc {
    flex: 1;
    padding: 6px 10px;
    border: none; background: transparent;
    cursor: pointer;
    font-family: inherit; font-size: 12px; font-weight: 600;
    color: hsl(var(--secondary) / 0.65);
    border-radius: 6px;
    transition: all 150ms;
}
.tab-doc:hover:not(.ativa) { color: hsl(var(--primary-dark)); }
.tab-doc.ativa {
    background: white;
    color: hsl(var(--primary-dark));
    box-shadow: 0 1px 3px rgb(0 0 0 / 0.08);
}

/* Tags */
.tag-grid { display: flex; flex-wrap: wrap; gap: 6px; }
.tag-pick {
    display: inline-flex; align-items: center; gap: 6px;
    background: white;
    border: 1.5px solid hsl(var(--secondary) / 0.15);
    padding: 6px 12px; border-radius: 999px;
    font-family: inherit; font-size: 12px; font-weight: 600;
    color: hsl(var(--secondary) / 0.7); cursor: pointer;
    transition: all 150ms;
}
.tag-pick:hover:not(:disabled) {
    border-color: hsl(var(--primary) / 0.4);
    color: hsl(var(--primary-dark));
}
.tag-pick:disabled { opacity: 0.5; cursor: not-allowed; }
.tag-pick i { font-size: 11px; }

/* Alertas */
.alertas-lista { display: flex; flex-direction: column; gap: 6px; margin-bottom: 8px; }
.alerta-item {
    display: flex; align-items: center; gap: 8px;
    background: hsl(var(--error) / 0.06);
    border: 1px solid hsl(var(--error) / 0.2);
    border-left-width: 3px;
    padding: 8px 12px; border-radius: 8px;
    font-size: 13px; color: hsl(0 70% 30%);
}
.alerta-item > i:first-child { color: hsl(var(--error)); flex-shrink: 0; font-size: 12px; }
.alerta-item > span { flex: 1; }
.alerta-remover {
    background: none; border: none; cursor: pointer;
    color: hsl(var(--error) / 0.7); font-size: 13px;
    padding: 2px 6px; border-radius: 4px;
}
.alerta-remover:hover:not(:disabled) {
    background: hsl(var(--error) / 0.12);
    color: hsl(var(--error));
}

.alerta-novo { display: flex; gap: 8px; align-items: stretch; }
.alerta-novo > :first-child { flex: 1; }

.msg-erro {
    color: hsl(var(--error));
    background: hsl(var(--error) / 0.06);
    border: 1px solid hsl(var(--error) / 0.2);
    border-radius: 8px; padding: 8px 12px;
    font-size: 13px; margin: 12px 0 0;
}

@media (max-width: 720px) {
    .form-grid { grid-template-columns: 1fr; }
    .alerta-novo { flex-direction: column; }
}
</style>
