import type { DisponibilidadeDia } from "@/services/agendaService"

/**
 * Determina se é possível criar um encaixe agora com base nos dados de disponibilidade do dia.
 *
 * Regras (espelho de UX — a fonte da verdade é o backend via 422):
 * - dia null (falha de consulta) → permite (falha-fechada não-bloqueante, CA9).
 * - status "fechado" → não permite (dia não-funcional ou data bloqueada).
 * - antes do primeiro slot → não permite (fora do expediente).
 * - slot vigente com motivo "bloqueado" → não permite (intervalo/almoço).
 * - não existe nenhum slot futuro com motivo diferente de "passado" → expediente encerrado → não permite.
 * - caso contrário → permite (inclui agenda cheia de "agendado": encaixe sobrepõe por design).
 *
 * @param dia   Dados de disponibilidade do dia (ou null se consulta falhou).
 * @param horaAtual Hora atual no formato "HH:mm" (injetada para tornar a função pura/testável).
 */
export function calcularPodeEncaixar(dia: DisponibilidadeDia | null, horaAtual: string): boolean {
    if (dia === null) return true
    if (dia.status === "fechado") return false

    const slotVigente = [...dia.slots]
        .reverse()
        .find(s => s.hora <= horaAtual)

    if (!slotVigente) return false

    if (slotVigente.motivo === "bloqueado") return false

    // "Fim de expediente" = não existe nenhum slot futuro que não seja "passado".
    // Slots com motivo "agendado" ou null indicam que o expediente ainda está em curso —
    // motivo "passado" só aparece em slots que já decorreram, jamais em futuros, mas
    // a comparação explícita protege contra mudanças de backend sem quebrar aqui.
    const temSlotFuturoNaoPassado = dia.slots.some(s => s.hora > horaAtual && s.motivo !== "passado")
    if (!temSlotFuturoNaoPassado && dia.status === "indisponivel") return false

    return true
}
