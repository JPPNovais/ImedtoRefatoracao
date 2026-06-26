-- ─────────────────────────────────────────────────────────────────────────────
-- Seed do catálogo global de regiões anatômicas — nível 1 anterior + posterior.
--
-- Tabela global (sem estabelecimento_id): é um catálogo de referência clínica,
-- equivalente a TUSS/CBHPM. Todos os tenants compartilham o mesmo conjunto.
--
-- Decisão 2026-06-25 (briefing 002, fusão estrutural do tronco): Tórax, Abdome e
-- Pelve (anterior + posterior) foram FUNDIDOS em uma única região "Tronco" por
-- vista. A Lombossacra está absorvida no Tronco (posterior). As 6 antigas
-- (torax-anterior/abdome-anterior/pelve-anterior/torax-posterior/
-- lombossacra-posterior/pelve-posterior) são removidas pelo arquivo
-- 20260625000001_fusao_tronco_remover_regioes_antigas.sql (que roda depois).
-- Este seed já NÃO as insere — assim numa instalação limpa elas nunca surgem.
--
-- Sub-regiões (nível 2 e 3) são gerenciadas manualmente — NÃO seedadas.
-- Idempotente: ON CONFLICT (codigo) DO NOTHING — seguro para re-runs.
-- Índice único: uq_regioes_anatomicas_catalogo_codigo.
-- Sem BEGIN/COMMIT — a pipeline (deploy/scripts/migrate.sh) gerencia a transação.
-- ─────────────────────────────────────────────────────────────────────────────

-- ── NÍVEL 1 — Vista ANTERIOR (6 regiões: cabeça, pescoço, tronco, membros) ───
INSERT INTO public.regioes_anatomicas_catalogo
    (codigo, nome, pai_codigo, nivel, vista, template_texto, ordem, lateralidade)
VALUES
    ('cabeca-anterior', 'Cabeça (anterior)', NULL, 1, 'anterior',
     'Crânio normocefálico, sem deformidades. Couro cabeludo sem lesões visíveis.', 1, false),
    ('pescoco-anterior', 'Pescoço (anterior)', NULL, 1, 'anterior',
     'Pescoço cilíndrico, sem massas palpáveis. Tireoide sem alterações à palpação.', 2, false),
    ('tronco-anterior', 'Tronco (anterior)', NULL, 1, 'anterior',
     'Tórax simétrico, expansibilidade preservada. Abdome plano, flácido, indolor à palpação. Ruídos hidroaéreos presentes.', 3, false),
    ('membro-superior-direito-anterior', 'Membro superior direito (anterior)', NULL, 1, 'anterior',
     'Sem edema, sem deformidades. Pulsos radial e ulnar palpáveis e simétricos. Força muscular preservada.', 4, false),
    ('membro-superior-esquerdo-anterior', 'Membro superior esquerdo (anterior)', NULL, 1, 'anterior',
     'Sem edema, sem deformidades. Pulsos radial e ulnar palpáveis e simétricos. Força muscular preservada.', 5, false),
    ('membro-inferior-direito-anterior', 'Membro inferior direito (anterior)', NULL, 1, 'anterior',
     'Sem edema, sem varizes. Pulsos pedioso e tibial posterior palpáveis. Força muscular preservada.', 6, false),
    ('membro-inferior-esquerdo-anterior', 'Membro inferior esquerdo (anterior)', NULL, 1, 'anterior',
     'Sem edema, sem varizes. Pulsos pedioso e tibial posterior palpáveis. Força muscular preservada.', 7, false)
ON CONFLICT (codigo) DO NOTHING;

-- ── NÍVEL 1 — Vista POSTERIOR (6 regiões: cabeça, pescoço, tronco, membros) ──
-- Nota: Lombossacra está absorvida no Tronco (posterior) — briefing 2026-06-25_002.
INSERT INTO public.regioes_anatomicas_catalogo
    (codigo, nome, pai_codigo, nivel, vista, template_texto, ordem, lateralidade)
VALUES
    ('cabeca-posterior', 'Cabeça (posterior)', NULL, 1, 'posterior',
     'Região occipital sem massas ou deformidades. Couro cabeludo sem lesões.', 8, false),
    ('pescoco-posterior', 'Pescoço (posterior)', NULL, 1, 'posterior',
     'Nuca livre. Sem rigidez de nuca. Musculatura cervical posterior sem contraturas.', 9, false),
    ('tronco-posterior', 'Tronco (posterior)', NULL, 1, 'posterior',
     'Dorso simétrico. Coluna torácica sem desvios. Murmúrio vesicular fisiológico bilateral. Coluna lombar sem desvios. Lasègue negativo bilateral.', 10, false),
    ('membro-superior-direito-posterior', 'Membro superior direito (posterior)', NULL, 1, 'posterior',
     'Face posterior sem lesões. Tríceps e musculatura posterior do antebraço com tônus preservado.', 11, false),
    ('membro-superior-esquerdo-posterior', 'Membro superior esquerdo (posterior)', NULL, 1, 'posterior',
     'Face posterior sem lesões. Tríceps e musculatura posterior do antebraço com tônus preservado.', 12, false),
    ('membro-inferior-direito-posterior', 'Membro inferior direito (posterior)', NULL, 1, 'posterior',
     'Face posterior sem lesões. Panturrilha sem empastamento. Sinal de Homans negativo.', 13, false),
    ('membro-inferior-esquerdo-posterior', 'Membro inferior esquerdo (posterior)', NULL, 1, 'posterior',
     'Face posterior sem lesões. Panturrilha sem empastamento. Sinal de Homans negativo.', 14, false)
ON CONFLICT (codigo) DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- Verificação pós-seed (apenas informativa — não bloqueia deploy):
-- SELECT nivel, COUNT(*) FROM public.regioes_anatomicas_catalogo GROUP BY nivel ORDER BY nivel;
-- Esperado (após fusão): nivel 1 = 21 (14 anterior/posterior + 7 circunferenciais),
--                         nivel 2 = 1 (cabeca-anterior-olho, gerenciada manualmente)
-- As 9 antigas são removidas pelo arquivo 20260625000001_fusao_tronco.sql.
-- ─────────────────────────────────────────────────────────────────────────────
