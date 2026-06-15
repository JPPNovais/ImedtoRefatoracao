namespace Imedto.Backend.Contracts.Admin.Migracao;

public sealed class SalvarTemplateDeOrigemCommand
{
    public long JobId { get; init; }
    public string NomeTemplate { get; init; } = string.Empty;
    public Guid RevisadoPorUsuarioId { get; init; }
}
