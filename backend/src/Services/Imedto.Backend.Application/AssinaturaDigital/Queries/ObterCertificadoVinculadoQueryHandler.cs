using Imedto.Backend.Contracts.AssinaturaDigital.Queries;
using Imedto.Backend.Domain.AssinaturaDigital;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.AssinaturaDigital.Queries;

/// <summary>
/// Retorna metadados do certificado do médico — nunca expõe o refresh_token.
/// Singleton-safe: IAssinaturaCertificadoRepository deve ser Scoped —
/// handler registrado como Scoped para compatibilidade.
/// </summary>
public class ObterCertificadoVinculadoQueryHandler
    : Imedto.Backend.SharedKernel.Cqrs.IRequestHandler<ObterCertificadoVinculadoQuery, CertificadoVinculadoDto?>
{
    private readonly IAssinaturaCertificadoRepository _repo;

    public ObterCertificadoVinculadoQueryHandler(IAssinaturaCertificadoRepository repo)
    {
        _repo = repo;
    }

    public async Task<CertificadoVinculadoDto?> Handle(ObterCertificadoVinculadoQuery query)
    {
        var cert = await _repo.ObterPorMedicoAsync(query.MedicoId);
        if (cert is null) return null;

        return new CertificadoVinculadoDto
        {
            Provedor = cert.Provedor,
            ExpiraEm = cert.ExpiraEm,
        };
    }
}
