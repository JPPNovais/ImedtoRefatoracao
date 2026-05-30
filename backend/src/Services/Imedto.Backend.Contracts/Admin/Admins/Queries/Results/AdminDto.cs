namespace Imedto.Backend.Contracts.Admin.Admins.Queries.Results;

public record AdminListItemDto(
    Guid Id,
    string Email,
    string Nome,
    bool Ativo,
    bool ForcePasswordReset,
    DateTime CriadoEm,
    DateTime? UltimoLoginEm);

public record AdminDetalheDto(
    Guid Id,
    string Email,
    string Nome,
    bool Ativo,
    bool ForcePasswordReset,
    DateTime CriadoEm,
    Guid? CriadoPor,
    DateTime? DesativadoEm,
    Guid? DesativadoPor,
    DateTime? UltimoLoginEm);

public record ListarAdminsResult(
    IEnumerable<AdminListItemDto> Itens,
    int Total,
    int Pagina,
    int Tamanho);

/// <summary>Retorno do comando CriarAdmin — inclui a senha temporária UMA VEZ.</summary>
public record AdminCriadoResult(
    Guid Id,
    string Email,
    string Nome,
    string SenhaTemporaria);

/// <summary>Retorno do comando ResetSenha — inclui a nova senha temporária UMA VEZ.</summary>
public record SenhaResetadaResult(string SenhaTemporaria);
