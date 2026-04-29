import { ref } from "vue"

/**
 * useConfirm — substitui confirm() nativo por um Promise-based que integra com AppModal.
 *
 * Uso:
 *   const { confirmar, dialogo } = useConfirm()
 *   // No template: <ConfirmDialogo v-bind="dialogo" />
 *   // No código:
 *   if (await confirmar("Tem certeza?")) { ... }
 */
export interface ConfirmDialogo {
    aberto: boolean
    titulo: string
    mensagem: string
    variant: "danger" | "primary"
    labelConfirmar: string
}

export function useConfirm() {
    const dialogo = ref<ConfirmDialogo>({
        aberto: false,
        titulo: "Confirmar ação",
        mensagem: "",
        variant: "danger",
        labelConfirmar: "Confirmar",
    })

    let resolver: ((value: boolean) => void) | null = null

    function confirmar(
        mensagem: string,
        opcoes?: Partial<Omit<ConfirmDialogo, "aberto" | "mensagem">>,
    ): Promise<boolean> {
        dialogo.value = {
            aberto: true,
            titulo: opcoes?.titulo ?? "Confirmar ação",
            mensagem,
            variant: opcoes?.variant ?? "danger",
            labelConfirmar: opcoes?.labelConfirmar ?? "Confirmar",
        }
        return new Promise<boolean>((resolve) => {
            resolver = resolve
        })
    }

    function responder(resultado: boolean) {
        dialogo.value.aberto = false
        resolver?.(resultado)
        resolver = null
    }

    return { confirmar, dialogo, responder }
}
