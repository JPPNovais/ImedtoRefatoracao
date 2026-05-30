namespace Imedto.Backend.Contracts.Admin.Catalogos.Variaveis.Commands;

public record CriarVariavelGlobalCommand(
    string Nome,
    string Tipo,
    string? ValoresJson,
    string? Descricao,
    string Motivo,
    Guid AdminId);

public record AtualizarVariavelGlobalCommand(
    Guid Id,
    string Nome,
    string Tipo,
    string? ValoresJson,
    string? Descricao,
    string Motivo,
    Guid AdminId);

public record DesativarVariavelGlobalCommand(Guid Id, string Motivo, Guid AdminId);

public record ReativarVariavelGlobalCommand(Guid Id, string Motivo, Guid AdminId);
