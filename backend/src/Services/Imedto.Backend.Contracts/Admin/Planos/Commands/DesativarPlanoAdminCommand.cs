namespace Imedto.Backend.Contracts.Admin.Planos.Commands;

public record DesativarPlanoAdminCommand(Guid PlanoId, string Motivo, Guid AdminId);
