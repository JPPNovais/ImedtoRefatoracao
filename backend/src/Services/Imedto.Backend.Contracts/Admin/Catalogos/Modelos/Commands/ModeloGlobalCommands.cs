namespace Imedto.Backend.Contracts.Admin.Catalogos.Modelos.Commands;

public record CriarModeloGlobalCommand(
    string Nome,
    string? Descricao,
    string ConteudoJson,
    string Motivo,
    Guid AdminId);

public record AtualizarModeloGlobalCommand(
    Guid Id,
    string Nome,
    string? Descricao,
    string ConteudoJson,
    string Motivo,
    Guid AdminId);

public record DesativarModeloGlobalCommand(Guid Id, string Motivo, Guid AdminId);

public record ReativarModeloGlobalCommand(Guid Id, string Motivo, Guid AdminId);
