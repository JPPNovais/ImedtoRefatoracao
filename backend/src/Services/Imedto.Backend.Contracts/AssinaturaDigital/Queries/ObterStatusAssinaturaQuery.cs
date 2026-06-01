using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.AssinaturaDigital.Queries;


public class ObterStatusAssinaturaQuery : IQuery<StatusAssinaturaDto>
{
    public long ReceitaId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid CallerUsuarioId { get; set; }
}

public class StatusAssinaturaDto
{
    public string Status { get; set; } = "NaoAssinada";
    /// <summary>Presigned URL S3 com TTL 5 min. Null quando não AssinadaIcp.</summary>
    public string? PdfAssinadoUrl { get; set; }
}
