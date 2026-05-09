import {
    HttpTransportType,
    HubConnection,
    HubConnectionBuilder,
    HubConnectionState,
    LogLevel,
    type IRetryPolicy,
    type RetryContext,
} from "@microsoft/signalr"

/**
 * Cliente SignalR singleton.
 *
 * - Conecta em /hubs/estabelecimento (mesmo origin → cookie HttpOnly `access-token` é enviado
 *   automaticamente no negotiate HTTP). O backend tem fallback para query string `?access_token=`
 *   apenas quando o WebSocket handshake não propagar cookie — não usamos isso aqui pois rodamos
 *   atrás do proxy Vite/nginx (same-origin).
 * - Reconexão automática com backoff: 0, 2s, 5s, 10s, 30s.
 * - Em dev (import.meta.env.DEV) loga em LogLevel.Information; em prod só Error.
 * - **Page Visibility**: pausa a conexão quando a aba fica em background e religa quando volta.
 *   Elimina ERR_NETWORK_IO_SUSPENDED (Chrome aborta IO de tab inativa) e economiza um poll
 *   pendurado por aba ociosa.
 *
 * Uso:
 *   await realtimeService.start()
 *   realtimeService.on("notificacao-recebida", n => { ... })
 *   await realtimeService.stop()
 *
 * Idempotente: chamar start() múltiplas vezes não duplica conexão.
 */
class RealtimeService {
    private connection: HubConnection | null = null
    private startPromise: Promise<void> | null = null

    // Intenção do consumidor (login/logout). Diferente do estado real da conexão, que pode
    // estar fechada porque a aba está em background.
    private wantsConnected = false
    private visibilityHandlerRegistered = false

    /**
     * Handlers registrados ANTES do start() são reaplicados na conexão recém-criada.
     * Mantemos a lista para sobreviver a reconexões após `stop()` + `start()`.
     */
    private handlers: Map<string, Set<(...args: any[]) => void>> = new Map()

    async start(): Promise<void> {
        this.wantsConnected = true
        this.registerVisibilityHandler()

        // Aba já em background no momento do login/refresh: não conecta agora; o
        // visibilitychange religa quando voltar.
        if (this.isHidden()) return

        return this.connect()
    }

    async stop(): Promise<void> {
        this.wantsConnected = false
        this.unregisterVisibilityHandler()
        await this.disconnect()
    }

    private async connect(): Promise<void> {
        if (this.connection?.state === HubConnectionState.Connected) return
        if (this.startPromise) return this.startPromise

        // Hub URL: same-origin sempre. Em dev o proxy do Vite leva pra localhost:5050.
        // Em prod, a Vercel reescreve /hubs/* para o backend (Render). Isso mantém
        // o cookie HttpOnly como first-party (essencial em modo anônimo / Privacy
        // Sandbox), com a contrapartida que o WebSocket upgrade não atravessa
        // o proxy da Vercel — por isso forçamos LongPolling em prod.
        const hubUrl = "/hubs/estabelecimento"

        const isProd = import.meta.env.PROD

        const connection = new HubConnectionBuilder()
            .withUrl(hubUrl, {
                // SignalR JS propaga cookies same-origin por default no negotiate HTTP.
                // Não setamos accessTokenFactory: o backend lê do cookie HttpOnly.
                withCredentials: true,
                // Em produção atrás do proxy da Vercel, WebSocket upgrade não
                // funciona; LongPolling (HTTP) atravessa o proxy normalmente.
                ...(isProd ? { transport: HttpTransportType.LongPolling } : {}),
            })
            .withAutomaticReconnect(new BackoffRetryPolicy([0, 2000, 5000, 10_000, 30_000]))
            .configureLogging(import.meta.env.DEV ? LogLevel.Information : LogLevel.Error)
            .build()

        // Tem que casar com Program.cs (KeepAliveInterval=5min, ClientTimeoutInterval=10min).
        // serverTimeout >= 2x keepAlive do servidor — se ficar abaixo, cliente declara desconectado
        // e dispara reconnect/negotiate desnecessário.
        connection.serverTimeoutInMilliseconds = 10 * 60 * 1000
        connection.keepAliveIntervalInMilliseconds = 5 * 60 * 1000

        // Reaplica handlers já registrados (sobrevive a stop()+start())
        for (const [eventName, set] of this.handlers) {
            for (const fn of set) connection.on(eventName, fn)
        }

        this.connection = connection

        this.startPromise = connection
            .start()
            .catch((err) => {
                // Não derruba a app: usuário continua usando o sistema sem realtime;
                // notificações ainda chegam via REST polling/refresh manual.
                if (import.meta.env.DEV) {
                    console.warn("[realtime] Falha ao conectar ao hub:", err)
                }
                this.connection = null
                throw err
            })
            .finally(() => {
                this.startPromise = null
            })

        return this.startPromise
    }

    private async disconnect(): Promise<void> {
        const conn = this.connection
        this.connection = null
        this.startPromise = null
        if (conn && conn.state !== HubConnectionState.Disconnected) {
            try {
                await conn.stop()
            } catch {
                /* ignore */
            }
        }
    }

    private isHidden(): boolean {
        return typeof document !== "undefined" && document.visibilityState === "hidden"
    }

    private registerVisibilityHandler(): void {
        if (this.visibilityHandlerRegistered) return
        if (typeof document === "undefined") return
        document.addEventListener("visibilitychange", this.onVisibilityChange)
        this.visibilityHandlerRegistered = true
    }

    private unregisterVisibilityHandler(): void {
        if (!this.visibilityHandlerRegistered) return
        if (typeof document === "undefined") return
        document.removeEventListener("visibilitychange", this.onVisibilityChange)
        this.visibilityHandlerRegistered = false
    }

    private onVisibilityChange = (): void => {
        if (!this.wantsConnected) return
        if (this.isHidden()) {
            void this.disconnect()
        } else {
            void this.connect()
        }
    }

    /**
     * Registra um handler para um evento do hub. Persiste mesmo se a conexão for
     * encerrada e reaberta (via stop/start) — ideal para stores que se inscrevem no setup.
     */
    on<T = unknown>(eventName: string, handler: (payload: T) => void): void {
        let set = this.handlers.get(eventName)
        if (!set) {
            set = new Set()
            this.handlers.set(eventName, set)
        }
        set.add(handler as any)

        if (this.connection) {
            this.connection.on(eventName, handler as any)
        }
    }

    off(eventName: string, handler: (...args: any[]) => void): void {
        this.handlers.get(eventName)?.delete(handler)
        this.connection?.off(eventName, handler)
    }

    get isConnected(): boolean {
        return this.connection?.state === HubConnectionState.Connected
    }
}

/**
 * Retry policy que itera por uma sequência fixa de delays (ms). Após esgotar a sequência,
 * mantém o último delay (30s) — assim o cliente continua tentando indefinidamente sem
 * spam. O SignalR só retorna `null` para parar de tentar; nunca retornamos `null`.
 */
class BackoffRetryPolicy implements IRetryPolicy {
    constructor(private readonly delays: number[]) {}

    nextRetryDelayInMilliseconds(ctx: RetryContext): number | null {
        const idx = Math.min(ctx.previousRetryCount, this.delays.length - 1)
        return this.delays[idx]
    }
}

const realtimeService = new RealtimeService()
export default realtimeService
