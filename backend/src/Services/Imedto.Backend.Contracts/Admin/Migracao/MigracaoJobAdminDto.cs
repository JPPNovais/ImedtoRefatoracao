namespace Imedto.Backend.Contracts.Admin.Migracao;

public sealed class MigracaoJobAdminDto
{
    public long Id { get; init; }
    public long EstabelecimentoId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Origem { get; init; }
    public Guid CriadoPorUsuarioId { get; init; }
    public DateTime CriadoEm { get; init; }
    public DateTime AtualizadoEm { get; init; }

    /// <summary>
    /// Template de origem usado para pré-preencher os mapas (CA18/R10).
    /// Null quando não há template para a origem do job.
    /// </summary>
    public long? TemplateOrigemId { get; init; }

    /// <summary>Nome do template de origem (join na query). Null quando TemplateOrigemId é null.</summary>
    public string? NomeTemplate { get; init; }

    public List<MigracaoMapaDto> Mapas { get; set; } = [];
}
