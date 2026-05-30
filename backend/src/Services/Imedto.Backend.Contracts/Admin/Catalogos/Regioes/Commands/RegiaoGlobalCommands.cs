namespace Imedto.Backend.Contracts.Admin.Catalogos.Regioes.Commands;

public record CriarRegiaoGlobalCommand(
    string Nome,
    string[]? Sinonimos,
    string? SistemaCorporal,
    string Motivo,
    Guid AdminId);

public record AtualizarRegiaoGlobalCommand(
    Guid Id,
    string Nome,
    string[]? Sinonimos,
    string? SistemaCorporal,
    string Motivo,
    Guid AdminId);

public record DesativarRegiaoGlobalCommand(Guid Id, string Motivo, Guid AdminId);

public record ReativarRegiaoGlobalCommand(Guid Id, string Motivo, Guid AdminId);
