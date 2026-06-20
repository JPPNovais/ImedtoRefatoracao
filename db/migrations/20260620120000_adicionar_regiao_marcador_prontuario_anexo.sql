-- Adiciona metadados de foto clínica (região anatômica + marcador Antes/Depois/Evolução)
-- à tabela prontuario_anexos. Campos opcionais (nullable) — retro-compatível com
-- anexos existentes (PDFs, documentos) que não têm esses campos.
-- Idempotente: ADD COLUMN IF NOT EXISTS garante que re-execução não falha.

ALTER TABLE public.prontuario_anexos
    ADD COLUMN IF NOT EXISTS regiao_anatomica text NULL,
    ADD COLUMN IF NOT EXISTS marcador         text NULL;
