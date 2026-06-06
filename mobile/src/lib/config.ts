import { Capacitor } from "@capacitor/core"

/* Origem da API.
   - MVP: por padrão aponta para PRODUÇÃO (https://app.imedto.com) no app nativo.
   - Web/dev: origem relativa ("") para usar o proxy do Vite (evita CORS no browser).
   - Override explícito via VITE_API_BASE_URL (ex.: staging) tem prioridade.

   No device, as chamadas saem pelo CapacitorHttp (HTTP nativo), que não está
   sujeito a CORS e mantém o cookie de sessão no cookie jar nativo. */
const PROD_ORIGIN = "https://app.imedto.com"
const envBase = import.meta.env.VITE_API_BASE_URL

export const API_ORIGIN =
  envBase !== undefined && envBase !== ""
    ? envBase
    : Capacitor.isNativePlatform()
      ? PROD_ORIGIN
      : ""

export const API_BASE = `${API_ORIGIN}/api`
