using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Receitas.Commands;

/// <summary>
/// Emite uma nova receita. Item de validação fica no aggregate
/// (≥1 item, controlada exige validade futura + tipo de notificação, etc.).
/// </summary>
public class EmitirReceitaCommand : ICommand
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid ProfissionalUsuarioId { get; set; }
    /// <summary>"Comum" | "Controlada" | "Antibiotico" | "Especial".</summary>
    public string Tipo { get; set; } = "Comum";
    /// <summary>
    /// "A" | "B" | "C" | "Especial". Obrigatório quando <see cref="Tipo"/> == "Controlada";
    /// deve ser <c>null</c> nos demais casos. Portaria 344/98.
    /// </summary>
    public string? TipoNotificacao { get; set; }
    public DateTime? ValidadeAte { get; set; }
    public string? Observacoes { get; set; }
    public List<ItemReceitaPayload> Itens { get; set; } = new();

    /// <summary>Preenchido pelo handler — id da receita criada.</summary>
    public long ReceitaIdCriada { get; set; }
}

public class ItemReceitaPayload
{
    public string Medicamento { get; set; } = string.Empty;
    public string Posologia { get; set; } = string.Empty;
    public string? Quantidade { get; set; }
    /// <summary>"Oral" | "Topica" | "IM" | "EV" | "SC" | "Outra" | null.</summary>
    public string? Via { get; set; }
    public string? Observacao { get; set; }
    /// <summary>Concentração (ex.: "500mg", "20mg/mL"). Opcional.</summary>
    public string? Concentracao { get; set; }
    /// <summary>Forma farmacêutica (ex.: "Comprimido"). Opcional.</summary>
    public string? FormaFarmaceutica { get; set; }
    /// <summary>Duração do tratamento (ex.: "7 dias"). Opcional.</summary>
    public string? Duracao { get; set; }
}
