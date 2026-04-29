using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Receitas;

/// <summary>
/// Atalho de medicamento frequentemente prescrito por um profissional em um
/// estabelecimento. Único por (profissional, estabelecimento, medicamento, posologia).
///
/// Atualiza <see cref="UsoCount"/>/<see cref="UltimoUso"/> a cada uso para alimentar
/// o ranking ("mais usados primeiro"). A criação inicial vem com count=1 já contado.
/// </summary>
public class MedicamentoFavorito : Entity
{
    public virtual Guid ProfissionalUsuarioId { get; protected set; }
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual string Medicamento { get; protected set; } = string.Empty;
    public virtual string? Posologia { get; protected set; }
    public virtual ViaAdministracao? ViaAdministracao { get; protected set; }
    public virtual int UsoCount { get; protected set; }
    public virtual DateTime? UltimoUso { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }

    protected MedicamentoFavorito() { }

    /// <summary>
    /// Cria um favorito já com a primeira contagem de uso (count=1, ultimoUso=now).
    /// O caller (repositório) decide se persiste novo registro ou apenas chama
    /// <see cref="IncrementarUso"/> num registro existente.
    /// </summary>
    public static MedicamentoFavorito CriarOuIncrementar(
        Guid profissionalUsuarioId,
        long estabelecimentoId,
        string medicamento,
        string? posologia,
        ViaAdministracao? via)
    {
        if (profissionalUsuarioId == Guid.Empty)
            throw new BusinessException("Profissional é obrigatório.");
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (string.IsNullOrWhiteSpace(medicamento))
            throw new BusinessException("Medicamento é obrigatório.");
        if (medicamento.Length > 200)
            throw new BusinessException("Medicamento excede 200 caracteres.");
        if (posologia is not null && posologia.Length > 500)
            throw new BusinessException("Posologia excede 500 caracteres.");

        var agora = DateTime.UtcNow;
        return new MedicamentoFavorito
        {
            ProfissionalUsuarioId = profissionalUsuarioId,
            EstabelecimentoId = estabelecimentoId,
            Medicamento = medicamento.Trim(),
            Posologia = string.IsNullOrWhiteSpace(posologia) ? null : posologia.Trim(),
            ViaAdministracao = via,
            UsoCount = 1,
            UltimoUso = agora,
            CriadoEm = agora
        };
    }

    public virtual void IncrementarUso()
    {
        UsoCount += 1;
        UltimoUso = DateTime.UtcNow;
    }
}
