-- Normaliza foto_url existente: extrai apenas o caminho (S3 key) da presigned URL.
-- Histórico: o backend salvava a presigned URL completa (com TTL 24h). Depois disso
-- a imagem expirava no banco. Agora persistimos apenas o caminho — esta migração
-- converte os registros antigos.
--
-- Idempotente: o WHERE só pega valores que ainda começam com http/https.
-- Estratégia: extrair o que está entre ".com/" e "?" (ou fim) da URL.

UPDATE public.estabelecimentos
SET    foto_url = SUBSTRING(foto_url FROM 'amazonaws\.com/([^?]+)')
WHERE  foto_url IS NOT NULL
  AND  foto_url ~ '^https?://.*amazonaws\.com/';

UPDATE public.profissionais
SET    foto_url = SUBSTRING(foto_url FROM 'amazonaws\.com/([^?]+)')
WHERE  foto_url IS NOT NULL
  AND  foto_url ~ '^https?://.*amazonaws\.com/';
