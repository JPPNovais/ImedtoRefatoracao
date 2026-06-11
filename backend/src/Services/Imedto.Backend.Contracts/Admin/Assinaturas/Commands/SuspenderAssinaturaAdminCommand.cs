namespace Imedto.Backend.Contracts.Admin.Assinaturas.Commands;

public record SuspenderAssinaturaAdminCommand(
    long EstabelecimentoId,
    string Motivo,
    Guid AdminId);
