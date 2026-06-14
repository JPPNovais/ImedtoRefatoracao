-- =============================================================================
-- Migration: 20260613120000_seed_modelos_descricao_cirurgica_padrao
-- Aggregate: ModeloDescricaoCirurgica (bounded context Prontuários)
-- Aplicado:  pela pipeline CI/CD via deploy/scripts/migrate.sh (psql)
-- Transação: gerenciada externamente — NÃO adicionar BEGIN/COMMIT aqui
-- =============================================================================
--
-- Seed dos 6 templates padrão-sistema de descrição cirúrgica.
--
-- Padrão live-link (mesmo de ProntuarioVariavelPool):
--   eh_padrao_sistema = true  → estabelecimento_id = NULL  (global, todos os tenants)
-- Guard: WHERE NOT EXISTS — idempotente por titulo + eh_padrao_sistema.
-- =============================================================================

-- ── Template 1: Colecistectomia videolaparoscópica ───────────────────────────

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260613120000_SeedModelosDescricaoCirurgicaPadrao') THEN

    INSERT INTO public.modelos_descricao_cirurgica (estabelecimento_id, titulo, corpo, ativo, eh_padrao_sistema, criado_em, atualizado_em)
    SELECT NULL, 'Colecistectomia videolaparoscópica',
    'Paciente em decúbito dorsal horizontal, sob anestesia geral. Antissepsia e colocação de campos estéreis de rotina.

Realizada incisão supraumbilical e instalação do pneumoperitônio, atingindo pressão de 12 mmHg. Introdução do trocarte óptico de 10 mm e inventário da cavidade sem alterações dignas de nota.

Sob visão direta, introduzidos os demais trocartes (subxifoide e em flanco direito). Exposição do pedículo cístico com tração do fundo vesicular.

Dissecção do triângulo de Calot com identificação e individualização do ducto cístico e da artéria cística. Obtida a visão crítica de segurança. Clipagem e secção do ducto cístico e da artéria cística.

Descolamento da vesícula biliar do leito hepático com eletrocautério, sem intercorrências. Revisão da hemostasia do leito. Retirada da peça pela incisão umbilical.

Revisão final da cavidade e hemostasia rigorosa. Retirada dos trocartes sob visão direta e desfeito o pneumoperitônio. Síntese das incisões por planos.

Paciente encaminhado à recuperação pós-anestésica em bom estado geral, sem intercorrências.',
    true, true, now(), NULL
    WHERE NOT EXISTS (
        SELECT 1 FROM public.modelos_descricao_cirurgica
        WHERE eh_padrao_sistema = true AND titulo = 'Colecistectomia videolaparoscópica'
    );

    END IF;
END $EF$;

-- ── Template 2: Herniorrafia inguinal (técnica de Lichtenstein) ─────────────

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260613120000_SeedModelosDescricaoCirurgicaPadrao') THEN

    INSERT INTO public.modelos_descricao_cirurgica (estabelecimento_id, titulo, corpo, ativo, eh_padrao_sistema, criado_em, atualizado_em)
    SELECT NULL, 'Herniorrafia inguinal (técnica de Lichtenstein)',
    'Paciente em decúbito dorsal, sob anestesia [raquidiana/geral/local]. Antissepsia e colocação de campos estéreis.

Incisão inguinal oblíqua à [direita/esquerda], com abertura por planos até a aponeurose do músculo oblíquo externo, que é incisada no sentido de suas fibras, preservando-se o nervo ílio-inguinal.

Identificação e isolamento do cordão espermático. Inventário do canal inguinal evidenciando hérnia [indireta/direta]. Dissecção e redução do saco herniário à cavidade.

Posicionamento de tela de polipropileno sobre o assoalho do canal inguinal, fixada sem tensão (técnica de Lichtenstein), com confecção do novo anel inguinal profundo.

Revisão da hemostasia. Reposicionamento do cordão espermático. Síntese da aponeurose, do subcutâneo e da pele por planos.

Procedimento sem intercorrências. Paciente encaminhado à recuperação em bom estado geral.',
    true, true, now(), NULL
    WHERE NOT EXISTS (
        SELECT 1 FROM public.modelos_descricao_cirurgica
        WHERE eh_padrao_sistema = true AND titulo = 'Herniorrafia inguinal (técnica de Lichtenstein)'
    );

    END IF;
END $EF$;

-- ── Template 3: Apendicectomia videolaparoscópica ────────────────────────────

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260613120000_SeedModelosDescricaoCirurgicaPadrao') THEN

    INSERT INTO public.modelos_descricao_cirurgica (estabelecimento_id, titulo, corpo, ativo, eh_padrao_sistema, criado_em, atualizado_em)
    SELECT NULL, 'Apendicectomia videolaparoscópica',
    'Paciente em decúbito dorsal, sob anestesia geral. Antissepsia e colocação de campos estéreis.

Instalação do pneumoperitônio e introdução do trocarte óptico umbilical. Inventário da cavidade evidenciando apêndice cecal [hiperemiado/com fibrina/perfurado] e líquido [seroso/purulento] em fossa ilíaca direita.

Introdução dos trocartes acessórios sob visão direta. Identificação do apêndice cecal e do mesoapêndice.

Ligadura e secção do mesoapêndice. Ligadura da base apendicular com [endoloop/grampeador] e secção do apêndice. Retirada da peça em endobag.

Lavagem e aspiração da cavidade. Revisão da hemostasia. Retirada dos trocartes sob visão direta e síntese das incisões por planos.

Procedimento sem intercorrências. Paciente encaminhado à recuperação em bom estado geral.',
    true, true, now(), NULL
    WHERE NOT EXISTS (
        SELECT 1 FROM public.modelos_descricao_cirurgica
        WHERE eh_padrao_sistema = true AND titulo = 'Apendicectomia videolaparoscópica'
    );

    END IF;
END $EF$;

-- ── Template 4: Hemorroidectomia ─────────────────────────────────────────────

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260613120000_SeedModelosDescricaoCirurgicaPadrao') THEN

    INSERT INTO public.modelos_descricao_cirurgica (estabelecimento_id, titulo, corpo, ativo, eh_padrao_sistema, criado_em, atualizado_em)
    SELECT NULL, 'Hemorroidectomia',
    'Paciente em posição [de litotomia/canivete], sob anestesia [raquidiana/geral]. Antissepsia e colocação de campos estéreis.

Toque retal e anuscopia evidenciando mamilos hemorroidários [internos/externos/mistos] nas posições [___].

Exérese dos mamilos hemorroidários pela técnica [aberta de Milligan-Morgan / fechada de Ferguson], com ligadura dos pedículos vasculares.

Revisão da hemostasia, sem sangramento ativo. Preservação de pontes de mucosa entre as feridas para evitar estenose.

Curativo compressivo. Procedimento sem intercorrências. Paciente encaminhado à recuperação em bom estado geral.',
    true, true, now(), NULL
    WHERE NOT EXISTS (
        SELECT 1 FROM public.modelos_descricao_cirurgica
        WHERE eh_padrao_sistema = true AND titulo = 'Hemorroidectomia'
    );

    END IF;
END $EF$;

-- ── Template 5: Postectomia (circuncisão) ────────────────────────────────────

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260613120000_SeedModelosDescricaoCirurgicaPadrao') THEN

    INSERT INTO public.modelos_descricao_cirurgica (estabelecimento_id, titulo, corpo, ativo, eh_padrao_sistema, criado_em, atualizado_em)
    SELECT NULL, 'Postectomia (circuncisão)',
    'Paciente em decúbito dorsal, sob anestesia [geral/local/bloqueio peniano]. Antissepsia e colocação de campos estéreis.

Retração do prepúcio e liberação de aderências balanoprepuciais. Demarcação da incisão circular nas faces externa e interna do prepúcio.

Exérese do excesso de prepúcio, preservando o frênulo. Revisão da hemostasia com eletrocautério/ligaduras pontuais.

Aproximação das bordas mucosa e cutânea com sutura absorvível em pontos separados. Curativo.

Procedimento sem intercorrências. Paciente encaminhado à recuperação em bom estado geral.',
    true, true, now(), NULL
    WHERE NOT EXISTS (
        SELECT 1 FROM public.modelos_descricao_cirurgica
        WHERE eh_padrao_sistema = true AND titulo = 'Postectomia (circuncisão)'
    );

    END IF;
END $EF$;

-- ── Template 6: Exérese de lesão cutânea ─────────────────────────────────────

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260613120000_SeedModelosDescricaoCirurgicaPadrao') THEN

    INSERT INTO public.modelos_descricao_cirurgica (estabelecimento_id, titulo, corpo, ativo, eh_padrao_sistema, criado_em, atualizado_em)
    SELECT NULL, 'Exérese de lesão cutânea',
    'Paciente posicionado para acesso à lesão em [região ___], sob anestesia local com [lidocaína/bupivacaína] [com/sem] vasoconstritor. Antissepsia e colocação de campos estéreis.

Demarcação de incisão fusiforme com margem de segurança de [___] mm ao redor da lesão.

Exérese da lesão incluindo tecido subcutâneo adjacente. Peça encaminhada para exame anatomopatológico.

Revisão da hemostasia. Aproximação das bordas por planos com sutura [absorvível/inabsorvível]. Curativo.

Procedimento sem intercorrências. Paciente orientado quanto aos cuidados com a ferida e retorno para resultado do anatomopatológico.',
    true, true, now(), NULL
    WHERE NOT EXISTS (
        SELECT 1 FROM public.modelos_descricao_cirurgica
        WHERE eh_padrao_sistema = true AND titulo = 'Exérese de lesão cutânea'
    );

    END IF;
END $EF$;

-- ── Registro em __ef_migrations_history ──────────────────────────────────────

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260613120000_SeedModelosDescricaoCirurgicaPadrao') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260613120000_SeedModelosDescricaoCirurgicaPadrao', '10.0.0');
    END IF;
END $EF$;
