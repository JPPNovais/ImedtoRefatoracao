using Imedto.Backend.Contracts.Admin.Estabelecimentos.Queries;
using Imedto.Backend.Contracts.Admin.Estabelecimentos.Queries.Results;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Estabelecimentos.Queries;

/// <summary>
/// Retorna detalhe completo de um estabelecimento para o admin.
/// Registra audit de leitura de detalhe (CA13/CA15).
/// Scoped: depende de ImedtoAdminAuditWriter (scoped).
/// </summary>
public class ObterEstabelecimentoAdminQueryHandler
    : IRequestHandler<ObterEstabelecimentoAdminQuery, EstabelecimentoAdminDetalheDto?>
{
    private readonly IAdminEstabelecimentosQueryRepository _repo;
    private readonly ImedtoAdminAuditWriter _audit;

    public ObterEstabelecimentoAdminQueryHandler(
        IAdminEstabelecimentosQueryRepository repo,
        ImedtoAdminAuditWriter audit)
    {
        _repo = repo;
        _audit = audit;
    }

    public async Task<EstabelecimentoAdminDetalheDto?> Handle(ObterEstabelecimentoAdminQuery query)
    {
        var detalhe = await _repo.ObterDetalheAsync(query.EstabelecimentoId);

        if (detalhe is null)
            throw new BusinessException("Estabelecimento não encontrado.");

        // Audit de leitura: silencia falha (não bloqueia a operação de leitura).
        await _audit.RegistrarLeituraAsync(
            AcoesAuditAdmin.AbrirDetalheTenant,
            query.AdminId,
            "Estabelecimento",
            query.EstabelecimentoId.ToString(),
            tenantAfetadoId: query.EstabelecimentoId);

        return detalhe;
    }
}
