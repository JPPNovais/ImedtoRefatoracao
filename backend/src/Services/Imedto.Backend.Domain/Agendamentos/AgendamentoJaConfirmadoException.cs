namespace Imedto.Backend.Domain.Agendamentos;

/// <summary>
/// Sinaliza que o agendamento já está Confirmado ao tentar confirmar via link público.
/// Permite ao command handler tratar como idempotência (200 "já confirmado") em vez de erro.
/// </summary>
public sealed class AgendamentoJaConfirmadoException : Exception
{
    public AgendamentoJaConfirmadoException()
        : base("Agendamento já está confirmado.") { }
}
