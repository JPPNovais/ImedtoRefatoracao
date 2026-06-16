namespace Imedto.Backend.Domain.Migracao;

/// <summary>
/// Trilha de transições de status de um <see cref="MigracaoJob"/> (forward-only, sem PII).
///
/// Cada linha registra uma mudança de status. Quando <see cref="UsuarioId"/> é null,
/// a transição foi feita pelo sistema/job recorrente; quando preenchido, foi um admin.
///
/// Não é aggregate root — pertence ao job e é criado pelo handler de transição.
/// Multi-tenant: <see cref="EstabelecimentoId"/> herdado do job.
/// (addendum 003 — briefing 2026-06-15_004)
/// </summary>
public class MigracaoJobEvento
{
    public virtual long Id { get; protected set; }

    public virtual long MigracaoJobId { get; protected set; }

    /// <summary>Herdado do job para viabilizar queries multi-tenant direto nesta tabela.</summary>
    public virtual long EstabelecimentoId { get; protected set; }

    /// <summary>Null apenas no evento de criação do job (status anterior inexistente).</summary>
    public virtual string? StatusAnterior { get; protected set; }

    public virtual string StatusNovo { get; protected set; } = string.Empty;

    /// <summary>Null = sistema/job recorrente; preenchido = admin que fez a ação.</summary>
    public virtual Guid? UsuarioId { get; protected set; }

    public virtual DateTime CriadoEm { get; protected set; }

    protected MigracaoJobEvento() { }

    /// <summary>
    /// Cria um evento de transição de status para um job de migração.
    /// </summary>
    /// <param name="migracaoJobId">ID do job.</param>
    /// <param name="estabelecimentoId">Tenant do job (herdado).</param>
    /// <param name="statusAnterior">Status imediatamente anterior. Null no evento de criação.</param>
    /// <param name="statusNovo">Novo status após a transição.</param>
    /// <param name="usuarioId">Admin que disparou a ação; null se automático.</param>
    public static MigracaoJobEvento Criar(
        long migracaoJobId,
        long estabelecimentoId,
        string? statusAnterior,
        string statusNovo,
        Guid? usuarioId = null)
    {
        if (migracaoJobId <= 0)
            throw new ArgumentException("MigracaoJobId é obrigatório.", nameof(migracaoJobId));
        if (estabelecimentoId <= 0)
            throw new ArgumentException("EstabelecimentoId é obrigatório.", nameof(estabelecimentoId));
        if (string.IsNullOrWhiteSpace(statusNovo))
            throw new ArgumentException("StatusNovo é obrigatório.", nameof(statusNovo));

        return new MigracaoJobEvento
        {
            MigracaoJobId    = migracaoJobId,
            EstabelecimentoId = estabelecimentoId,
            StatusAnterior   = statusAnterior,
            StatusNovo       = statusNovo.Trim(),
            UsuarioId        = usuarioId,
            CriadoEm        = DateTime.UtcNow
        };
    }
}
