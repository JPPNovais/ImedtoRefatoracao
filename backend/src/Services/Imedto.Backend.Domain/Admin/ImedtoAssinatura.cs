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
/// Estado efetivo é DERIVADO (R3 do briefing 2026-06-11_003):
///   BLOQUEADO  : não há vigência, OU expira_em no passado, OU suspensa_em preenchido.
///   VITALÍCIO  : expira_em IS NULL e suspensa_em IS NULL.
///   TEMPORÁRIO : expira_em no futuro e suspensa_em IS NULL.
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

    /// <summary>
    /// Expiração da vigência. NULL = vitalício. Preenchido = liberado até essa data (trial/temporário).
    /// Estado derivado: se no passado → BLOQUEADO; se no futuro → TEMPORÁRIO.
    /// </summary>
    public virtual DateTimeOffset? ExpiraEm { get; protected set; }

    /// <summary>
    /// Suspensão manual pelo admin. NULL = não suspenso. Preenchido = BLOQUEADO independente de ExpiraEm.
    /// Suspensão é reversível via <see cref="Reativar"/> (sem abrir nova vigência).
    /// </summary>
    public virtual DateTimeOffset? SuspensaEm { get; protected set; }

    /// <summary>
    /// Origem desta vigência. 'admin_manual' (default) ou 'self_service' (futuro gateway).
    /// Coluna dormente — nenhuma regra de negócio consome nesta entrega.
    /// </summary>
    public virtual string Origem { get; protected set; } = "admin_manual";

    /// <summary>
    /// ID externo do gateway de pagamento (futuro). Dormente até o gateway existir.
    /// </summary>
    public virtual string? ReferenciaExterna { get; protected set; }

    /// <summary>
    /// Status de cobrança no gateway (futuro). Default 'nao_aplicavel'.
    /// Valores esperados: nao_aplicavel | pendente | pago | inadimplente.
    /// Coluna dormente nesta entrega.
    /// </summary>
    public virtual string StatusCobranca { get; protected set; } = "nao_aplicavel";

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
        Guid? criadaPorAdminId,
        DateTimeOffset? expiraEm = null)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("EstabelecimentoId inválido.");
        if (planoId == Guid.Empty)
            throw new BusinessException("PlanoId inválido.");
        if (gratuita && string.IsNullOrWhiteSpace(motivo))
            throw new BusinessException("Motivo é obrigatório para concessão de gratuidade (mínimo 10 caracteres).");
        if (gratuita && motivo!.Trim().Length < 10)
            throw new BusinessException("Motivo é obrigatório para concessão de gratuidade (mínimo 10 caracteres).");
        if (expiraEm.HasValue && expiraEm.Value <= DateTimeOffset.UtcNow)
            throw new BusinessException("A data de expiração deve ser no futuro.");

        return new ImedtoAssinatura
        {
            Id = Guid.NewGuid(),
            EstabelecimentoId = estabelecimentoId,
            PlanoId = planoId,
            IniciadaEm = DateTimeOffset.UtcNow,
            FimEm = null,
            ExpiraEm = expiraEm,
            SuspensaEm = null,
            Origem = "admin_manual",
            ReferenciaExterna = null,
            StatusCobranca = "nao_aplicavel",
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

    /// <summary>
    /// Suspende manualmente esta vigência. Preenche SuspensaEm; não abre nova vigência.
    /// Estado derivado vira BLOQUEADO até <see cref="Reativar"/> ser chamado.
    /// </summary>
    public virtual void Suspender()
    {
        if (FimEm is not null)
            throw new BusinessException("Não é possível suspender uma vigência já encerrada.");
        if (SuspensaEm is not null)
            throw new BusinessException("Esta assinatura já está suspensa.");
        SuspensaEm = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Remove a suspensão manual desta vigência. SuspensaEm volta a NULL.
    /// O estado derivado retorna ao que <see cref="ExpiraEm"/> ditar (vitalício ou temporário).
    /// </summary>
    public virtual void Reativar()
    {
        if (FimEm is not null)
            throw new BusinessException("Não é possível reativar uma vigência já encerrada.");
        if (SuspensaEm is null)
            throw new BusinessException("Esta assinatura não está suspensa.");
        SuspensaEm = null;
    }

    /// <summary>True se esta vigência ainda não foi encerrada (FimEm IS NULL).</summary>
    public bool EstaVigente() => FimEm is null;

    /// <summary>
    /// Estado derivado R3: assinatura ativa = vigente + não suspensa + não expirada.
    /// Usado pelo AssinaturaService (F3) para enforcement.
    /// </summary>
    public bool EstaAtiva()
    {
        if (!EstaVigente()) return false;
        if (SuspensaEm is not null) return false;
        if (ExpiraEm.HasValue && ExpiraEm.Value <= DateTimeOffset.UtcNow) return false;
        return true;
    }

    /// <summary>Estado legível derivado para exibição no admin.</summary>
    public EstadoAssinatura ObterEstado()
    {
        if (!EstaVigente()) return EstadoAssinatura.Encerrada;
        if (SuspensaEm is not null) return EstadoAssinatura.Suspensa;
        if (ExpiraEm.HasValue && ExpiraEm.Value <= DateTimeOffset.UtcNow) return EstadoAssinatura.Expirada;
        if (ExpiraEm is null) return EstadoAssinatura.Vitalicia;
        return EstadoAssinatura.Temporaria;
    }
}

/// <summary>Estado derivado da vigência (para exibição e enforcement).</summary>
public enum EstadoAssinatura
{
    /// <summary>Vigente, sem suspensão, sem expiração — LIBERADO.</summary>
    Vitalicia,
    /// <summary>Vigente, sem suspensão, expira_em no futuro — LIBERADO até a data.</summary>
    Temporaria,
    /// <summary>Suspensa manualmente — BLOQUEADO (reversível via Reativar).</summary>
    Suspensa,
    /// <summary>expira_em no passado — BLOQUEADO.</summary>
    Expirada,
    /// <summary>FimEm preenchido — vigência histórica, não é a vigente.</summary>
    Encerrada
}
