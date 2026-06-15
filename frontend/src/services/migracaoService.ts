import httpClient from "@/services/httpClient"

export interface MigracaoJobStatus {
    jobId: number
    status: string
}

export const LIMITE_UPLOAD_BYTES = 50 * 1024 * 1024 // 50 MB — R11/CA19

/** Mensagem de rejeição do limite (consistente com o backend — CA19). */
export const MENSAGEM_LIMITE =
    "Arquivo acima de 50MB. Divida a migração em partes ou contate o suporte para migração assistida."

const migracaoService = {
    /**
     * Inicia uma migração fazendo upload do ZIP.
     * Valida o limite de 50MB no front antes de enviar (CA19).
     *
     * @param onda - undefined / null = Onda 1 (pacientes); "prontuario" = Onda 2 (CA13).
     * Retorna o job criado com status inicial.
     */
    async iniciarUpload(
        estabelecimentoId: number,
        arquivo: File,
        origem?: string,
        onda?: string
    ): Promise<MigracaoJobStatus> {
        // Trava de front (CA19) — o back também valida (422 se burlar).
        if (arquivo.size > LIMITE_UPLOAD_BYTES) {
            throw new Error(MENSAGEM_LIMITE)
        }

        const form = new FormData()
        form.append("arquivo", arquivo)
        if (origem) form.append("origem", origem)
        if (onda) form.append("onda", onda)

        const { data } = await httpClient.post<MigracaoJobStatus>(
            "/api/migracao/upload",
            form,
            {
                headers: {
                    "X-Estabelecimento-Id": String(estabelecimentoId),
                    "Content-Type": "multipart/form-data",
                },
            }
        )
        return data
    },

    /** Consulta o status de um job existente. */
    async obterStatus(
        estabelecimentoId: number,
        jobId: number
    ): Promise<MigracaoJobStatus> {
        const { data } = await httpClient.get<MigracaoJobStatus>(
            `/api/migracao/${jobId}`,
            { headers: { "X-Estabelecimento-Id": String(estabelecimentoId) } }
        )
        return data
    },
}

export default migracaoService
