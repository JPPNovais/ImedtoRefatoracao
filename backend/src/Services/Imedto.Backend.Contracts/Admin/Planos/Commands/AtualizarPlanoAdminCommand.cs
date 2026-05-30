namespace Imedto.Backend.Contracts.Admin.Planos.Commands;

public record AtualizarPlanoAdminCommand(
    Guid PlanoId,
    string Nome,
    string? DescricaoCurta,
    int? PrecoMensalCentavos,
    bool Gratuito,
    string LimitesJson,
    string Motivo,
    Guid AdminId);
