namespace Imedto.Backend.Contracts.Catalogo.Queries.Results;

public class ProfissaoListadaDto
{
    public long Id { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string? ConselhoSigla { get; init; }
    public bool Ativo { get; init; }
}
