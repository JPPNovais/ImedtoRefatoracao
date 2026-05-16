using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Inventario.Commands;

public class AtualizarItemInventarioCommand : ICommand
{
    public long ItemId { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public long CategoriaId { get; set; }
    public long? FabricanteId { get; set; }
    public long? FornecedorPadraoId { get; set; }
    public long? LocalPadraoId { get; set; }
    public string UnidadeMedida { get; set; } = string.Empty;
    public decimal QuantidadeMinima { get; set; }
    public decimal? CustoUnitario { get; set; }
}
