using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Orcamentos.Catalogos;

public class OrcamentoPacoteTeamRole : Entity
{
    public virtual long PacoteId { get; protected set; }
    public virtual long TeamRoleId { get; protected set; }

    protected OrcamentoPacoteTeamRole() { }

    internal static OrcamentoPacoteTeamRole Criar(long teamRoleId)
    {
        if (teamRoleId <= 0) throw new BusinessException("Papel de equipe é obrigatório.");
        return new OrcamentoPacoteTeamRole { TeamRoleId = teamRoleId };
    }
}
