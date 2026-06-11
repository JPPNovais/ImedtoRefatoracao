using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Financeiro;

/// <summary>
/// Aggregate raiz do caixa diário por estabelecimento + data.
/// Invariante: apenas 1 caixa por (estabelecimento_id, data) — UNIQUE no banco.
/// O caixa não materializa valores (resumo lido on-the-fly de Lancamento).
/// RBAC: abrir/fechar exige financeiro.fechar; reabrir exige ser Dono.
/// Multi-tenant: todo acesso filtrado por estabelecimento_id (falha-fechada).
/// </summary>
public class CaixaDiario : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual DateOnly Data { get; protected set; }
    public virtual StatusCaixaDiario Status { get; protected set; }

    public virtual Guid AbertoPorUsuarioId { get; protected set; }
    public virtual DateTime AbertoEm { get; protected set; }

    public virtual Guid? FechadoPorUsuarioId { get; protected set; }
    public virtual DateTime? FechadoEm { get; protected set; }
    public virtual string? Observacao { get; protected set; }

    /// <summary>Último reabrir (R10.1 — mínimo histórico; não versiona ciclo inteiro).</summary>
    public virtual Guid? ReabertoPorUsuarioId { get; protected set; }
    public virtual DateTime? ReabertoEm { get; protected set; }

    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    protected CaixaDiario() { }

    /// <summary>
    /// Abre o caixa do dia. Um único caixa por (estabelecimento_id, data) — o banco
    /// garante via UNIQUE; o handler faz verificação prévia (R7).
    /// </summary>
    public static CaixaDiario Abrir(long estabelecimentoId, DateOnly data, Guid abertoPorUsuarioId)
    {
        if (estabelecimentoId <= 0) throw new BusinessException("Estabelecimento é obrigatório.");
        if (abertoPorUsuarioId == Guid.Empty) throw new BusinessException("Usuário é obrigatório.");

        var agora = DateTime.UtcNow;
        return new CaixaDiario
        {
            EstabelecimentoId = estabelecimentoId,
            Data = data,
            Status = StatusCaixaDiario.Aberto,
            AbertoPorUsuarioId = abertoPorUsuarioId,
            AbertoEm = agora,
            CriadoEm = agora
        };
    }

    /// <summary>
    /// Fecha o caixa (R9). Lança BusinessException se já fechado (CA166).
    /// </summary>
    public void Fechar(Guid fechadoPorUsuarioId, string? observacao)
    {
        if (fechadoPorUsuarioId == Guid.Empty) throw new BusinessException("Usuário é obrigatório.");
        if (Status == StatusCaixaDiario.Fechado)
            throw new BusinessException("Caixa já está fechado.");

        Status = StatusCaixaDiario.Fechado;
        FechadoPorUsuarioId = fechadoPorUsuarioId;
        FechadoEm = DateTime.UtcNow;
        Observacao = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim();
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Reabre o caixa. Apenas para o Dono — verificação de papel no handler (R10/CA167).
    /// Registra quem reabriu/quando (R10.1).
    /// </summary>
    public void Reabrir(Guid reabertoPorUsuarioId)
    {
        if (reabertoPorUsuarioId == Guid.Empty) throw new BusinessException("Usuário é obrigatório.");
        if (Status == StatusCaixaDiario.Aberto)
            throw new BusinessException("Caixa já está aberto.");

        Status = StatusCaixaDiario.Aberto;
        ReabertoPorUsuarioId = reabertoPorUsuarioId;
        ReabertoEm = DateTime.UtcNow;
        AtualizadoEm = DateTime.UtcNow;
    }
}
