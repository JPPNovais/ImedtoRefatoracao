using Imedto.Backend.Contracts.Estabelecimentos.Queries.Results;
using Imedto.Backend.Contracts.Profissionais.Queries.Results;

namespace Imedto.Backend.Contracts.Auth.Queries.Results;

/// <summary>
/// Payload agregado do boot da SPA: usuário autenticado + cadastro profissional
/// (quando existir) + estabelecimentos vinculados. Substitui 3 round-trips
/// serializados (/auth/me, /profissional/me, /estabelecimento) por um único.
///
/// <c>DeveConfigurar2fa</c> (R10/CA14): true quando o usuário é Dono de pelo menos
/// um estabelecimento com <c>exigir_2fa_dono = true</c> e não tem 2FA ativo.
/// O front usa esta flag para forçar a configuração antes de liberar navegação.
/// </summary>
public record BootstrapMeDto(
    MeUsuarioDto Usuario,
    ProfissionalDto? Profissional,
    IEnumerable<EstabelecimentoDto> Estabelecimentos,
    bool DeveConfigurar2fa = false);
