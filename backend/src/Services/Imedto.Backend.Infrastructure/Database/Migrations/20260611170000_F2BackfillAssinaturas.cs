using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <summary>
    /// F2 — Backfill de todos os estabelecimentos para imedto_assinaturas.
    /// Briefing 2026-06-11_003 (CA8–CA15).
    ///
    /// Data migration idempotente: garante que todo estabelecimento tenha exatamente uma
    /// assinatura vigente (fim_em IS NULL) na estrutura nova, espelhando o estado legado.
    ///
    /// Regra de ouro: liberados antes (legada) = liberados depois (nova).
    /// Bloqueados antes permanecem bloqueados; ninguém é liberado indevidamente.
    ///
    /// Down() é propositalmente vazio: backfill de dados não é reversível sem
    /// risco de perda de vigências criadas posteriormente pelo admin (F4+).
    /// Rollback de F2 requer intervenção manual se necessário.
    /// </summary>
    public partial class F2BackfillAssinaturas : Migration
    {
        private const string MigrationId = "20260611170000_F2BackfillAssinaturas";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // O SQL completo vive no arquivo db/migrations/20260611170000_backfill_assinaturas_f2.sql.
            // Aqui reproduzimos inline para o EF runner (que não executa .sql separado).
            // O guard de idempotência do DO $BACKFILL$ verifica __ef_migrations_history antes de agir.
            migrationBuilder.Sql(@"
DO $BACKFILL$
DECLARE
    v_migration_id   TEXT := '20260611170000_F2BackfillAssinaturas';
    v_plano_gratuidade UUID := '00000000-0000-0000-0000-000000000001';
    v_motivo         TEXT := 'Backfill automático — migração para estrutura nova (2026-06-11_003)';

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
    IF EXISTS (
        SELECT 1 FROM public.__ef_migrations_history
        WHERE ""MigrationId"" = v_migration_id
    ) THEN
        RAISE NOTICE '[F2 Backfill] Migration % já registrada — skipping.', v_migration_id;
        RETURN;
    END IF;

    RAISE NOTICE '[F2 Backfill] Iniciando backfill de estabelecimentos para imedto_assinaturas...';

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

        IF EXISTS (
            SELECT 1 FROM public.imedto_assinaturas
            WHERE estabelecimento_id = rec.estab_id
              AND fim_em IS NULL
        ) THEN
            v_ja_tem_vigente := v_ja_tem_vigente + 1;
            CONTINUE;
        END IF;

        v_expira_em   := NULL;
        v_suspensa_em := NULL;

        IF rec.status_legado IS NULL THEN
            v_expira_em   := NULL;
            v_suspensa_em := NULL;
            v_vitalicios  := v_vitalicios + 1;

        ELSIF rec.status_legado = 'Ativa' AND rec.expira_em_legado IS NULL THEN
            v_expira_em   := NULL;
            v_suspensa_em := NULL;
            v_vitalicios  := v_vitalicios + 1;

        ELSIF rec.status_legado IN ('Ativa', 'Trial') AND rec.expira_em_legado > now() THEN
            v_expira_em   := rec.expira_em_legado AT TIME ZONE 'UTC';
            v_suspensa_em := NULL;
            v_temporarios := v_temporarios + 1;

        ELSIF rec.status_legado IN ('Suspensa', 'Cancelada') THEN
            v_expira_em   := NULL;
            v_suspensa_em := now();
            v_bloqueados  := v_bloqueados + 1;

        ELSE
            IF rec.expira_em_legado IS NOT NULL THEN
                v_expira_em := rec.expira_em_legado AT TIME ZONE 'UTC';
            ELSE
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
            COALESCE(rec.iniciada_em_legado AT TIME ZONE 'UTC', now()),
            NULL,
            v_expira_em,
            v_suspensa_em,
            'admin_manual',
            NULL,
            'nao_aplicavel',
            true,
            v_motivo,
            now(),
            NULL
        );

        v_inseridos := v_inseridos + 1;
    END LOOP;

    RAISE NOTICE '[F2 Backfill] Concluído.';
    RAISE NOTICE '[F2 Backfill]   Total processados : %', v_total_estabs;
    RAISE NOTICE '[F2 Backfill]   Skipped (já tinham vigente) : %', v_ja_tem_vigente;
    RAISE NOTICE '[F2 Backfill]   Inseridos : %', v_inseridos;
    RAISE NOTICE '[F2 Backfill]   Vitalícios : %', v_vitalicios;
    RAISE NOTICE '[F2 Backfill]   Temporários : %', v_temporarios;
    RAISE NOTICE '[F2 Backfill]   Bloqueados : %', v_bloqueados;

    INSERT INTO public.__ef_migrations_history (""MigrationId"", ""ProductVersion"")
    VALUES (v_migration_id, '10.0.0');

END $BACKFILL$;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Backfill de dados não é revertível via migration automática.
            // Rollback manual: DELETE FROM imedto_assinaturas WHERE motivo LIKE 'Backfill automático%';
            // Executar apenas em ambiente de desenvolvimento e com ciência do impacto.
        }
    }
}
