using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Assinaturas;

/// <summary>
/// Aggregate root de Assinatura — vínculo 1:1 entre estabelecimento e plano vigente.
/// Não modela histórico de planos — quando o dono troca de plano, sobrescrevemos
/// <see cref="PlanoId"/> e atualizamos <see cref="AtualizadaEm"/>. Histórico fica em audit trail.
///
/// Transições válidas:
/// <list type="bullet">
/// <item>Trial   → Ativa | Suspensa | Cancelada | Expirada</item>
/// <item>Ativa   → Suspensa | Cancelada (renovações ficam em Ativa)</item>
/// <item>Suspensa→ Ativa | Cancelada</item>
/// <item>Cancelada/Expirada → estados terminais (precisam de nova assinatura para sair)</item>
/// </list>
/// </summary>
public class Assinatura : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual long PlanoId { get; protected set; }
    public virtual StatusAssinatura Status { get; protected set; }
    public virtual DateTime IniciadaEm { get; protected set; }
    public virtual DateTime? ExpiraEm { get; protected set; }
    public virtual DateTime? CanceladaEm { get; protected set; }
    public virtual DateTime? RenovadaEm { get; protected set; }
    public virtual DateTime CriadaEm { get; protected set; }
    public virtual DateTime? AtualizadaEm { get; protected set; }

    protected Assinatura() { }

    /// <summary>
    /// Inicia um trial atrelado ao plano de trial. <paramref name="duracaoTrial"/> default é 14 dias
    /// (controlado pelo chamador para permitir promoções pontuais sem mudar o domínio).
    /// </summary>
    public static Assinatura IniciarTrial(long estabelecimentoId, long planoTrialId, TimeSpan duracaoTrial)
    {
        ValidarEstabelecimento(estabelecimentoId);
        ValidarPlano(planoTrialId);
        if (duracaoTrial <= TimeSpan.Zero)
            throw new BusinessException("Duração do trial deve ser positiva.");

        var agora = DateTime.UtcNow;
        return new Assinatura
        {
            EstabelecimentoId = estabelecimentoId,
            PlanoId = planoTrialId,
            Status = StatusAssinatura.Trial,
            IniciadaEm = agora,
            ExpiraEm = agora.Add(duracaoTrial),
            CriadaEm = agora
        };
    }

    /// <summary>
    /// Cria uma assinatura em estado arbitrário (uso administrativo / migração). Para trial, prefira
    /// <see cref="IniciarTrial"/> que aplica a regra de duração padrão.
    /// </summary>
    public static Assinatura Criar(
        long estabelecimentoId,
        long planoId,
        StatusAssinatura status,
        DateTime? expiraEm)
    {
        ValidarEstabelecimento(estabelecimentoId);
        ValidarPlano(planoId);

        var agora = DateTime.UtcNow;
        return new Assinatura
        {
            EstabelecimentoId = estabelecimentoId,
            PlanoId = planoId,
            Status = status,
            IniciadaEm = agora,
            ExpiraEm = expiraEm,
            CriadaEm = agora
        };
    }

    public virtual void Ativar()
    {
        if (Status != StatusAssinatura.Trial)
            throw new BusinessException("Apenas assinaturas em trial podem ser ativadas por este fluxo.");

        Status = StatusAssinatura.Ativa;
        AtualizadaEm = DateTime.UtcNow;
    }

    public virtual void Suspender(string motivo)
    {
        if (Status is not (StatusAssinatura.Trial or StatusAssinatura.Ativa))
            throw new BusinessException("Apenas assinaturas em trial ou ativas podem ser suspensas.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new BusinessException("Motivo da suspensão é obrigatório.");

        Status = StatusAssinatura.Suspensa;
        AtualizadaEm = DateTime.UtcNow;
    }

    public virtual void Cancelar()
    {
        if (Status == StatusAssinatura.Cancelada)
            throw new BusinessException("Assinatura já está cancelada.");
        if (Status == StatusAssinatura.Expirada)
            throw new BusinessException("Assinatura expirada não pode ser cancelada — inicie uma nova.");

        var agora = DateTime.UtcNow;
        Status = StatusAssinatura.Cancelada;
        CanceladaEm = agora;
        AtualizadaEm = agora;
    }

    /// <summary>
    /// Marca o trial como expirado. Idempotente — se já está em Expirada, não faz nada
    /// (job de expiração roda 1x/h, dois ticks consecutivos não devem falhar).
    /// </summary>
    public virtual void Expirar()
    {
        if (Status == StatusAssinatura.Expirada) return;

        if (Status != StatusAssinatura.Trial)
            throw new BusinessException("Apenas assinaturas em trial podem expirar por tempo.");

        Status = StatusAssinatura.Expirada;
        AtualizadaEm = DateTime.UtcNow;
    }

    public virtual void RenovarAte(DateTime novaExpiracao)
    {
        if (Status != StatusAssinatura.Ativa)
            throw new BusinessException("Somente assinaturas ativas podem ser renovadas.");
        if (novaExpiracao <= DateTime.UtcNow)
            throw new BusinessException("Nova data de expiração deve ser futura.");
        if (ExpiraEm.HasValue && novaExpiracao <= ExpiraEm.Value)
            throw new BusinessException("Nova data de expiração deve ser posterior à atual.");

        var agora = DateTime.UtcNow;
        ExpiraEm = novaExpiracao;
        RenovadaEm = agora;
        AtualizadaEm = agora;
    }

    public virtual void MudarPlano(long novoPlanoId)
    {
        if (Status != StatusAssinatura.Ativa)
            throw new BusinessException("Apenas assinaturas ativas podem trocar de plano.");
        ValidarPlano(novoPlanoId);
        if (novoPlanoId == PlanoId)
            throw new BusinessException("Plano informado é o mesmo plano vigente.");

        PlanoId = novoPlanoId;
        AtualizadaEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Helper para o serviço de feature gating: trial dentro do prazo OU ativa.
    /// Não consulta banco — usa apenas o estado do aggregate.
    /// </summary>
    public bool EstaAtiva(DateTime referencia)
    {
        return Status switch
        {
            StatusAssinatura.Ativa => true,
            StatusAssinatura.Trial => !ExpiraEm.HasValue || ExpiraEm.Value > referencia,
            _ => false
        };
    }

    private static void ValidarEstabelecimento(long estabelecimentoId)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento inválido.");
    }

    private static void ValidarPlano(long planoId)
    {
        if (planoId <= 0)
            throw new BusinessException("Plano inválido.");
    }
}
