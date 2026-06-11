using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Admin;

/// <summary>
/// Configuração global de trial automático para novos estabelecimentos (R7 do briefing 2026-06-11_003).
///
/// Singleton: existe exatamente uma linha nesta tabela (id fixo <see cref="IdFixo"/>).
/// A criação do estabelecimento lê esta config para decidir se cria trial automático
/// e com qual plano/duração.
///
/// Nome da tabela no Postgres: <c>imedto_config_trial</c>.
/// </summary>
public class ImedtoConfigTrial : Entity<Guid>
{
    /// <summary>UUID fixo do singleton de config. Usado pelo seed e pelo handler de criação de trial.</summary>
    public static readonly Guid IdFixo = new("10000000-0000-0000-0000-000000000001");

    /// <summary>Plano atribuído ao trial automático. FK para imedto_planos.</summary>
    public virtual Guid PlanoTrialId { get; protected set; }

    /// <summary>Duração do trial em dias. Default 14 (espelha o legado).</summary>
    public virtual int DuracaoTrialDias { get; protected set; }

    /// <summary>
    /// Quando true: novo estabelecimento recebe trial automático.
    /// Quando false: novo estabelecimento nasce sem assinatura vigente (estado BLOQUEADO).
    /// </summary>
    public virtual bool TrialHabilitado { get; protected set; }

    public virtual DateTimeOffset AtualizadoEm { get; protected set; }

    /// <summary>Admin que fez a última atualização. Null se nunca atualizado manualmente.</summary>
    public virtual Guid? AtualizadoPorUsuarioId { get; protected set; }

    protected ImedtoConfigTrial() { }

    /// <summary>Cria o singleton de config com valores padrão (usado pelo seed).</summary>
    public static ImedtoConfigTrial CriarPadrao(Guid planoTrialId)
    {
        if (planoTrialId == Guid.Empty)
            throw new BusinessException("PlanoTrialId inválido.");

        return new ImedtoConfigTrial
        {
            Id = IdFixo,
            PlanoTrialId = planoTrialId,
            DuracaoTrialDias = 14,
            TrialHabilitado = true,
            AtualizadoEm = DateTimeOffset.UtcNow,
            AtualizadoPorUsuarioId = null
        };
    }

    /// <summary>Atualiza os valores da config.</summary>
    public virtual void Atualizar(Guid planoTrialId, int duracaoTrialDias, bool trialHabilitado, Guid? adminId)
    {
        if (planoTrialId == Guid.Empty)
            throw new BusinessException("PlanoTrialId inválido.");
        if (duracaoTrialDias <= 0)
            throw new BusinessException("Duração do trial deve ser maior que zero.");

        PlanoTrialId = planoTrialId;
        DuracaoTrialDias = duracaoTrialDias;
        TrialHabilitado = trialHabilitado;
        AtualizadoEm = DateTimeOffset.UtcNow;
        AtualizadoPorUsuarioId = adminId;
    }
}
