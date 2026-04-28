import { defineConfig } from "vite"
import vue from "@vitejs/plugin-vue"
import { resolve } from "path"

export default defineConfig({
  plugins: [vue()],
  root: resolve(__dirname, "playground"),
  resolve: {
    alias: { "@": resolve(__dirname, "./src") },
  },
  css: {
    postcss: resolve(__dirname, "postcss.config.js"),
  },
  server: {
    port: 5173,
    open: true,
  },
})
