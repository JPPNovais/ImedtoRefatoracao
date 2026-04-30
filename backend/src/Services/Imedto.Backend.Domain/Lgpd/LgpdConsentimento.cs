using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Lgpd;

/// <summary>
/// Registro de consentimento LGPD do titular. Imutável após criação.
/// Cada nova aceitação (nova versão, novo tipo) gera um novo registro — não há update.
/// </summary>
public class LgpdConsentimento : Entity
{
    public virtual Guid UsuarioId { get; protected set; }
    public virtual TipoConsentimentoLgpd Tipo { get; protected set; }
    /// <summary>Versão do documento aceito, ex: "v1.0".</summary>
    public virtual string Versao { get; protected set; }
    public virtual DateTime AceitoEm { get; protected set; }
    /// <summary>IP de origem (IPv4 ou IPv6). Null quando indisponível.</summary>
    public virtual string IpOrigem { get; protected set; }
    public virtual string UserAgent { get; protected set; }

    protected LgpdConsentimento() { }

    public static LgpdConsentimento Aceitar(
        Guid usuarioId,
        TipoConsentimentoLgpd tipo,
        string versao,
        string ipOrigem = null,
        string userAgent = null)
    {
        if (usuarioId == Guid.Empty)
            throw new BusinessException("Usuário é obrigatório para registrar consentimento.");
        if (string.IsNullOrWhiteSpace(versao))
            throw new BusinessException("Versão do documento é obrigatória.");

        return new LgpdConsentimento
        {
            UsuarioId = usuarioId,
            Tipo = tipo,
            Versao = versao.Trim(),
            AceitoEm = DateTime.UtcNow,
            IpOrigem = string.IsNullOrWhiteSpace(ipOrigem) ? null : ipOrigem.Trim(),
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim()
        };
    }
}
