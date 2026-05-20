using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Termos.Commands;

/// <summary>
/// Reenvia o link público por e-mail (canal "email") ou apenas devolve a URL
/// pra ser copiada pelo emissor (canal "copia"). Cooldown de 5 min entre envios
/// por e-mail (anti-spam) — "copia" não tem cooldown.
/// </summary>
public class ReenviarLinkTermoCommand : ICommand
{
    public long TermoEmitidoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    /// <summary>"email" (default) ou "copia".</summary>
    public string Canal { get; set; } = "email";

    /// <summary>Preenchido pelo handler — token a ser exibido pra "copiar link".</summary>
    public string TokenAceite { get; set; }
}
