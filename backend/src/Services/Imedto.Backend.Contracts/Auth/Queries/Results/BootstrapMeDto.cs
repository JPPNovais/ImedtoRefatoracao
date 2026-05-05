using Imedto.Backend.Contracts.Estabelecimentos.Queries.Results;
using Imedto.Backend.Contracts.Profissionais.Queries.Results;

namespace Imedto.Backend.Contracts.Auth.Queries.Results;

/// <summary>
/// Payload agregado do boot da SPA: usuário autenticado + cadastro profissional
/// (quando existir) + estabelecimentos vinculados. Substitui 3 round-trips
/// serializados (/auth/me, /profissional/me, /estabelecimento) por um único.
/// </summary>
public record BootstrapMeDto(
    MeUsuarioDto Usuario,
    ProfissionalDto? Profissional,
    IEnumerable<EstabelecimentoDto> Estabelecimentos);
