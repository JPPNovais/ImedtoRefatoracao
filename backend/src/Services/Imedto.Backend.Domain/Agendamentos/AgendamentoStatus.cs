namespace Imedto.Backend.Domain.Agendamentos;

public enum AgendamentoStatus
{
    Agendado,
    Confirmado,
    Cancelado,
    Concluido,
    /// <summary>
    /// Aplicado automaticamente pelo job noturno a agendamentos de D-1 que
    /// permaneceram em Agendado ou Confirmado sem receber baixa manual.
    /// Estado terminal neutro — distinto de Cancelado (que é ação deliberada).
    /// </summary>
    Expirado
}
