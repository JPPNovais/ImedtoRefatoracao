using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Inventario.Commands;

public class CriarItemInventarioCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string UnidadeMedida { get; set; } = string.Empty;
    public decimal QuantidadeInicial { get; set; }
    public decimal QuantidadeMinima { get; set; }
    /// <summary>Custo unitário do estoque inicial. Obrigatório (>0) se <see cref="QuantidadeInicial"/> > 0.</summary>
    public decimal CustoUnitarioInicial { get; set; }
    public Guid CriadoPorUsuarioId { get; set; }

    public long ItemIdCriado { get; set; }
}
