-- Migration: 20260611170000_backfill_assinaturas_f2
-- Briefing 2026-06-11_003 — F2: Backfill de todos os estabelecimentos para imedto_assinaturas.
--
-- OBJETIVO: garantir que todo estabelecimento existente tenha exatamente uma assinatura vigente
-- (fim_em IS NULL) na estrutura nova, espelhando o estado da estrutura legada (assinaturas).
-- REGRA DE OURO: estabelecimentos liberados ANTES continuam liberados DEPOIS. Ninguém bloqueado
-- indevidamente; ninguém bloqueado hoje é "desacidentalmente liberado".
--
-- Mapeamento legado → nova vigência (R8 do briefing):
--   Ativa + ExpiraEm=NULL           → expira_em=NULL, suspensa_em=NULL     (VITALÍCIO — liberado)
--   Ativa/Trial + ExpiraEm futuro   → expira_em=ExpiraEm, suspensa_em=NULL  (TEMPORÁRIO — liberado)
--   Suspensa / Cancelada            → suspensa_em=now(), expira_em=NULL      (BLOQUEADO — suspensão)
--   Expirada / ExpiraEm no passado  → expira_em=ExpiraEm (ou now()), suspensa_em=NULL (BLOQUEADO — expirado)
--   Sem registro legado (edge case) → expira_em=NULL, suspensa_em=NULL     (VITALÍCIO — conservador)
--
-- Todos os casos: plano_id = '00000000-0000-0000-0000-000000000001' (Gratuidade Vitalícia),
--   origem='admin_manual', status_cobranca='nao_aplicavel',
--   gratuita=true, motivo='Backfill automático — migração para estrutura nova (2026-06-11_003)'.
--
-- IDEMPOTÊNCIA: checagem por (estabelecimento_id WHERE fim_em IS NULL) antes de cada INSERT.
--   Reexecutar 2x não cria segunda vigência.
--
-- BEGIN/COMMIT removidos — a pipeline (migrate.sh) gerencia transação externamente.

DO $BACKFILL$
DECLARE
    v_migration_id   TEXT := '20260611170000_F2BackfillAssinaturas';
    v_plano_gratuidade UUID := '00000000-0000-0000-0000-000000000001';
    v_motivo         TEXT := 'Backfill automático — migração para estrutura nova (2026-06-11_003)';

    -- Contadores para RAISE NOTICE (CA12/CA37)
    v_total_estabs   INT := 0;
    v_ja_tem_vigente INT := 0;
    v_inseridos      INT := 0;
    v_vitalicios     INT := 0;
    v_temporarios    INT := 0;
    v_bloqueados     INT := 0;

    rec RECORD;
    v_expira_em      TIMESTAMPTZ;
    v_suspensa_em    TIMESTAMPTZ;
BEGIN
    -- Guard de idempotência no nível de migration completa:
    -- Se a migration já foi registrada no histórico EF, não faz nada.
    IF EXISTS (
        SELECT 1 FROM public.__ef_migrations_history
        WHERE "MigrationId" = v_migration_id
    ) THEN
        RAISE NOTICE '[F2 Backfill] Migration % já registrada — skipping.', v_migration_id;
        RETURN;
    END IF;

    RAISE NOTICE '[F2 Backfill] Iniciando backfill de estabelecimentos para imedto_assinaturas...';

    -- Iterar todos os estabelecimentos existentes.
    -- LEFT JOIN com assinaturas para pegar o estado legado (se existir).
    -- Um estabelecimento pode não ter registro legado (edge case de dado antigo).
    FOR rec IN
        SELECT
            e.id                        AS estab_id,
            a.status                    AS status_legado,
            a.expira_em                 AS expira_em_legado,
            a.iniciada_em               AS iniciada_em_legado
        FROM public.estabelecimentos e
        LEFT JOIN public.assinaturas a ON a.estabelecimento_id = e.id
        ORDER BY e.id
    LOOP
        v_total_estabs := v_total_estabs + 1;

        -- Verificar se já existe vigente (idempotência por estabelecimento)
        IF EXISTS (
            SELECT 1 FROM public.imedto_assinaturas
            WHERE estabelecimento_id = rec.estab_id
              AND fim_em IS NULL
        ) THEN
            v_ja_tem_vigente := v_ja_tem_vigente + 1;
            CONTINUE;
        END IF;

        -- Determinar expira_em e suspensa_em conforme mapeamento R8
        v_expira_em   := NULL;
        v_suspensa_em := NULL;

        IF rec.status_legado IS NULL THEN
            -- Edge case: sem registro legado → conservador → vitalício liberado
            v_expira_em   := NULL;
            v_suspensa_em := NULL;
            v_vitalicios  := v_vitalicios + 1;

        ELSIF rec.status_legado = 'Ativa' AND rec.expira_em_legado IS NULL THEN
            -- Ativa vitalícia → vitalício
            v_expira_em   := NULL;
            v_suspensa_em := NULL;
            v_vitalicios  := v_vitalicios + 1;

        ELSIF rec.status_legado IN ('Ativa', 'Trial') AND rec.expira_em_legado > now() THEN
            -- Trial ou Ativa com expiração futura → temporário (liberado até a data)
            v_expira_em   := rec.expira_em_legado AT TIME ZONE 'UTC';
            v_suspensa_em := NULL;
            v_temporarios := v_temporarios + 1;

        ELSIF rec.status_legado IN ('Suspensa', 'Cancelada') THEN
            -- Bloqueado por suspensão/cancelamento → manter bloqueado via suspensa_em
            -- Usa expira_em=NULL para não criar ambiguidade de "expirado vs suspenso"
            v_expira_em   := NULL;
            v_suspensa_em := now();
            v_bloqueados  := v_bloqueados + 1;

        ELSE
            -- Expirada, ou Ativa/Trial com expira_em no passado → bloqueado por expiração
            -- Preserva a data original se existir; caso contrário usa now() - 1s para marcar expirado
            IF rec.expira_em_legado IS NOT NULL THEN
                v_expira_em := rec.expira_em_legado AT TIME ZONE 'UTC';
            ELSE
                -- Expirada sem data (dado inconsistente) → 1 segundo no passado para garantir bloqueio
                v_expira_em := now() - interval '1 second';
            END IF;
            v_suspensa_em := NULL;
            v_bloqueados  := v_bloqueados + 1;
        END IF;

        INSERT INTO public.imedto_assinaturas (
            id,
            estabelecimento_id,
            plano_id,
            iniciada_em,
            fim_em,
            expira_em,
            suspensa_em,
            origem,
            referencia_externa,
            status_cobranca,
            gratuita,
            motivo,
            criada_em,
            criada_por_admin_id
        )
        VALUES (
            gen_random_uuid(),
            rec.estab_id,
            v_plano_gratuidade,
            -- Preservar data original de início quando disponível; caso contrário now()
            COALESCE(rec.iniciada_em_legado AT TIME ZONE 'UTC', now()),
            NULL,                       -- fim_em = NULL → vigente
            v_expira_em,
            v_suspensa_em,
            'admin_manual',
            NULL,                       -- referencia_externa dormente
            'nao_aplicavel',
            true,                       -- gratuita = true (Gratuidade Vitalícia)
            v_motivo,
            now(),
            NULL                        -- criada_por_admin_id = NULL (operação de sistema)
        );

        v_inseridos := v_inseridos + 1;
    END LOOP;

    RAISE NOTICE '[F2 Backfill] Concluído.';
    RAISE NOTICE '[F2 Backfill]   Total de estabelecimentos processados : %', v_total_estabs;
    RAISE NOTICE '[F2 Backfill]   Já tinham vigente (skipped)           : %', v_ja_tem_vigente;
    RAISE NOTICE '[F2 Backfill]   Inserções realizadas                  : %', v_inseridos;
    RAISE NOTICE '[F2 Backfill]   -> Vitalícios (liberados)             : %', v_vitalicios;
    RAISE NOTICE '[F2 Backfill]   -> Temporários (liberados)            : %', v_temporarios;
    RAISE NOTICE '[F2 Backfill]   -> Bloqueados (suspensos/expirados)   : %', v_bloqueados;
    RAISE NOTICE '[F2 Backfill] Liberados antes (legada) devem = Liberados depois (nova): % + % = % liberados', v_vitalicios, v_temporarios, (v_vitalicios + v_temporarios);

    -- Registrar no histórico EF para idempotência de nível de migration
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES (v_migration_id, '10.0.0');

END $BACKFILL$;
