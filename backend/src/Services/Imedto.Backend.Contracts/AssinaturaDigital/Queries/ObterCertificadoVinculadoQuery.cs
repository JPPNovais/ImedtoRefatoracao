using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.AssinaturaDigital.Queries;

public class ObterCertificadoVinculadoQuery : IQuery<CertificadoVinculadoDto?>
{
    public Guid MedicoId { get; set; }
}

/// <summary>
/// Retorna apenas metadados do certificado — nunca o refresh_token.
/// </summary>
public class CertificadoVinculadoDto
{
    public string Provedor { get; set; } = string.Empty;
    public DateTime? ExpiraEm { get; set; }
}
