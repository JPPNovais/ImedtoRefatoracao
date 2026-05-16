using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Orcamentos.Catalogos;

public class OrcamentoAnestesistaFaixa : Entity
{
    public virtual long AnestesistaId { get; protected set; }
    public virtual string Descricao { get; protected set; } = string.Empty;
    public virtual decimal Valor { get; protected set; }
    public virtual int Ordem { get; protected set; }

    protected OrcamentoAnestesistaFaixa() { }

    internal static OrcamentoAnestesistaFaixa Criar(string descricao, decimal valor, int ordem)
    {
        Validar(descricao, valor);
        return new OrcamentoAnestesistaFaixa { Descricao = descricao.Trim(), Valor = valor, Ordem = ordem };
    }

    private static void Validar(string descricao, decimal valor)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            throw new BusinessException("Descrição da faixa é obrigatória.");
        if (descricao.Trim().Length > 120)
            throw new BusinessException("Descrição da faixa não pode ter mais de 120 caracteres.");
        if (valor < 0) throw new BusinessException("Valor da faixa não pode ser negativo.");
    }
}
