namespace Imedto.Backend.Contracts.Admin.Assinaturas.Commands;

public record ConcederGratuidadeAdminCommand(
    long EstabelecimentoId,
    string GratuidadeMotivo,
    DateTimeOffset? FimEm,
    string Motivo,
    Guid AdminId);
