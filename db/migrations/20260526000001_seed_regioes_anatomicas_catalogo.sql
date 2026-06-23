-- ─────────────────────────────────────────────────────────────────────────────
-- Seed do catálogo global de regiões anatômicas — 18 registros (nível 1 apenas).
--
-- Tabela global (sem estabelecimento_id): é um catálogo de referência clínica,
-- equivalente a TUSS/CBHPM. Todos os tenants compartilham o mesmo conjunto.
--
-- Decisão 2026-06-23: seed contém SOMENTE os 18 nós nível 1 (anterior + posterior).
-- Sub-regiões (nível 2 e 3) são gerenciadas manualmente pelo usuário — NÃO seedadas.
-- Este arquivo é reaplicado a cada deploy; ON CONFLICT (codigo) DO NOTHING garante
-- idempotência total.
--
-- Idempotente: ON CONFLICT (codigo) DO NOTHING — seguro para re-runs e rollbacks
-- parciais. Índice único: uq_regioes_anatomicas_catalogo_codigo.
--
-- Sem BEGIN/COMMIT — a pipeline (deploy/scripts/migrate.sh) gerencia a transação.
-- ─────────────────────────────────────────────────────────────────────────────

-- ── NÍVEL 1 — Vista ANTERIOR (9 registros) ───────────────────────────────────
INSERT INTO public.regioes_anatomicas_catalogo
    (codigo, nome, pai_codigo, nivel, vista, template_texto, ordem, lateralidade)
VALUES
    ('cabeca-anterior', 'Cabeça (anterior)', NULL, 1, 'anterior',
     'Crânio normocefálico, sem deformidades. Couro cabeludo sem lesões visíveis.', 1, false),
    ('pescoco-anterior', 'Pescoço (anterior)', NULL, 1, 'anterior',
     'Pescoço cilíndrico, sem massas palpáveis. Tireoide sem alterações à palpação.', 2, false),
    ('torax-anterior', 'Tórax (anterior)', NULL, 1, 'anterior',
     'Tórax simétrico, expansibilidade preservada. Murmúrio vesicular fisiológico bilateral.', 3, false),
    ('abdome-anterior', 'Abdome (anterior)', NULL, 1, 'anterior',
     'Abdome plano, flácido, indolor à palpação superficial e profunda. Ruídos hidroaéreos presentes.', 4, false),
    ('pelve-anterior', 'Pelve (anterior)', NULL, 1, 'anterior',
     'Região pélvica sem alterações à inspeção. Sem sinais de irritação peritoneal.', 5, false),
    ('membro-superior-direito-anterior', 'Membro superior direito (anterior)', NULL, 1, 'anterior',
     'Sem edema, sem deformidades. Pulsos radial e ulnar palpáveis e simétricos. Força muscular preservada.', 6, false),
    ('membro-superior-esquerdo-anterior', 'Membro superior esquerdo (anterior)', NULL, 1, 'anterior',
     'Sem edema, sem deformidades. Pulsos radial e ulnar palpáveis e simétricos. Força muscular preservada.', 7, false),
    ('membro-inferior-direito-anterior', 'Membro inferior direito (anterior)', NULL, 1, 'anterior',
     'Sem edema, sem varizes. Pulsos pedioso e tibial posterior palpáveis. Força muscular preservada.', 8, false),
    ('membro-inferior-esquerdo-anterior', 'Membro inferior esquerdo (anterior)', NULL, 1, 'anterior',
     'Sem edema, sem varizes. Pulsos pedioso e tibial posterior palpáveis. Força muscular preservada.', 9, false)
ON CONFLICT (codigo) DO NOTHING;

-- ── NÍVEL 1 — Vista POSTERIOR (9 registros) ──────────────────────────────────
INSERT INTO public.regioes_anatomicas_catalogo
    (codigo, nome, pai_codigo, nivel, vista, template_texto, ordem, lateralidade)
VALUES
    ('cabeca-posterior', 'Cabeça (posterior)', NULL, 1, 'posterior',
     'Região occipital sem massas ou deformidades. Couro cabeludo sem lesões.', 10, false),
    ('pescoco-posterior', 'Pescoço (posterior)', NULL, 1, 'posterior',
     'Nuca livre. Sem rigidez de nuca. Musculatura cervical posterior sem contraturas.', 11, false),
    ('torax-posterior', 'Tórax (posterior)', NULL, 1, 'posterior',
     'Dorso simétrico. Coluna torácica sem desvios. Murmúrio vesicular fisiológico bilateral.', 12, false),
    ('lombossacra-posterior', 'Região lombossacra (posterior)', NULL, 1, 'posterior',
     'Coluna lombar sem desvios. Sem dor à palpação de processos espinhosos. Lasègue negativo bilateral.', 13, false),
    ('pelve-posterior', 'Pelve (posterior)', NULL, 1, 'posterior',
     'Região glútea sem alterações. Sem sinais de fístulas ou lesões perianais.', 14, false),
    ('membro-superior-direito-posterior', 'Membro superior direito (posterior)', NULL, 1, 'posterior',
     'Face posterior sem lesões. Tríceps e musculatura posterior do antebraço com tônus preservado.', 15, false),
    ('membro-superior-esquerdo-posterior', 'Membro superior esquerdo (posterior)', NULL, 1, 'posterior',
     'Face posterior sem lesões. Tríceps e musculatura posterior do antebraço com tônus preservado.', 16, false),
    ('membro-inferior-direito-posterior', 'Membro inferior direito (posterior)', NULL, 1, 'posterior',
     'Face posterior sem lesões. Panturrilha sem empastamento. Sinal de Homans negativo.', 17, false),
    ('membro-inferior-esquerdo-posterior', 'Membro inferior esquerdo (posterior)', NULL, 1, 'posterior',
     'Face posterior sem lesões. Panturrilha sem empastamento. Sinal de Homans negativo.', 18, false)
ON CONFLICT (codigo) DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- Verificação pós-seed (apenas informativa — não bloqueia deploy):
-- SELECT nivel, COUNT(*) FROM public.regioes_anatomicas_catalogo GROUP BY nivel ORDER BY nivel;
-- Esperado: nivel 1 = 27 (18 anterior/posterior + 9 circunferenciais do seed seguinte),
--           nivel 2 = 0, nivel 3 = 0 (sub-regiões são gerenciadas manualmente)
-- ─────────────────────────────────────────────────────────────────────────────
