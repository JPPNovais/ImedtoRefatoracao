using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarCanalWhatsappLembrete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Briefing 2026-06-18_005 — lembrete de consulta via WhatsApp (MVP)
            // DDL idempotente via SQL raw (gotcha: AddColumn cru não é idempotente —
            // QA cria schema no mesmo Postgres da EC2 sem gravar __EFMigrationsHistory).

            // 1. configuracoes_automacao: habilitar canal WhatsApp por estabelecimento
            migrationBuilder.Sql("""
                ALTER TABLE public.configuracoes_automacao
                    ADD COLUMN IF NOT EXISTS lembretes_whatsapp_habilitados boolean NOT NULL DEFAULT false;
                """);

            // 2. pacientes: consentimento (opt-in) LGPD para lembretes via WhatsApp
            migrationBuilder.Sql("""
                ALTER TABLE public.pacientes
                    ADD COLUMN IF NOT EXISTS whatsapp_lembrete_opt_in boolean NOT NULL DEFAULT false,
                    ADD COLUMN IF NOT EXISTS whatsapp_lembrete_opt_in_em timestamp with time zone NULL,
                    ADD COLUMN IF NOT EXISTS whatsapp_lembrete_opt_in_por_usuario_id uuid NULL;
                """);

            // 3. agendamentos: controle de idempotência de envio (espelho de lembrete_por_email_enviado)
            migrationBuilder.Sql("""
                ALTER TABLE public.agendamentos
                    ADD COLUMN IF NOT EXISTS lembrete_por_whatsapp_enviado boolean NOT NULL DEFAULT false;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE public.agendamentos
                    DROP COLUMN IF EXISTS lembrete_por_whatsapp_enviado;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE public.pacientes
                    DROP COLUMN IF EXISTS whatsapp_lembrete_opt_in_por_usuario_id,
                    DROP COLUMN IF EXISTS whatsapp_lembrete_opt_in_em,
                    DROP COLUMN IF EXISTS whatsapp_lembrete_opt_in;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE public.configuracoes_automacao
                    DROP COLUMN IF EXISTS lembretes_whatsapp_habilitados;
                """);
        }
    }
}
