import { defineStore } from "pinia"
import { computed, ref } from "vue"
import type { ProfissionalPerfil, Usuario } from "@/types"
import { authService } from "@/services/auth.service"
import { useTenantStore } from "./tenant"
import { usePermissoesStore } from "./permissoes"
import { localDb } from "@/lib/db"

/** BFF pattern: nenhum token em memória — só o usuário. Cookie HttpOnly faz a sessão. */
export const useAuthStore = defineStore("auth", () => {
  const usuario = ref<Usuario | null>(null)
  const profissional = ref<ProfissionalPerfil | null>(null)
  const pronto = ref(false) // bootstrap concluído

  const isAuthenticated = computed(() => !!usuario.value)
  const onboardingPendente = computed(
    () => !!usuario.value && !usuario.value.onboardingCompleto,
  )

  async function bootstrap(): Promise<void> {
    try {
      const data = await authService.bootstrap()
      if (data) {
        usuario.value = data.usuario
        profissional.value = data.profissional ?? null
        const tenant = useTenantStore()
        const { usouFallback } = await tenant.popular(
          data.estabelecimentos,
          data.usuario.ultimoEstabelecimentoId,
        )
        if (usouFallback && tenant.estabelecimentoAtivoId) {
          void authService.registrarUltimoEstabelecimento(tenant.estabelecimentoAtivoId)
        }
      }
    } catch {
      // anônimo / sessão inválida → segue para login
    } finally {
      pronto.value = true
    }
  }

  async function login(email: string, password: string): Promise<void> {
    await authService.login(email, password)
    await limparSessao()
    await bootstrap()
  }

  async function logout(): Promise<void> {
    try {
      await authService.logout()
    } finally {
      await limparSessao()
    }
  }

  async function limparSessao() {
    usuario.value = null
    profissional.value = null
    await useTenantStore().limpar()
    usePermissoesStore().limpar()
    await localDb.cacheClear()
  }

  return {
    usuario,
    profissional,
    pronto,
    isAuthenticated,
    onboardingPendente,
    bootstrap,
    login,
    logout,
    limparSessao,
  }
})
