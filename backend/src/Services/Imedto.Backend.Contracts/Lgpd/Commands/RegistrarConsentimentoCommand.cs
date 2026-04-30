using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Lgpd.Commands;

public class RegistrarConsentimentoCommand : ICommand
{
    public Guid UsuarioId { get; init; }
    /// <summary>Tipo do consentimento — valores: TermosUso, PoliticaPrivacidade, UsoIA, UsoDadosClinicos.</summary>
    public string Tipo { get; init; }
    public string Versao { get; init; }
    public string IpOrigem { get; init; }
    public string UserAgent { get; init; }
}
