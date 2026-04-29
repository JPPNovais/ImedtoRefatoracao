using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Orcamentos;

/// <summary>
/// Comissão financeira de um profissional dentro do orçamento (não confundir com
/// <c>MembroEquipeCirurgica</c>, que é a equipe operacional). Pode existir mesmo sem
/// procedimento cirúrgico (ex.: comissão de consulta cara).
/// </summary>
public class OrcamentoEquipe : Entity
{
    public virtual long OrcamentoId { get; protected set; }
    public virtual Guid ProfissionalUsuarioId { get; protected set; }
    public virtual string Papel { get; protected set; } = string.Empty;
    public virtual decimal Valor { get; protected set; }
    public virtual int Ordem { get; protected set; }

    protected OrcamentoEquipe() { }

    internal static OrcamentoEquipe Criar(
        long orcamentoId,
        Guid profissionalUsuarioId,
        string papel,
        decimal valor,
        int ordem)
    {
        if (profissionalUsuarioId == Guid.Empty)
            throw new BusinessException("Profissional da equipe é obrigatório.");
        if (string.IsNullOrWhiteSpace(papel))
            throw new BusinessException("Papel do profissional é obrigatório.");
        if (papel.Length > 40)
            throw new BusinessException("Papel não pode ter mais de 40 caracteres.");
        if (valor < 0)
            throw new BusinessException("Valor da comissão não pode ser negativo.");
        if (ordem < 0)
            throw new BusinessException("Ordem não pode ser negativa.");

        return new OrcamentoEquipe
        {
            OrcamentoId = orcamentoId,
            ProfissionalUsuarioId = profissionalUsuarioId,
            Papel = papel.Trim(),
            Valor = Math.Round(valor, 2),
            Ordem = ordem
        };
    }
}
