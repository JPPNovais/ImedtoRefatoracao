import { createApp } from "vue"
import "@/assets/main.css"
import { createPinia } from "pinia"
import App from "./App.vue"
import router from "./router"
import { useAuthStore } from "./stores/authStore"
import { useProfissionalStore } from "./stores/profissionalStore"

async function bootstrap() {
    const app = createApp(App)
    const pinia = createPinia()

    app.use(pinia)

    // init() ANTES de app.use(router): o beforeEach dispara no install() do router
    // e precisa ver isAuthenticated correto; se rodar depois, redireciona para /login.
    const auth = useAuthStore()
    await auth.init()

    // Após autenticar, carrega o perfil profissional para popular avatar do sidebar.
    if (auth.isAuthenticated) {
        await useProfissionalStore().init()
    }

    app.use(router)
    await router.isReady()
    app.mount("#app")
}

bootstrap()
