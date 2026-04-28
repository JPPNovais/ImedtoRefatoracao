using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.Infrastructure.Tenancy;

/// <summary>
/// Resolve o papel do usuário (Dono, Profissional, Recepcionista, SemAcesso, NaoEncontrado)
/// consultando o estabelecimento e o tipo_acesso do modelo de permissão vinculado.
/// </summary>
public class TenantAccessResolver : ITenantAccessResolver
{
    private readonly IEstabelecimentoRepository _estabelecimentoRepo;
    private readonly VinculoQueryRepository _vinculoQueryRepo;

    public TenantAccessResolver(
        IEstabelecimentoRepository estabelecimentoRepo,
        VinculoQueryRepository vinculoQueryRepo)
    {
        _estabelecimentoRepo = estabelecimentoRepo;
        _vinculoQueryRepo = vinculoQueryRepo;
    }

    public async Task<TenantPapel> ResolverPapelAsync(Guid usuarioId, long estabelecimentoId)
    {
        var estab = await _estabelecimentoRepo.ObterPorIdOuNulo(estabelecimentoId);
        if (estab is null)
            return TenantPapel.NaoEncontrado;

        if (estab.DonoUsuarioId == usuarioId)
            return TenantPapel.Dono;

        var tipoAcesso = await _vinculoQueryRepo.ObterTipoAcessoVinculoAtivo(usuarioId, estabelecimentoId);
        if (tipoAcesso is null)
            return TenantPapel.SemAcesso;

        return tipoAcesso == nameof(TipoAcessoModelo.Recepcionista)
            ? TenantPapel.Recepcionista
            : TenantPapel.Profissional;
    }
}
