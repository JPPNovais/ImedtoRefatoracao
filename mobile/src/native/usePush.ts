import { Capacitor } from "@capacitor/core"
import { PushNotifications } from "@capacitor/push-notifications"
import { useNotificacoesStore } from "@/stores/notificacoes"
import { useUiStore } from "@/stores/ui"
import type { Router } from "vue-router"

/** Push (APNs/FCM) — o grande destravamento do mobile. O backend já dispara
    via Notificacoes/SignalR; aqui só registramos o canal nativo + deep-link. */
export function usePush(router: Router) {
  async function registrar(): Promise<void> {
    if (!Capacitor.isNativePlatform()) return
    const perm = await PushNotifications.requestPermissions()
    if (perm.receive !== "granted") return
    await PushNotifications.register()

    // Push em foreground → mostra o PushBanner + insere no centro de avisos.
    PushNotifications.addListener("pushNotificationReceived", (n) => {
      const ui = useUiStore()
      ui.showPush(n.title || "Imedto", n.body || "", (n.data as { link?: string })?.link)
      const store = useNotificacoesStore()
      void store.atualizarContador()
    })

    // Tocar na push → deep-link pra tela certa.
    PushNotifications.addListener("pushNotificationActionPerformed", (action) => {
      const link = (action.notification.data as { link?: string })?.link
      if (link) router.push(link).catch(() => {})
      else router.push("/avisos").catch(() => {})
    })
  }

  return { registrar }
}
