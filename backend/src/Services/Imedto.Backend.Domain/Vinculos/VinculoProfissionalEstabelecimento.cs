using Imedto.Backend.Domain.Vinculos.Events;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Vinculos;

/// <summary>
/// Vínculo de um profissional (usuário) a um estabelecimento, com um modelo de permissão aplicado.
/// Fluxo: Convidado → Ativo (profissional aceita) → Inativo (dono ou profissional encerra).
/// </summary>
public class VinculoProfissionalEstabelecimento : Entity
{
    public virtual Guid ProfissionalUsuarioId { get; protected set; }
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual long ModeloPermissaoId { get; protected set; }
    public virtual Guid ConvidadoPorUsuarioId { get; protected set; }
    public virtual VinculoStatus Status { get; protected set; }
    public virtual DateTime ConvidadoEm { get; protected set; }
    public virtual DateTime? AceitoEm { get; protected set; }
    public virtual DateTime? InativadoEm { get; protected set; }

    protected VinculoProfissionalEstabelecimento() { }

    public static VinculoProfissionalEstabelecimento Convidar(
        Guid profissionalUsuarioId,
        long estabelecimentoId,
        long modeloPermissaoId,
        Guid convidadoPorUsuarioId)
    {
        if (profissionalUsuarioId == Guid.Empty)
            throw new BusinessException("Profissional é obrigatório.");
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (modeloPermissaoId <= 0)
            throw new BusinessException("Modelo de permissão é obrigatório.");
        if (convidadoPorUsuarioId == Guid.Empty)
            throw new BusinessException("Usuário que convida é obrigatório.");
        if (profissionalUsuarioId == convidadoPorUsuarioId)
            throw new BusinessException("Você não pode convidar a si mesmo.");

        return new VinculoProfissionalEstabelecimento
        {
            ProfissionalUsuarioId = profissionalUsuarioId,
            EstabelecimentoId = estabelecimentoId,
            ModeloPermissaoId = modeloPermissaoId,
            ConvidadoPorUsuarioId = convidadoPorUsuarioId,
            Status = VinculoStatus.Convidado,
            ConvidadoEm = DateTime.UtcNow
        };
    }

    /// <summary>Anexa <see cref="ProfissionalConvidadoEvent"/> — chamar após persistir o aggregate.</summary>
    public virtual void MarcarComoConvidado()
    {
        if (Id == 0)
            throw new InvalidOperationException("Vínculo ainda não foi persistido — Id é 0.");

        AddDomainEvent(new ProfissionalConvidadoEvent(
            Id, ProfissionalUsuarioId, EstabelecimentoId, ConvidadoPorUsuarioId));
    }

    public virtual void Aceitar()
    {
        if (Status != VinculoStatus.Convidado)
            throw new BusinessException("Apenas convites pendentes podem ser aceitos.");

        Status = VinculoStatus.Ativo;
        AceitoEm = DateTime.UtcNow;

        AddDomainEvent(new VinculoAceitoEvent(Id, ProfissionalUsuarioId, EstabelecimentoId));
    }

    public virtual void Inativar()
    {
        if (Status == VinculoStatus.Inativo)
            throw new BusinessException("Vínculo já está inativo.");

        Status = VinculoStatus.Inativo;
        InativadoEm = DateTime.UtcNow;
    }

    public virtual void AtualizarModeloPermissao(long novoModeloPermissaoId)
    {
        if (Status == VinculoStatus.Inativo)
            throw new BusinessException("Não é possível alterar permissões de vínculo inativo.");
        if (novoModeloPermissaoId <= 0)
            throw new BusinessException("Modelo de permissão é obrigatório.");

        ModeloPermissaoId = novoModeloPermissaoId;
    }

    /// <summary>
    /// Reativa um vínculo previamente inativado, transformando-o em novo Convite.
    /// O profissional precisará aceitar de novo, mas o histórico (datas anteriores) é preservado
    /// na linha — apenas <see cref="AceitoEm"/>/<see cref="InativadoEm"/> são zerados.
    /// </summary>
    public virtual void ReativarComoConvite(long novoModeloPermissaoId, Guid convidadoPorUsuarioId)
    {
        if (Status != VinculoStatus.Inativo)
            throw new BusinessException("Apenas vínculos inativos podem ser reativados.");
        if (novoModeloPermissaoId <= 0)
            throw new BusinessException("Modelo de permissão é obrigatório.");
        if (convidadoPorUsuarioId == Guid.Empty)
            throw new BusinessException("Usuário que convida é obrigatório.");

        Status = VinculoStatus.Convidado;
        ModeloPermissaoId = novoModeloPermissaoId;
        ConvidadoPorUsuarioId = convidadoPorUsuarioId;
        ConvidadoEm = DateTime.UtcNow;
        AceitoEm = null;
        InativadoEm = null;

        AddDomainEvent(new ProfissionalConvidadoEvent(
            Id, ProfissionalUsuarioId, EstabelecimentoId, ConvidadoPorUsuarioId));
    }
}
