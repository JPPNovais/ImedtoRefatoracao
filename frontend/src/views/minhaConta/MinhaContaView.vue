<script setup lang="ts">
import { computed, ref, watchEffect, onMounted, onBeforeUnmount } from "vue"
import { useRouter } from "vue-router"
import { vMaska } from "maska/vue"
import { useAuthStore } from "@/stores/authStore"
import { usuarioService } from "@/services/usuarioService"
import { profissionalService } from "@/services/profissionalService"
import { estabelecimentoService, type Estabelecimento } from "@/services/estabelecimentoService"
import { useTenantStore } from "@/stores/tenantStore"
import { useProfissionalStore } from "@/stores/profissionalStore"
import { AppButton, AppPhotoUpload, AppConfirmDialog } from "@/components/ui"
import { redimensionarImagem } from "@/services/imageUtils"
import AlterarSenhaModal from "@/components/minhaConta/AlterarSenhaModal.vue"

const auth         = useAuthStore()
const tenant       = useTenantStore()
const profissional = useProfissionalStore()
const router       = useRouter()

// ─── Campos pessoais ──────────────────────────────────────────────────────────

const nomeCompleto = ref("")
const telefone     = ref("")

watchEffect(() => {
    if (auth.usuario) {
        nomeCompleto.value = auth.usuario.nomeCompleto ?? ""
        telefone.value     = auth.usuario.telefone ?? ""
    }
})

// ─── Campos profissionais ─────────────────────────────────────────────────────

// Catálogo portado do legado (db/migrations/20251212000000_especialidades_por_profissao.sql).
// Mantido no front porque é estático e raramente muda — evita endpoint dedicado.
const PROFISSOES = [
    { label: "Médico",                conselho: "CRM"     },
    { label: "Enfermeiro",            conselho: "COREN"   },
    { label: "Técnico de Enfermagem", conselho: "COREN"   },
    { label: "Dentista",              conselho: "CRO"     },
    { label: "Fisioterapeuta",        conselho: "CREFITO" },
    { label: "Psicólogo",             conselho: "CRP"     },
    { label: "Nutricionista",         conselho: "CRN"     },
    { label: "Fonoaudiólogo",         conselho: "CRFa"    },
    { label: "Terapeuta Ocupacional", conselho: "CREFITO" },
    { label: "Biomédico",             conselho: "CFBM"    },
    { label: "Farmacêutico",          conselho: "CRF"     },
    { label: "Veterinário",           conselho: "CRMV"    },
    { label: "Assistente Social",     conselho: "CRESS"   },
    { label: "Outro",                 conselho: ""        },
]

const ESPECIALIDADES_POR_PROFISSAO: Record<string, string[]> = {
    "Médico": [
        "Clínica Geral", "Cardiologia", "Pediatria", "Ginecologia e Obstetrícia",
        "Ortopedia e Traumatologia", "Dermatologia", "Endocrinologia", "Psiquiatria",
        "Gastroenterologia", "Oftalmologia", "Geriatria", "Neurologia",
        "Anestesiologia", "Cirurgia Geral", "Cirurgia Plástica", "Cirurgia Cardiovascular",
        "Cirurgia Torácica", "Infectologia", "Nefrologia", "Pneumologia",
        "Reumatologia", "Urologia", "Hematologia", "Oncologia",
        "Radioterapia", "Medicina Nuclear", "Radiologia", "Patologia",
        "Medicina do Trabalho", "Medicina Esportiva", "Medicina de Família e Comunidade",
        "Alergia e Imunologia", "Medicina Intensiva", "Nutrologia",
        "Otorrinolaringologia", "Cirurgia de Cabeça e Pescoço", "Coloproctologia",
        "Angiologia", "Cirurgia Vascular", "Genética Médica", "Mastologia",
        "Medicina Preventiva e Social", "Neonatologia", "Neurocirurgia",
        "Ortopedia Pediátrica", "Medicina Paliativa",
    ],
    "Dentista": [
        "Odontologia Geral", "Odontopediatria", "Ortodontia", "Periodontia",
        "Endodontia", "Implantodontia", "Prótese Dentária",
        "Cirurgia e Traumatologia Bucomaxilofacial", "Dentística", "Odontologia Estética",
        "Odontologia para Pacientes com Necessidades Especiais", "Odontogeriatria",
        "Patologia Oral", "Radiologia Odontológica", "Estomatologia",
        "Harmonização Orofacial", "Odontologia do Trabalho", "Odontologia Legal",
        "Saúde Coletiva (Odontologia)",
    ],
    "Fisioterapeuta": [
        "Fisioterapia Geral", "Fisioterapia Traumato-Ortopédica", "Fisioterapia Neurofuncional",
        "Fisioterapia Cardiorrespiratória", "Fisioterapia Dermatofuncional",
        "Fisioterapia Uroginecológica", "Fisioterapia em Terapia Intensiva",
        "Fisioterapia Esportiva", "Fisioterapia do Trabalho", "Fisioterapia Aquática",
        "Fisioterapia Oncológica", "Fisioterapia em Gerontologia", "Fisioterapia Pediátrica",
    ],
    "Psicólogo": [
        "Psicologia Clínica", "Psicologia Organizacional e do Trabalho",
        "Psicologia Escolar e Educacional", "Psicologia do Trânsito", "Psicologia Jurídica",
        "Psicologia Social", "Psicologia Hospitalar", "Neuropsicologia",
        "Psicopedagogia", "Psicologia do Esporte", "Terapia Cognitivo-Comportamental",
        "Psicanálise", "Terapia Familiar", "Psicologia Infantil",
    ],
    "Nutricionista": [
        "Nutrição Clínica", "Nutrição Esportiva", "Nutrição Materno-Infantil",
        "Nutrição Oncológica", "Nutrição em Cardiologia", "Nutrição em Nefrologia",
        "Nutrição Comportamental", "Nutrição Funcional", "Fitoterapia Aplicada à Nutrição",
    ],
    "Enfermeiro": [
        "Enfermagem Geral", "Enfermagem Obstétrica", "Enfermagem Pediátrica",
        "Enfermagem em Centro Cirúrgico", "Enfermagem em Terapia Intensiva",
        "Enfermagem do Trabalho", "Enfermagem em Cardiologia", "Enfermagem em Oncologia",
        "Enfermagem em Nefrologia", "Enfermagem em Saúde Mental",
        "Enfermagem em Emergência", "Enfermagem Domiciliar", "Enfermagem Estética",
    ],
    "Fonoaudiólogo": [
        "Fonoaudiologia Geral", "Audiologia", "Linguagem", "Motricidade Orofacial",
        "Voz", "Disfagia", "Fonoaudiologia Educacional",
        "Fonoaudiologia Neurofuncional", "Fonoaudiologia Hospitalar",
    ],
    "Terapeuta Ocupacional": [
        "Terapia Ocupacional Geral", "Terapia Ocupacional Pediátrica",
        "Terapia Ocupacional em Saúde Mental", "Terapia Ocupacional Neurológica",
        "Terapia Ocupacional em Gerontologia",
        "Terapia Ocupacional em Reabilitação Física", "Terapia Ocupacional Social",
    ],
}

const UFS = [
    "AC","AL","AM","AP","BA","CE","DF","ES",
    "GO","MA","MG","MS","MT","PA","PB","PE",
    "PI","PR","RJ","RN","RO","RR","RS","SC","SE","SP","TO",
]

const profissao      = ref("")
const conselho       = ref("")
const uf             = ref("")
const numeroRegistro = ref("")
const especialidade  = ref("")
const bio            = ref("")

const profissionalExiste = ref(false)
const carregandoProf     = ref(true)

// ─── Upload de foto (profissional) ────────────────────────────────────────────

const enviandoFoto = ref(false)
const erroFoto = ref<string | null>(null)
const confirmRemoverFotoAberto = ref(false)

async function aoUploadFoto(arquivo: File) {
    erroFoto.value = null
    if (!profissionalExiste.value) {
        erroFoto.value = "Salve o cadastro profissional antes de adicionar a foto."
        return
    }

    enviandoFoto.value = true
    try {
        // Reduz para 512×512 com qualidade 0.85 — fica em torno de 50–100 KB
        // sem perder nitidez visível em avatares.
        const reduzida = await redimensionarImagem(arquivo, 512, 0.85)
        const novaUrl  = await profissionalService.uploadFoto(reduzida)
        // Atualiza a store reativa → sidebar e qualquer outro consumidor
        // refletem a nova foto sem precisar de reload.
        profissional.setFotoUrl(novaUrl)
    } catch (e: any) {
        erroFoto.value = e?.response?.data?.mensagem ?? e?.message ?? "Não foi possível enviar a foto."
    } finally {
        enviandoFoto.value = false
    }
}

function aoRemoverFoto() {
    erroFoto.value = null
    if (!profissional.fotoUrl) return
    confirmRemoverFotoAberto.value = true
}

async function confirmarRemoverFoto() {
    enviandoFoto.value = true
    try {
        await profissionalService.removerFoto()
        profissional.setFotoUrl(null)
        confirmRemoverFotoAberto.value = false
    } catch (e: any) {
        erroFoto.value = e?.response?.data?.mensagem ?? e?.message ?? "Não foi possível remover a foto."
        confirmRemoverFotoAberto.value = false
    } finally {
        enviandoFoto.value = false
    }
}

// Especialidades disponíveis para a profissão selecionada (vazio se "Outro" ou nenhuma).
const especialidadesDisponiveis = computed<string[]>(() => {
    const lista = ESPECIALIDADES_POR_PROFISSAO[profissao.value] ?? []
    return [...lista].sort((a, b) => a.localeCompare(b, "pt-BR"))
})

function onProfissaoChange() {
    const p = PROFISSOES.find(p => p.label === profissao.value)
    if (p?.conselho) conselho.value = p.conselho

    // Limpa especialidade se não pertence à nova profissão.
    if (especialidade.value && !especialidadesDisponiveis.value.includes(especialidade.value)) {
        especialidade.value = ""
    }
}

// ─── Estado de salvamento ─────────────────────────────────────────────────────

const salvando = ref(false)
const msg      = ref<string | null>(null)
const erro     = ref<string | null>(null)

async function salvarTudo() {
    msg.value   = null
    erro.value  = null
    salvando.value = true
    try {
        await usuarioService.atualizarPerfil({
            nomeCompleto: nomeCompleto.value,
            telefone: telefone.value || undefined,
        })

        if (conselho.value && uf.value && numeroRegistro.value) {
            await profissionalService.salvar({
                conselho:       conselho.value,
                uf:             uf.value,
                numeroRegistro: numeroRegistro.value,
                especialidade:  especialidade.value || undefined,
                bio:            bio.value || undefined,
            }, profissionalExiste.value)
            profissionalExiste.value = true
            profissional.marcarComoExistente()
        }

        await auth.recarregarMe()
        msg.value = "Dados salvos com sucesso."
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao salvar."
    } finally {
        salvando.value = false
    }
}

// ─── Segurança (trocar senha) ────────────────────────────────────────────────

const trocarSenhaAberto = ref(false)
const senhaTrocadaMsg   = ref<string | null>(null)
let senhaMsgTimer: number | null = null

function onSenhaAlterada() {
    trocarSenhaAberto.value = false
    senhaTrocadaMsg.value = "Senha alterada com sucesso. As sessões em outros dispositivos foram encerradas."
    if (senhaMsgTimer !== null) window.clearTimeout(senhaMsgTimer)
    senhaMsgTimer = window.setTimeout(() => {
        senhaTrocadaMsg.value = null
        senhaMsgTimer = null
    }, 8000)
}

onBeforeUnmount(() => {
    if (senhaMsgTimer !== null) {
        window.clearTimeout(senhaMsgTimer)
        senhaMsgTimer = null
    }
})

// ─── Estabelecimentos ─────────────────────────────────────────────────────────

const estabelecimentos   = ref<Estabelecimento[]>([])
const carregandoEstabs   = ref(false)

function selecionarEstab(e: Estabelecimento) {
    tenant.selecionar({
        id: e.id,
        nomeFantasia: e.nomeFantasia,
        papel: e.papelDoUsuario,
        permissoes: e.permissoes ?? [],
        permissoesExtras: e.permissoesExtras ?? [],
    })
    router.push({ name: "Home" })
}

// ─── Init ─────────────────────────────────────────────────────────────────────

onMounted(async () => {
    try {
        const p = await profissionalService.obterMeu()
        if (p) {
            profissionalExiste.value = true
            conselho.value       = p.conselho
            uf.value             = p.uf
            numeroRegistro.value = p.numeroRegistro
            especialidade.value  = p.especialidade ?? ""
            bio.value            = p.bio ?? ""
            // Garante que a store está sincronizada (caso o bootstrap não tenha
            // carregado, ex.: usuário acabou de criar o cadastro nesta sessão).
            profissional.setFotoUrl(p.fotoUrl ?? null)
            profissional.marcarComoExistente()
            const found = PROFISSOES.find(pr => pr.conselho === p.conselho)
            profissao.value = found?.label ?? ""
        }
    } finally {
        carregandoProf.value = false
    }

    carregandoEstabs.value = true
    try {
        estabelecimentos.value = await estabelecimentoService.listarMeus()
    } finally {
        carregandoEstabs.value = false
    }
})
</script>

<template>
    <div class="app-page app-page--narrow">
        <!-- ── Cabeçalho ── -->
        <div class="page-header">
            <div>
                <h1 class="page-titulo">Cadastro do profissional</h1>
                <p class="page-sub">Preencha os dados profissionais para concluir o seu cadastro.</p>
            </div>
        </div>

        <!-- ── Card: perfil ── -->
        <div class="card">

            <!-- Foto -->
            <AppPhotoUpload
                :foto-url="profissional.fotoUrl"
                :iniciais-fallback="auth.usuario?.nomeCompleto ?? nomeCompleto"
                titulo="Foto do profissional"
                descricao="Aparece nas listas de equipe, agenda, prontuário e PDFs. Recomendado: imagem quadrada ≥ 200×200px — é otimizada automaticamente antes do envio. Máx. 2 MB · JPG, PNG, WebP ou GIF."
                :loading="enviandoFoto"
                :disabled="!profissionalExiste"
                :motivo-disabled="!profissionalExiste ? 'Salve o cadastro profissional para habilitar a foto' : null"
                :erro="erroFoto ?? (!profissionalExiste ? 'Salve o cadastro profissional abaixo para liberar o upload de foto.' : null)"
                @upload="aoUploadFoto"
                @remover="aoRemoverFoto"
                @erro-validacao="(msg) => (erroFoto = msg)"
            />

            <div class="separador" />

            <!-- Linha 1: Nome + Profissão -->
            <div class="grade-2">
                <div class="campo">
                    <label class="campo-label">Nome completo</label>
                    <input v-model="nomeCompleto" class="input-field" placeholder="Nome completo" />
                </div>
                <div class="campo">
                    <label class="campo-label">Profissão</label>
                    <select v-model="profissao" class="input-field" @change="onProfissaoChange">
                        <option value="">Selecione...</option>
                        <option v-for="p in PROFISSOES" :key="p.label" :value="p.label">{{ p.label }}</option>
                    </select>
                </div>
            </div>

            <!-- Especialidade -->
            <div class="campo">
                <label class="campo-label">Especialidade</label>
                <select
                    v-model="especialidade"
                    class="input-field"
                    :disabled="!profissao || (especialidadesDisponiveis.length === 0 && !especialidade)"
                >
                    <option value="">
                        {{ !profissao
                            ? "Selecione a profissão primeiro"
                            : especialidadesDisponiveis.length === 0
                                ? "Sem especialidades cadastradas para esta profissão"
                                : "Selecione..." }}
                    </option>
                    <!-- Mantém o valor já cadastrado mesmo que esteja fora do catálogo
                         atual (proteção contra divergência de string com o banco). -->
                    <option
                        v-if="especialidade && !especialidadesDisponiveis.includes(especialidade)"
                        :value="especialidade"
                    >{{ especialidade }}</option>
                    <option v-for="e in especialidadesDisponiveis" :key="e" :value="e">{{ e }}</option>
                </select>
            </div>

            <!-- Linha: Número + Tipo + UF -->
            <div class="grade-3">
                <div class="campo">
                    <label class="campo-label">Número do conselho</label>
                    <input v-model="numeroRegistro" class="input-field" placeholder="Ex: 123456" />
                </div>
                <div class="campo">
                    <label class="campo-label">Tipo de conselho</label>
                    <input v-model="conselho" class="input-field" placeholder="CRM, CRO, CRF..." />
                </div>
                <div class="campo">
                    <label class="campo-label">UF</label>
                    <select v-model="uf" class="input-field">
                        <option value="">UF</option>
                        <option v-for="u in UFS" :key="u" :value="u">{{ u }}</option>
                    </select>
                </div>
            </div>

            <!-- Linha: Telefone + Bio -->
            <div class="grade-2">
                <div class="campo">
                    <label class="campo-label">Telefone</label>
                    <input
                        v-model="telefone"
                        v-maska="'(##) #####-####'"
                        class="input-field"
                        type="tel"
                        inputmode="numeric"
                        placeholder="(00) 00000-0000"
                    />
                </div>
                <div class="campo">
                    <label class="campo-label">Biografia (opcional)</label>
                    <input v-model="bio" class="input-field" placeholder="Breve descrição profissional..." />
                </div>
            </div>

            <!-- Feedback -->
            <p v-if="erro" class="msg-erro">{{ erro }}</p>
            <p v-if="msg"  class="msg-ok">{{ msg }}</p>

            <div class="card-footer">
                <AppButton :loading="salvando" @click="salvarTudo">Salvar dados</AppButton>
            </div>
        </div>

        <!-- ── Card: Segurança ── -->
        <div class="card">
            <h2 class="card-titulo">Segurança</h2>
            <p class="card-sub">
                Atualize sua senha periodicamente para proteger sua conta. Ao trocar a senha,
                as sessões abertas em outros dispositivos são encerradas automaticamente.
            </p>
            <p v-if="senhaTrocadaMsg" class="msg-ok">{{ senhaTrocadaMsg }}</p>
            <div class="card-footer">
                <AppButton variant="secondary" icon="fa-solid fa-key" @click="trocarSenhaAberto = true">
                    Trocar senha
                </AppButton>
            </div>
        </div>

        <!-- ── Card: Privacidade e LGPD ── -->
        <div class="card">
            <h2 class="card-titulo">Privacidade e LGPD</h2>
            <p class="card-sub">
                Exporte seus dados pessoais (Art. 18 LGPD), revise os consentimentos aceitos e,
                se desejar, solicite a anonimização permanente da sua conta.
            </p>
            <div class="card-footer">
                <router-link
                    :to="{ name: 'MinhaContaLgpd' }"
                    class="lgpd-link"
                >
                    <i class="fa-solid fa-shield-halved" aria-hidden="true"></i>
                    Gerenciar dados e privacidade
                </router-link>
            </div>
        </div>

        <!-- ── Card: Trocar estabelecimento ── -->
        <div class="card" v-if="!carregandoEstabs && estabelecimentos.length">
            <h2 class="card-titulo">Trocar estabelecimento</h2>
            <p class="card-sub">
                Você está vinculado a múltiplos estabelecimentos. Selecione qual deseja acessar:
            </p>
            <div class="estab-lista">
                <div
                    v-for="e in estabelecimentos" :key="e.id"
                    class="estab-item"
                >
                    <div class="estab-identidade">
                        <div class="estab-avatar">
                            <img v-if="e.fotoUrl" :src="e.fotoUrl" :alt="e.nomeFantasia" />
                            <span v-else>{{ (e.nomeFantasia[0] ?? "?").toUpperCase() }}</span>
                        </div>
                        <span class="estab-nome">{{ e.nomeFantasia }}</span>
                    </div>
                    <span v-if="e.id === tenant.ativo?.id" class="badge-ativo">Ativo</span>
                    <AppButton v-else size="sm" @click="selecionarEstab(e)">Selecionar</AppButton>
                </div>
            </div>
            <p class="card-rodape-info">
                Ao trocar de estabelecimento, você verá apenas os dados (pacientes, agendamentos, etc.) desse estabelecimento.
            </p>
        </div>

        <AlterarSenhaModal
            :aberto="trocarSenhaAberto"
            @fechar="trocarSenhaAberto = false"
            @alterada="onSenhaAlterada"
        />

        <AppConfirmDialog
            v-model:aberto="confirmRemoverFotoAberto"
            titulo="Remover foto?"
            mensagem="Deseja remover sua foto de perfil? Você poderá enviar outra a qualquer momento."
            confirmar-rotulo="Remover"
            variante="danger"
            :executando="enviandoFoto"
            @confirmar="confirmarRemoverFoto"
        />
    </div>
</template>

<style scoped>
/* Layout/largura via .app-page--narrow (main.css). */

.page-header { margin-bottom: 0.25rem; }
.page-titulo { font-size: 1.4rem; font-weight: 800; margin: 0 0 0.2rem; }
.page-sub    { margin: 0; color: var(--text-muted); font-size: 0.875em; }

/* ── Card ────────────────────────────────────────────── */
.card {
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 1.75rem;
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
}

.card-titulo { font-size: 1.05rem; font-weight: 700; margin: 0; color: var(--primary); }
.card-sub    { margin: 0; color: var(--text-muted); font-size: 0.875em; }
.card-footer { display: flex; justify-content: flex-end; margin-top: 0.25rem; }
.card-rodape-info { font-size: 0.78em; color: var(--text-muted); margin: 0; }

/* ── Separador ───────────────────────────────────────── */
.separador {
    border: none;
    border-top: 1px solid var(--border);
    margin: 0;
}

/* ── Grids ───────────────────────────────────────────── */
.grade-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
.grade-3 { display: grid; grid-template-columns: 2fr 1fr 1fr; gap: 1rem; }

/* ── Campos ──────────────────────────────────────────── */
.campo       { display: flex; flex-direction: column; gap: 0.3rem; }
.campo-label { font-size: 0.82em; font-weight: 600; color: var(--text-muted); }

.input-field {
    padding: 0.5rem 0.75rem;
    border: 1px solid var(--border-strong);
    border-radius: var(--radius);
    font-family: inherit;
    font-size: 0.875em;
    background: var(--bg-card);
    color: var(--text);
    transition: border-color 0.15s;
}
.input-field:focus    { outline: none; border-color: var(--primary); }
.input-field:disabled { background: #f9fafb; color: var(--text-muted); cursor: not-allowed; }

/* ── Feedback ────────────────────────────────────────── */
.msg-erro { color: var(--danger);  font-size: 0.875em; margin: 0; }
.msg-ok   { color: #15803d;        font-size: 0.875em; margin: 0; }

/* ── Estabelecimentos ────────────────────────────────── */
.estab-lista {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.estab-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0.8rem 1rem;
    border: 1px solid var(--border);
    border-radius: var(--radius);
    background: var(--bg-card);
    transition: background 0.12s;
}

.estab-item:hover { background: var(--bg-hover); }

.estab-identidade {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    min-width: 0;
}
.estab-avatar {
    width: 36px;
    height: 36px;
    border-radius: 50%;
    background: hsl(var(--primary) / 0.12);
    color: hsl(var(--primary-dark));
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: 700;
    flex-shrink: 0;
    overflow: hidden;
}
.estab-avatar img {
    width: 100%;
    height: 100%;
    object-fit: cover;
}
.estab-nome { font-weight: 600; font-size: 0.95em; }

.badge-ativo {
    font-size: 0.72em;
    font-weight: 700;
    padding: 0.2rem 0.65rem;
    border-radius: 99px;
    background: #dcfce7;
    color: #15803d;
}

.lgpd-link {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.55rem 1rem;
    border-radius: var(--radius);
    border: 1px solid var(--border-strong);
    background: var(--bg-card);
    color: var(--text);
    font-size: 0.875em;
    font-weight: 600;
    text-decoration: none;
    transition: all 0.15s;
}
.lgpd-link:hover {
    border-color: hsl(var(--primary));
    color: hsl(var(--primary));
    background: hsl(var(--primary) / 0.04);
}

/* Botões: usar AppButton ou as classes globais .btn / .btn-primary /
   .btn-secondary definidas em main.css. Nenhuma regra de botão scoped
   nesta view — duplicar com `var(--primary)` (HSL cru) deixaria o background
   inválido e ainda atingiria o root do AppButton via scoped data-attr. */
</style>
