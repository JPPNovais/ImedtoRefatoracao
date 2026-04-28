-- Bucket público do Supabase Storage para fotos de profissionais e estabelecimentos.
-- Como é público, qualquer URL gerada por `/storage/v1/object/public/imedto-fotos/<path>`
-- é acessível sem autenticação — adequado para avatares.
--
-- O upload em si é feito pelo backend via service_role key, então não precisamos de
-- policies de INSERT/UPDATE: a service_role bypassa RLS no schema storage.

INSERT INTO storage.buckets (id, name, public)
VALUES ('imedto-fotos', 'imedto-fotos', true)
ON CONFLICT (id) DO UPDATE SET public = EXCLUDED.public;
