namespace Imedto.Backend.Contracts.Admin.Assinaturas.Commands;

public record TrocarPlanoAdminCommand(
    long EstabelecimentoId,
    Guid PlanoId,
    DateTimeOffset Inicio,
    DateTimeOffset? FimEm,
    string Motivo,
    Guid AdminId);
