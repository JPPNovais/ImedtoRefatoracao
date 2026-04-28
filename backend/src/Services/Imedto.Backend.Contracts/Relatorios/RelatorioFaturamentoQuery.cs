using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Relatorios;

public class RelatorioFaturamentoQuery : IQuery<IEnumerable<FaturamentoCategoriaDto>>
{
    public long EstabelecimentoId { get; set; }
    public DateOnly? DataInicio { get; set; }
    public DateOnly? DataFim { get; set; }
}

public class FaturamentoCategoriaDto
{
    public string Categoria { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public decimal TotalPago { get; set; }
    public decimal TotalPendente { get; set; }
    public int Quantidade { get; set; }
}
