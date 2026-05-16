using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Inventario.Commands;

public class CriarItemInventarioCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    /// <summary>FK para CategoriaEstoque do mesmo estabelecimento (obrigatório).</summary>
    public long CategoriaId { get; set; }
    /// <summary>FK opcional para FabricanteEstoque (substitui "marca" string legada).</summary>
    public long? FabricanteId { get; set; }
    /// <summary>FK opcional para FornecedorEstoque padrão.</summary>
    public long? FornecedorPadraoId { get; set; }
    /// <summary>FK opcional para LocalEstoque padrão.</summary>
    public long? LocalPadraoId { get; set; }
    public string UnidadeMedida { get; set; } = string.Empty;
    public decimal QuantidadeInicial { get; set; }
    public decimal QuantidadeMinima { get; set; }
    /// <summary>Custo unitário do estoque inicial. Obrigatório (>0) se <see cref="QuantidadeInicial"/> > 0.</summary>
    public decimal CustoUnitarioInicial { get; set; }
    /// <summary>Custo unitário de referência (sugestão para pedido de compra). Opcional, independente do CustoUnitarioInicial.</summary>
    public decimal? CustoUnitario { get; set; }
    public Guid CriadoPorUsuarioId { get; set; }

    public long ItemIdCriado { get; set; }
}
