<script setup lang="ts">
/**
 * OnboardingView — wizard de 5 passos fiel ao design Anthropic/Onboarding.html.
 *
 *   Step 1 — Sua conta (tipo dono/convidado + nome + cpf + telefone)
 *   Step 2 — Sua clínica (nome, cnpj, telefone, cep, cidade/uf, endereço, tamanho)
 *   Step 3 — Especialidade (chips multi-select + conselho + número + tipos atendimento)
 *   Step 4 — Horários (editor por dia + duração + intervalo)
 *   Step 5 — Tour (cards visuais)
 *
 * Backend integration (salva tudo ao final):
 *   - Step 1 → usuarioService.completarOnboarding (nome, cpf, telefone)
 *   - Step 2 → estabelecimentoService.criar (apenas se dono)
 *   - Step 3 → profissionalService.salvar (conselho, uf, numeroRegistro, especialidade)
 *   - Step 4 → estabelecimentoService.atualizarFuncionamento (apenas se dono)
 */
import { computed, onMounted, reactive, ref, watch } from "vue"
import { useRouter } from "vue-router"
import { vMaska } from "maska/vue"
import { useAuthStore } from "@/stores/authStore"
import { useTenantStore } from "@/stores/tenantStore"
import { usuarioService } from "@/services/usuarioService"
import { estabelecimentoService } from "@/services/estabelecimentoService"
import { profissionalService } from "@/services/profissionalService"
import { vinculoService } from "@/services/vinculoService"
import { buscarPorCep } from "@/services/viaCepService"
import { useDebouncedRef } from "@/composables/useDebouncedRef"

const router = useRouter()
const auth = useAuthStore()
const tenant = useTenantStore()

const TOTAL = 5
const step = ref<1 | 2 | 3 | 4 | 5>(1)
const carregando = ref(false)
const erro = ref<string | null>(null)

/**
 * Origem do tipo de conta:
 * - "convite": detectado por convite pendente — usuário não pode escolher dono.
 * - "manual": usuário marcou owner/invited na primeira tela.
 */
const origemTipoConta = ref<"convite" | "manual">("manual")

// ─── Step 1: Sua conta ───
type TipoConta = "owner" | "invited"

const conta = reactive({
    tipo: "owner" as TipoConta,
    nomeCompleto: "",
    telefone: "",
    cpf: "",
})

// ─── Validação de CPF (formato + duplicidade no backend) ───
type CpfStatus = "vazio" | "incompleto" | "checando" | "valido" | "invalido" | "duplicado"

const cpfStatus = ref<CpfStatus>("vazio")
const cpfMensagem = ref<string | null>(null)
const cpfRef = computed(() => conta.cpf)
const cpfDebounced = useDebouncedRef(cpfRef, 350)

/** Validador do dígito verificador do CPF (algoritmo padrão brasileiro). */
function cpfFormatoValido(digitos: string): boolean {
    if (digitos.length !== 11) return false
    if (/^(\d)\1{10}$/.test(digitos)) return false
    const nums = [...digitos].map(Number)
    let soma1 = 0
    for (let i = 0; i < 9; i++) soma1 += nums[i] * (10 - i)
    let dv1 = (soma1 * 10) % 11
    if (dv1 === 10) dv1 = 0
    if (dv1 !== nums[9]) return false
    let soma2 = 0
    for (let i = 0; i < 10; i++) soma2 += nums[i] * (11 - i)
    let dv2 = (soma2 * 10) % 11
    if (dv2 === 10) dv2 = 0
    return dv2 === nums[10]
}

let cpfReqId = 0
watch(cpfDebounced, async (valor) => {
    const digitos = valor.replace(/\D/g, "")
    if (digitos.length === 0) {
        cpfStatus.value = "vazio"
        cpfMensagem.value = null
        return
    }
    if (digitos.length < 11) {
        cpfStatus.value = "incompleto"
        cpfMensagem.value = null
        return
    }
    if (!cpfFormatoValido(digitos)) {
        cpfStatus.value = "invalido"
        cpfMensagem.value = "CPF inválido."
        return
    }
    // Formato OK → consulta backend para duplicidade.
    cpfStatus.value = "checando"
    cpfMensagem.value = null
    const reqId = ++cpfReqId
    try {
        const r = await usuarioService.verificarCpfDisponivel(digitos)
        if (reqId !== cpfReqId) return // descarta resposta obsoleta
        if (!r.valido) {
            cpfStatus.value = "invalido"
            cpfMensagem.value = r.motivo ?? "CPF inválido."
        } else if (!r.disponivel) {
            cpfStatus.value = "duplicado"
            cpfMensagem.value = r.motivo ?? "CPF já cadastrado em outra conta."
        } else {
            cpfStatus.value = "valido"
            cpfMensagem.value = null
        }
    } catch {
        if (reqId !== cpfReqId) return
        // Falha de rede não bloqueia: o backend valida de novo no submit.
        cpfStatus.value = "valido"
        cpfMensagem.value = null
    }
}, { immediate: true })

// ─── Step 2: Sua clínica ───
const clinica = reactive({
    nome: "",
    cnpj: "",
    telefone: "",
    cep: "",
    cidadeUf: "",
    endereco: "",
    numero: "",
    tamanho: "Só eu",  // "Só eu" | "2 a 5" | "6 a 20" | "Mais de 20"
})

const TAMANHOS = ["Só eu", "2 a 5", "6 a 20", "Mais de 20"]

// ── CNPJ: validação inline (formato + duplicidade) ──
type CnpjStatus = "vazio" | "incompleto" | "checando" | "valido" | "invalido" | "duplicado"

const cnpjStatus = ref<CnpjStatus>("vazio")
const cnpjMensagem = ref<string | null>(null)
const cnpjRef = computed(() => clinica.cnpj)
const cnpjDebounced = useDebouncedRef(cnpjRef, 350)

function cnpjFormatoValido(digitos: string): boolean {
    if (digitos.length !== 14) return false
    if (/^(\d)\1{13}$/.test(digitos)) return false
    const nums = [...digitos].map(Number)
    const pesos1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]
    const pesos2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]
    let s1 = 0
    for (let i = 0; i < 12; i++) s1 += nums[i] * pesos1[i]
    let dv1 = s1 % 11
    dv1 = dv1 < 2 ? 0 : 11 - dv1
    if (dv1 !== nums[12]) return false
    let s2 = 0
    for (let i = 0; i < 13; i++) s2 += nums[i] * pesos2[i]
    let dv2 = s2 % 11
    dv2 = dv2 < 2 ? 0 : 11 - dv2
    return dv2 === nums[13]
}

let cnpjReqId = 0
watch(cnpjDebounced, async (valor) => {
    const digitos = valor.replace(/\D/g, "")
    if (digitos.length === 0) {
        cnpjStatus.value = "vazio"
        cnpjMensagem.value = null
        return
    }
    if (digitos.length < 14) {
        cnpjStatus.value = "incompleto"
        cnpjMensagem.value = null
        return
    }
    if (!cnpjFormatoValido(digitos)) {
        cnpjStatus.value = "invalido"
        cnpjMensagem.value = "CNPJ inválido."
        return
    }
    cnpjStatus.value = "checando"
    cnpjMensagem.value = null
    const reqId = ++cnpjReqId
    try {
        const r = await estabelecimentoService.verificarCnpjDisponivel(digitos)
        if (reqId !== cnpjReqId) return
        if (!r.valido) {
            cnpjStatus.value = "invalido"
            cnpjMensagem.value = r.motivo ?? "CNPJ inválido."
        } else if (!r.disponivel) {
            cnpjStatus.value = "duplicado"
            cnpjMensagem.value = r.motivo ?? "CNPJ já cadastrado em outro estabelecimento."
        } else {
            cnpjStatus.value = "valido"
            cnpjMensagem.value = null
        }
    } catch {
        if (reqId !== cnpjReqId) return
        cnpjStatus.value = "valido"  // se falhar a checagem, deixa avançar
        cnpjMensagem.value = null
    }
}, { immediate: true })

// ── CEP: busca automática no ViaCEP e preenche cidade/UF + endereço ──
const cepBuscando = ref(false)
const cepRef = computed(() => clinica.cep)
const cepDebounced = useDebouncedRef(cepRef, 400)

let cepReqId = 0
watch(cepDebounced, async (valor) => {
    const digitos = valor.replace(/\D/g, "")
    if (digitos.length !== 8) return
    cepBuscando.value = true
    const reqId = ++cepReqId
    const endereco = await buscarPorCep(digitos)
    if (reqId !== cepReqId) return
    cepBuscando.value = false
    if (endereco) {
        clinica.cidadeUf = `${endereco.cidade} / ${endereco.uf}`
        // Preenche endereço com logradouro + bairro se ainda não foi digitado nada.
        if (!clinica.endereco.trim()) {
            const partes = [endereco.logradouro, endereco.bairro].filter(Boolean)
            clinica.endereco = partes.join(" — ")
        }
    }
})

// ─── Step 3: Especialidade ───
const OUTROS_KEY = "Outros"

const ESPECIALIDADES = [
    { v: "Clínica Geral",   icon: "fa-stethoscope" },
    { v: "Cardiologia",     icon: "fa-heart-pulse" },
    { v: "Pediatria",       icon: "fa-baby" },
    { v: "Odontologia",     icon: "fa-tooth" },
    { v: "Oftalmologia",    icon: "fa-eye" },
    { v: "Psiquiatria",     icon: "fa-brain" },
    { v: "Psicologia",      icon: "fa-comments" },
    { v: "Ortopedia",       icon: "fa-bone" },
    { v: "Ginecologia",     icon: "fa-person-pregnant" },
    { v: "Dermatologia",    icon: "fa-spa" },
    { v: "Endocrinologia",  icon: "fa-disease" },
    { v: "Nutrição",        icon: "fa-apple-whole" },
    { v: "Fisioterapia",    icon: "fa-person-walking" },
    { v: OUTROS_KEY,        icon: "fa-plus" },
]

const especialidadesOutras = ref<string[]>([])  // lista de especialidades digitadas
const outraInput = ref("")                       // input atual

const TIPOS_ATEND = [
    { v: "Presencial",   icon: "fa-user-doctor" },
    { v: "Telemedicina", icon: "fa-video" },
    { v: "Home care",    icon: "fa-house-medical" },
]

const especialidadesSelecionadas = ref<string[]>([])
const conselho = ref("CRM")
const ufRegistro = ref("SP")
const numeroRegistro = ref("")
const tiposAtendimento = ref<string[]>(["Presencial"])

const CONSELHOS = [
    { v: "CRM",     l: "CRM — Conselho Regional de Medicina" },
    { v: "CRO",     l: "CRO — Odontologia" },
    { v: "CRP",     l: "CRP — Psicologia" },
    { v: "CRN",     l: "CRN — Nutrição" },
    { v: "CREFITO", l: "CREFITO — Fisioterapia" },
    { v: "Outro",   l: "Outro" },
]

function toggleEspecialidade(v: string) {
    const idx = especialidadesSelecionadas.value.indexOf(v)
    if (idx >= 0) especialidadesSelecionadas.value.splice(idx, 1)
    else especialidadesSelecionadas.value.push(v)
}

const outrosAtivo = computed(() => especialidadesSelecionadas.value.includes(OUTROS_KEY))

function adicionarOutra() {
    const valor = outraInput.value.trim()
    if (!valor) return
    if (especialidadesOutras.value.some(e => e.toLowerCase() === valor.toLowerCase())) {
        outraInput.value = ""
        return
    }
    especialidadesOutras.value.push(valor)
    outraInput.value = ""
}

function removerOutra(idx: number) {
    especialidadesOutras.value.splice(idx, 1)
}

function toggleTipoAtendimento(v: string) {
    const idx = tiposAtendimento.value.indexOf(v)
    if (idx >= 0) tiposAtendimento.value.splice(idx, 1)
    else tiposAtendimento.value.push(v)
}

// ─── Step 4: Horários ───
type DiaKey = "seg" | "ter" | "qua" | "qui" | "sex" | "sab" | "dom"

interface DiaConfig {
    key: DiaKey
    nome: string
    indice: number      // 0=Domingo .. 6=Sábado
    ativo: boolean
    inicio: string
    fim: string
}

const dias = ref<DiaConfig[]>([
    { key: "seg", nome: "Segunda", indice: 1, ativo: true, inicio: "08:00", fim: "18:00" },
    { key: "ter", nome: "Terça",   indice: 2, ativo: true, inicio: "08:00", fim: "18:00" },
    { key: "qua", nome: "Quarta",  indice: 3, ativo: true, inicio: "08:00", fim: "18:00" },
    { key: "qui", nome: "Quinta",  indice: 4, ativo: true, inicio: "08:00", fim: "18:00" },
    { key: "sex", nome: "Sexta",   indice: 5, ativo: true, inicio: "08:00", fim: "17:00" },
    { key: "sab", nome: "Sábado",  indice: 6, ativo: false, inicio: "09:00", fim: "13:00" },
    { key: "dom", nome: "Domingo", indice: 0, ativo: false, inicio: "09:00", fim: "13:00" },
])

const duracaoConsulta = ref("30")
const intervaloConsulta = ref("5")

function copiarParaTodos(idx: number) {
    const d = dias.value[idx]
    for (let i = 0; i < dias.value.length; i++) {
        if (i === idx) continue
        dias.value[i].inicio = d.inicio
        dias.value[i].fim = d.fim
    }
}

// ─── Validação por passo ───
const podeAvancar = computed(() => {
    if (step.value === 1) {
        return conta.nomeCompleto.trim().length >= 3
            && cpfStatus.value === "valido"
    }
    if (step.value === 2) {
        if (conta.tipo !== "owner") return true  // convidado pula clínica
        if (clinica.nome.trim().length < 2) return false
        // CNPJ é opcional, mas se preenchido precisa estar válido e disponível.
        if (cnpjStatus.value === "incompleto"
            || cnpjStatus.value === "invalido"
            || cnpjStatus.value === "duplicado"
            || cnpjStatus.value === "checando") return false
        return true
    }
    if (step.value === 3) {
        if (conta.tipo === "invited") return true
        const semSelecao = especialidadesSelecionadas.value.length === 0
        if (semSelecao) return false
        // Se marcou "Outros", precisa ter ao menos uma especialidade digitada.
        if (outrosAtivo.value && especialidadesOutras.value.length === 0) return false
        return true
    }
    if (step.value === 4) return true
    return true
})

// ─── Navegação ───
function podeMostrar(n: number): boolean {
    // Convidado pula passos de clínica e horários (2 e 4).
    if (conta.tipo === "invited" && (n === 2 || n === 4)) return false
    return true
}

function passoAtualLabel(n: number) {
    return ["Sua conta", "Sua clínica", "Especialidade", "Horários", "Tour"][n - 1]
}

function avancar() {
    if (!podeAvancar.value) return
    if (step.value === TOTAL) return
    let next = step.value + 1
    while (!podeMostrar(next) && next < TOTAL) next++
    step.value = next as 1 | 2 | 3 | 4 | 5
    scrollTopo()
}

function voltar() {
    if (step.value === 1) return
    let prev = step.value - 1
    while (!podeMostrar(prev) && prev > 1) prev--
    step.value = prev as 1 | 2 | 3 | 4 | 5
    scrollTopo()
}

function pular() {
    avancar()
}

function scrollTopo() {
    document.getElementById("onbCard")?.scrollIntoView({ behavior: "smooth", block: "start" })
}

// ─── Submit final ───
async function finalizar() {
    erro.value = null
    carregando.value = true
    try {
        // 1) Completa cadastro do usuário (sempre)
        await usuarioService.completarOnboarding({
            nomeCompleto: conta.nomeCompleto.trim(),
            cpf: conta.cpf.replace(/\D/g, ""),
            telefone: conta.telefone.trim() || undefined,
        })

        // 2) Cria estabelecimento (apenas se dono)
        if (conta.tipo === "owner" && clinica.nome.trim()) {
            // Endereço final: "Rua X, 123 — Bairro Y, Cidade/UF, CEP"
            const ruaENumero = [clinica.endereco.trim(), clinica.numero.trim()].filter(Boolean).join(", ")
            const enderecoCompleto = [ruaENumero, clinica.cidadeUf, clinica.cep].filter(Boolean).join(" — ")
            await estabelecimentoService.criar({
                nomeFantasia: clinica.nome.trim(),
                cnpj: clinica.cnpj.replace(/\D/g, "") || undefined,
                telefone: clinica.telefone || undefined,
                endereco: enderecoCompleto || undefined,
            })
        }

        // 3) Salva perfil profissional (especialidade + conselho)
        if (conta.tipo === "owner" || conta.tipo === "invited") {
            // Lista final de especialidades: predefinidas + customizadas (sem o token "Outros").
            const lista = [
                ...especialidadesSelecionadas.value.filter(e => e !== OUTROS_KEY),
                ...especialidadesOutras.value,
            ]
            // O backend hoje aceita apenas uma string única — concatenamos com vírgula
            // para preservar todas. Quando o domínio suportar lista, basta trocar aqui.
            const especialidade = lista.join(", ")
            if (especialidade && numeroRegistro.value.trim()) {
                try {
                    const existente = await profissionalService.obterMeu()
                    await profissionalService.salvar(
                        {
                            conselho: conselho.value,
                            uf: ufRegistro.value,
                            numeroRegistro: numeroRegistro.value.trim(),
                            especialidade,
                        },
                        existente !== null,
                    )
                } catch { /* não crítico — pode preencher depois */ }
            }
        }

        // 4) Atualiza horários do estabelecimento (apenas se dono)
        if (conta.tipo === "owner") {
            const lista = await estabelecimentoService.listarMeus()
            const meuEstab = lista.find(e => e.nomeFantasia === clinica.nome.trim()) ?? lista[0]
            if (meuEstab) {
                const ativos = dias.value.filter(d => d.ativo)
                if (ativos.length > 0) {
                    const inicio = ativos.reduce((min, d) => d.inicio < min ? d.inicio : min, ativos[0].inicio)
                    const fim = ativos.reduce((max, d) => d.fim > max ? d.fim : max, ativos[0].fim)
                    try {
                        await estabelecimentoService.atualizarFuncionamento(meuEstab.id, {
                            horarioInicio: inicio,
                            horarioFim: fim,
                            duracaoConsultaPadraoMinutos: Number(duracaoConsulta.value) || 30,
                            intervaloEntreConsultasMinutos: Number(intervaloConsulta.value) || 0,
                            diasSemana: ativos.map(d => d.indice),
                            horariosBloqueados: [],
                            datasBloqueadas: [],
                        })
                    } catch { /* não crítico */ }
                }
            }
        }

        // 5) Recarrega usuário e seleciona estabelecimento
        await auth.recarregarMe()
        const lista = await estabelecimentoService.listarMeus()
        if (lista.length === 1) {
            tenant.selecionar({
                id: lista[0].id,
                nomeFantasia: lista[0].nomeFantasia,
                papel: lista[0].papelDoUsuario,
            })
            router.replace({ name: "Home" })
        } else if (lista.length > 1) {
            router.replace({ name: "SelecionarEstabelecimento" })
        } else {
            router.replace({ name: "Home" })
        }
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Não foi possível concluir o cadastro."
    } finally {
        carregando.value = false
    }
}

// ─── Pré-preenchimento via convite pendente ───
//
// Se o usuário foi convidado, há ao menos um vínculo em status "Convidado" com
// possíveis dados pré-cadastrados (nome/telefone/especialidade). Trazemos do
// backend para evitar que o convidado redigite informações que o convidador já
// passou. Quando há convite, o tipo é forçado em "invited".
onMounted(async () => {
    try {
        const convites = await vinculoService.listarMeusConvites()
        if (convites.length === 0) return

        // Pega o convite mais recente (já vem ordenado DESC por convidado_em).
        const c = convites[0]
        conta.tipo = "invited"
        origemTipoConta.value = "convite"

        if (c.nomeConvidado && !conta.nomeCompleto) {
            conta.nomeCompleto = c.nomeConvidado
        }
        if (c.telefoneConvidado && !conta.telefone) {
            // Aplica máscara visual leve para telefone brasileiro (10 ou 11 dígitos).
            const d = c.telefoneConvidado
            if (d.length === 11) {
                conta.telefone = `(${d.slice(0, 2)}) ${d.slice(2, 7)}-${d.slice(7)}`
            } else if (d.length === 10) {
                conta.telefone = `(${d.slice(0, 2)}) ${d.slice(2, 6)}-${d.slice(6)}`
            } else {
                conta.telefone = d
            }
        }
        if (c.especialidadeConvidada) {
            // Tenta encontrar uma das especialidades padrão (case-insensitive);
            // se não bater, adiciona como "Outra" digitada pelo usuário.
            const padrao = ESPECIALIDADES.find(e =>
                e.v.toLowerCase() === c.especialidadeConvidada!.toLowerCase()
            )
            if (padrao) {
                if (!especialidadesSelecionadas.value.includes(padrao.v)) {
                    especialidadesSelecionadas.value.push(padrao.v)
                }
            } else {
                if (!especialidadesSelecionadas.value.includes(OUTROS_KEY)) {
                    especialidadesSelecionadas.value.push(OUTROS_KEY)
                }
                if (!especialidadesOutras.value.some(e =>
                    e.toLowerCase() === c.especialidadeConvidada!.toLowerCase()
                )) {
                    especialidadesOutras.value.push(c.especialidadeConvidada)
                }
            }
        }
    } catch {
        // Falha ao buscar convites não bloqueia o onboarding — usuário preenche do zero.
    }
})

// ─── UI helpers ───
const stepperPassos = computed(() => [
    { n: 1, label: "Sua conta",     visivel: true },
    { n: 2, label: "Sua clínica",   visivel: podeMostrar(2) },
    { n: 3, label: "Especialidade", visivel: true },
    { n: 4, label: "Horários",      visivel: podeMostrar(4) },
    { n: 5, label: "Tour",          visivel: true },
].filter(p => p.visivel))
</script>

<template>
    <main class="onb-shell">
        <!-- ── Top bar ── -->
        <header class="onb-top">
            <div class="auth-logo">
                <div class="auth-logo-mark">i</div>
                <div class="auth-logo-text">im<em>edto</em></div>
            </div>
            <div class="help">
                Precisa de ajuda?
                <a href="mailto:contato.imedto@gmail.com">Falar com suporte</a>
            </div>
        </header>

        <!-- ── Stepper ── -->
        <div class="onb-progress">
            <div class="onb-progress-track">
                <div
                    v-for="(s, i) in stepperPassos"
                    :key="s.n"
                    class="onb-step"
                    :class="{ active: step === s.n, done: step > s.n }"
                    :style="{ '--last': i === stepperPassos.length - 1 ? '1' : '0' }"
                >
                    <div class="num">
                        <i v-if="step > s.n" class="fa-solid fa-check" aria-hidden="true"></i>
                        <template v-else>{{ s.n }}</template>
                    </div>
                    <div class="lbl">{{ s.label }}</div>
                </div>
            </div>
        </div>

        <!-- ── Body ── -->
        <section class="onb-body">
            <div class="onb-card" id="onbCard">

                <!-- ── Step 1 — Sua conta ── -->
                <div v-if="step === 1" class="step-pane fade-step">
                    <h2>Vamos começar pela sua conta</h2>
                    <p class="onb-sub">
                        Estes dados serão usados para identificar você dentro da plataforma.
                        Você poderá editá-los depois nas configurações.
                    </p>

                    <div v-if="origemTipoConta === 'convite'" class="info-convite">
                        <i class="fa-solid fa-circle-check" aria-hidden="true"></i>
                        <div>
                            <b>Você foi convidado para uma clínica.</b>
                            <span>Já preenchemos o que pudemos com base no convite — confira e ajuste se precisar.</span>
                        </div>
                    </div>

                    <div v-else class="choice-grid">
                        <button
                            type="button"
                            class="choice-card"
                            :class="{ active: conta.tipo === 'owner' }"
                            @click="conta.tipo = 'owner'"
                        >
                            <div class="ch-icon"><i class="fa-solid fa-building-user" aria-hidden="true"></i></div>
                            <div class="ch-info">
                                <b>Sou dono de clínica</b>
                                <span>Vou cadastrar minha clínica e convidar minha equipe</span>
                            </div>
                            <div class="ch-check"><i class="fa-solid fa-check" aria-hidden="true"></i></div>
                        </button>
                        <button
                            type="button"
                            class="choice-card"
                            :class="{ active: conta.tipo === 'invited' }"
                            @click="conta.tipo = 'invited'"
                        >
                            <div class="ch-icon"><i class="fa-solid fa-user-plus" aria-hidden="true"></i></div>
                            <div class="ch-info">
                                <b>Fui convidado</b>
                                <span>Já tenho um código ou link de convite da minha clínica</span>
                            </div>
                            <div class="ch-check"><i class="fa-solid fa-check" aria-hidden="true"></i></div>
                        </button>
                    </div>

                    <div class="form-grid">
                        <div class="field">
                            <label>Nome completo</label>
                            <div class="input-wrap">
                                <i class="fa-regular fa-user" aria-hidden="true"></i>
                                <input
                                    v-model="conta.nomeCompleto"
                                    type="text"
                                    placeholder="Como devemos te chamar?"
                                    autocomplete="name"
                                />
                            </div>
                        </div>
                        <div class="field">
                            <label>Telefone <span class="hint">(opcional)</span></label>
                            <div class="input-wrap">
                                <i class="fa-solid fa-phone" aria-hidden="true"></i>
                                <input
                                    v-model="conta.telefone"
                                    v-maska="'(##) #####-####'"
                                    type="tel"
                                    placeholder="(11) 99999-0000"
                                    inputmode="numeric"
                                />
                            </div>
                        </div>
                        <div class="field full">
                            <label>CPF</label>
                            <div class="input-wrap" :class="`input-wrap--${cpfStatus}`">
                                <i class="fa-regular fa-id-card" aria-hidden="true"></i>
                                <input
                                    v-model="conta.cpf"
                                    v-maska="'###.###.###-##'"
                                    type="text"
                                    placeholder="000.000.000-00"
                                    inputmode="numeric"
                                />
                                <span class="cpf-status">
                                    <i v-if="cpfStatus === 'checando'" class="fa-solid fa-spinner fa-spin" aria-hidden="true"></i>
                                    <i v-else-if="cpfStatus === 'valido'" class="fa-solid fa-circle-check ok" aria-hidden="true"></i>
                                    <i v-else-if="cpfStatus === 'invalido' || cpfStatus === 'duplicado'" class="fa-solid fa-circle-exclamation err" aria-hidden="true"></i>
                                </span>
                            </div>
                            <p v-if="cpfMensagem" class="cpf-msg">{{ cpfMensagem }}</p>
                        </div>
                    </div>
                </div>

                <!-- ── Step 2 — Sua clínica ── -->
                <div v-else-if="step === 2" class="step-pane fade-step">
                    <h2>Conte sobre sua clínica</h2>
                    <p class="onb-sub">
                        Estes dados aparecem em recibos, prescrições e comprovantes enviados para os pacientes.
                    </p>

                    <div class="form-grid">
                        <div class="field full">
                            <label>Nome da clínica ou consultório</label>
                            <div class="input-wrap">
                                <i class="fa-solid fa-hospital" aria-hidden="true"></i>
                                <input
                                    v-model="clinica.nome"
                                    type="text"
                                    placeholder="Ex: Clínica Vida Saudável"
                                />
                            </div>
                        </div>
                        <div class="field">
                            <label>CNPJ <span class="hint">(opcional)</span></label>
                            <div class="input-wrap" :class="`input-wrap--${cnpjStatus}`">
                                <i class="fa-regular fa-id-card" aria-hidden="true"></i>
                                <input
                                    v-model="clinica.cnpj"
                                    v-maska="'##.###.###/####-##'"
                                    type="text"
                                    placeholder="00.000.000/0000-00"
                                />
                                <span class="cpf-status">
                                    <i v-if="cnpjStatus === 'checando'" class="fa-solid fa-spinner fa-spin" aria-hidden="true"></i>
                                    <i v-else-if="cnpjStatus === 'valido'" class="fa-solid fa-circle-check ok" aria-hidden="true"></i>
                                    <i v-else-if="cnpjStatus === 'invalido' || cnpjStatus === 'duplicado'" class="fa-solid fa-circle-exclamation err" aria-hidden="true"></i>
                                </span>
                            </div>
                            <p v-if="cnpjMensagem" class="cpf-msg">{{ cnpjMensagem }}</p>
                        </div>
                        <div class="field">
                            <label>Telefone principal</label>
                            <div class="input-wrap">
                                <i class="fa-solid fa-phone" aria-hidden="true"></i>
                                <input
                                    v-model="clinica.telefone"
                                    v-maska="'(##) ####-####'"
                                    type="tel"
                                />
                            </div>
                        </div>
                        <div class="field">
                            <label>
                                CEP
                                <span v-if="cepBuscando" class="hint">
                                    <i class="fa-solid fa-spinner fa-spin" aria-hidden="true"></i> buscando...
                                </span>
                            </label>
                            <div class="input-wrap">
                                <i class="fa-solid fa-location-dot" aria-hidden="true"></i>
                                <input
                                    v-model="clinica.cep"
                                    v-maska="'#####-###'"
                                    type="text"
                                    placeholder="00000-000"
                                />
                            </div>
                        </div>
                        <div class="field">
                            <label>Cidade / UF</label>
                            <div class="input-wrap">
                                <i class="fa-solid fa-city" aria-hidden="true"></i>
                                <input
                                    v-model="clinica.cidadeUf"
                                    type="text"
                                    placeholder="São Paulo / SP"
                                />
                            </div>
                        </div>
                        <div class="field full endereco-row">
                            <div class="endereco-main">
                                <label>Endereço</label>
                                <div class="input-wrap">
                                    <i class="fa-solid fa-map-pin" aria-hidden="true"></i>
                                    <input
                                        v-model="clinica.endereco"
                                        type="text"
                                        placeholder="Rua, complemento, bairro"
                                    />
                                </div>
                            </div>
                            <div class="endereco-numero">
                                <label>Número</label>
                                <div class="input-wrap">
                                    <i class="fa-solid fa-hashtag" aria-hidden="true"></i>
                                    <input
                                        v-model="clinica.numero"
                                        type="text"
                                        placeholder="123"
                                        inputmode="numeric"
                                    />
                                </div>
                            </div>
                        </div>
                        <div class="field full">
                            <label>Quantos profissionais atendem na clínica?</label>
                            <div class="choice-grid choice-grid--small">
                                <button
                                    v-for="t in TAMANHOS"
                                    :key="t"
                                    type="button"
                                    class="choice-card choice-card--small"
                                    :class="{ active: clinica.tamanho === t }"
                                    @click="clinica.tamanho = t"
                                >
                                    <div class="ch-info"><b>{{ t }}</b></div>
                                    <div class="ch-check"><i class="fa-solid fa-check" aria-hidden="true"></i></div>
                                </button>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- ── Step 3 — Especialidade ── -->
                <div v-else-if="step === 3" class="step-pane fade-step">
                    <h2>Qual sua área de atuação?</h2>
                    <p class="onb-sub">
                        Selecione todas que se aplicam. Vamos personalizar o prontuário,
                        modelos de receita e relatórios para a sua especialidade.
                    </p>

                    <div class="specialty-grid">
                        <button
                            v-for="e in ESPECIALIDADES"
                            :key="e.v"
                            type="button"
                            class="spec-chip"
                            :class="{ active: especialidadesSelecionadas.includes(e.v) }"
                            @click="toggleEspecialidade(e.v)"
                        >
                            <i :class="['fa-solid', e.icon]" aria-hidden="true"></i>
                            {{ e.v }}
                            <i v-if="especialidadesSelecionadas.includes(e.v)" class="fa-solid fa-xmark x" aria-hidden="true"></i>
                        </button>

                        <!-- Especialidades digitadas pelo usuário (vindas do botão Outros) -->
                        <button
                            v-for="(out, i) in especialidadesOutras"
                            :key="`out-${i}`"
                            type="button"
                            class="spec-chip active"
                            @click="removerOutra(i)"
                        >
                            <i class="fa-solid fa-circle-dot" aria-hidden="true"></i>
                            {{ out }}
                            <i class="fa-solid fa-xmark x" aria-hidden="true"></i>
                        </button>
                    </div>

                    <!-- Input para digitar especialidades quando "Outros" está marcado -->
                    <div v-if="outrosAtivo" class="outra-input-wrap">
                        <i class="fa-solid fa-pen" aria-hidden="true"></i>
                        <input
                            v-model="outraInput"
                            type="text"
                            placeholder="Digite a especialidade e pressione Enter"
                            @keydown.enter.prevent="adicionarOutra"
                        />
                        <button
                            type="button"
                            class="outra-add-btn"
                            :disabled="!outraInput.trim()"
                            @click="adicionarOutra"
                        >
                            <i class="fa-solid fa-plus" aria-hidden="true"></i> Adicionar
                        </button>
                    </div>

                    <div class="form-grid">
                        <div class="field">
                            <label>Conselho profissional</label>
                            <div class="input-wrap input-wrap--select">
                                <select v-model="conselho">
                                    <option v-for="c in CONSELHOS" :key="c.v" :value="c.v">{{ c.l }}</option>
                                </select>
                            </div>
                        </div>
                        <div class="field">
                            <label>Número do registro</label>
                            <div class="input-wrap">
                                <i class="fa-solid fa-id-badge" aria-hidden="true"></i>
                                <input
                                    v-model="numeroRegistro"
                                    type="text"
                                    :placeholder="`Ex: ${conselho}/SP 184.532`"
                                />
                            </div>
                        </div>
                        <div class="field full">
                            <label>Tipos de atendimento <span class="hint">selecione todos que oferece</span></label>
                            <div class="choice-grid choice-grid--triple">
                                <button
                                    v-for="t in TIPOS_ATEND"
                                    :key="t.v"
                                    type="button"
                                    class="choice-card choice-card--small"
                                    :class="{ active: tiposAtendimento.includes(t.v) }"
                                    @click="toggleTipoAtendimento(t.v)"
                                >
                                    <div class="ch-icon ch-icon--xs"><i :class="['fa-solid', t.icon]" aria-hidden="true"></i></div>
                                    <div class="ch-info"><b>{{ t.v }}</b></div>
                                    <div class="ch-check"><i class="fa-solid fa-check" aria-hidden="true"></i></div>
                                </button>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- ── Step 4 — Horários ── -->
                <div v-else-if="step === 4" class="step-pane fade-step">
                    <h2>Defina seu horário de atendimento</h2>
                    <p class="onb-sub">
                        Os pacientes só conseguirão agendar dentro destes horários.
                        Você pode ajustar dias específicos ou criar exceções depois.
                    </p>

                    <div class="schedule-editor">
                        <div
                            v-for="(d, i) in dias"
                            :key="d.key"
                            class="day-row"
                            :class="{ active: d.ativo, inactive: !d.ativo }"
                        >
                            <div class="dn">{{ d.nome }}</div>
                            <div class="switch" @click="d.ativo = !d.ativo"></div>
                            <div class="time-field"><input type="time" v-model="d.inicio" :disabled="!d.ativo" /></div>
                            <div class="arr"><i class="fa-solid fa-arrow-right" aria-hidden="true"></i></div>
                            <div class="time-field"><input type="time" v-model="d.fim" :disabled="!d.ativo" /></div>
                            <button
                                type="button"
                                class="copy"
                                title="Aplicar para todos os dias"
                                @click="copiarParaTodos(i)"
                            >
                                <i class="fa-solid fa-clone" aria-hidden="true"></i>
                            </button>
                        </div>
                    </div>

                    <div class="form-grid">
                        <div class="field">
                            <label>Duração padrão da consulta</label>
                            <div class="input-wrap input-wrap--select">
                                <select v-model="duracaoConsulta">
                                    <option value="20">20 minutos</option>
                                    <option value="30">30 minutos</option>
                                    <option value="45">45 minutos</option>
                                    <option value="60">60 minutos</option>
                                </select>
                            </div>
                        </div>
                        <div class="field">
                            <label>Intervalo entre consultas</label>
                            <div class="input-wrap input-wrap--select">
                                <select v-model="intervaloConsulta">
                                    <option value="0">Sem intervalo</option>
                                    <option value="5">5 minutos</option>
                                    <option value="10">10 minutos</option>
                                    <option value="15">15 minutos</option>
                                </select>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- ── Step 5 — Tour ── -->
                <div v-else-if="step === 5" class="step-pane fade-step">
                    <h2>Tudo pronto, {{ conta.nomeCompleto.split(" ")[0] || "" }} 🎉</h2>
                    <p class="onb-sub">
                        Sua conta está configurada. Aqui está um resumo rápido do que você pode fazer agora —
                        você sempre pode revisitar isso no menu de ajuda.
                    </p>

                    <div class="tour-grid">
                        <div class="tour-card">
                            <div class="tc-icon"><i class="fa-solid fa-calendar-days" aria-hidden="true"></i></div>
                            <b>Agenda inteligente</b>
                            <span>Visualize, confirme e remarque consultas. Receba pacientes no walk-in com 1 clique.</span>
                        </div>
                        <div class="tour-card">
                            <div class="tc-icon tc-icon--info"><i class="fa-solid fa-file-medical" aria-hidden="true"></i></div>
                            <b>Prontuário eletrônico</b>
                            <span>Templates por especialidade, anamnese, exames e evolução em um único lugar.</span>
                        </div>
                        <div class="tour-card">
                            <div class="tc-icon tc-icon--success"><i class="fa-solid fa-prescription-bottle-medical" aria-hidden="true"></i></div>
                            <b>Prescrição digital</b>
                            <span>Receituário e atestados assinados digitalmente, válidos em todo o território nacional.</span>
                        </div>
                        <div class="tour-card">
                            <div class="tc-icon tc-icon--warning"><i class="fa-solid fa-chart-line" aria-hidden="true"></i></div>
                            <b>Financeiro & relatórios</b>
                            <span>Acompanhe faturamento, taxa de ocupação, no-shows e indicadores em tempo real.</span>
                        </div>
                    </div>

                    <div class="tour-tip">
                        <div class="tip-icon"><i class="fa-solid fa-lightbulb" aria-hidden="true"></i></div>
                        <div>
                            <b>Quer começar com sua agenda já preenchida?</b>
                            <span>
                                Importe seus contatos do Google Agenda ou de uma planilha.
                                Você pode fazer isso depois também — fica em
                                <em>Configurações → Importar dados</em>.
                            </span>
                        </div>
                    </div>
                </div>

                <!-- ── Erro ── -->
                <div v-if="erro" class="alerta-erro">{{ erro }}</div>

                <!-- ── Footer nav ── -->
                <div class="onb-foot">
                    <div class="left">
                        <span class="step-of">
                            Etapa <b>{{ stepperPassos.findIndex(s => s.n === step) + 1 }}</b>
                            de <b>{{ stepperPassos.length }}</b>
                            <template v-if="step !== 1"> · {{ passoAtualLabel(step) }}</template>
                        </span>
                    </div>
                    <div class="right">
                        <button
                            v-if="step !== 1 && step !== TOTAL"
                            type="button"
                            class="skip"
                            @click="pular"
                        >Pular por agora</button>

                        <button
                            v-if="step !== 1"
                            type="button"
                            class="btn-secondary"
                            :disabled="carregando"
                            @click="voltar"
                        >
                            <i class="fa-solid fa-arrow-left" aria-hidden="true"></i> Voltar
                        </button>

                        <button
                            v-if="step !== TOTAL"
                            type="button"
                            class="btn-primary"
                            :disabled="!podeAvancar || carregando"
                            @click="avancar"
                        >
                            Continuar <i class="fa-solid fa-arrow-right" aria-hidden="true"></i>
                        </button>

                        <button
                            v-else
                            type="button"
                            class="btn-primary"
                            :disabled="carregando"
                            @click="finalizar"
                        >
                            <i v-if="carregando" class="fa-solid fa-spinner fa-spin" aria-hidden="true"></i>
                            {{ carregando ? "Salvando..." : "Acessar minha agenda" }}
                            <i v-if="!carregando" class="fa-solid fa-arrow-right" aria-hidden="true"></i>
                        </button>
                    </div>
                </div>
            </div>
        </section>
    </main>
</template>

<style scoped>
.onb-shell {
    min-height: 100vh;
    display: flex;
    flex-direction: column;
    background: hsl(var(--primary-light));
}

/* ── Top ── */
.onb-top {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 20px 40px;
    border-bottom: 1px solid hsl(var(--secondary) / 0.06);
    background: white;
}
.auth-logo {
    display: inline-flex;
    align-items: center;
    gap: 10px;
}
.auth-logo-mark {
    width: 38px;
    height: 38px;
    border-radius: 10px;
    background: linear-gradient(135deg, hsl(var(--primary)), hsl(var(--primary-dark)));
    display: flex;
    align-items: center;
    justify-content: center;
    color: white;
    font-weight: 700;
    font-size: 18px;
    letter-spacing: -0.02em;
}
.auth-logo-text {
    font-size: 22px;
    font-weight: 700;
    color: hsl(var(--primary-dark));
    letter-spacing: -0.02em;
}
.auth-logo-text em {
    font-style: normal;
    color: hsl(var(--primary));
}
.onb-top .help {
    font-size: 13px;
    color: hsl(var(--secondary) / 0.65);
}
.onb-top .help a {
    color: hsl(var(--primary));
    text-decoration: none;
    font-weight: 600;
}

/* ── Stepper ── */
.onb-progress {
    background: white;
    padding: 24px 40px 28px;
    border-bottom: 1px solid hsl(var(--secondary) / 0.06);
}
.onb-progress-track {
    position: relative;
    display: flex;
    justify-content: space-between;
    gap: 8px;
    max-width: 880px;
    margin: 0 auto;
}
.onb-step {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 8px;
    position: relative;
    flex: 1;
}
.onb-step::before {
    content: '';
    position: absolute;
    top: 14px;
    left: calc(50% + 18px);
    right: calc(-50% + 18px);
    height: 2px;
    background: hsl(var(--secondary) / 0.1);
    z-index: 0;
}
.onb-step:last-child::before { display: none; }
.onb-step.done::before { background: hsl(var(--success)); }
.onb-step .num {
    position: relative;
    z-index: 1;
    width: 28px;
    height: 28px;
    border-radius: 50%;
    background: white;
    border: 2px solid hsl(var(--secondary) / 0.15);
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 12px;
    font-weight: 700;
    color: hsl(var(--secondary) / 0.5);
    transition: all 160ms;
}
.onb-step.active .num {
    background: hsl(var(--primary));
    border-color: hsl(var(--primary));
    color: white;
    box-shadow: 0 0 0 4px hsl(var(--primary) / 0.15);
}
.onb-step.done .num {
    background: hsl(var(--success));
    border-color: hsl(var(--success));
    color: white;
}
.onb-step .lbl {
    font-size: 11px;
    font-weight: 600;
    color: hsl(var(--secondary) / 0.55);
    text-align: center;
    line-height: 1.3;
}
.onb-step.active .lbl { color: hsl(var(--primary-dark)); }
.onb-step.done .lbl { color: hsl(var(--success)); }

/* ── Body + Card ── */
.onb-body {
    flex: 1;
    display: flex;
    align-items: flex-start;
    justify-content: center;
    padding: 48px 24px;
}
.onb-card {
    width: 100%;
    max-width: 720px;
    background: white;
    border-radius: 18px;
    box-shadow: 0 1px 2px hsl(var(--primary-dark) / 0.04), 0 24px 60px hsl(var(--primary-dark) / 0.12);
    padding: 40px 48px;
}
.onb-card h2 {
    margin: 0 0 8px;
    font-size: 26px;
    font-weight: 700;
    color: hsl(var(--primary-dark));
    letter-spacing: -0.02em;
}
.onb-sub {
    margin: 0 0 32px;
    font-size: 14px;
    color: hsl(var(--secondary) / 0.65);
    line-height: 1.5;
}

/* ── Form grid ── */
.form-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 16px;
}
.field { display: flex; flex-direction: column; gap: 6px; }
.field.full { grid-column: 1 / -1; }
.field label {
    font-size: 12px;
    font-weight: 600;
    color: hsl(var(--primary-dark));
    display: flex;
    justify-content: space-between;
    align-items: center;
}
.field label .hint {
    font-size: 11px;
    font-weight: 400;
    color: hsl(var(--secondary) / 0.55);
}
.input-wrap { position: relative; }
.input-wrap > i {
    position: absolute;
    left: 14px;
    top: 50%;
    transform: translateY(-50%);
    color: hsl(var(--secondary) / 0.4);
    font-size: 13px;
    pointer-events: none;
}
.input-wrap input,
.input-wrap select {
    width: 100%;
    padding: 12px 14px 12px 40px;
    font-family: inherit;
    font-size: 14px;
    color: hsl(var(--primary-dark));
    background: white;
    border: 1.5px solid hsl(var(--secondary) / 0.12);
    border-radius: 10px;
    outline: none;
    transition: all 160ms;
}
.input-wrap input:focus,
.input-wrap select:focus {
    border-color: hsl(var(--primary));
    box-shadow: 0 0 0 4px hsl(var(--primary) / 0.1);
}
.input-wrap--select select {
    padding-left: 14px;
    appearance: none;
    background-image: url("data:image/svg+xml;charset=utf-8,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='12' viewBox='0 0 12 12'%3E%3Cpath d='M3 5l3 3 3-3' stroke='%23999' stroke-width='1.5' fill='none' stroke-linecap='round'/%3E%3C/svg%3E");
    background-repeat: no-repeat;
    background-position: right 14px center;
}

/* ── Linha endereço + número ── */
.endereco-row {
    display: grid;
    grid-template-columns: 1fr 140px;
    gap: 16px;
}
.endereco-main,
.endereco-numero {
    display: flex;
    flex-direction: column;
    gap: 6px;
    min-width: 0;
}

/* ── Feedback do CPF ── */
.input-wrap--valido input { border-color: hsl(var(--success)) !important; padding-right: 40px; }
.input-wrap--valido input:focus { box-shadow: 0 0 0 4px hsl(var(--success) / 0.12) !important; }
.input-wrap--invalido input,
.input-wrap--duplicado input { border-color: hsl(var(--error)) !important; padding-right: 40px; }
.input-wrap--invalido input:focus,
.input-wrap--duplicado input:focus { box-shadow: 0 0 0 4px hsl(var(--error) / 0.12) !important; }
.input-wrap--checando input { padding-right: 40px; }

.cpf-status {
    position: absolute;
    right: 14px;
    top: 50%;
    transform: translateY(-50%);
    pointer-events: none;
    font-size: 14px;
    color: hsl(var(--secondary) / 0.5);
    display: flex;
    align-items: center;
}
.cpf-status .ok { color: hsl(var(--success)); }
.cpf-status .err { color: hsl(var(--error)); }

.cpf-msg {
    margin: 4px 0 0;
    font-size: 11px;
    color: hsl(var(--error));
    display: inline-flex;
    align-items: center;
    gap: 4px;
}

/* ── Banner convite detectado ── */
.info-convite {
    display: flex;
    gap: 12px;
    align-items: flex-start;
    padding: 14px 16px;
    background: hsl(var(--primary) / 0.06);
    border: 1px solid hsl(var(--primary) / 0.2);
    border-radius: 12px;
    margin-bottom: 24px;
}
.info-convite > i {
    color: hsl(var(--primary));
    font-size: 18px;
    flex-shrink: 0;
    margin-top: 2px;
}
.info-convite b {
    display: block;
    font-size: 14px;
    font-weight: 600;
    color: hsl(var(--primary-dark));
    margin-bottom: 2px;
}
.info-convite span {
    font-size: 12px;
    color: hsl(var(--secondary) / 0.7);
    line-height: 1.4;
}

/* ── Choice cards ── */
.choice-grid {
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    gap: 12px;
    margin-bottom: 24px;
}
.choice-grid--small { grid-template-columns: repeat(4, 1fr); margin-bottom: 0; }
.choice-grid--triple { grid-template-columns: repeat(3, 1fr); margin-bottom: 0; }
.choice-card {
    position: relative;
    padding: 18px 16px;
    border: 1.5px solid hsl(var(--secondary) / 0.12);
    border-radius: 12px;
    background: white;
    cursor: pointer;
    display: flex;
    align-items: flex-start;
    gap: 12px;
    text-align: left;
    font-family: inherit;
    transition: all 160ms;
}
.choice-card:hover {
    border-color: hsl(var(--primary) / 0.4);
    background: hsl(var(--primary) / 0.02);
}
.choice-card.active {
    border-color: hsl(var(--primary));
    background: hsl(var(--primary) / 0.05);
    box-shadow: 0 0 0 4px hsl(var(--primary) / 0.08);
}
.choice-card--small { padding: 14px; }
.choice-card .ch-icon {
    width: 38px;
    height: 38px;
    border-radius: 10px;
    background: hsl(var(--primary) / 0.1);
    color: hsl(var(--primary));
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 16px;
    flex-shrink: 0;
}
.choice-card .ch-icon--xs { width: 32px; height: 32px; font-size: 13px; }
.choice-card.active .ch-icon { background: hsl(var(--primary)); color: white; }
.choice-card .ch-info b {
    display: block;
    font-size: 14px;
    font-weight: 600;
    color: hsl(var(--primary-dark));
    margin: 0 0 2px;
}
.choice-card .ch-info span {
    font-size: 12px;
    color: hsl(var(--secondary) / 0.65);
    line-height: 1.4;
}
.choice-card .ch-check {
    position: absolute;
    top: 12px;
    right: 12px;
    width: 20px;
    height: 20px;
    border-radius: 50%;
    border: 1.5px solid hsl(var(--secondary) / 0.2);
    display: flex;
    align-items: center;
    justify-content: center;
    background: white;
}
.choice-card .ch-check i { font-size: 10px; color: white; opacity: 0; }
.choice-card.active .ch-check {
    background: hsl(var(--primary));
    border-color: hsl(var(--primary));
}
.choice-card.active .ch-check i { opacity: 1; }

/* ── Specialty chips ── */
.specialty-grid {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
    margin-bottom: 24px;
}
.spec-chip {
    padding: 8px 14px;
    border-radius: 99px;
    background: white;
    border: 1.5px solid hsl(var(--secondary) / 0.12);
    font-size: 13px;
    font-weight: 500;
    color: hsl(var(--secondary) / 0.75);
    cursor: pointer;
    font-family: inherit;
    display: inline-flex;
    align-items: center;
    gap: 6px;
    transition: all 160ms;
}
.spec-chip:hover { border-color: hsl(var(--primary) / 0.4); }
.spec-chip.active {
    background: hsl(var(--primary) / 0.08);
    border-color: hsl(var(--primary));
    color: hsl(var(--primary-dark));
}
.spec-chip i { font-size: 11px; }
.spec-chip .x { opacity: 0.6; }

/* ── Input para "Outros" ── */
.outra-input-wrap {
    position: relative;
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 4px 4px 4px 14px;
    background: white;
    border: 1.5px solid hsl(var(--secondary) / 0.12);
    border-radius: 12px;
    margin-bottom: 24px;
    transition: all 160ms;
}
.outra-input-wrap:focus-within {
    border-color: hsl(var(--primary));
    box-shadow: 0 0 0 4px hsl(var(--primary) / 0.1);
}
.outra-input-wrap > i {
    color: hsl(var(--secondary) / 0.4);
    font-size: 12px;
}
.outra-input-wrap input {
    flex: 1;
    border: none;
    outline: none;
    background: transparent;
    font-family: inherit;
    font-size: 14px;
    color: hsl(var(--primary-dark));
    padding: 8px 0;
}
.outra-input-wrap input::placeholder { color: hsl(var(--secondary) / 0.45); }
.outra-add-btn {
    background: hsl(var(--primary));
    color: white;
    border: none;
    border-radius: 8px;
    padding: 8px 14px;
    font-family: inherit;
    font-size: 12px;
    font-weight: 600;
    cursor: pointer;
    display: inline-flex;
    align-items: center;
    gap: 6px;
    transition: background 160ms;
}
.outra-add-btn:hover:not(:disabled) { background: hsl(var(--primary-dark)); }
.outra-add-btn:disabled { opacity: 0.4; cursor: not-allowed; }

/* ── Schedule editor ── */
.schedule-editor {
    display: flex;
    flex-direction: column;
    gap: 8px;
    margin-bottom: 24px;
}
.day-row {
    display: grid;
    grid-template-columns: 90px 38px 1fr 14px 1fr 36px;
    align-items: center;
    gap: 12px;
    padding: 12px 14px;
    background: white;
    border: 1px solid hsl(var(--secondary) / 0.1);
    border-radius: 10px;
    transition: all 160ms;
}
.day-row.active {
    border-color: hsl(var(--primary) / 0.3);
    background: hsl(var(--primary) / 0.02);
}
.day-row .dn {
    font-size: 13px;
    font-weight: 600;
    color: hsl(var(--primary-dark));
}
.day-row.inactive .dn { color: hsl(var(--secondary) / 0.4); }
.day-row .switch {
    width: 32px;
    height: 18px;
    border-radius: 99px;
    background: hsl(var(--secondary) / 0.18);
    position: relative;
    cursor: pointer;
    transition: background 160ms;
}
.day-row .switch::after {
    content: '';
    position: absolute;
    top: 2px;
    left: 2px;
    width: 14px;
    height: 14px;
    border-radius: 50%;
    background: white;
    box-shadow: 0 1px 2px hsl(var(--secondary) / 0.3);
    transition: transform 160ms;
}
.day-row.active .switch { background: hsl(var(--primary)); }
.day-row.active .switch::after { transform: translateX(14px); }
.day-row input[type="time"] {
    width: 100%;
    padding: 8px 10px;
    font-family: inherit;
    font-size: 13px;
    background: white;
    border: 1px solid hsl(var(--secondary) / 0.12);
    border-radius: 8px;
    outline: none;
    color: hsl(var(--primary-dark));
    transition: all 160ms;
}
.day-row.inactive input {
    background: hsl(var(--secondary) / 0.04);
    color: hsl(var(--secondary) / 0.3);
}
.day-row input:focus {
    border-color: hsl(var(--primary));
    box-shadow: 0 0 0 3px hsl(var(--primary) / 0.1);
}
.day-row .arr {
    text-align: center;
    color: hsl(var(--secondary) / 0.4);
}
.day-row .copy {
    background: none;
    border: none;
    color: hsl(var(--secondary) / 0.4);
    cursor: pointer;
    padding: 4px;
    border-radius: 6px;
    transition: all 160ms;
}
.day-row .copy:hover {
    color: hsl(var(--primary));
    background: hsl(var(--primary) / 0.08);
}

/* ── Tour ── */
.tour-grid {
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    gap: 14px;
    margin-bottom: 24px;
}
.tour-card {
    padding: 18px;
    background: linear-gradient(180deg, hsl(var(--primary) / 0.04), white);
    border: 1px solid hsl(var(--primary) / 0.12);
    border-radius: 12px;
}
.tour-card .tc-icon {
    width: 36px;
    height: 36px;
    border-radius: 10px;
    background: hsl(var(--primary));
    color: white;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 14px;
    margin-bottom: 12px;
}
.tour-card .tc-icon--info { background: hsl(var(--info)); }
.tour-card .tc-icon--success { background: hsl(var(--success)); }
.tour-card .tc-icon--warning { background: hsl(var(--warning)); }
.tour-card b {
    display: block;
    font-size: 14px;
    font-weight: 600;
    color: hsl(var(--primary-dark));
    margin-bottom: 4px;
}
.tour-card span {
    font-size: 12px;
    color: hsl(var(--secondary) / 0.65);
    line-height: 1.5;
}

.tour-tip {
    background: hsl(var(--primary) / 0.06);
    border: 1px dashed hsl(var(--primary) / 0.25);
    border-radius: 12px;
    padding: 16px;
    display: flex;
    gap: 12px;
    align-items: flex-start;
}
.tour-tip .tip-icon {
    width: 32px;
    height: 32px;
    border-radius: 8px;
    background: hsl(var(--primary));
    color: white;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
}
.tour-tip b {
    display: block;
    font-size: 14px;
    color: hsl(var(--primary-dark));
    margin-bottom: 4px;
}
.tour-tip span {
    font-size: 13px;
    color: hsl(var(--secondary) / 0.7);
    line-height: 1.5;
}

/* ── Erro ── */
.alerta-erro {
    background: hsl(var(--error) / 0.08);
    color: hsl(var(--error));
    border: 1px solid hsl(var(--error) / 0.2);
    padding: 10px 14px;
    border-radius: 10px;
    font-size: 13px;
    margin-top: 16px;
}

/* ── Footer nav ── */
.onb-foot {
    display: flex;
    align-items: center;
    margin-top: 40px;
    padding-top: 24px;
    border-top: 1px solid hsl(var(--secondary) / 0.08);
}
.onb-foot .left { flex: 1; }
.onb-foot .right { display: flex; gap: 10px; align-items: center; flex-wrap: wrap; justify-content: flex-end; }
.onb-foot .step-of {
    font-size: 12px;
    color: hsl(var(--secondary) / 0.55);
}
.onb-foot .step-of b { color: hsl(var(--primary-dark)); }
.onb-foot .skip {
    background: none;
    border: none;
    padding: 12px 16px;
    font-family: inherit;
    font-size: 13px;
    color: hsl(var(--secondary) / 0.6);
    cursor: pointer;
}
.onb-foot .skip:hover { color: hsl(var(--primary-dark)); }
.btn-primary,
.btn-secondary {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 10px;
    padding: 12px 24px;
    font-family: inherit;
    font-size: 14px;
    font-weight: 600;
    border-radius: 10px;
    cursor: pointer;
    transition: all 160ms;
}
.btn-primary {
    background: hsl(var(--primary));
    color: white;
    border: none;
    box-shadow: 0 1px 2px hsl(var(--primary-dark) / 0.2);
}
.btn-primary:hover:not(:disabled) {
    background: hsl(var(--primary-dark));
    transform: translateY(-1px);
    box-shadow: 0 4px 12px hsl(var(--primary) / 0.3);
}
.btn-primary:disabled { opacity: 0.5; cursor: not-allowed; transform: none; }
.btn-secondary {
    background: white;
    color: hsl(var(--primary-dark));
    border: 1.5px solid hsl(var(--secondary) / 0.15);
}
.btn-secondary:hover:not(:disabled) {
    border-color: hsl(var(--primary) / 0.4);
    background: hsl(var(--primary) / 0.03);
}
.btn-secondary:disabled { opacity: 0.5; cursor: not-allowed; }

/* ── Animação de transição ── */
.fade-step { animation: fadeUp 320ms cubic-bezier(.2,.8,.2,1); }
@keyframes fadeUp {
    from { opacity: 0; transform: translateY(8px); }
    to { opacity: 1; transform: translateY(0); }
}

/* ── Responsive ── */
@media (max-width: 960px) {
    .onb-top { padding: 16px 20px; }
    .onb-progress { padding: 16px 20px; }
    .onb-progress .lbl { display: none; }
    .form-grid,
    .choice-grid,
    .choice-grid--small,
    .choice-grid--triple,
    .tour-grid,
    .endereco-row { grid-template-columns: 1fr; }
    .day-row { grid-template-columns: 80px 32px 1fr 12px 1fr 30px; gap: 8px; padding: 10px; }
    .onb-card { padding: 28px 24px; border-radius: 14px; }
    .onb-foot { flex-direction: column; align-items: stretch; gap: 12px; }
    .onb-foot .right { justify-content: stretch; }
    .onb-foot .btn-primary, .onb-foot .btn-secondary { flex: 1; }
}
</style>
