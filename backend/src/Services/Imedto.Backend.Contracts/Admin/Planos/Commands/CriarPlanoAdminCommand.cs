namespace Imedto.Backend.Contracts.Admin.Planos.Commands;

public record CriarPlanoAdminCommand(
    string Nome,
    string? DescricaoCurta,
    int? PrecoMensalCentavos,
    bool Gratuito,
    string LimitesJson,
    string FeaturesJson,
    string Motivo,
    Guid AdminId);
