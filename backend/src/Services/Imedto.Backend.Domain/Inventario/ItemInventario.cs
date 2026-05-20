using Imedto.Backend.Domain.Inventario.Events;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Inventario;

public class ItemInventario : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual string Codigo { get; protected set; } = string.Empty;
    public virtual string Nome { get; protected set; } = string.Empty;
    /// <summary>Coluna texto livre legada — preenchida com o nome da CategoriaEstoque vinculada.
    /// Mantida durante o período de deprecation (queries antigas ainda lêem categoria string).</summary>
    public virtual string Categoria { get; protected set; } = string.Empty;
    /// <summary>FK para a CategoriaEstoque do estabelecimento. Obrigatório a partir desta migration.</summary>
    public virtual long CategoriaId { get; protected set; }
    /// <summary>FK opcional para FabricanteEstoque (substitui campo "marca" string legado).</summary>
    public virtual long? FabricanteId { get; protected set; }
    /// <summary>FK opcional para FornecedorEstoque padrão (default ao gerar pedido de compra).</summary>
    public virtual long? FornecedorPadraoId { get; protected set; }
    /// <summary>FK opcional para LocalEstoque padrão (onde o item costuma ficar armazenado).</summary>
    public virtual long? LocalPadraoId { get; protected set; }
    public virtual string UnidadeMedida { get; protected set; } = string.Empty;
    public virtual decimal QuantidadeAtual { get; protected set; }
    public virtual decimal QuantidadeMinima { get; protected set; }
    /// <summary>Custo médio ponderado atual (R$/unidade). Recalculado a cada entrada.</summary>
    public virtual decimal CustoMedio { get; protected set; }
    /// <summary>Custo unitário de referência informado no cadastro (sugestão para pedido de compra).
    /// Opcional — diferente do CustoMedio, que é dinâmico.</summary>
    public virtual decimal? CustoUnitario { get; protected set; }
    public virtual bool Ativo { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    protected ItemInventario() { }

    public static ItemInventario Criar(
        long estabelecimentoId,
        string codigo,
        string nome,
        long categoriaId,
        string categoriaNomeSnapshot,
        string unidadeMedida,
        decimal quantidadeMinima,
        long? fabricanteId,
        long? fornecedorPadraoId,
        long? localPadraoId,
        decimal? custoUnitario)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (string.IsNullOrWhiteSpace(codigo))
            throw new BusinessException("Código do item é obrigatório.");
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome do item é obrigatório.");
        if (categoriaId <= 0)
            throw new BusinessException("Categoria é obrigatória.");
        if (string.IsNullOrWhiteSpace(categoriaNomeSnapshot))
            throw new BusinessException("Categoria é obrigatória.");
        if (string.IsNullOrWhiteSpace(unidadeMedida))
            throw new BusinessException("Unidade de medida é obrigatória.");
        if (quantidadeMinima < 0)
            throw new BusinessException("Quantidade mínima não pode ser negativa.");
        if (custoUnitario.HasValue && custoUnitario.Value < 0)
            throw new BusinessException("Custo unitário não pode ser negativo.");

        return new ItemInventario
        {
            EstabelecimentoId = estabelecimentoId,
            Codigo = codigo.Trim().ToUpperInvariant(),
            Nome = nome.Trim(),
            CategoriaId = categoriaId,
            Categoria = categoriaNomeSnapshot.Trim(),
            FabricanteId = fabricanteId,
            FornecedorPadraoId = fornecedorPadraoId,
            LocalPadraoId = localPadraoId,
            UnidadeMedida = unidadeMedida.Trim(),
            QuantidadeAtual = 0,            // a entrada inicial é registrada pelo handler
            QuantidadeMinima = quantidadeMinima,
            CustoMedio = 0m,                // primeira entrada redefine
            CustoUnitario = custoUnitario,
            Ativo = true,
            CriadoEm = DateTime.UtcNow
        };
    }

    public virtual void Atualizar(
        string nome,
        long categoriaId,
        string categoriaNomeSnapshot,
        string unidadeMedida,
        decimal quantidadeMinima,
        long? fabricanteId,
        long? fornecedorPadraoId,
        long? localPadraoId,
        decimal? custoUnitario)
    {
        if (!Ativo)
            throw new BusinessException("Item inativo não pode ser alterado.");
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome do item é obrigatório.");
        if (categoriaId <= 0)
            throw new BusinessException("Categoria é obrigatória.");
        if (string.IsNullOrWhiteSpace(categoriaNomeSnapshot))
            throw new BusinessException("Categoria é obrigatória.");
        if (quantidadeMinima < 0)
            throw new BusinessException("Quantidade mínima não pode ser negativa.");
        if (custoUnitario.HasValue && custoUnitario.Value < 0)
            throw new BusinessException("Custo unitário não pode ser negativo.");

        Nome = nome.Trim();
        CategoriaId = categoriaId;
        Categoria = categoriaNomeSnapshot.Trim();
        FabricanteId = fabricanteId;
        FornecedorPadraoId = fornecedorPadraoId;
        LocalPadraoId = localPadraoId;
        UnidadeMedida = string.IsNullOrWhiteSpace(unidadeMedida) ? UnidadeMedida : unidadeMedida.Trim();
        QuantidadeMinima = quantidadeMinima;
        CustoUnitario = custoUnitario;
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Registra entrada de estoque. Recalcula <see cref="CustoMedio"/> ponderado.
    /// Retorna a movimentação criada para ser salva separadamente.
    /// </summary>
    public virtual MovimentacaoEstoque RegistrarEntrada(decimal quantidade, Guid usuarioId, decimal custoUnitario, string? observacao)
    {
        if (!Ativo)
            throw new BusinessException("Não é possível movimentar um item inativo.");
        if (quantidade <= 0)
            throw new BusinessException("Quantidade de entrada deve ser maior que zero.");
        if (custoUnitario < 0)
            throw new BusinessException("Custo unitário não pode ser negativo.");

        var anterior = QuantidadeAtual;

        // Custo médio ponderado: se não havia estoque, redefine; senão, ponderação clássica.
        if (anterior <= 0)
            CustoMedio = custoUnitario;
        else
            CustoMedio = ((anterior * CustoMedio) + (quantidade * custoUnitario)) / (anterior + quantidade);

        QuantidadeAtual += quantidade;
        AtualizadoEm = DateTime.UtcNow;

        return MovimentacaoEstoque.Criar(Id, EstabelecimentoId,
            TipoMovimentacaoEstoque.Entrada, quantidade, anterior, QuantidadeAtual, usuarioId, custoUnitario, observacao);
    }

    /// <summary>
    /// Registra saída de estoque. O custo unitário da movimentação é snapshot do
    /// <see cref="CustoMedio"/> atual — saída não altera o custo médio do item.
    /// Dispara <see cref="EstoqueAbaixoMinimoEvent"/> se necessário.
    /// Retorna a movimentação criada para ser salva separadamente.
    /// </summary>
    public virtual MovimentacaoEstoque RegistrarSaida(decimal quantidade, Guid usuarioId, string? observacao)
    {
        if (!Ativo)
            throw new BusinessException("Não é possível movimentar um item inativo.");
        if (quantidade <= 0)
            throw new BusinessException("Quantidade de saída deve ser maior que zero.");
        if (quantidade > QuantidadeAtual)
            throw new BusinessException($"Estoque insuficiente. Disponível: {QuantidadeAtual} {UnidadeMedida}.");

        var anterior = QuantidadeAtual;
        var custoUnitarioSnapshot = CustoMedio;
        QuantidadeAtual -= quantidade;
        AtualizadoEm = DateTime.UtcNow;

        if (QuantidadeAtual < QuantidadeMinima)
            AddDomainEvent(new EstoqueAbaixoMinimoEvent(Id, EstabelecimentoId, Nome, QuantidadeAtual, QuantidadeMinima));

        return MovimentacaoEstoque.Criar(Id, EstabelecimentoId,
            TipoMovimentacaoEstoque.Saida, quantidade, anterior, QuantidadeAtual, usuarioId, custoUnitarioSnapshot, observacao);
    }

    /// <summary>
    /// Inativa o item e retorna uma movimentação de auditoria (Tipo=Inativacao, sem alterar estoque).
    /// </summary>
    public virtual MovimentacaoEstoque Inativar(Guid usuarioId, string? observacao = null)
    {
        if (!Ativo) throw new BusinessException("Item já está inativo.");
        if (usuarioId == Guid.Empty)
            throw new BusinessException("Usuário responsável pela inativação é obrigatório.");

        var quantidade = QuantidadeAtual;
        Ativo = false;
        AtualizadoEm = DateTime.UtcNow;

        return MovimentacaoEstoque.Criar(Id, EstabelecimentoId,
            TipoMovimentacaoEstoque.Inativacao, 0m, quantidade, quantidade,
            usuarioId, CustoMedio, observacao);
    }

    public virtual void Reativar()
    {
        if (Ativo) throw new BusinessException("Item já está ativo.");
        Ativo = true;
        AtualizadoEm = DateTime.UtcNow;
    }

    public bool EstoqueAbaixoDoMinimo => QuantidadeAtual < QuantidadeMinima;
}
