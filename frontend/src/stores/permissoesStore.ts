import { defineStore } from "pinia"
import { ref, computed } from "vue"
import { temArea, temAcao } from "@/constants/permissions"
import httpClient from "@/services/httpClient"

/**
 * Permissões do usuário no tenant ativo.
 *
 * Hidratado por `tenantStore.popularEstabelecimentos`/`selecionar` a partir do
 * `BootstrapMeDto` (campos `permissoes` + `permissoesExtras` por estabelecimento).
 *
 *  - Dono: `papel === "Dono"`, `permissoes/extras` vazios. Tudo é true.
 *  - Profissional: arrays do modelo do vínculo. Itens fora dos arrays = false.
 *
 * Helpers:
 *  - `pode("agenda")` — qualquer ação na área (formato legado)
 *  - `pode("agenda.ver")` — ação granular específica
 *  - `podeExtra("config_estabelecimento")` — permissão fina extra
 *  - `tudo` — true se Dono (atalho)
 */
export const usePermissoesStore = defineStore("permissoes", () => {
    const papel = ref<"Dono" | "Profissional" | null>(null)
    const permissoes = ref<string[]>([])
    const permissoesExtras = ref<string[]>([])

    const ehDono = computed(() => papel.value === "Dono")
    const tudo = computed(() => ehDono.value)

    function definir(p: {
        papel: "Dono" | "Profissional" | null
        permissoes?: string[] | null
        permissoesExtras?: string[] | null
    }) {
        papel.value = p.papel
        permissoes.value = p.permissoes ?? []
        permissoesExtras.value = p.permissoesExtras ?? []
    }

    function limpar() {
        papel.value = null
        permissoes.value = []
        permissoesExtras.value = []
    }

    /**
     * Aceita "area" (qualquer ação) ou "area.acao" (granular). Dono = sempre true.
     * Sem tenant ativo (papel=null) = sempre false.
     */
    function pode(chave: string): boolean {
        if (!papel.value) return false
        if (ehDono.value) return true
        if (chave.includes(".")) {
            const [area, acao] = chave.split(".", 2)
            return temAcao(permissoes.value, area, acao)
        }
        return temArea(permissoes.value, chave)
    }

    /** Permissões finas (gerir_permissoes, config_estabelecimento, etc.). */
    function podeExtra(chave: string): boolean {
        if (!papel.value) return false
        if (ehDono.value) return true
        return permissoesExtras.value.includes(chave)
    }

    /**
     * Re-busca as permissões do usuário no tenant ativo via /api/tenant/me/permissoes.
     * Chamado após:
     *  - Receber evento SignalR `permissoes-alteradas`
     *  - `visibilitychange` quando a aba volta ao foco (rede de segurança contra
     *    SignalR perdido)
     *
     * Não-fatal: falha silenciosa (mantém o cached). Também atualiza o sessionStorage
     * via tenantStore.atualizarPermissoesAtivo() para consistência em refresh de página.
     */
    async function revalidar(): Promise<void> {
        try {
            const { data } = await httpClient.get<{
                papel: "Dono" | "Profissional"
                permissoes: string[]
                permissoesExtras: string[]
            }>("/tenant/me/permissoes")
            definir({
                papel: data.papel,
                permissoes: data.permissoes ?? [],
                permissoesExtras: data.permissoesExtras ?? [],
            })
            // Mantém o sessionStorage do tenantStore alinhado — sem isso, ao dar F5
            // o front reidrata com permissões antigas até o próximo evento.
            const tenantStoreModule = await import("@/stores/tenantStore")
            tenantStoreModule.useTenantStore().atualizarPermissoesAtivo({
                permissoes: data.permissoes ?? [],
                permissoesExtras: data.permissoesExtras ?? [],
            })
        } catch {
            // ignora — backend gateia 403 nas operações sensíveis de qualquer jeito
        }
    }

    return {
        papel,
        permissoes,
        permissoesExtras,
        ehDono,
        tudo,
        definir,
        limpar,
        pode,
        podeExtra,
        revalidar,
    }
})
