/**
 * Labels amigáveis de enums do domínio de Orçamento.
 *
 * Centralizar aqui evita duplicar o mapeamento entre o formulário (AppSelect)
 * e a tela de detalhe, que precisam exibir o mesmo texto.
 */
import type { TipoLocalCirurgia } from "@/services/orcamentoService"

const LABELS_LOCAL_CIRURGIA: Record<TipoLocalCirurgia, string> = {
    IntLocal:      "Com Internação — Anestesia Local + Sedação",
    IntPeridural:  "Com Internação — Peridural/Raqui + Sedação",
    IntGeral:      "Com Internação — Anestesia Geral + TOT",
    SemInternacao: "Sem Internação — Anestesia Local",
    Ambulatorio:   "Ambulatório — Anestesia Local",
}

export function labelTipoLocalCirurgia(tipo: TipoLocalCirurgia | string): string {
    return LABELS_LOCAL_CIRURGIA[tipo as TipoLocalCirurgia] ?? tipo
}
