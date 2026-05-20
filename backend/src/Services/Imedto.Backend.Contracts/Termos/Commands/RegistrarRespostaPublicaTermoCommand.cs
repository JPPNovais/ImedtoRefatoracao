using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Termos.Commands;

/// <summary>
/// Registra aceite ou recusa de termo via link público (Fase 4). Endpoint anônimo —
/// autenticação pelo próprio token (32 bytes de entropia). Todos os erros devolvem
/// 410 Gone genérico no controller para evitar enumeração de tokens.
/// </summary>
public class RegistrarRespostaPublicaTermoCommand : ICommand
{
    public string TokenAceite { get; set; }
    public bool Aceito { get; set; }
    /// <summary>Opcional — confirmar nome do paciente (case/acentos ignorados).</summary>
    public string NomeConfirmado { get; set; }
    public string IpOrigem { get; set; }
    public string UserAgent { get; set; }

    /// <summary>
    /// Preenchido pelo handler:
    /// <list type="bullet">
    ///   <item><c>RespondidoAgora</c>: status mudou nesta chamada.</item>
    ///   <item><c>JaRespondido</c>: idempotência — termo já estava assinado/recusado.</item>
    /// </list>
    /// </summary>
    public ResultadoRespostaPublica Resultado { get; set; }
}

public enum ResultadoRespostaPublica
{
    RespondidoAgora,
    JaRespondido,
}
