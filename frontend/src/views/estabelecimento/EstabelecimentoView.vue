<script setup lang="ts">
import { onMounted, ref } from "vue"
import { useRouter } from "vue-router"
import { vMaska } from "maska/vue"
import { estabelecimentoService, type Estabelecimento } from "@/services/estabelecimentoService"
import { useTenantStore } from "@/stores/tenantStore"
import FuncionamentoTab from "@/components/estabelecimento/FuncionamentoTab.vue"
import UnidadesTab from "@/components/estabelecimento/UnidadesTab.vue"
import ReparticoesTab from "@/components/estabelecimento/ReparticoesTab.vue"
import ListasVariaveisTab from "@/components/estabelecimento/ListasVariaveisTab.vue"
import { AppButton } from "@/components/ui"

const router = useRouter()
const tenant = useTenantStore()

const carregando = ref(false)
const salvando   = ref(false)
const erro       = ref<string | null>(null)
const msg        = ref<string | null>(null)
const estab      = ref<Estabelecimento | null>(null)

const nomeFantasia = ref("")
const razaoSocial  = ref("")
const cnpj         = ref("")
const telefone     = ref("")
const endereco     = ref("")

// Aba ativa — mesma estrutura do legado.
type Aba = "geral" | "dados" | "funcionamento" | "unidades" | "reparticoes" | "variaveis"
const abaAtiva = ref<Aba>("geral")

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        const todos = await estabelecimentoService.listarMeus()
        const ativoId = tenant.ativo?.id
        const atual = todos.find(e => e.id === ativoId) ?? todos[0] ?? null
        estab.value = atual
        if (atual) {
            nomeFantasia.value = atual.nomeFantasia
            razaoSocial.value  = atual.razaoSocial ?? ""
            cnpj.value         = atual.cnpj ?? ""
            telefone.value     = atual.telefone ?? ""
            endereco.value     = atual.endereco ?? ""
        }
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar dados."
    } finally {
        carregando.value = false
    }
}

async function salvar() {
    if (!estab.value) return
    salvando.value = true
    erro.value = null
    msg.value = null
    try {
        await estabelecimentoService.atualizar(estab.value.id, {
            nomeFantasia: nomeFantasia.value,
            razaoSocial:  razaoSocial.value || undefined,
            cnpj:         cnpj.value || undefined,
            telefone:     telefone.value || undefined,
            endereco:     endereco.value || undefined,
        })
        if (tenant.ativo && tenant.ativo.id === estab.value.id) {
            tenant.selecionar({
                id: tenant.ativo.id,
                nomeFantasia: nomeFantasia.value,
                papel: tenant.ativo.papel,
            })
        }
        msg.value = "Dados do estabelecimento atualizados."
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao salvar."
    } finally {
        salvando.value = false
    }
}

const podeEditar = ref(true)

onMounted(async () => {
    podeEditar.value = tenant.papel === "Dono"
    await carregar()
})
</script>

<template>
    <div class="app-page estab">
        <div class="page-header">
            <div>
                <h1 class="page-titulo">Configurações do estabelecimento</h1>
                <p class="page-sub">Complete as informações básicas e cadastre as repartições.</p>
            </div>
        </div>

        <!-- ── Abas (padrão do legado) ── -->
        <nav class="abas">
            <button
                class="aba"
                :class="{ ativa: abaAtiva === 'geral' }"
                @click="abaAtiva = 'geral'"
            >Geral</button>
            <button
                class="aba"
                :class="{ ativa: abaAtiva === 'dados' }"
                @click="abaAtiva = 'dados'"
            >Dados do estabelecimento</button>
            <button
                class="aba"
                :class="{ ativa: abaAtiva === 'funcionamento' }"
                @click="abaAtiva = 'funcionamento'"
            >Funcionamento</button>
            <button
                class="aba"
                :class="{ ativa: abaAtiva === 'unidades' }"
                @click="abaAtiva = 'unidades'"
            >Unidades</button>
            <button
                class="aba"
                :class="{ ativa: abaAtiva === 'reparticoes' }"
                @click="abaAtiva = 'reparticoes'"
            >Repartições</button>
            <button
                class="aba"
                :class="{ ativa: abaAtiva === 'variaveis' }"
                @click="abaAtiva = 'variaveis'"
            >Listas de variáveis</button>
        </nav>

        <!-- ── Aba Geral ── -->
        <section v-if="abaAtiva === 'geral'" class="aba-conteudo">
            <h3 class="secao-titulo">Configurações gerais</h3>
            <p class="secao-sub">Acesse as configurações avançadas e recursos adicionais do seu estabelecimento.</p>

            <div class="atalhos">
                <div class="atalho-card">
                    <div class="atalho-icone">🤖</div>
                    <div class="atalho-info">
                        <h4 class="atalho-titulo">Automações</h4>
                        <p class="atalho-desc">Configure automações de tarefas, lembretes e notificações do sistema.</p>
                        <AppButton @click="router.push({ name: 'Automacoes' })">
                            Acessar automações
                        </AppButton>
                    </div>
                </div>

                <div class="atalho-card">
                    <div class="atalho-icone">📋</div>
                    <div class="atalho-info">
                        <h4 class="atalho-titulo">Modelos de prontuário</h4>
                        <p class="atalho-desc">Configure os modelos de prontuário utilizados nos atendimentos do estabelecimento.</p>
                        <AppButton @click="router.push({ name: 'ModelosProntuario' })">
                            Gerenciar modelos
                        </AppButton>
                    </div>
                </div>
            </div>
        </section>

        <!-- ── Aba Dados ── -->
        <section v-else-if="abaAtiva === 'dados'" class="aba-conteudo">
            <div v-if="carregando" class="estado-msg">Carregando...</div>

            <div v-else-if="!estab" class="estado-msg">Nenhum estabelecimento selecionado.</div>

            <div v-else class="card">
                <p v-if="!podeEditar" class="aviso-somente-leitura">
                    Apenas o dono pode alterar estes dados. Você está visualizando em modo leitura.
                </p>

                <div class="grade-2">
                    <div class="campo">
                        <label class="campo-label">Nome fantasia <span class="obrig">*</span></label>
                        <input v-model="nomeFantasia" class="input-field" :disabled="!podeEditar" />
                    </div>
                    <div class="campo">
                        <label class="campo-label">Razão social</label>
                        <input v-model="razaoSocial" class="input-field" :disabled="!podeEditar" />
                    </div>
                </div>

                <div class="grade-2">
                    <div class="campo">
                        <label class="campo-label">CNPJ</label>
                        <input
                            v-model="cnpj"
                            v-maska="'##.###.###/####-##'"
                            class="input-field"
                            placeholder="00.000.000/0000-00"
                            :disabled="!podeEditar"
                        />
                    </div>
                    <div class="campo">
                        <label class="campo-label">Telefone</label>
                        <input
                            v-model="telefone"
                            v-maska="'(##) #####-####'"
                            class="input-field"
                            type="tel"
                            placeholder="(00) 00000-0000"
                            :disabled="!podeEditar"
                        />
                    </div>
                </div>

                <div class="campo">
                    <label class="campo-label">Endereço</label>
                    <input
                        v-model="endereco"
                        class="input-field"
                        placeholder="Rua, número, bairro, cidade - UF"
                        :disabled="!podeEditar"
                    />
                </div>

                <p v-if="erro" class="msg-erro">{{ erro }}</p>
                <p v-if="msg"  class="msg-ok">{{ msg }}</p>

                <div class="card-footer">
                    <AppButton
                        :disabled="salvando || !podeEditar || !nomeFantasia.trim()"
                        :loading="salvando"
                        @click="salvar"
                    >{{ salvando ? "Salvando..." : "Salvar alterações" }}</AppButton>
                </div>
            </div>
        </section>

        <!-- ── Aba Funcionamento ── -->
        <section v-else-if="abaAtiva === 'funcionamento'" class="aba-conteudo">
            <div v-if="carregando" class="estado-msg">Carregando...</div>
            <div v-else-if="!estab" class="estado-msg">Nenhum estabelecimento selecionado.</div>
            <FuncionamentoTab
                v-else
                :estabelecimento="estab"
                :pode-editar="podeEditar"
                @atualizado="carregar"
            />
        </section>

        <!-- ── Aba Unidades ── -->
        <section v-else-if="abaAtiva === 'unidades'" class="aba-conteudo">
            <div v-if="!estab" class="estado-msg">Nenhum estabelecimento selecionado.</div>
            <UnidadesTab
                v-else
                :estabelecimento-id="estab.id"
                :pode-editar="podeEditar"
            />
        </section>

        <!-- ── Aba Repartições ── -->
        <section v-else-if="abaAtiva === 'reparticoes'" class="aba-conteudo">
            <div v-if="!estab" class="estado-msg">Nenhum estabelecimento selecionado.</div>
            <ReparticoesTab
                v-else
                :estabelecimento-id="estab.id"
                :pode-editar="podeEditar"
            />
        </section>

        <!-- ── Aba Listas de variáveis ── -->
        <section v-else-if="abaAtiva === 'variaveis'" class="aba-conteudo">
            <ListasVariaveisTab :pode-editar="podeEditar" />
        </section>
    </div>
</template>

<style scoped>
.page-header { margin-bottom: 1.25rem; }
.page-titulo { font-size: 1.5rem; font-weight: 800; margin: 0 0 0.2rem; }
.page-sub    { margin: 0; color: var(--text-muted); font-size: 0.875em; }

/* ── Abas (pill horizontal como no legado) ── */
.abas {
    display: inline-flex; flex-wrap: wrap; gap: 4px;
    padding: 4px;
    background: rgba(30, 27, 75, 0.05); border-radius: 999px;
}
.aba {
    border: none; background: none; cursor: pointer;
    padding: 0.35rem 0.95rem; border-radius: 999px;
    font-family: inherit; font-size: 0.78em; font-weight: 600;
    color: rgba(30, 27, 75, 0.55); transition: all 0.12s;
    white-space: nowrap;
}
.aba:hover:not(.ativa) { color: rgba(30, 27, 75, 0.8); }
.aba.ativa {
    background: var(--primary-light, #ede9fe);
    color: var(--primary-dark, #4c1d95);
    box-shadow: 0 1px 2px rgba(0,0,0,0.04);
}

.aba-conteudo { animation: fadein 0.18s ease-out; }
@keyframes fadein {
    from { opacity: 0; transform: translateY(4px); }
    to   { opacity: 1; transform: translateY(0); }
}

.secao-titulo { font-size: 0.95em; font-weight: 700; margin: 0 0 0.3rem; }
.secao-sub    { font-size: 0.82em; color: var(--text-muted); margin: 0 0 1rem; }

/* ── Atalhos (cards na aba Geral) ── */
.atalhos {
    display: grid; gap: 1rem;
    grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
}
.atalho-card {
    background: var(--bg-card); border: 1px solid var(--border);
    border-radius: var(--radius); padding: 1rem 1.25rem;
    display: flex; gap: 1rem; align-items: flex-start;
}
.atalho-icone {
    font-size: 1.5rem; width: 44px; height: 44px;
    display: flex; align-items: center; justify-content: center;
    background: var(--primary-light, #ede9fe); border-radius: 10px; flex-shrink: 0;
}
.atalho-info { flex: 1; display: flex; flex-direction: column; gap: 0.35rem; }
.atalho-titulo { font-size: 0.92em; font-weight: 700; margin: 0; }
.atalho-desc { font-size: 0.82em; color: var(--text-muted); margin: 0 0 0.5rem; }

/* ── Card padrão ── */
.card {
    background: var(--bg-card); border: 1px solid var(--border);
    border-radius: var(--radius); padding: 1.75rem;
    display: flex; flex-direction: column; gap: 1.25rem;
}
.card-em-breve { align-items: flex-start; gap: 0.5rem; }

.aviso-somente-leitura {
    background: #fef3c7; color: #92400e; padding: 0.65rem 0.9rem;
    border-radius: var(--radius); font-size: 0.82em; margin: 0;
}

.em-breve-tag {
    background: #fef3c7; color: #92400e;
    padding: 0.4rem 0.75rem; border-radius: 999px;
    font-size: 0.78em; font-weight: 700; margin: 0.5rem 0 0;
}

.card-footer { display: flex; justify-content: flex-end; }

.grade-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }

.campo       { display: flex; flex-direction: column; gap: 0.3rem; }
.campo-label { font-size: 0.82em; font-weight: 600; color: var(--text-muted); }
.obrig       { color: var(--danger); }

.input-field {
    padding: 0.5rem 0.75rem; border: 1px solid var(--border-strong);
    border-radius: var(--radius); font-family: inherit; font-size: 0.875em;
    background: var(--bg-card); color: var(--text);
}
.input-field:focus    { outline: none; border-color: var(--primary); }
.input-field:disabled { background: #f9fafb; color: var(--text-muted); cursor: not-allowed; }

.msg-erro { color: var(--danger); font-size: 0.875em; margin: 0; }
.msg-ok   { color: #15803d;      font-size: 0.875em; margin: 0; }

.estado-msg { text-align: center; color: var(--text-muted); padding: 3rem 1rem; font-size: 0.9em; }

</style>
