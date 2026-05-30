namespace Imedto.Backend.Contracts.Admin.Planos.Commands;

public record AtivarPlanoAdminCommand(Guid PlanoId, string Motivo, Guid AdminId);
