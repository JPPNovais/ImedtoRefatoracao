namespace Imedto.Backend.Contracts.Admin.Migracao;

public sealed class MigracaoJobEventoDto
{
    public string? StatusAnterior { get; init; }
    public string StatusNovo { get; init; } = string.Empty;
    public Guid? UsuarioId { get; init; }
    public DateTime CriadoEm { get; init; }
}
