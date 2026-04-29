namespace Imedto.Backend.Contracts.Catalogo.Queries.Results;

public class EspecialidadeListadaDto
{
    public long Id { get; init; }
    public long ProfissaoId { get; init; }
    public string ProfissaoNome { get; init; } = string.Empty;
    public string Nome { get; init; } = string.Empty;
    public bool Ativo { get; init; }
}
