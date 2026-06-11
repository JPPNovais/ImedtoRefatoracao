namespace Imedto.Backend.Contracts.Admin.Assinaturas.Commands;

public record ReativarAssinaturaAdminCommand(
    long EstabelecimentoId,
    string Motivo,
    Guid AdminId);
