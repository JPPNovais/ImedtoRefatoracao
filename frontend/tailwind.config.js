import { resolve, dirname } from "path"
import { fileURLToPath } from "url"
import dsPreset from "../design-system/src/tailwind/preset.js"

const __dirname = dirname(fileURLToPath(import.meta.url))

/** @type {import('tailwindcss').Config} */
export default {
  presets: [dsPreset],
  corePlugins: {
    preflight: false,
  },
  content: [
    "./index.html",
    "./src/**/*.{vue,ts,tsx}",
    resolve(__dirname, "../design-system/src/**/*.{vue,ts}"),
  ],
  theme: {
    extend: {},
  },
  plugins: [],
}
