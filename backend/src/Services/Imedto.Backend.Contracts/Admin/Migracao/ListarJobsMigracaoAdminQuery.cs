namespace Imedto.Backend.Contracts.Admin.Migracao;

public sealed class ListarJobsMigracaoAdminQuery
{
    public long? EstabelecimentoId { get; init; }
    public string? Status { get; init; }
    public int Pagina { get; init; } = 1;
    public int Tamanho { get; init; } = 25;
}

public sealed class ListarJobsMigracaoAdminResult
{
    public List<MigracaoJobAdminDto> Itens { get; init; } = [];
    public int Total { get; init; }
    public int Pagina { get; init; }
    public int Tamanho { get; init; }
}
