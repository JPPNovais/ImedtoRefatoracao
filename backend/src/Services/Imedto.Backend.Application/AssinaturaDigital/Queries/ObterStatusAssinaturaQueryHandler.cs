using Amazon.S3;
using Amazon.S3.Model;
using Imedto.Backend.Contracts.AssinaturaDigital.Queries;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.Infrastructure.Storage;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.Extensions.Options;

namespace Imedto.Backend.Application.AssinaturaDigital.Queries;

/// <summary>
/// Retorna status de assinatura + presigned URL (se AssinadaIcp).
/// Scoped — depende de IAmazonS3 (singleton) mas faz I/O de leitura leve.
/// </summary>
public class ObterStatusAssinaturaQueryHandler
    : Imedto.Backend.SharedKernel.Cqrs.IRequestHandler<ObterStatusAssinaturaQuery, StatusAssinaturaDto>
{
    private readonly IAssinaturaDigitalQueryRepository _queryRepo;
    private readonly IAmazonS3 _s3;
    private readonly StorageOptions _storageOptions;

    public ObterStatusAssinaturaQueryHandler(
        IAssinaturaDigitalQueryRepository queryRepo,
        IAmazonS3 s3,
        IOptions<StorageOptions> storageOptions)
    {
        _queryRepo = queryRepo;
        _s3 = s3;
        _storageOptions = storageOptions.Value;
    }

    public async Task<StatusAssinaturaDto> Handle(ObterStatusAssinaturaQuery query)
    {
        // CA-21: filtro por estabelecimento garante multi-tenant.
        var info = await _queryRepo.ObterStatusAsync(query.ReceitaId, query.EstabelecimentoId)
            ?? throw new BusinessException("Receita não encontrada.");

        string? presignedUrl = null;
        if (info.Status == "AssinadaIcp" && !string.IsNullOrWhiteSpace(info.PdfAssinadoS3Key))
        {
            // LGPD: presigned URL TTL 5 min — não persiste URL em lugar algum.
            var req = new GetPreSignedUrlRequest
            {
                BucketName = _storageOptions.BucketAnexosProntuario,
                Key = info.PdfAssinadoS3Key,
                Expires = DateTime.UtcNow.AddSeconds(300), // 5 min
                Verb = HttpVerb.GET,
            };
            presignedUrl = _s3.GetPreSignedURL(req);
        }

        return new StatusAssinaturaDto
        {
            Status = info.Status,
            PdfAssinadoUrl = presignedUrl,
        };
    }
}
