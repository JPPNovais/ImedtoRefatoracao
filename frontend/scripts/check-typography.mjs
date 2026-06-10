#!/usr/bin/env node
/**
 * check-typography.mjs
 *
 * Verifica violações da regra tipográfica §5 do CLAUDE.md:
 * nenhum `font-size` ou `font-weight` com valor LITERAL em blocos <style> de .vue.
 * Valores via `var(--...)` são permitidos e ignorados.
 *
 * Modo de operação — comparação com baseline:
 *   - Arquivo novo com violações              → FALHA
 *   - Arquivo com mais violações que baseline → FALHA
 *   - Arquivo com menos violações que baseline → AVISO (sugere rodar --update-baseline)
 *
 * Flags:
 *   --update-baseline   Regera frontend/typography-baseline.json com contagem atual e sai 0.
 *   --ci                Silencia avisos de melhoria (só imprime falhas). Não afeta código de saída.
 */

import fs from "node:fs"
import path from "node:path"
import { fileURLToPath } from "node:url"

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const SRC_DIR = path.resolve(__dirname, "../src")
const BASELINE_PATH = path.resolve(__dirname, "../typography-baseline.json")

const UPDATE_BASELINE = process.argv.includes("--update-baseline")
const CI_MODE = process.argv.includes("--ci")

// Regex para valor literal — NÃO começa com var( e contém caractere não-branco
const LITERAL_FONT_SIZE = /font-size\s*:\s*(?!var\()[^\s;{}][^;{}]*/gi
const LITERAL_FONT_WEIGHT = /font-weight\s*:\s*(?!var\()[^\s;{}][^;{}]*/gi

function extractStyleBlocks(content) {
    const blocks = []
    const re = /<style[^>]*>([\s\S]*?)<\/style>/gi
    let m
    while ((m = re.exec(content)) !== null) {
        blocks.push(m[1])
    }
    return blocks
}

function countViolations(filePath) {
    const content = fs.readFileSync(filePath, "utf8")
    const combined = extractStyleBlocks(content).join("\n")
    const sizes = (combined.match(LITERAL_FONT_SIZE) || []).length
    const weights = (combined.match(LITERAL_FONT_WEIGHT) || []).length
    return sizes + weights
}

function walkVue(dir) {
    const files = []
    for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
        const full = path.join(dir, entry.name)
        if (entry.isDirectory()) files.push(...walkVue(full))
        else if (entry.isFile() && entry.name.endsWith(".vue")) files.push(full)
    }
    return files
}

// ─── Coleta estado atual ────────────────────────────────────────────────────
const vueFiles = walkVue(SRC_DIR)
const current = {}
for (const file of vueFiles) {
    const count = countViolations(file)
    if (count > 0) {
        current[path.relative(SRC_DIR, file)] = count
    }
}

// ─── Modo --update-baseline ─────────────────────────────────────────────────
if (UPDATE_BASELINE) {
    fs.writeFileSync(BASELINE_PATH, JSON.stringify(current, null, 2) + "\n", "utf8")
    const total = Object.values(current).reduce((a, b) => a + b, 0)
    console.log(`Baseline atualizada: ${total} violações em ${Object.keys(current).length} arquivos.`)
    process.exit(0)
}

// ─── Carrega baseline ───────────────────────────────────────────────────────
if (!fs.existsSync(BASELINE_PATH)) {
    console.error("typography-baseline.json não encontrado. Rode: npm run check:typography -- --update-baseline")
    process.exit(1)
}
const baseline = JSON.parse(fs.readFileSync(BASELINE_PATH, "utf8"))

// ─── Compara ────────────────────────────────────────────────────────────────
let failures = 0
const improvements = []

// Arquivos novos ou piorados
for (const [file, count] of Object.entries(current)) {
    const expected = baseline[file] ?? 0
    if (count > expected) {
        console.error(
            `[FALHA] ${file}: ${count} violações tipográficas literais (baseline: ${expected}, novas: +${count - expected})`
        )
        failures++
    } else if (count < expected) {
        improvements.push({ file, expected, count })
    }
}

// Arquivos que sumiram do baseline (contagem caiu a zero)
for (const [file, expected] of Object.entries(baseline)) {
    if (!(file in current) && expected > 0) {
        improvements.push({ file, expected, count: 0 })
    }
}

if (!CI_MODE && improvements.length > 0) {
    console.log(`\n[MELHORIA] ${improvements.length} arquivo(s) reduziram violações tipográficas:`)
    for (const { file, expected, count } of improvements) {
        console.log(`  ${file}: ${expected} → ${count} (pode rodar --update-baseline para registrar)`)
    }
}

if (failures > 0) {
    console.error(`\ncheck-typography: ${failures} arquivo(s) com violações acima da baseline. Corrija ou use var(--token).`)
    process.exit(1)
}

const totalBaseline = Object.values(baseline).reduce((a, b) => a + b, 0)
console.log(
    `check-typography: OK. ${totalBaseline} violações congeladas na baseline — nenhuma nova introduzida.`
)
process.exit(0)
