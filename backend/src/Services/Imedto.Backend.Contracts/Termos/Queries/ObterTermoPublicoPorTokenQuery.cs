using Imedto.Backend.Contracts.Termos.Dtos;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Termos.Queries;

/// <summary>
/// Lookup anônimo via token público (Fase 4). Não tem tenant filter — o token
/// é segredo de 256 bits. Retorna apenas dados mínimos (sem PII do paciente).
/// </summary>
public class ObterTermoPublicoPorTokenQuery : IQuery<TermoPublicoDto>
{
    public string Token { get; set; }
    public string IpOrigem { get; set; }
    public string UserAgent { get; set; }
}
