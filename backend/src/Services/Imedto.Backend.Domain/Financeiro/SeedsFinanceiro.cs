namespace Imedto.Backend.Domain.Financeiro;

/// <summary>
/// Conjunto mínimo de categorias e formas de pagamento criadas automaticamente quando um
/// estabelecimento é cadastrado, para que a área financeira já esteja utilizável no primeiro
/// acesso. Itens daqui são marcados com <c>Padrao = true</c> e ficam protegidos contra edição.
/// </summary>
public static class SeedsFinanceiro
{
    public static readonly IReadOnlyList<(string Nome, TipoCategoria Tipo)> Categorias = new[]
    {
        ("Receita: Consulta",     TipoCategoria.Receita),
        ("Receita: Procedimento", TipoCategoria.Receita),
        ("Receita: Outros",       TipoCategoria.Receita),
        ("Despesa: Folha",        TipoCategoria.Despesa),
        ("Despesa: Aluguel",      TipoCategoria.Despesa),
        ("Despesa: Insumos",      TipoCategoria.Despesa),
        ("Despesa: Outros",       TipoCategoria.Despesa)
    };

    public static readonly IReadOnlyList<string> FormasPagamento = new[]
    {
        "Dinheiro",
        "PIX",
        "Cartão de Crédito",
        "Cartão de Débito",
        "Transferência",
        "Boleto"
    };
}
