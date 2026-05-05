import type { ProntuarioCompleto } from "@/services/prontuarioService"

function dataFmt(s: string) {
    return new Date(s).toLocaleString("pt-BR")
}

export function useProntuarioPdf() {
    async function gerarPdf(pront: ProntuarioCompleto, pacienteNome: string) {
        // Lazy: jsPDF + autotable (~600 KB) só carregam ao clicar "Baixar PDF".
        const [{ jsPDF }, { default: autoTable }] = await Promise.all([
            import("jspdf"),
            import("jspdf-autotable"),
        ])
        const doc = new jsPDF({ orientation: "portrait", unit: "mm", format: "a4" })
        const pageW = doc.internal.pageSize.getWidth()

        // Cabeçalho
        doc.setFontSize(18)
        doc.setFont("helvetica", "bold")
        doc.text("Imedto — Prontuário Médico", 14, 20)

        doc.setFontSize(11)
        doc.setFont("helvetica", "normal")
        doc.setTextColor(60)
        doc.text(`Paciente: ${pacienteNome}`, 14, 30)
        doc.text(`Modelo: ${pront.prontuario.modeloNome}`, 14, 37)
        doc.setTextColor(30)

        doc.setDrawColor(200)
        doc.line(14, 42, pageW - 14, 42)

        let y = 50

        if (pront.evolucoes.length === 0) {
            doc.setFontSize(10)
            doc.setTextColor(150)
            doc.text("Nenhuma evolução registrada.", 14, y)
        }

        for (const evol of pront.evolucoes) {
            // Verifica se cabe na página atual
            if (y > 250) {
                doc.addPage()
                y = 20
            }

            doc.setFontSize(10)
            doc.setFont("helvetica", "bold")
            doc.setTextColor(37, 99, 235)
            doc.text(`Evolução — ${dataFmt(evol.criadaEm)}`, 14, y)

            doc.setFontSize(9)
            doc.setFont("helvetica", "normal")
            doc.setTextColor(100)
            doc.text(`Por: ${evol.autorNome ?? "—"}`, 14, y + 5)
            doc.setTextColor(30)

            y += 12

            const seccoesPreenchidas = evol.modeloSnapshot.filter(
                s => evol.conteudo[s.chave],
            )

            autoTable(doc, {
                startY: y,
                body: seccoesPreenchidas.map(s => [s.titulo, evol.conteudo[s.chave] ?? ""]),
                styles: { fontSize: 9, cellPadding: 3 },
                columnStyles: {
                    0: { fontStyle: "bold", cellWidth: 40, textColor: [55, 65, 81] },
                    1: { cellWidth: "auto", minCellHeight: 8 },
                },
                theme: "plain",
                tableLineColor: [229, 231, 235],
                tableLineWidth: 0.1,
            })

            y = (doc as any).lastAutoTable.finalY + 8

            doc.setDrawColor(240)
            doc.line(14, y - 2, pageW - 14, y - 2)
        }

        // Rodapé
        const rodapeY = doc.internal.pageSize.getHeight() - 10
        doc.setFontSize(8)
        doc.setTextColor(150)
        doc.text(
            `Imedto — Prontuário Médico  |  Gerado em ${new Date().toLocaleString("pt-BR")}`,
            14,
            rodapeY,
        )

        doc.save(`prontuario-${pacienteNome.toLowerCase().replace(/\s+/g, "-")}.pdf`)
    }

    // Captura visual da seção de prontuário usando html2canvas
    async function capturarImagemPdf(elemento: HTMLElement, pacienteNome: string) {
        // Lazy: html2canvas (~200 KB) + jsPDF (~150 KB) só carregam ao usar a captura visual.
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

        let posY = 10
        if (imgH <= pageH - 20) {
            doc.addImage(imgData, "PNG", 10, posY, imgW, imgH)
        } else {
            // Imagem maior que a página: corta em múltiplas páginas
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

        doc.save(`prontuario-${pacienteNome.toLowerCase().replace(/\s+/g, "-")}.pdf`)
    }

    return { gerarPdf, capturarImagemPdf }
}
