using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Agendamentos.Commands;

/// <summary>
/// Confirma presença do paciente via link público anônimo (Fase 2).
/// Autenticação pelo token (256 bits). Todos os erros devolvem 410 genérico no controller.
/// Idempotência: se já Confirmado, devolve <see cref="ResultadoConfirmacaoPresenca.JaConfirmado"/>.
/// </summary>
public class ConfirmarPresencaPublicaCommand : ICommand
{
    public string Token { get; set; } = string.Empty;
    public string? IpOrigem { get; set; }
    public string? UserAgent { get; set; }

    /// <summary>Preenchido pelo handler.</summary>
    public ResultadoConfirmacaoPresenca Resultado { get; set; }
}

public enum ResultadoConfirmacaoPresenca
{
    ConfirmadoAgora,
    JaConfirmado,
}
