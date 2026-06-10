/**
 * Utilitário de geração e download de CSV.
 *
 * Formato padrão Imedto:
 * - Encoding: UTF-8 com BOM (abre corretamente no Excel BR)
 * - Separador: ponto-e-vírgula (;)
 * - Datas: dd/MM/yyyy
 * - Decimais: vírgula (R$ sem o símbolo na célula — facilita o Excel interpretar como número)
 * - Escaping: campos com ;, aspas ou quebra de linha envolvidos em aspas duplas;
 *             aspas internas duplicadas ("")
 */

/** BOM UTF-8 para abertura correta no Excel Brasil. */
const BOM = "﻿"

/**
 * Escapa um valor de célula CSV.
 * Envolve em aspas duplas se contém `;`, `"` ou quebras de linha.
 * Aspas internas são duplicadas conforme RFC 4180.
 */
export function escaparCelula(valor: string): string {
    const precisaEsquivar = valor.includes(";") || valor.includes('"') || valor.includes("\n") || valor.includes("\r")
    if (!precisaEsquivar) return valor
    return `"${valor.replace(/"/g, '""')}"`
}

/**
 * Converte uma matriz de strings em conteúdo CSV (sem BOM).
 * Separador: ponto-e-vírgula.
 */
export function construirCsv(linhas: string[][]): string {
    return linhas
        .map(cols => cols.map(escaparCelula).join(";"))
        .join("\r\n")
}

/**
 * Formata um número decimal para o padrão CSV Imedto (vírgula decimal, 2 casas).
 * Não inclui símbolo de moeda — o cabeçalho da coluna indica "Valor (R$)".
 */
export function formatarDecimal(n: number): string {
    return n.toFixed(2).replace(".", ",")
}

/**
 * Formata uma quantidade inteira (sem casas decimais).
 */
export function formatarInteiro(n: number): string {
    return Math.round(n).toString()
}

/**
 * Formata uma data ISO (yyyy-MM-dd ou datetime ISO) para dd/MM/yyyy.
 * Retorna "—" para valores inválidos/nulos.
 */
export function formatarData(iso: string | null | undefined): string {
    if (!iso) return "—"
    // Aceita tanto "2026-06-10" quanto "2026-06-10T..."
    const partes = iso.slice(0, 10).split("-")
    if (partes.length !== 3) return iso
    return `${partes[2]}/${partes[1]}/${partes[0]}`
}

/**
 * Dispara o download de um arquivo CSV no navegador via Blob.
 * O BOM UTF-8 é adicionado automaticamente.
 * A URL do Blob é revogada imediatamente após o clique.
 */
export function baixarCsv(conteudo: string, nomeArquivo: string): void {
    const blob = new Blob([BOM + conteudo], { type: "text/csv;charset=utf-8;" })
    const url = URL.createObjectURL(blob)
    const a = document.createElement("a")
    a.href = url
    a.download = nomeArquivo
    a.click()
    URL.revokeObjectURL(url)
}

/**
 * Gera o nome de arquivo padrão para relatório CSV.
 * Formato: relatorio-{aba}-{yyyy-MM-dd}-a-{yyyy-MM-dd}.csv
 */
export function nomeArquivoCsv(aba: string, dataInicio: string, dataFim: string): string {
    return `relatorio-${aba}-${dataInicio}-a-${dataFim}.csv`
}
