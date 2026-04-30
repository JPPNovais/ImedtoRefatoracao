using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.Infrastructure.Database;

namespace Imedto.Backend.Infrastructure.Admin;

/// <summary>
/// Implementação do reset administrativo de estabelecimento.
///
/// Usa <see cref="NpgsqlConnection"/> direta (não passa pelo EF Core) para bypassar o
/// <c>SoftDeleteInterceptor</c>. A ordem de DELETE respeita a topologia de FKs do schema
/// (filhos antes de pais). Toda a operação roda em uma única transação Postgres.
///
/// Reseed pós-delete: se <see cref="ResetModulos.Configuracoes"/> = true, recria os modelos
/// de permissão padrão e os seeds financeiros via EF Core (após o commit da transação de delete).
///
/// AUDITORIA (LGPD): registrada dentro da transação em <c>audit_delete_attempts</c>.
/// </summary>
public class AdminResetService : IAdminResetService
{
    private readonly AppReadConnectionString _connStr;
    private readonly AppDbContext _db;

    public AdminResetService(AppReadConnectionString connStr, AppDbContext db)
    {
        _connStr = connStr;
        _db = db;
    }

    public async Task ResetEstabelecimentoAsync(
        long estabelecimentoId,
        ResetModulos modulos,
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
            await RegistrarAuditAsync(conn, tx, estabelecimentoId, executadoPorUsuarioId, motivoCompleto, ct);

            // ── Dependências cruzadas: automation_events referencia agendamentos e pacientes ──
            // Apaga antes de processar os módulos proprietários para evitar violação de FK.
            if (modulos.Automacoes || modulos.Agenda || modulos.Pacientes)
            {
                await conn.ExecuteAsync("""
                    DELETE FROM public.automation_events ae
                    USING public.automation_rules ar
                    WHERE ae.regra_id = ar.id AND ar.estabelecimento_id = @Id
                    """, new { Id = estabelecimentoId }, tx);
            }

            // ── Notificações ──────────────────────────────────────────────────────────────────
            if (modulos.Notificacoes)
            {
                await conn.ExecuteAsync(
                    "DELETE FROM public.notificacoes WHERE estabelecimento_id = @Id",
                    new { Id = estabelecimentoId }, tx);
            }

            // ── Automações ────────────────────────────────────────────────────────────────────
            if (modulos.Automacoes)
            {
                // automation_events já deletado acima (dependência cruzada)
                await conn.ExecuteAsync(
                    "DELETE FROM public.automation_rules WHERE estabelecimento_id = @Id",
                    new { Id = estabelecimentoId }, tx);
            }

            // ── Agendamentos ──────────────────────────────────────────────────────────────────
            if (modulos.Agenda)
            {
                await conn.ExecuteAsync(
                    "DELETE FROM public.agendamentos WHERE estabelecimento_id = @Id",
                    new { Id = estabelecimentoId }, tx);
            }

            // ── Exame físico (filho de prontuário — apagar antes de prontuário e de pacientes) ─
            if (modulos.ExameFisico || modulos.Prontuario || modulos.Pacientes)
            {
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
            }

            // ── Receitas ──────────────────────────────────────────────────────────────────────
            if (modulos.Receitas || modulos.Pacientes)
            {
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
            }

            // ── Cirurgias ─────────────────────────────────────────────────────────────────────
            if (modulos.Cirurgias || modulos.Pacientes)
            {
                await conn.ExecuteAsync("""
                    DELETE FROM public.equipe_cirurgica ec
                    USING public.procedimentos_cirurgicos pc
                    WHERE ec.procedimento_id = pc.id AND pc.estabelecimento_id = @Id
                    """, new { Id = estabelecimentoId }, tx);
                await conn.ExecuteAsync(
                    "DELETE FROM public.procedimentos_cirurgicos WHERE estabelecimento_id = @Id",
                    new { Id = estabelecimentoId }, tx);
            }

            // ── Prontuários (log, anexos, evoluções, prontuário) ──────────────────────────────
            if (modulos.Prontuario || modulos.Pacientes)
            {
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
            }

            // ── Pacientes ─────────────────────────────────────────────────────────────────────
            if (modulos.Pacientes)
            {
                await conn.ExecuteAsync(
                    "DELETE FROM public.pacientes WHERE estabelecimento_id = @Id",
                    new { Id = estabelecimentoId }, tx);
            }

            // ── Orçamentos ────────────────────────────────────────────────────────────────────
            if (modulos.Orcamentos)
            {
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
            }

            // ── Financeiro ────────────────────────────────────────────────────────────────────
            if (modulos.Financeiro)
            {
                await conn.ExecuteAsync(
                    "DELETE FROM public.lancamentos WHERE estabelecimento_id = @Id",
                    new { Id = estabelecimentoId }, tx);
                await conn.ExecuteAsync(
                    "DELETE FROM public.categorias_financeiras WHERE estabelecimento_id = @Id",
                    new { Id = estabelecimentoId }, tx);
                await conn.ExecuteAsync(
                    "DELETE FROM public.formas_pagamento WHERE estabelecimento_id = @Id",
                    new { Id = estabelecimentoId }, tx);
            }

            // ── Inventário ────────────────────────────────────────────────────────────────────
            if (modulos.Inventario)
            {
                await conn.ExecuteAsync(
                    "DELETE FROM public.movimentacoes_estoque WHERE estabelecimento_id = @Id",
                    new { Id = estabelecimentoId }, tx);
                await conn.ExecuteAsync(
                    "DELETE FROM public.itens_inventario WHERE estabelecimento_id = @Id",
                    new { Id = estabelecimentoId }, tx);
            }

            // ── Vínculos ──────────────────────────────────────────────────────────────────────
            if (modulos.Vinculos)
            {
                await conn.ExecuteAsync(
                    "DELETE FROM public.solicitacoes_vinculo WHERE estabelecimento_id = @Id",
                    new { Id = estabelecimentoId }, tx);
                await conn.ExecuteAsync(
                    "DELETE FROM public.vinculo_profissional_estabelecimento WHERE estabelecimento_id = @Id",
                    new { Id = estabelecimentoId }, tx);
            }

            // ── Configurações ─────────────────────────────────────────────────────────────────
            if (modulos.Configuracoes)
            {
                await conn.ExecuteAsync(
                    "DELETE FROM public.modelo_permissao_estabelecimento WHERE estabelecimento_id = @Id",
                    new { Id = estabelecimentoId }, tx);
                await conn.ExecuteAsync(
                    "DELETE FROM public.establishment_ai_settings WHERE estabelecimento_id = @Id",
                    new { Id = estabelecimentoId }, tx);
                await conn.ExecuteAsync(
                    "DELETE FROM public.assinaturas WHERE estabelecimento_id = @Id",
                    new { Id = estabelecimentoId }, tx);
                await conn.ExecuteAsync(
                    "DELETE FROM public.configuracoes_automacao WHERE estabelecimento_id = @Id",
                    new { Id = estabelecimentoId }, tx);
                await conn.ExecuteAsync(
                    "DELETE FROM public.prontuario_variaveis_pool WHERE estabelecimento_id = @Id AND estabelecimento_id IS NOT NULL",
                    new { Id = estabelecimentoId }, tx);
                await conn.ExecuteAsync(
                    "DELETE FROM public.modelo_de_prontuario WHERE estabelecimento_id = @Id",
                    new { Id = estabelecimentoId }, tx);
                await conn.ExecuteAsync(
                    "DELETE FROM public.sala_atendimento WHERE estabelecimento_id = @Id",
                    new { Id = estabelecimentoId }, tx);
                await conn.ExecuteAsync(
                    "DELETE FROM public.unidades_estabelecimento WHERE estabelecimento_id = @Id",
                    new { Id = estabelecimentoId }, tx);
            }

            // NÃO deletar: estabelecimentos, usuarios, profissionais, planos.

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        // ── Reseed pós-delete (fora da transação de delete, via EF Core) ─────────────────────
        // Configuracoes apagou os seeds — recriar os padrões para o estabelecimento não ficar
        // inutilizável após o reset.
        if (modulos.Configuracoes)
            await ReseedConfiguracoes(estabelecimentoId, ct);

        // Financeiro apagou categorias e formas de pagamento — recriar seeds mínimos.
        if (modulos.Financeiro)
            await ReseedFinanceiro(estabelecimentoId, ct);
    }

    // ── Reseed helpers ────────────────────────────────────────────────────────────────────────

    private async Task ReseedConfiguracoes(long estabelecimentoId, CancellationToken ct)
    {
        foreach (var modelo in ModeloPermissaoEstabelecimento.CriarPadroes(estabelecimentoId))
            _db.ModelosPermissao.Add(modelo);

        await _db.SaveChangesAsync(ct);
    }

    private async Task ReseedFinanceiro(long estabelecimentoId, CancellationToken ct)
    {
        foreach (var (nome, tipo) in SeedsFinanceiro.Categorias)
            _db.CategoriasFinanceiras.Add(CategoriaFinanceira.CriarPadrao(estabelecimentoId, nome, tipo));

        foreach (var nome in SeedsFinanceiro.FormasPagamento)
            _db.FormasPagamento.Add(FormaPagamento.CriarPadrao(estabelecimentoId, nome));

        await _db.SaveChangesAsync(ct);
    }

    // ── Auditoria ─────────────────────────────────────────────────────────────────────────────

    private static async Task RegistrarAuditAsync(
        NpgsqlConnection conn,
        NpgsqlTransaction tx,
        long estabelecimentoId,
        Guid executadoPorUsuarioId,
        string motivo,
        CancellationToken ct)
    {
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
