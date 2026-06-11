namespace Imedto.Backend.Contracts.Admin.Assinaturas.Commands;

public record IniciarTrialAdminCommand(
    long EstabelecimentoId,
    int Dias,
    Guid PlanoId,
    string Motivo,
    Guid AdminId);
