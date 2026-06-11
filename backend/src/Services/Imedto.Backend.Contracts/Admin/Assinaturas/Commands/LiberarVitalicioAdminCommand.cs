namespace Imedto.Backend.Contracts.Admin.Assinaturas.Commands;

public record LiberarVitalicioAdminCommand(
    long EstabelecimentoId,
    Guid PlanoId,
    string Motivo,
    Guid AdminId);
