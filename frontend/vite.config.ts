import { defineConfig } from "vite"
import vue from "@vitejs/plugin-vue"
import { fileURLToPath, URL } from "node:url"
import { resolve } from "path"

export default defineConfig({
    plugins: [vue()],
    resolve: {
        alias: {
            "@": fileURLToPath(new URL("./src", import.meta.url)),
            // Aponta para o dist/ já compilado — sem conflitos de @/ interno.
            // Rebuild do DS: cd design-system && npm run build
            "@imedto/ui": resolve(__dirname, "../design-system/dist/index.js"),
        },
    },
    server: {
        port: 3000,
        proxy: {
            "/api": {
                target: "http://localhost:5050",
                changeOrigin: true,
            },
            // SignalR (item 2.4): negotiate HTTP + WebSocket upgrade.
            // ws: true habilita o tunelamento do upgrade HTTP→WS pelo dev server.
            "/hubs": {
                target: "http://localhost:5050",
                changeOrigin: true,
                ws: true,
            },
        },
    },
    optimizeDeps: {
        include: ["reka-ui", "@vueuse/core", "lucide-vue-next"],
    },
})
