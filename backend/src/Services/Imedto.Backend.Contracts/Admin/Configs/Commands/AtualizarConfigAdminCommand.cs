namespace Imedto.Backend.Contracts.Admin.Configs.Commands;

public record AtualizarConfigAdminCommand(
    string Chave,
    string Valor,
    string Motivo,
    Guid AdminId);
