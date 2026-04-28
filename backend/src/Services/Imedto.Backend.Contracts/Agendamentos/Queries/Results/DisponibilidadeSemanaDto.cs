namespace Imedto.Backend.Contracts.Agendamentos.Queries.Results;

public class DisponibilidadeSemanaDto
{
    public Guid ProfissionalUsuarioId { get; set; }
    public List<DisponibilidadeDiaDto> Dias { get; set; } = new();
}

public class DisponibilidadeDiaDto
{
    public DateOnly Data { get; set; }
    /// <summary>DOM, SEG, TER, QUA, QUI, SEX, SAB</summary>
    public string DiaSemana { get; set; } = string.Empty;
    /// <summary>fechado | disponivel | indisponivel</summary>
    public string Status { get; set; } = string.Empty;
    public List<DisponibilidadeSlotDto> Slots { get; set; } = new();
}

public class DisponibilidadeSlotDto
{
    /// <summary>Formato HH:mm, ex: "08:30"</summary>
    public string Hora { get; set; } = string.Empty;
    public bool Disponivel { get; set; }
    /// <summary>null | "agendado" | "bloqueado"</summary>
    public string? Motivo { get; set; }
    public string? PacienteNome { get; set; }
}
