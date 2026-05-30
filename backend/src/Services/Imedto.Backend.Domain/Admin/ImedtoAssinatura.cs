using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Admin;

/// <summary>
/// Histórico imutável de assinaturas gerenciado pelo admin global. Tabela global.
///
/// Regra fundamental: alterar plano = INSERT nova linha + fechar <see cref="FimEm"/> da anterior
/// em transação. NUNCA UPDATE in-place. O histórico é a fonte de verdade do plano vigente.
///
/// "Plano vigente" = linha com <see cref="FimEm"/> IS NULL para o estabelecimento.
///
/// ATENÇÃO: entidade separada de <c>Imedto.Backend.Domain.Assinaturas.Assinatura</c> que usa
/// IDs bigint e representa o estado atual (1:1). Esta entidade representa o histórico (1:N).
/// Nome da tabela no Postgres: <c>imedto_assinaturas</c>.
/// </summary>
public class ImedtoAssinatura : Entity<Guid>
{
    /// <summary>FK para estabelecimentos.id (bigint).</summary>
    public virtual long EstabelecimentoId { get; protected set; }

    /// <summary>FK para imedto_planos.id (uuid).</summary>
    public virtual Guid PlanoId { get; protected set; }

    /// <summary>Início da vigência desta linha.</summary>
    public virtual DateTimeOffset IniciadaEm { get; protected set; }

    /// <summary>Fim da vigência. NULL = assinatura vigente atual.</summary>
    public virtual DateTimeOffset? FimEm { get; protected set; }

    /// <summary>True quando concessão administrativa de gratuidade.</summary>
    public virtual bool Gratuita { get; protected set; }

    /// <summary>Obrigatório quando <see cref="Gratuita"/> = true.</summary>
    public virtual string? Motivo { get; protected set; }

    public virtual DateTimeOffset CriadaEm { get; protected set; }

    /// <summary>Admin que criou. Null para assinaturas geradas por self-signup (futuro).</summary>
    public virtual Guid? CriadaPorAdminId { get; protected set; }

    protected ImedtoAssinatura() { }

    public static ImedtoAssinatura Criar(
        long estabelecimentoId,
        Guid planoId,
        bool gratuita,
        string? motivo,
        Guid? criadaPorAdminId)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("EstabelecimentoId inválido.");
        if (planoId == Guid.Empty)
            throw new BusinessException("PlanoId inválido.");
        if (gratuita && string.IsNullOrWhiteSpace(motivo))
            throw new BusinessException("Motivo é obrigatório para concessão de gratuidade (mínimo 10 caracteres).");
        if (gratuita && motivo!.Trim().Length < 10)
            throw new BusinessException("Motivo é obrigatório para concessão de gratuidade (mínimo 10 caracteres).");

        return new ImedtoAssinatura
        {
            Id = Guid.NewGuid(),
            EstabelecimentoId = estabelecimentoId,
            PlanoId = planoId,
            IniciadaEm = DateTimeOffset.UtcNow,
            FimEm = null,
            Gratuita = gratuita,
            Motivo = motivo?.Trim(),
            CriadaEm = DateTimeOffset.UtcNow,
            CriadaPorAdminId = criadaPorAdminId
        };
    }

    /// <summary>
    /// Fecha a vigência desta assinatura ao trocar de plano. Chamado em transação junto com
    /// a criação da nova linha.
    /// </summary>
    public virtual void FecharVigencia()
    {
        if (FimEm is not null)
            throw new BusinessException("Esta assinatura já foi encerrada.");
        FimEm = DateTimeOffset.UtcNow;
    }

    public bool EstaVigente() => FimEm is null;
}
