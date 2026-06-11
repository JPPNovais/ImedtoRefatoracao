using Imedto.Backend.Contracts.Assinaturas.Queries;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Assinaturas.Queries;

/// <summary>
/// Handler de <c>GET /api/minha-assinatura</c>.
///
/// Fonte migrada para a estrutura nova (imedto_assinaturas/imedto_planos) via
/// <see cref="MinhaAssinaturaQueryRepository"/>, alinhando a resposta do front com o
/// enforcement do backend (AssinaturaService F3). Gap do épico 2026-06-11_003 fechado.
///
/// O estado derivado (status, diasRestantes, features) usa a mesma lógica de
/// ImedtoAssinatura.ObterEstado() — liberado no back ⟺ não-bloqueado no front.
/// </summary>
public class ObterMinhaAssinaturaQueryHandlers : IRequestHandler<ObterMinhaAssinaturaQuery, AssinaturaDto?>
{
    private readonly MinhaAssinaturaQueryRepository _repo;

    public ObterMinhaAssinaturaQueryHandlers(MinhaAssinaturaQueryRepository repo) => _repo = repo;

    public Task<AssinaturaDto?> Handle(ObterMinhaAssinaturaQuery query)
        => _repo.ObterDoEstabelecimento(query.EstabelecimentoId);
}
