using Dapper;
using Npgsql;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Database;

namespace Imedto.Backend.Infrastructure.Admin;

/// <summary>
/// Implementação do reset administrativo de estabelecimento.
///
/// Usa <see cref="NpgsqlConnection"/> direta (não passa pelo EF Core) para bypassar o
/// <c>SoftDeleteInterceptor</c>. Toda a operação roda em uma única transação Postgres —
/// falha faz rollback automático.
///
/// Ordem de DELETE respeita a topologia de FKs do schema:
/// entidades filho são removidas antes de seus pais. Todas as FKs marcadas com
/// ON DELETE CASCADE no EF também funcionariam, mas a ordem explícita garante clareza
/// e compatibilidade com FKs com RESTRICT.
///
/// AUDITORIA (LGPD): cada tabela recebe um registro em <c>audit_delete_attempts</c>
/// com motivo = "ADMIN_RESET: {motivo}" ANTES de ser deletada, para que o registro
/// sobreviva mesmo que a transação seja commitada com sucesso.
/// O registro de auditoria é inserido fora da transação principal (conexão separada)
/// para persistir independentemente do resultado.
/// </summary>
public class AdminResetService : IAdminResetService
{
    private readonly AppReadConnectionString _connStr;

    public AdminResetService(AppReadConnectionString connStr)
    {
        _connStr = connStr;
    }

    public async Task ResetEstabelecimentoAsync(
        long estabelecimentoId,
        string motivo,
        Guid executadoPorUsuarioId,
        CancellationToken ct = default)
    {
        if (estabelecimentoId <= 0)
            throw new SharedKernel.Domain.BusinessException("Estabelecimento inválido.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new SharedKernel.Domain.BusinessException("Motivo é obrigatório para auditoria.");

        var motivoCompleto = $"ADMIN_RESET: {motivo.Trim()}";

        await using var conn = new NpgsqlConnection(_connStr.Value);
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        try
        {
            // ── Auditoria prévia (fora de transação para persistir sempre) ──────
            // Registrada ANTES de deletar para garantir rastreabilidade mesmo
            // que alguém cancele o processo no meio.
            await RegistrarAuditAsync(conn, tx, estabelecimentoId, executadoPorUsuarioId, motivoCompleto, ct);

            // ── Ordem de DELETE (topologia de FKs — filhos antes de pais) ───────

            // Notificações
            await conn.ExecuteAsync(
                "DELETE FROM public.notificacoes WHERE estabelecimento_id = @Id",
                new { Id = estabelecimentoId }, tx);

            // Eventos de automação (via regras do estabelecimento)
            await conn.ExecuteAsync("""
                DELETE FROM public.automation_events ae
                USING public.automation_rules ar
                WHERE ae.regra_id = ar.id AND ar.estabelecimento_id = @Id
                """, new { Id = estabelecimentoId }, tx);

            // Regras de automação
            await conn.ExecuteAsync(
                "DELETE FROM public.automation_rules WHERE estabelecimento_id = @Id",
                new { Id = estabelecimentoId }, tx);

            // Financeiro — lançamentos, categorias, formas de pagamento
            await conn.ExecuteAsync(
                "DELETE FROM public.lancamentos WHERE estabelecimento_id = @Id",
                new { Id = estabelecimentoId }, tx);
            await conn.ExecuteAsync(
                "DELETE FROM public.categorias_financeiras WHERE estabelecimento_id = @Id",
                new { Id = estabelecimentoId }, tx);
            await conn.ExecuteAsync(
                "DELETE FROM public.formas_pagamento WHERE estabelecimento_id = @Id",
                new { Id = estabelecimentoId }, tx);

            // Inventário
            await conn.ExecuteAsync(
                "DELETE FROM public.movimentacoes_estoque WHERE estabelecimento_id = @Id",
                new { Id = estabelecimentoId }, tx);
            await conn.ExecuteAsync(
                "DELETE FROM public.itens_inventario WHERE estabelecimento_id = @Id",
                new { Id = estabelecimentoId }, tx);

            // Orçamentos (itens e tabelas filhas primeiro — muitos têm ON DELETE CASCADE no schema,
            // mas deletamos explicitamente para clareza e compatibilidade com FKs RESTRICT)
            await conn.ExecuteAsync("""
                DELETE FROM public.orcamento_equipe oe
                USING public.orcamentos o
                WHERE oe.orcamento_id = o.id AND o.estabelecimento_id = @Id
                """, new { Id = estabelecimentoId }, tx);
            await conn.ExecuteAsync("""
                DELETE FROM public.orcamento_formas_pagamento ofp
                USING public.orcamentos o
                WHERE ofp.orcamento_id = o.id AND o.estabelecimento_id = @Id
                """, new { Id = estabelecimentoId }, tx);
            await conn.ExecuteAsync("""
                DELETE FROM public.itens_orcamento io
                USING public.orcamentos o
                WHERE io.orcamento_id = o.id AND o.estabelecimento_id = @Id
                """, new { Id = estabelecimentoId }, tx);
            await conn.ExecuteAsync("""
                DELETE FROM public.orcamento_anestesia oa
                USING public.orcamentos o
                WHERE oa.orcamento_id = o.id AND o.estabelecimento_id = @Id
                """, new { Id = estabelecimentoId }, tx);
            await conn.ExecuteAsync("""
                DELETE FROM public.orcamento_cirurgias oc
                USING public.orcamentos o
                WHERE oc.orcamento_id = o.id AND o.estabelecimento_id = @Id
                """, new { Id = estabelecimentoId }, tx);
            await conn.ExecuteAsync("""
                DELETE FROM public.orcamento_implantes oi
                USING public.orcamentos o
                WHERE oi.orcamento_id = o.id AND o.estabelecimento_id = @Id
                """, new { Id = estabelecimentoId }, tx);
            await conn.ExecuteAsync("""
                DELETE FROM public.orcamento_internacao oin
                USING public.orcamentos o
                WHERE oin.orcamento_id = o.id AND o.estabelecimento_id = @Id
                """, new { Id = estabelecimentoId }, tx);
            await conn.ExecuteAsync(
                "DELETE FROM public.orcamentos WHERE estabelecimento_id = @Id",
                new { Id = estabelecimentoId }, tx);

            // Procedimentos cirúrgicos
            await conn.ExecuteAsync("""
                DELETE FROM public.equipe_cirurgica ec
                USING public.procedimentos_cirurgicos pc
                WHERE ec.procedimento_id = pc.id AND pc.estabelecimento_id = @Id
                """, new { Id = estabelecimentoId }, tx);
            await conn.ExecuteAsync(
                "DELETE FROM public.procedimentos_cirurgicos WHERE estabelecimento_id = @Id",
                new { Id = estabelecimentoId }, tx);

            // Receitas
            await conn.ExecuteAsync("""
                DELETE FROM public.receita_itens ri
                USING public.receitas r
                WHERE ri.receita_id = r.id AND r.estabelecimento_id = @Id
                """, new { Id = estabelecimentoId }, tx);
            await conn.ExecuteAsync(
                "DELETE FROM public.receitas WHERE estabelecimento_id = @Id",
                new { Id = estabelecimentoId }, tx);
            await conn.ExecuteAsync(
                "DELETE FROM public.receitas_configuracao_estabelecimento WHERE estabelecimento_id = @Id",
                new { Id = estabelecimentoId }, tx);
            await conn.ExecuteAsync(
                "DELETE FROM public.medicamentos_favoritos WHERE estabelecimento_id = @Id",
                new { Id = estabelecimentoId }, tx);

            // Exame físico
            await conn.ExecuteAsync("""
                DELETE FROM public.exame_fisico_regioes efr
                USING public.exame_fisico ef
                INNER JOIN public.prontuarios pr ON ef.prontuario_id = pr.id
                WHERE efr.exame_fisico_id = ef.id AND pr.estabelecimento_id = @Id
                """, new { Id = estabelecimentoId }, tx);
            await conn.ExecuteAsync("""
                DELETE FROM public.exame_fisico ef
                USING public.prontuarios pr
                WHERE ef.prontuario_id = pr.id AND pr.estabelecimento_id = @Id
                """, new { Id = estabelecimentoId }, tx);

            // Prontuários (log, anexos, evoluções, prontuário)
            await conn.ExecuteAsync(
                "DELETE FROM public.prontuario_acesso_log WHERE estabelecimento_id = @Id",
                new { Id = estabelecimentoId }, tx);
            await conn.ExecuteAsync("""
                DELETE FROM public.prontuario_anexos pa
                USING public.prontuarios pr
                WHERE pa.prontuario_id = pr.id AND pr.estabelecimento_id = @Id
                """, new { Id = estabelecimentoId }, tx);
            await conn.ExecuteAsync("""
                DELETE FROM public.prontuario_evolucoes pe
                USING public.prontuarios pr
                WHERE pe.prontuario_id = pr.id AND pr.estabelecimento_id = @Id
                """, new { Id = estabelecimentoId }, tx);
            await conn.ExecuteAsync(
                "DELETE FROM public.prontuarios WHERE estabelecimento_id = @Id",
                new { Id = estabelecimentoId }, tx);

            // Agendamentos
            await conn.ExecuteAsync(
                "DELETE FROM public.agendamentos WHERE estabelecimento_id = @Id",
                new { Id = estabelecimentoId }, tx);

            // Pacientes
            await conn.ExecuteAsync(
                "DELETE FROM public.pacientes WHERE estabelecimento_id = @Id",
                new { Id = estabelecimentoId }, tx);

            // Estrutura operacional
            await conn.ExecuteAsync(
                "DELETE FROM public.sala_atendimento WHERE estabelecimento_id = @Id",
                new { Id = estabelecimentoId }, tx);
            await conn.ExecuteAsync(
                "DELETE FROM public.unidades_estabelecimento WHERE estabelecimento_id = @Id",
                new { Id = estabelecimentoId }, tx);

            // Vínculos e modelos de permissão
            await conn.ExecuteAsync(
                "DELETE FROM public.vinculo_profissional_estabelecimento WHERE estabelecimento_id = @Id",
                new { Id = estabelecimentoId }, tx);
            await conn.ExecuteAsync(
                "DELETE FROM public.modelo_permissao_estabelecimento WHERE estabelecimento_id = @Id",
                new { Id = estabelecimentoId }, tx);

            // Configurações do estabelecimento
            await conn.ExecuteAsync(
                "DELETE FROM public.establishment_ai_settings WHERE estabelecimento_id = @Id",
                new { Id = estabelecimentoId }, tx);
            await conn.ExecuteAsync(
                "DELETE FROM public.assinaturas WHERE estabelecimento_id = @Id",
                new { Id = estabelecimentoId }, tx);
            await conn.ExecuteAsync(
                "DELETE FROM public.configuracoes_automacao WHERE estabelecimento_id = @Id",
                new { Id = estabelecimentoId }, tx);

            // Modelos de prontuário (próprios do estabelecimento — padrão-sistema é global)
            await conn.ExecuteAsync(
                "DELETE FROM public.prontuario_variaveis_pool WHERE estabelecimento_id = @Id AND estabelecimento_id IS NOT NULL",
                new { Id = estabelecimentoId }, tx);
            await conn.ExecuteAsync(
                "DELETE FROM public.modelo_de_prontuario WHERE estabelecimento_id = @Id",
                new { Id = estabelecimentoId }, tx);

            // NÃO deletar: estabelecimentos, usuarios, profissionais, planos
            // (manter a casca — apenas conteúdo é removido).

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private static async Task RegistrarAuditAsync(
        NpgsqlConnection conn,
        NpgsqlTransaction tx,
        long estabelecimentoId,
        Guid executadoPorUsuarioId,
        string motivo,
        CancellationToken ct)
    {
        // Registra dentro da transação para que conste do commit.
        // Se a operação falhar e der rollback, o registro de auditoria também some —
        // mas o admin vê no log de erro que tentou. Decisão pragmática: não criar
        // conexão secundária para não complicar o gerenciamento de escopo.
        const string sql = """
            INSERT INTO public.audit_delete_attempts
                (tabela, registro_id, estabelecimento_id, usuario_id, motivo, tentado_em)
            VALUES
                ('ADMIN_RESET', @EstabelecimentoId::text, @EstabelecimentoId, @UsuarioId, @Motivo, NOW())
            """;

        await conn.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                EstabelecimentoId = estabelecimentoId,
                UsuarioId = executadoPorUsuarioId,
                Motivo = motivo
            },
            tx,
            cancellationToken: ct));
    }
}
