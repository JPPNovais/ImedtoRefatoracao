namespace Imedto.Backend.Contracts.Admin.Assinaturas.Commands;

public record LiberarAteDataAdminCommand(
    long EstabelecimentoId,
    Guid PlanoId,
    DateTimeOffset DataExpiracao,
    string Motivo,
    Guid AdminId);
