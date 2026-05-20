namespace Imedto.Backend.Domain.Inventario;

public enum TipoMovimentacaoEstoque
{
    Entrada,
    Saida,
    /// <summary>Registro de auditoria — não altera estoque. Criado quando o item é inativado.</summary>
    Inativacao
}
