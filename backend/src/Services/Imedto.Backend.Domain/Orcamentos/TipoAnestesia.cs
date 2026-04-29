namespace Imedto.Backend.Domain.Orcamentos;

/// <summary>
/// Tipo de anestesia aplicada ao orçamento (item 6 — paridade com legado
/// <c>orcamento_anestesia</c>). Mapeada como string no banco.
/// </summary>
public enum TipoAnestesia
{
    Local,
    Sedacao,
    Geral,
    Raquianestesia,
    Peridural,
    Bloqueio
}
