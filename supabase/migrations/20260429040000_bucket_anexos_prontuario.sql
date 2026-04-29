-- Bucket privado para anexos de prontuário (PDFs, imagens, DICOM).
-- Acesso direto pelo cliente é PROIBIDO — somente service_role lê/escreve;
-- usuários acessam blobs apenas via URL assinada gerada pelo backend (TTL curto).
--
-- Espelha StorageOptions do backend: limite de 50 MB e MIME whitelist
-- (PDF, PNG, JPEG, WebP, DICOM). Esta é a primeira camada da defense-in-depth
-- (o storage do Supabase também rejeita tamanho/MIME fora do permitido); o
-- backend valida de novo antes de chamar o storage para falhar cedo.

INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES (
    'imedto_anexos_prontuario',
    'imedto_anexos_prontuario',
    false,
    52428800,  -- 50 MB
    ARRAY[
        'application/pdf',
        'image/png',
        'image/jpeg',
        'image/webp',
        'application/dicom'
    ]
)
ON CONFLICT (id) DO UPDATE SET
    public             = EXCLUDED.public,
    file_size_limit    = EXCLUDED.file_size_limit,
    allowed_mime_types = EXCLUDED.allowed_mime_types;

-- RLS: storage.objects já vem com RLS habilitada por padrão no Supabase.
-- Garantimos via policies explícitas que apenas service_role acessa este bucket.
-- (REVOGAR para authenticated/anon é redundante porque sem policy permissiva
-- já não há acesso, mas declaramos a intenção em forma de policy negativa-por-omissão.)

-- SELECT: apenas service_role.
DROP POLICY IF EXISTS "anexos_prontuario_select_service_role" ON storage.objects;
CREATE POLICY "anexos_prontuario_select_service_role"
    ON storage.objects FOR SELECT
    USING (
        bucket_id = 'imedto_anexos_prontuario'
        AND auth.role() = 'service_role'
    );

-- INSERT: apenas service_role.
DROP POLICY IF EXISTS "anexos_prontuario_insert_service_role" ON storage.objects;
CREATE POLICY "anexos_prontuario_insert_service_role"
    ON storage.objects FOR INSERT
    WITH CHECK (
        bucket_id = 'imedto_anexos_prontuario'
        AND auth.role() = 'service_role'
    );

-- UPDATE: apenas service_role.
DROP POLICY IF EXISTS "anexos_prontuario_update_service_role" ON storage.objects;
CREATE POLICY "anexos_prontuario_update_service_role"
    ON storage.objects FOR UPDATE
    USING (
        bucket_id = 'imedto_anexos_prontuario'
        AND auth.role() = 'service_role'
    )
    WITH CHECK (
        bucket_id = 'imedto_anexos_prontuario'
        AND auth.role() = 'service_role'
    );

-- DELETE: apenas service_role (job de limpeza futuro chamará via backend).
DROP POLICY IF EXISTS "anexos_prontuario_delete_service_role" ON storage.objects;
CREATE POLICY "anexos_prontuario_delete_service_role"
    ON storage.objects FOR DELETE
    USING (
        bucket_id = 'imedto_anexos_prontuario'
        AND auth.role() = 'service_role'
    );

COMMENT ON POLICY "anexos_prontuario_select_service_role" ON storage.objects IS
    'Anexos de prontuário (LGPD/saúde sensível): só service_role acessa. Cliente recebe URL assinada do backend (TTL 5min, audit em prontuario_acesso_log).';
