namespace Imedto.Backend.Domain.Cirurgias;

/// <summary>
/// POCO que representa a ficha anestésica de um procedimento cirúrgico.
/// Serializado como JSONB na coluna <c>ficha_anestesica</c>.
/// </summary>
public class FichaAnestesica
{
    public string? Tecnica { get; init; }
    public DateTime? InicioAnestesia { get; init; }
    public DateTime? FimAnestesia { get; init; }
    public IList<DrogaAnestesica> Drogas { get; init; } = new List<DrogaAnestesica>();
    public string? Intercorrencias { get; init; }
    public string? Observacoes { get; init; }
    public IList<MonitorizacaoVital>? Monitorizacao { get; init; }
}

public class DrogaAnestesica
{
    public string Nome { get; init; } = "";
    public string Dose { get; init; } = "";
    public string Via { get; init; } = "";
    public DateTime? Hora { get; init; }
}

public class MonitorizacaoVital
{
    public DateTime Hora { get; init; }
    public string? PressaoArterial { get; init; }
    public int? FrequenciaCardiaca { get; init; }
    public int? FrequenciaRespiratoria { get; init; }
    public int? SaturacaoO2 { get; init; }
    public decimal? Temperatura { get; init; }
}
