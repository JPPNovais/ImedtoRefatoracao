/*
 * Marca local (por usuário/navegador) qual atendimento está em curso.
 * Persiste em localStorage para que o timer "Em atendimento agora" sobreviva
 * a navegações entre Meus Atendimentos e Prontuário.
 *
 * Não é fonte de verdade — backend continua dono do status real do agendamento.
 * Esta marca apenas alimenta a UX visual de timer/destaque até o profissional
 * clicar em Finalizar (que aí sim chama o backend).
 */
import { computed, ref, type ComputedRef } from "vue"

const STORAGE_KEY = "imedto.atendimento_ativo"

interface AtendimentoAtivo {
    agendamentoId: number
    pacienteId: number
    iniciadoEm: string  // ISO datetime
}

function ler(): AtendimentoAtivo | null {
    try {
        const raw = localStorage.getItem(STORAGE_KEY)
        if (!raw) return null
        const obj = JSON.parse(raw)
        if (!obj?.agendamentoId || !obj?.iniciadoEm) return null
        return obj as AtendimentoAtivo
    } catch {
        return null
    }
}

const ativo = ref<AtendimentoAtivo | null>(ler())

function gravar(v: AtendimentoAtivo | null) {
    if (v) {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(v))
    } else {
        localStorage.removeItem(STORAGE_KEY)
    }
    ativo.value = v
}

// Sincroniza entre abas
if (typeof window !== "undefined") {
    window.addEventListener("storage", (e) => {
        if (e.key === STORAGE_KEY) ativo.value = ler()
    })
}

export function useAtendimentoAtivo() {
    function iniciar(agendamentoId: number, pacienteId: number) {
        gravar({
            agendamentoId,
            pacienteId,
            iniciadoEm: new Date().toISOString(),
        })
    }

    function finalizar() {
        gravar(null)
    }

    function ehEsteAtendimento(agendamentoId: number): boolean {
        return ativo.value?.agendamentoId === agendamentoId
    }

    function ehEstePaciente(pacienteId: number): boolean {
        return ativo.value?.pacienteId === pacienteId
    }

    const atual: ComputedRef<AtendimentoAtivo | null> = computed(() => ativo.value)

    return { atual, iniciar, finalizar, ehEsteAtendimento, ehEstePaciente }
}
