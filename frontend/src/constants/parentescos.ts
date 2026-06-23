/**
 * Lista fixa de parentescos para o campo Responsável do paciente.
 * Briefing 2026-06-23_002 §4.1 — R5: lista controlada pelo front,
 * valor armazenado como texto (não enum no banco).
 */
export const PARENTESCOS: { value: string; label: string }[] = [
    { value: "Mãe",                    label: "Mãe" },
    { value: "Pai",                    label: "Pai" },
    { value: "Avó / Avô",             label: "Avó / Avô" },
    { value: "Tio / Tia",             label: "Tio / Tia" },
    { value: "Irmão / Irmã",          label: "Irmão / Irmã" },
    { value: "Filho / Filha",         label: "Filho / Filha" },
    { value: "Cônjuge / Companheiro(a)", label: "Cônjuge / Companheiro(a)" },
    { value: "Tutor / Responsável legal", label: "Tutor / Responsável legal" },
    { value: "Outro",                  label: "Outro" },
]
