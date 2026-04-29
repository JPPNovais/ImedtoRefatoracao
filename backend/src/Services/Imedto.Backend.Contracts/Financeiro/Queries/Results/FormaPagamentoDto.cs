namespace Imedto.Backend.Contracts.Financeiro.Queries.Results;

public class FormaPagamentoDto
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Padrao { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadaEm { get; set; }
    public DateTime? AtualizadaEm { get; set; }
}
