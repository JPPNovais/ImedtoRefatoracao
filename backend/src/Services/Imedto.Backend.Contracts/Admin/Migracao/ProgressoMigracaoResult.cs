namespace Imedto.Backend.Contracts.Admin.Migracao;

public sealed class ProgressoEntidadeDto
{
    public int Total { get; init; }
    public int Pendentes { get; init; }
    public int Criados { get; init; }
    public int Atualizados { get; init; }
    public int Rejeitados { get; init; }
    public int Pulados { get; init; }
    public int Percentual { get; init; }
}

public sealed class ProgressoMigracaoResult
{
    public Dictionary<string, ProgressoEntidadeDto> PorEntidade { get; init; } = [];
    public int PercentualAgregado { get; init; }
}
