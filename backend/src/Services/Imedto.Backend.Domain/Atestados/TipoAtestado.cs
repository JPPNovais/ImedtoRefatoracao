namespace Imedto.Backend.Domain.Atestados;

/// <summary>
/// Tipo do atestado emitido. Persistido como string (HasConversion<string>) para
/// preservar legibilidade em consultas SQL diretas.
/// </summary>
public enum TipoAtestado
{
    /// <summary>Afastamento do trabalho/estudo. Exige número de dias.</summary>
    Afastamento,
    /// <summary>Atestado de comparecimento (paciente esteve no atendimento).</summary>
    Comparecimento,
    /// <summary>Atestado de aptidão (apto para atividade física, profissional, etc.).</summary>
    Aptidao,
    /// <summary>Demais casos — texto livre.</summary>
    Outro,
}
