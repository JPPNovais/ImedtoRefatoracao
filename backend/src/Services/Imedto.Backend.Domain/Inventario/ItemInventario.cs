using Imedto.Backend.Domain.Inventario.Events;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Inventario;

public class ItemInventario : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual string Codigo { get; protected set; } = string.Empty;
    public virtual string Nome { get; protected set; } = string.Empty;
    public virtual string Categoria { get; protected set; } = string.Empty;
    public virtual string UnidadeMedida { get; protected set; } = string.Empty;
    public virtual decimal QuantidadeAtual { get; protected set; }
    public virtual decimal QuantidadeMinima { get; protected set; }
    public virtual bool Ativo { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    protected ItemInventario() { }

    public static ItemInventario Criar(
        long estabelecimentoId,
        string codigo,
        string nome,
        string categoria,
        string unidadeMedida,
        decimal quantidadeMinima)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (string.IsNullOrWhiteSpace(codigo))
            throw new BusinessException("Código do item é obrigatório.");
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome do item é obrigatório.");
        if (string.IsNullOrWhiteSpace(categoria))
            throw new BusinessException("Categoria é obrigatória.");
        if (string.IsNullOrWhiteSpace(unidadeMedida))
            throw new BusinessException("Unidade de medida é obrigatória.");
        if (quantidadeMinima < 0)
            throw new BusinessException("Quantidade mínima não pode ser negativa.");

        return new ItemInventario
        {
            EstabelecimentoId = estabelecimentoId,
            Codigo = codigo.Trim().ToUpperInvariant(),
            Nome = nome.Trim(),
            Categoria = categoria.Trim(),
            UnidadeMedida = unidadeMedida.Trim(),
            QuantidadeAtual = 0,            // a entrada inicial é registrada pelo handler
            QuantidadeMinima = quantidadeMinima,
            Ativo = true,
            CriadoEm = DateTime.UtcNow
        };
    }

    public virtual void Atualizar(
        string nome,
        string categoria,
        string unidadeMedida,
        decimal quantidadeMinima)
    {
        if (!Ativo)
            throw new BusinessException("Item inativo não pode ser alterado.");
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome do item é obrigatório.");
        if (string.IsNullOrWhiteSpace(categoria))
            throw new BusinessException("Categoria é obrigatória.");
        if (quantidadeMinima < 0)
            throw new BusinessException("Quantidade mínima não pode ser negativa.");

        Nome = nome.Trim();
        Categoria = categoria.Trim();
        UnidadeMedida = string.IsNullOrWhiteSpace(unidadeMedida) ? UnidadeMedida : unidadeMedida.Trim();
        QuantidadeMinima = quantidadeMinima;
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Registra entrada de estoque. Retorna a movimentação criada para ser salva separadamente.
    /// </summary>
    public virtual MovimentacaoEstoque RegistrarEntrada(decimal quantidade, Guid usuarioId, string? observacao)
    {
        if (!Ativo)
            throw new BusinessException("Não é possível movimentar um item inativo.");
        if (quantidade <= 0)
            throw new BusinessException("Quantidade de entrada deve ser maior que zero.");

        var anterior = QuantidadeAtual;
        QuantidadeAtual += quantidade;
        AtualizadoEm = DateTime.UtcNow;

        return MovimentacaoEstoque.Criar(Id, EstabelecimentoId,
            TipoMovimentacaoEstoque.Entrada, quantidade, anterior, QuantidadeAtual, usuarioId, observacao);
    }

    /// <summary>
    /// Registra saída de estoque. Dispara <see cref="EstoqueAbaixoMinimoEvent"/> se necessário.
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
        QuantidadeAtual -= quantidade;
        AtualizadoEm = DateTime.UtcNow;

        if (QuantidadeAtual < QuantidadeMinima)
            AddDomainEvent(new EstoqueAbaixoMinimoEvent(Id, EstabelecimentoId, Nome, QuantidadeAtual, QuantidadeMinima));

        return MovimentacaoEstoque.Criar(Id, EstabelecimentoId,
            TipoMovimentacaoEstoque.Saida, quantidade, anterior, QuantidadeAtual, usuarioId, observacao);
    }

    public virtual void Inativar()
    {
        if (!Ativo) throw new BusinessException("Item já está inativo.");
        Ativo = false;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Reativar()
    {
        if (Ativo) throw new BusinessException("Item já está ativo.");
        Ativo = true;
        AtualizadoEm = DateTime.UtcNow;
    }

    public bool EstoqueAbaixoDoMinimo => QuantidadeAtual < QuantidadeMinima;
}
