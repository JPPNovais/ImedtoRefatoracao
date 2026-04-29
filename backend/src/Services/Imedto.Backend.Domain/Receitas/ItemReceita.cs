using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Receitas;

/// <summary>
/// Item da receita (medicamento prescrito). Entity child do aggregate <see cref="Receita"/>.
/// É construído pela fábrica <see cref="Receita.Emitir"/> — não tem ciclo de vida próprio.
///
/// Campos clínicos opcionais (<see cref="Concentracao"/>, <see cref="FormaFarmaceutica"/>,
/// <see cref="Duracao"/>) refletem a paridade com o legado: o profissional pode preencher
/// "500mg" / "Comprimido" / "7 dias" como metadados além da posologia, ou deixar tudo
/// embutido na própria posologia (texto livre). Nada disso é obrigatório.
///
/// O campo <c>instrucoes_adicionais</c> do legado é mapeado para o já existente
/// <see cref="Observacao"/> — economiza migration e mantém o vocabulário interno.
/// </summary>
public class ItemReceita : Entity
{
    public virtual long ReceitaId { get; protected set; }
    public virtual int Ordem { get; protected set; }
    public virtual string Medicamento { get; protected set; } = string.Empty;
    public virtual string Posologia { get; protected set; } = string.Empty;
    public virtual string? Quantidade { get; protected set; }
    public virtual ViaAdministracao? Via { get; protected set; }
    public virtual string? Observacao { get; protected set; }

    /// <summary>Concentração do medicamento (ex.: "500mg", "20mg/mL"). Opcional.</summary>
    public virtual string? Concentracao { get; protected set; }
    /// <summary>Forma farmacêutica (ex.: "Comprimido", "Solução injetável"). Opcional.</summary>
    public virtual string? FormaFarmaceutica { get; protected set; }
    /// <summary>Duração do tratamento (ex.: "7 dias", "Uso contínuo"). Opcional.</summary>
    public virtual string? Duracao { get; protected set; }

    protected ItemReceita() { }

    internal static ItemReceita Criar(
        int ordem,
        string medicamento,
        string posologia,
        string? quantidade,
        ViaAdministracao? via,
        string? observacao,
        string? concentracao = null,
        string? formaFarmaceutica = null,
        string? duracao = null)
    {
        if (string.IsNullOrWhiteSpace(medicamento))
            throw new BusinessException("Medicamento é obrigatório.");
        if (medicamento.Length > 200)
            throw new BusinessException("Medicamento excede 200 caracteres.");
        if (string.IsNullOrWhiteSpace(posologia))
            throw new BusinessException("Posologia é obrigatória.");
        if (posologia.Length > 500)
            throw new BusinessException("Posologia excede 500 caracteres.");
        if (quantidade is not null && quantidade.Length > 80)
            throw new BusinessException("Quantidade excede 80 caracteres.");
        if (observacao is not null && observacao.Length > 500)
            throw new BusinessException("Observação do item excede 500 caracteres.");
        if (concentracao is not null && concentracao.Length > 100)
            throw new BusinessException("Concentração excede 100 caracteres.");
        if (formaFarmaceutica is not null && formaFarmaceutica.Length > 60)
            throw new BusinessException("Forma farmacêutica excede 60 caracteres.");
        if (duracao is not null && duracao.Length > 80)
            throw new BusinessException("Duração excede 80 caracteres.");
        if (ordem < 0)
            throw new BusinessException("Ordem do item inválida.");

        return new ItemReceita
        {
            Ordem = ordem,
            Medicamento = medicamento.Trim(),
            Posologia = posologia.Trim(),
            Quantidade = string.IsNullOrWhiteSpace(quantidade) ? null : quantidade.Trim(),
            Via = via,
            Observacao = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim(),
            Concentracao = string.IsNullOrWhiteSpace(concentracao) ? null : concentracao!.Trim(),
            FormaFarmaceutica = string.IsNullOrWhiteSpace(formaFarmaceutica) ? null : formaFarmaceutica!.Trim(),
            Duracao = string.IsNullOrWhiteSpace(duracao) ? null : duracao!.Trim()
        };
    }
}
