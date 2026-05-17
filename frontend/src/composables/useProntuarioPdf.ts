import type { Evolucao, ProntuarioCompleto } from "@/services/prontuarioService"
import type { Paciente } from "@/services/pacienteService"
import { useTenantStore } from "@/stores/tenantStore"

function dataFmt(s: string) {
    return new Date(s).toLocaleString("pt-BR")
}

function nomePaciente(p: Paciente | string | null | undefined): string {
    if (!p) return "paciente"
    return typeof p === "string" ? p : p.nomeCompleto
}

function slug(nome: string): string {
    return nome.toLowerCase().normalize("NFD").replace(/[̀-ͯ]/g, "")
        .replace(/[^a-z0-9]+/g, "-").replace(/^-+|-+$/g, "") || "paciente"
}

/**
 * Modo de saída do PDF:
 *  - "download"   → `doc.save(arquivo)` (comportamento padrão).
 *  - "visualizar" → retorna o blob URL e seta o título de aba. Quem chama é
 *    responsável por abrir a janela (precisa ser sincrônico ao clique, senão
 *    o browser bloqueia o popup).
 *
 * Em "visualizar", a função NÃO chama `window.open` por dentro — o handler
 * da view abre uma janela `about:blank` antes do `await` e depois aponta
 * `w.location.href = blobUrl`. O blob é liberado via `URL.revokeObjectURL`
 * após ~60s.
 */
export type PdfSaidaModo = "download" | "visualizar"

/** Resultado do gerador: vazio quando download; blob URL quando visualizar. */
export interface PdfResultado {
    blobUrl: string | null
}

/**
 * Finaliza o documento conforme o modo escolhido. Para "visualizar",
 * agenda `URL.revokeObjectURL` em ~60s para evitar leak no tab atual.
 */
function finalizarDocumento(doc: any, nomeArquivo: string, modo: PdfSaidaModo): PdfResultado {
    if (modo === "download") {
        doc.save(nomeArquivo)
        return { blobUrl: null }
    }
    // Define o título da aba do PDF para "<nome sem .pdf>".
    const tituloAba = nomeArquivo.replace(/\.pdf$/i, "")
    doc.setProperties({ title: tituloAba })
    const blobUrl: string = doc.output("bloburl") as unknown as string
    // Libera o blob depois de 60s — o tab já carregou o PDF nesse intervalo.
    setTimeout(() => {
        try { URL.revokeObjectURL(blobUrl) } catch { /* ignore */ }
    }, 60_000)
    return { blobUrl }
}

/**
 * Formato YYYYMMDD-HHmm a partir de um ISO string. Usado em nomes de arquivo
 * de PDF de evolução para evitar trafegar nome/id no filename (LGPD: nome do
 * paciente já vai por slug, mas a data identifica univocamente a evolução).
 */
function dataParaArquivo(iso: string): string {
    const d = new Date(iso)
    if (!Number.isFinite(d.getTime())) return "sem-data"
    const pad = (n: number) => String(n).padStart(2, "0")
    return `${d.getFullYear()}${pad(d.getMonth() + 1)}${pad(d.getDate())}-${pad(d.getHours())}${pad(d.getMinutes())}`
}

/**
 * Renderiza o valor textual de uma seção da evolução. O conteúdo de cada
 * seção é `unknown` no DTO — pode ser string, array, ou objeto JSON. Quando
 * é objeto, formatamos como `chave: valor` em múltiplas linhas. Vazio retorna "".
 */
function valorParaTexto(v: unknown): string {
    if (v == null) return ""
    if (typeof v === "string") return v.trim()
    if (typeof v === "number" || typeof v === "boolean") return String(v)
    if (Array.isArray(v)) {
        return v.map(item => valorParaTexto(item)).filter(Boolean).join("\n")
    }
    if (typeof v === "object") {
        const entradas: string[] = []
        for (const [k, val] of Object.entries(v as Record<string, unknown>)) {
            const texto = valorParaTexto(val)
            if (texto) entradas.push(`${k}: ${texto}`)
        }
        return entradas.join("\n")
    }
    return ""
}

export function useProntuarioPdf() {
    /**
     * Gera o PDF do prontuário (todas as evoluções) aplicando o layout
     * institucional do Imedto (cabeçalho com logo + marca d'água IMEDTO
     * diagonal + rodapé com aviso "Assine manualmente"). Tudo lazy: o
     * primeiro `await import` é o que dispara o chunk com jsPDF + Nunito.
     *
     * Aceita o paciente como objeto completo (preferido — preenche o bloco
     * de paciente do design) ou como string (modo legado — só usa o nome).
     */
    async function gerarPdf(
        pront: ProntuarioCompleto,
        paciente: Paciente | string,
        modo: PdfSaidaModo = "download",
    ): Promise<PdfResultado> {
        // Lazy: jsPDF + autotable + Nunito (~700 KB combinados) só carregam
        // quando o usuário clica "Baixar PDF".
        const [{ jsPDF }, { default: autoTable }, helper] = await Promise.all([
            import("jspdf"),
            import("jspdf-autotable"),
            import("@/composables/usePdfHeader"),
        ])
        const {
            registrarFontesNunito,
            carregarEstabelecimentoAtivo,
            carregarLogoComoDataUrl,
            desenharCabecalho,
            desenharBlocoPaciente,
            finalizarPaginas,
            PDF_MARGIN,
            PDF_THEME,
            NUNITO_FAMILY,
        } = helper

        const tenant = useTenantStore()
        const est = await carregarEstabelecimentoAtivo(tenant.estabelecimentoAtivoId)
        const logo = await carregarLogoComoDataUrl(est)

        const doc = new jsPDF({ orientation: "portrait", unit: "mm", format: "a4" })
        registrarFontesNunito(doc)

        const w = doc.internal.pageSize.getWidth()
        const h = doc.internal.pageSize.getHeight()
        const left = PDF_MARGIN.side
        const right = w - PDF_MARGIN.side
        const limiteY = h - PDF_MARGIN.bottom - 18 // reserva 18mm para o rodapé

        // ── Cabeçalho institucional ─────────────────────────────────────────
        let y = desenharCabecalho(doc, est, logo, {
            docTitle: "PRONTUÁRIO MÉDICO",
            docSubtitle: `Emitido em ${new Date().toLocaleDateString("pt-BR")}`,
        })

        // ── Bloco do paciente ───────────────────────────────────────────────
        const pacObj: Paciente | { nomeCompleto: string } = typeof paciente === "string"
            ? { nomeCompleto: paciente }
            : paciente
        y = desenharBlocoPaciente(doc, pacObj, y)

        // ── Modelo do prontuário (informação curta) ─────────────────────────
        doc.setFont(NUNITO_FAMILY, "normal")
        doc.setFontSize(8)
        doc.setTextColor(PDF_THEME.mute[0], PDF_THEME.mute[1], PDF_THEME.mute[2])
        doc.text(`Modelo: ${pront.prontuario.modeloNome}`, left, y + 1)
        y += 6

        // ── Evoluções ───────────────────────────────────────────────────────
        if (pront.evolucoes.length === 0) {
            // Empty state pontilhado
            const altura = 24
            doc.setDrawColor(PDF_THEME.borderStrong[0], PDF_THEME.borderStrong[1], PDF_THEME.borderStrong[2])
            doc.setLineWidth(0.3)
            doc.setLineDashPattern([1, 1], 0)
            doc.roundedRect(left, y, right - left, altura, 2, 2, "D")
            doc.setLineDashPattern([], 0)
            doc.setFont(NUNITO_FAMILY, "normal")
            doc.setFontSize(10)
            doc.setTextColor(PDF_THEME.muteLight[0], PDF_THEME.muteLight[1], PDF_THEME.muteLight[2])
            doc.text("Nenhuma evolução registrada para este paciente.",
                left + (right - left) / 2, y + altura / 2 + 1, { align: "center", baseline: "middle" })
        }

        for (const evol of pront.evolucoes) {
            // Quebra de página se a evolução não cabe minimamente (16mm de margem)
            if (y > limiteY - 16) {
                doc.addPage()
                y = desenharCabecalho(doc, est, logo, {
                    docTitle: "PRONTUÁRIO MÉDICO",
                    docSubtitle: `Emitido em ${new Date().toLocaleDateString("pt-BR")}`,
                })
                y = desenharBlocoPaciente(doc, pacObj, y)
                y += 2
            }

            // Cabeçalho da evolução (h3 azul-marinho uppercase)
            doc.setFont(NUNITO_FAMILY, "bold")
            doc.setFontSize(9)
            doc.setTextColor(PDF_THEME.inkTitle[0], PDF_THEME.inkTitle[1], PDF_THEME.inkTitle[2])
            doc.text(`EVOLUÇÃO — ${dataFmt(evol.criadaEm)}`.toUpperCase(), left, y, { charSpace: 0.4 })

            // Linha divisória
            doc.setDrawColor(PDF_THEME.borderStrong[0], PDF_THEME.borderStrong[1], PDF_THEME.borderStrong[2])
            doc.setLineWidth(0.2)
            doc.line(left, y + 1.5, right, y + 1.5)

            // Autor
            doc.setFont(NUNITO_FAMILY, "normal")
            doc.setFontSize(8)
            doc.setTextColor(PDF_THEME.mute[0], PDF_THEME.mute[1], PDF_THEME.mute[2])
            doc.text(`Por: ${evol.autorNome ?? "—"}`, left, y + 5)

            const seccoesPreenchidas = evol.modeloSnapshot
                .map(s => ({ titulo: s.titulo, valor: valorParaTexto(evol.conteudo[s.chave]) }))
                .filter(s => s.valor.length > 0)

            autoTable(doc, {
                startY: y + 7,
                body: seccoesPreenchidas.map(s => [s.titulo, s.valor]),
                styles: {
                    font: NUNITO_FAMILY,
                    fontSize: 8.5,
                    cellPadding: 2.5,
                    textColor: [PDF_THEME.ink[0], PDF_THEME.ink[1], PDF_THEME.ink[2]],
                    lineColor: [PDF_THEME.border[0], PDF_THEME.border[1], PDF_THEME.border[2]],
                    lineWidth: 0.1,
                },
                columnStyles: {
                    0: { fontStyle: "bold", cellWidth: 42, textColor: [PDF_THEME.inkTitle[0], PDF_THEME.inkTitle[1], PDF_THEME.inkTitle[2]] },
                    1: { cellWidth: "auto" },
                },
                margin: { left, right: PDF_MARGIN.side },
                theme: "plain",
                pageBreak: "auto",
                rowPageBreak: "avoid",
                didDrawPage: () => {
                    // Em quebra de página, redesenha cabeçalho (rodape vem do finalizarPaginas)
                    const pageAtual = doc.getCurrentPageInfo().pageNumber
                    if (pageAtual > 1 && (doc as any).__cabecalhoDesenhadoEmPagina !== pageAtual) {
                        ;(doc as any).__cabecalhoDesenhadoEmPagina = pageAtual
                        desenharCabecalho(doc, est, logo, {
                            docTitle: "PRONTUÁRIO MÉDICO",
                            docSubtitle: `Emitido em ${new Date().toLocaleDateString("pt-BR")}`,
                        })
                    }
                },
            })

            y = (doc as any).lastAutoTable.finalY + 6
        }

        // ── Watermark + Rodapé em todas as páginas ──────────────────────────
        finalizarPaginas(doc, {
            assinatura: {
                nome: pront.evolucoes[0]?.autorNome ?? "Profissional responsável",
                aviso: "Assine manualmente no espaço acima",
            },
        })

        return finalizarDocumento(doc, `prontuario-${slug(nomePaciente(paciente))}.pdf`, modo)
    }

    /**
     * Gera o PDF de UMA evolução específica (não o histórico completo).
     * Reusa todo o cabeçalho/rodapé institucional do <see cref="gerarPdf"/> mas
     * só renderiza as seções preenchidas dessa evolução. Nome do arquivo carrega
     * a data da evolução (sem id/CPF) para LGPD-mínimo.
     */
    async function gerarPdfEvolucao(
        pront: ProntuarioCompleto,
        evolucao: Evolucao,
        paciente: Paciente | string,
        modo: PdfSaidaModo = "download",
    ): Promise<PdfResultado> {
        const [{ jsPDF }, { default: autoTable }, helper] = await Promise.all([
            import("jspdf"),
            import("jspdf-autotable"),
            import("@/composables/usePdfHeader"),
        ])
        const {
            registrarFontesNunito,
            carregarEstabelecimentoAtivo,
            carregarLogoComoDataUrl,
            desenharCabecalho,
            desenharBlocoPaciente,
            finalizarPaginas,
            PDF_MARGIN,
            PDF_THEME,
            NUNITO_FAMILY,
        } = helper

        const tenant = useTenantStore()
        const est = await carregarEstabelecimentoAtivo(tenant.estabelecimentoAtivoId)
        const logo = await carregarLogoComoDataUrl(est)

        const doc = new jsPDF({ orientation: "portrait", unit: "mm", format: "a4" })
        registrarFontesNunito(doc)

        const w = doc.internal.pageSize.getWidth()
        const left = PDF_MARGIN.side
        const right = w - PDF_MARGIN.side

        const docTitle = "PRONTUÁRIO MÉDICO — EVOLUÇÃO"
        const docSubtitle = `Evolução de ${dataFmt(evolucao.criadaEm)}`

        let y = desenharCabecalho(doc, est, logo, { docTitle, docSubtitle })

        const pacObj: Paciente | { nomeCompleto: string } = typeof paciente === "string"
            ? { nomeCompleto: paciente }
            : paciente
        y = desenharBlocoPaciente(doc, pacObj, y)

        // Modelo do prontuário (informação curta)
        doc.setFont(NUNITO_FAMILY, "normal")
        doc.setFontSize(8)
        doc.setTextColor(PDF_THEME.mute[0], PDF_THEME.mute[1], PDF_THEME.mute[2])
        doc.text(`Modelo: ${pront.prontuario.modeloNome}`, left, y + 1)
        y += 6

        // Cabeçalho da evolução
        doc.setFont(NUNITO_FAMILY, "bold")
        doc.setFontSize(9)
        doc.setTextColor(PDF_THEME.inkTitle[0], PDF_THEME.inkTitle[1], PDF_THEME.inkTitle[2])
        doc.text(`EVOLUÇÃO — ${dataFmt(evolucao.criadaEm)}`.toUpperCase(), left, y, { charSpace: 0.4 })

        doc.setDrawColor(PDF_THEME.borderStrong[0], PDF_THEME.borderStrong[1], PDF_THEME.borderStrong[2])
        doc.setLineWidth(0.2)
        doc.line(left, y + 1.5, right, y + 1.5)

        doc.setFont(NUNITO_FAMILY, "normal")
        doc.setFontSize(8)
        doc.setTextColor(PDF_THEME.mute[0], PDF_THEME.mute[1], PDF_THEME.mute[2])
        doc.text(`Por: ${evolucao.autorNome ?? "—"}`, left, y + 5)

        const seccoesPreenchidas = evolucao.modeloSnapshot
            .map(s => ({ titulo: s.titulo, valor: valorParaTexto(evolucao.conteudo[s.chave]) }))
            .filter(s => s.valor.length > 0)

        autoTable(doc, {
            startY: y + 7,
            body: seccoesPreenchidas.map(s => [s.titulo, s.valor]),
            styles: {
                font: NUNITO_FAMILY,
                fontSize: 8.5,
                cellPadding: 2.5,
                textColor: [PDF_THEME.ink[0], PDF_THEME.ink[1], PDF_THEME.ink[2]],
                lineColor: [PDF_THEME.border[0], PDF_THEME.border[1], PDF_THEME.border[2]],
                lineWidth: 0.1,
            },
            columnStyles: {
                0: { fontStyle: "bold", cellWidth: 42, textColor: [PDF_THEME.inkTitle[0], PDF_THEME.inkTitle[1], PDF_THEME.inkTitle[2]] },
                1: { cellWidth: "auto" },
            },
            margin: { left, right: PDF_MARGIN.side },
            theme: "plain",
            pageBreak: "auto",
            rowPageBreak: "avoid",
            didDrawPage: () => {
                const pageAtual = doc.getCurrentPageInfo().pageNumber
                if (pageAtual > 1 && (doc as any).__cabecalhoDesenhadoEmPagina !== pageAtual) {
                    ;(doc as any).__cabecalhoDesenhadoEmPagina = pageAtual
                    desenharCabecalho(doc, est, logo, { docTitle, docSubtitle })
                }
            },
        })

        finalizarPaginas(doc, {
            assinatura: {
                nome: evolucao.autorNome ?? "Profissional responsável",
                aviso: "Assine manualmente no espaço acima",
            },
        })

        return finalizarDocumento(
            doc,
            `evolucao-${slug(nomePaciente(paciente))}-${dataParaArquivo(evolucao.criadaEm)}.pdf`,
            modo,
        )
    }

    // Captura visual da seção de prontuário usando html2canvas
    // Mantida sem mudanças — usada em fluxo separado, fora do escopo do redesign.
    async function capturarImagemPdf(elemento: HTMLElement, pacienteNome: string) {
        const [{ default: html2canvas }, { jsPDF }] = await Promise.all([
            import("html2canvas"),
            import("jspdf"),
        ])
        const canvas = await html2canvas(elemento, {
            scale: 2,
            useCORS: true,
            logging: false,
        })

        const imgData = canvas.toDataURL("image/png")
        const doc = new jsPDF({
            orientation: canvas.width > canvas.height ? "landscape" : "portrait",
            unit: "mm",
            format: "a4",
        })

        const pageW = doc.internal.pageSize.getWidth()
        const pageH = doc.internal.pageSize.getHeight()
        const ratio = canvas.width / canvas.height
        const imgW = pageW - 20
        const imgH = imgW / ratio

        const posY = 10
        if (imgH <= pageH - 20) {
            doc.addImage(imgData, "PNG", 10, posY, imgW, imgH)
        } else {
            const paginaH = (pageH - 20) * (canvas.width / imgW)
            let srcY = 0
            while (srcY < canvas.height) {
                const alturaCorte = Math.min(paginaH, canvas.height - srcY)
                const pageCanvas = document.createElement("canvas")
                pageCanvas.width = canvas.width
                pageCanvas.height = alturaCorte
                const ctx = pageCanvas.getContext("2d")!
                ctx.drawImage(canvas, 0, srcY, canvas.width, alturaCorte, 0, 0, canvas.width, alturaCorte)
                const pageImg = pageCanvas.toDataURL("image/png")
                if (srcY > 0) doc.addPage()
                doc.addImage(pageImg, "PNG", 10, posY, imgW, (alturaCorte * imgW) / canvas.width)
                srcY += paginaH
            }
        }

        doc.save(`prontuario-${slug(pacienteNome)}.pdf`)
    }

    return { gerarPdf, gerarPdfEvolucao, capturarImagemPdf }
}
