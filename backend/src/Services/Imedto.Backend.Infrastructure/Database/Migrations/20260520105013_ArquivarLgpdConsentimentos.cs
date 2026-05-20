using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <summary>
    /// Arquiva a tabela `lgpd_consentimentos` (módulo legado de consentimentos LGPD por
    /// usuário) renomeando-a para `lgpd_consentimentos_arquivo`. Não dropamos a tabela
    /// imediatamente para permitir rollback. Pode ser dropada via DROP TABLE em 30 dias.
    ///
    /// Fase 5 da feature Termos de Consentimento — a partir desta migration o aceite
    /// passa a viver em `termo_emitido` por paciente, não mais em consentimentos por
    /// usuário desacoplados de paciente.
    /// </summary>
    public partial class ArquivarLgpdConsentimentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // RENAME idempotente (não falha se já foi renomeada manualmente).
            migrationBuilder.Sql(
                "ALTER TABLE IF EXISTS public.lgpd_consentimentos RENAME TO lgpd_consentimentos_arquivo;");

            migrationBuilder.Sql(
                "COMMENT ON TABLE public.lgpd_consentimentos_arquivo IS " +
                "'Arquivada em 2026-05-20 — módulo legado removido na Fase 5 de Termos de Consentimento. " +
                "Tabela mantida pra rollback. Apagar via DROP em 30 dias.';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverte o RENAME — restaura a tabela original.
            migrationBuilder.Sql(
                "ALTER TABLE IF EXISTS public.lgpd_consentimentos_arquivo RENAME TO lgpd_consentimentos;");
            migrationBuilder.Sql("COMMENT ON TABLE public.lgpd_consentimentos IS NULL;");
        }
    }
}
