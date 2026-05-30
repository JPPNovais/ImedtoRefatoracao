namespace Imedto.Backend.Contracts.Admin.Assinaturas.Commands;

public record EncerrarAssinaturaAdminCommand(
    Guid AssinaturaId,
    string Motivo,
    Guid AdminId);
