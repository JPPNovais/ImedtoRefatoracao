namespace Imedto.Backend.Contracts.Termos.Dtos;

public class TermoModeloDto
{
    public long Id { get; set; }
    public long? EstabelecimentoId { get; set; }
    public string Categoria { get; set; }
    public string Titulo { get; set; }
    public string ConteudoHtml { get; set; }
    public bool Ativo { get; set; }
    public int VersaoAtual { get; set; }
    public long? PadraoClonadoDeId { get; set; }
    public bool EhPadraoDoSistema { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
}

public class PaginaModelosTermoDto
{
    public IReadOnlyList<TermoModeloDto> Itens { get; set; } = Array.Empty<TermoModeloDto>();
    public int Pagina { get; set; }
    public int Tamanho { get; set; }
    public int Total { get; set; }
}
