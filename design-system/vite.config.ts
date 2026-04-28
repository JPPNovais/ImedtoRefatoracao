import { defineConfig } from "vite"
import vue from "@vitejs/plugin-vue"
import dts from "vite-plugin-dts"
import { resolve } from "path"

export default defineConfig({
  plugins: [
    vue(),
    dts({ tsconfigPath: "./tsconfig.build.json", rollupTypes: true }),
  ],
  build: {
    lib: {
      entry: resolve(__dirname, "src/index.ts"),
      name: "ImedtoUI",
      fileName: "index",
      formats: ["es", "cjs"],
    },
    rollupOptions: {
      external: ["vue", "tailwindcss"],
      output: {
        globals: { vue: "Vue" },
      },
    },
  },
  resolve: {
    alias: { "@": resolve(__dirname, "./src") },
  },
})
