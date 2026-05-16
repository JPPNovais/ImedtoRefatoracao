import { defineConfig } from "vitest/config"
import vue from "@vitejs/plugin-vue"
import { fileURLToPath, URL } from "node:url"
import { resolve } from "node:path"

export default defineConfig({
    plugins: [vue()],
    resolve: {
        alias: {
            "@": fileURLToPath(new URL("./src", import.meta.url)),
            // Mesmo alias do vite.config.ts — sem ele, qualquer teste que
            // disparar lazy-load de views (router.push) estoura ao tentar
            // resolver `@imedto/ui` importado por AppBadge / AppRolePill etc.
            "@imedto/ui": resolve(__dirname, "../design-system/dist/index.js"),
        },
    },
    test: {
        environment: "happy-dom",
        globals: true,
        include: ["src/**/*.{test,spec}.ts"],
        exclude: ["node_modules", "dist"],
        setupFiles: ["./src/test/setup.ts"],
    },
})
