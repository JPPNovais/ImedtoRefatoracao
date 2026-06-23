-- Migration: padronização dos códigos de membros nível 1 no catálogo de regiões anatômicas.
-- Briefing: 2026-06-23_001, Parte B (R6, R7, R8).
--
-- Renomeia os 12 códigos abreviados de membros (msd-*, mse-*, mid-*, mie-*)
-- para a forma por extenso (membro-superior-direito-*, etc.).
-- Atualiza pai_codigo dos filhos em cascata (R7) para não deixar órfãos em banco novo.
--
-- Idempotência (R8): cada UPDATE filtra por WHERE codigo = '<antigo>' (ou pai_codigo = '<antigo>').
-- Em banco já migrado, o código antigo não existe → 0 linhas afetadas, sem erro.
-- Multi-tenant: tabela global sem estabelecimento_id — ref data, não é PII.
-- LGPD: não contém dados de paciente. Sem audit de prontuário necessário.
--
-- NÃO há FK declarada em pai_codigo → não há risco de violação de constraint.
-- NÃO edita a migration 20260526000001 (imutável, já aplicada).
-- Maior código novo: membro-superior-esquerdo-circunferencial = 40 chars (limite da coluna: 60).

-- ============================================================
-- BLOCO 1: renomear codigo (nível 1 de membros)
-- ============================================================

-- membro-superior-direito
UPDATE public.regioes_anatomicas_catalogo SET codigo = 'membro-superior-direito-anterior'        WHERE codigo = 'msd-anterior';
UPDATE public.regioes_anatomicas_catalogo SET codigo = 'membro-superior-direito-posterior'       WHERE codigo = 'msd-posterior';
UPDATE public.regioes_anatomicas_catalogo SET codigo = 'membro-superior-direito-circunferencial' WHERE codigo = 'msd-circunferencial';

-- membro-superior-esquerdo
UPDATE public.regioes_anatomicas_catalogo SET codigo = 'membro-superior-esquerdo-anterior'        WHERE codigo = 'mse-anterior';
UPDATE public.regioes_anatomicas_catalogo SET codigo = 'membro-superior-esquerdo-posterior'       WHERE codigo = 'mse-posterior';
UPDATE public.regioes_anatomicas_catalogo SET codigo = 'membro-superior-esquerdo-circunferencial' WHERE codigo = 'mse-circunferencial';

-- membro-inferior-direito
UPDATE public.regioes_anatomicas_catalogo SET codigo = 'membro-inferior-direito-anterior'        WHERE codigo = 'mid-anterior';
UPDATE public.regioes_anatomicas_catalogo SET codigo = 'membro-inferior-direito-posterior'       WHERE codigo = 'mid-posterior';
UPDATE public.regioes_anatomicas_catalogo SET codigo = 'membro-inferior-direito-circunferencial' WHERE codigo = 'mid-circunferencial';

-- membro-inferior-esquerdo
UPDATE public.regioes_anatomicas_catalogo SET codigo = 'membro-inferior-esquerdo-anterior'        WHERE codigo = 'mie-anterior';
UPDATE public.regioes_anatomicas_catalogo SET codigo = 'membro-inferior-esquerdo-posterior'       WHERE codigo = 'mie-posterior';
UPDATE public.regioes_anatomicas_catalogo SET codigo = 'membro-inferior-esquerdo-circunferencial' WHERE codigo = 'mie-circunferencial';

-- ============================================================
-- BLOCO 2: atualizar pai_codigo dos filhos em cascata (R7)
-- Necessário para banco novo onde seed 20260526000001 inseriu
-- nível 2/3 com pai_codigo apontando para os códigos antigos.
-- Em produção (0 filhos), afeta 0 linhas — inofensivo.
-- ============================================================

-- filhos de msd-*
UPDATE public.regioes_anatomicas_catalogo SET pai_codigo = 'membro-superior-direito-anterior'        WHERE pai_codigo = 'msd-anterior';
UPDATE public.regioes_anatomicas_catalogo SET pai_codigo = 'membro-superior-direito-posterior'       WHERE pai_codigo = 'msd-posterior';
UPDATE public.regioes_anatomicas_catalogo SET pai_codigo = 'membro-superior-direito-circunferencial' WHERE pai_codigo = 'msd-circunferencial';

-- filhos de mse-*
UPDATE public.regioes_anatomicas_catalogo SET pai_codigo = 'membro-superior-esquerdo-anterior'        WHERE pai_codigo = 'mse-anterior';
UPDATE public.regioes_anatomicas_catalogo SET pai_codigo = 'membro-superior-esquerdo-posterior'       WHERE pai_codigo = 'mse-posterior';
UPDATE public.regioes_anatomicas_catalogo SET pai_codigo = 'membro-superior-esquerdo-circunferencial' WHERE pai_codigo = 'mse-circunferencial';

-- filhos de mid-*
UPDATE public.regioes_anatomicas_catalogo SET pai_codigo = 'membro-inferior-direito-anterior'        WHERE pai_codigo = 'mid-anterior';
UPDATE public.regioes_anatomicas_catalogo SET pai_codigo = 'membro-inferior-direito-posterior'       WHERE pai_codigo = 'mid-posterior';
UPDATE public.regioes_anatomicas_catalogo SET pai_codigo = 'membro-inferior-direito-circunferencial' WHERE pai_codigo = 'mid-circunferencial';

-- filhos de mie-*
UPDATE public.regioes_anatomicas_catalogo SET pai_codigo = 'membro-inferior-esquerdo-anterior'        WHERE pai_codigo = 'mie-anterior';
UPDATE public.regioes_anatomicas_catalogo SET pai_codigo = 'membro-inferior-esquerdo-posterior'       WHERE pai_codigo = 'mie-posterior';
UPDATE public.regioes_anatomicas_catalogo SET pai_codigo = 'membro-inferior-esquerdo-circunferencial' WHERE pai_codigo = 'mie-circunferencial';
