namespace Imedto.Backend.Domain.Inventario.Cadastros;

/// <summary>
/// Tipos de local físico onde estoque é armazenado. Lista fechada (enum
/// fixo aprovado pelo produto). Persistido como string para legibilidade no DB.
/// </summary>
public enum TipoLocalEstoque
{
    Armario,
    Gaveta,
    Refrigerado,
    Cofre,
    Estante,
    Sala
}
