using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Cirurgias;

/// <summary>
/// Entity child do aggregate <see cref="ProcedimentoCirurgico"/>. Não é Aggregate Root —
/// só é alterada via métodos do procedimento (<c>AdicionarMembroEquipe</c>/<c>RemoverMembroEquipe</c>).
/// Unique (procedimento_id, profissional_usuario_id, papel): a mesma pessoa pode atuar em
/// papéis diferentes (ex.: cirurgião e instrumentador), mas não duas vezes no mesmo papel.
/// </summary>
public class MembroEquipeCirurgica : Entity
{
    public virtual long ProcedimentoId { get; protected set; }
    public virtual Guid ProfissionalUsuarioId { get; protected set; }
    public virtual PapelCirurgia Papel { get; protected set; }
    public virtual int Ordem { get; protected set; }

    protected MembroEquipeCirurgica() { }

    internal static MembroEquipeCirurgica Criar(
        long procedimentoId,
        Guid profissionalUsuarioId,
        PapelCirurgia papel,
        int ordem)
    {
        if (profissionalUsuarioId == Guid.Empty)
            throw new BusinessException("Profissional do membro da equipe é obrigatório.");
        if (ordem < 0)
            throw new BusinessException("Ordem do membro da equipe não pode ser negativa.");

        return new MembroEquipeCirurgica
        {
            ProcedimentoId = procedimentoId,
            ProfissionalUsuarioId = profissionalUsuarioId,
            Papel = papel,
            Ordem = ordem
        };
    }
}
