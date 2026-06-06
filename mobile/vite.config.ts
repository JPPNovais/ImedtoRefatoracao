import { fileURLToPath, URL } from "node:url"
import { defineConfig } from "vite"
import vue from "@vitejs/plugin-vue"

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      "@": fileURLToPath(new URL("./src", import.meta.url)),
    },
  },
  server: {
    port: 5174,
    // Dev/preview no browser: proxy para o backend (default = PRODUÇÃO no MVP).
    // O proxy é server-side, então o browser não dispara CORS; o cookie de sessão
    // é reescrito para o host local para sobreviver entre requests.
    proxy: {
      "/api": {
        target: process.env.VITE_API_PROXY_TARGET || "https://app.imedto.com",
        changeOrigin: true,
        secure: true,
        cookieDomainRewrite: "",
      },
      "/hubs": {
        target: process.env.VITE_API_PROXY_TARGET || "https://app.imedto.com",
        changeOrigin: true,
        secure: true,
        ws: true,
        cookieDomainRewrite: "",
      },
    },
  },
})
