namespace Imedto.Backend.Contracts.Salas.Queries.Results;

public class SalaDto
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public long UnidadeId { get; set; }
    public string UnidadeNome { get; set; }
    public long? TipoSalaId { get; set; }
    public string TipoSalaNome { get; set; }
    public string Nome { get; set; }
    public string Descricao { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}
