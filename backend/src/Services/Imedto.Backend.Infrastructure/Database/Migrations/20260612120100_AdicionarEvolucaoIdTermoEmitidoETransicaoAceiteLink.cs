using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <summary>
    /// Briefing 2026-06-12_002 — Termo de consentimento físico-primeiro.
    ///
    /// Necessidade 1: adiciona coluna evolucao_id (bigint, nullable) em termo_emitido,
    /// com FK para prontuario_evolucoes (ON DELETE SET NULL). Termos avulsos ficam com NULL.
    /// Índice simples em evolucao_id para leitura da timeline.
    ///
    /// Necessidade 2: transição idempotente — marca como 'Expirado' todos os termos
    /// com assinatura_tipo = 'AceiteLink' e status = 'Pendente' (fluxo removido do produto),
    /// e limpa tokens mortos de registros já finalizados (segredos desnecessários).
    ///
    /// Down() reverte o schema (coluna + FK + índice). O UPDATE de dados não é revertido
    /// automaticamente — rollback manual via script de restauração caso necessário.
    /// </summary>
    public partial class AdicionarEvolucaoIdTermoEmitidoETransicaoAceiteLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Necessidade 1: coluna evolucao_id ─────────────────────────────────
            // Implementação via SQL raw idempotente para tolerar o caso em que o
            // schema foi aplicado manualmente antes do registro em __ef_migrations_history
            // (incidente deploy 2026-06-12, run Actions 27449733719).
            // ADD COLUMN IF NOT EXISTS e DO-guards em FK/índice garantem que o bloco
            // pode rodar com qualquer estado pré-existente sem erro.

            migrationBuilder.Sql(@"
ALTER TABLE public.termo_emitido
    ADD COLUMN IF NOT EXISTS evolucao_id bigint NULL;
");

            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_termo_emitido_evolucao'
          AND conrelid = 'public.termo_emitido'::regclass
    ) THEN
        ALTER TABLE public.termo_emitido
            ADD CONSTRAINT fk_termo_emitido_evolucao
            FOREIGN KEY (evolucao_id)
            REFERENCES public.prontuario_evolucoes (id)
            ON DELETE SET NULL;
    END IF;
END $$;
");

            migrationBuilder.Sql(@"
CREATE INDEX IF NOT EXISTS ix_termo_emitido_evolucao_id
    ON public.termo_emitido (evolucao_id);
");

            // ── Necessidade 2: transição AceiteLink → Expirado ────────────────────
            // Idempotente: WHERE duplo garante que rodar 2x não altera mais nada.
            migrationBuilder.Sql(@"
UPDATE public.termo_emitido
SET    status        = 'Expirado',
       atualizado_em = now()
WHERE  assinatura_tipo = 'AceiteLink'
  AND  status          = 'Pendente';
");

            // Limpeza de tokens mortos (segredos desnecessários — LGPD minimização).
            // Idempotente: WHERE token_aceite IS NOT NULL garante que rodar 2x não altera.
            migrationBuilder.Sql(@"
UPDATE public.termo_emitido
SET    token_aceite    = NULL,
       token_expira_em = NULL,
       atualizado_em   = now()
WHERE  assinatura_tipo = 'AceiteLink'
  AND  token_aceite    IS NOT NULL
  AND  status          IN ('Expirado', 'Assinado', 'Recusado', 'Revogado');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_termo_emitido_evolucao",
                schema: "public",
                table: "termo_emitido");

            migrationBuilder.DropIndex(
                name: "ix_termo_emitido_evolucao_id",
                schema: "public",
                table: "termo_emitido");

            migrationBuilder.DropColumn(
                name: "evolucao_id",
                schema: "public",
                table: "termo_emitido");

            // UPDATE de transição (AceiteLink → Expirado) NÃO é revertido aqui.
            // Rollback manual se necessário:
            //   UPDATE public.termo_emitido SET status = 'Pendente', atualizado_em = now()
            //   WHERE assinatura_tipo = 'AceiteLink' AND status = 'Expirado';
        }
    }
}
