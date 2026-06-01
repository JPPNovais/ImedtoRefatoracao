<script setup lang="ts">
import { reactive, ref } from "vue"
import AppModal from "@/components/ui/AppModal.vue"
import { assinaturaDigitalService } from "@/services/assinaturaDigitalService"
import { useAssinaturaDigitalStore } from "@/stores/assinaturaDigitalStore"

const props = defineProps<{
    aberto: boolean
}>()

const emit = defineEmits<{
    fechar: []
    vinculado: []
}>()

const store = useAssinaturaDigitalStore()

const passo = ref<1 | 2>(1)
const carregando = ref(false)
const erro = ref<string | null>(null)

const form = reactive({
    refreshToken: "",
})

async function vincular() {
    if (!form.refreshToken.trim()) {
        erro.value = "Informe o token do BirdID."
        return
    }
    carregando.value = true
    erro.value = null
    try {
        await assinaturaDigitalService.vincularCertificado({
            provedor: "BirdId",
            refreshToken: form.refreshToken.trim(),
        })
        await store.carregarCertificado()
        emit("vinculado")
        emit("fechar")
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao vincular certificado. Tente novamente."
    } finally {
        carregando.value = false
    }
}

function resetar() {
    passo.value = 1
    form.refreshToken = ""
    erro.value = null
    carregando.value = false
}

function fechar() {
    resetar()
    emit("fechar")
}
</script>

<template>
    <AppModal :aberto="aberto" titulo="Vincular certificado digital" largura="md" @fechar="fechar">
        <!-- Passo 1: instruções -->
        <template v-if="passo === 1">
            <div class="instrucoes">
                <p>
                    Para assinar receitas com validade jurídica (ICP-Brasil), você precisa de um
                    <strong>certificado em nuvem</strong>. O Imedto utiliza o <strong>BirdID (Soluti)</strong>
                    para essa integração.
                </p>
                <div class="card-info">
                    <p><strong>Não tem certificado?</strong></p>
                    <p>
                        O CFM oferece certificado digital <strong>gratuito</strong> para médicos pelo convênio com a
                        Serpro/ICP-Brasil. Acesse o portal do CFM para obter o seu.
                    </p>
                    <a
                        href="https://www.birdid.com.br"
                        target="_blank"
                        rel="noopener noreferrer"
                        class="link-externo"
                    >
                        Acessar BirdID para criar sua conta
                    </a>
                </div>
                <p>
                    Após criar sua conta no BirdID, volte aqui e informe o <strong>token de autorização</strong>
                    gerado no aplicativo para vincular ao Imedto.
                </p>
            </div>
        </template>

        <!-- Passo 2: inserir token -->
        <template v-if="passo === 2">
            <div class="form-passo2">
                <p class="desc-passo2">
                    Cole abaixo o <strong>token de autorização</strong> gerado no aplicativo BirdID:
                </p>
                <div class="campo">
                    <label class="field-label">Token BirdID</label>
                    <textarea
                        v-model="form.refreshToken"
                        class="token-input"
                        rows="4"
                        placeholder="Cole o token aqui..."
                        :disabled="carregando"
                    />
                </div>
                <p v-if="erro" class="msg-erro">{{ erro }}</p>
            </div>
        </template>

        <template #rodape>
            <button class="btn-ghost" type="button" @click="fechar">Cancelar</button>
            <template v-if="passo === 1">
                <button class="btn-primary" type="button" @click="passo = 2">
                    Já tenho o token &rarr;
                </button>
            </template>
            <template v-else>
                <button class="btn-secondary" type="button" @click="passo = 1">
                    &larr; Voltar
                </button>
                <button
                    class="btn-primary"
                    type="button"
                    :disabled="carregando || !form.refreshToken.trim()"
                    @click="vincular"
                >
                    {{ carregando ? "Vinculando..." : "Vincular certificado" }}
                </button>
            </template>
        </template>
    </AppModal>
</template>

<style scoped>
.instrucoes { display: flex; flex-direction: column; gap: 0.75rem; font-size: 0.9rem; }
.card-info {
    background: hsl(var(--accent));
    border: 1px solid hsl(var(--border));
    border-radius: 0.5rem;
    padding: 1rem;
    display: flex; flex-direction: column; gap: 0.4rem;
}
.link-externo {
    color: hsl(var(--primary));
    text-decoration: underline;
    font-size: 0.85rem;
}
.form-passo2 { display: flex; flex-direction: column; gap: 0.75rem; }
.desc-passo2 { font-size: 0.9rem; color: hsl(var(--foreground)); }
.campo { display: flex; flex-direction: column; gap: 0.3rem; }
.field-label { font-size: 0.8rem; font-weight: 600; color: hsl(var(--muted-foreground)); }
.token-input {
    padding: 0.5rem 0.75rem;
    border: 1px solid hsl(var(--border));
    border-radius: 0.375rem;
    font-size: 0.85rem;
    font-family: monospace;
    resize: vertical;
    background: hsl(var(--background));
    color: hsl(var(--foreground));
}
.token-input:focus { outline: none; border-color: hsl(var(--primary)); }
.msg-erro { color: hsl(var(--destructive)); font-size: 0.85rem; }
.btn-primary {
    padding: 0.45rem 1rem; border-radius: 0.375rem; font-size: 0.875rem; font-weight: 600;
    background: hsl(var(--primary)); color: hsl(var(--primary-foreground));
    border: none; cursor: pointer; transition: opacity 0.12s;
}
.btn-primary:disabled { opacity: 0.5; cursor: not-allowed; }
.btn-secondary {
    padding: 0.45rem 1rem; border-radius: 0.375rem; font-size: 0.875rem; font-weight: 600;
    background: hsl(var(--secondary)); color: hsl(var(--secondary-foreground));
    border: 1px solid hsl(var(--border)); cursor: pointer;
}
.btn-ghost {
    padding: 0.45rem 0.75rem; border-radius: 0.375rem; font-size: 0.875rem;
    background: transparent; color: hsl(var(--muted-foreground));
    border: none; cursor: pointer;
}
</style>
