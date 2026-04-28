namespace Imedto.Backend.Contracts.Unidades.Queries.Results;

public class UnidadeDto
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Nome { get; set; }
    public bool IsPrincipal { get; set; }
    public string Cep { get; set; }
    public string Logradouro { get; set; }
    public string Numero { get; set; }
    public string Complemento { get; set; }
    public string Bairro { get; set; }
    public string Cidade { get; set; }
    public string Estado { get; set; }
    public string Telefone { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}
