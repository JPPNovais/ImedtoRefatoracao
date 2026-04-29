using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Financeiro;

/// <summary>
/// Forma de pagamento aceita por um estabelecimento. Formas com <see cref="Padrao"/> = true
/// são criadas pelo seed automático e não podem ser editadas/inativadas — formam o conjunto
/// mínimo (Dinheiro, PIX, Cartão de Crédito, Cartão de Débito, Transferência, Boleto).
/// </summary>
public class FormaPagamento : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual string Nome { get; protected set; } = string.Empty;
    public virtual bool Padrao { get; protected set; }
    public virtual bool Ativo { get; protected set; }
    public virtual DateTime CriadaEm { get; protected set; }
    public virtual DateTime? AtualizadaEm { get; protected set; }

    protected FormaPagamento() { }

    public static FormaPagamento Criar(long estabelecimentoId, string nome)
        => Construir(estabelecimentoId, nome, padrao: false);

    public static FormaPagamento CriarPadrao(long estabelecimentoId, string nome)
        => Construir(estabelecimentoId, nome, padrao: true);

    private static FormaPagamento Construir(long estabelecimentoId, string nome, bool padrao)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome da forma de pagamento é obrigatório.");

        return new FormaPagamento
        {
            EstabelecimentoId = estabelecimentoId,
            Nome = nome.Trim(),
            Padrao = padrao,
            Ativo = true,
            CriadaEm = DateTime.UtcNow
        };
    }

    public virtual void Atualizar(string nome)
    {
        if (Padrao)
            throw new BusinessException("Forma de pagamento padrão não pode ser editada.");
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome da forma de pagamento é obrigatório.");

        Nome = nome.Trim();
        AtualizadaEm = DateTime.UtcNow;
    }

    public virtual void Inativar()
    {
        if (Padrao)
            throw new BusinessException("Forma de pagamento padrão não pode ser inativada.");
        if (!Ativo) return;

        Ativo = false;
        AtualizadaEm = DateTime.UtcNow;
    }

    public virtual void Reativar()
    {
        if (Ativo) return;
        Ativo = true;
        AtualizadaEm = DateTime.UtcNow;
    }
}
