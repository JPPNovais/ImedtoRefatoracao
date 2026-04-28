namespace Imedto.Backend.Contracts.Prontuarios.Queries.Results;

public class VariavelPoolDto
{
    public long Id { get; set; }
    public long? EstabelecimentoId { get; set; }
    public string Tipo { get; set; }
    public string Nome { get; set; }
    public bool Ativo { get; set; }
    public bool EhPadraoSistema { get; set; }
}
