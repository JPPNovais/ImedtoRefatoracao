import preset from "./src/tailwind/preset.js"
export default {
  presets: [preset],
  content: ["./src/**/*.{vue,ts}", "./playground/**/*.{vue,ts,html}"],
}
