using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Catalogo;

public class ProcedimentoCatalogo : Entity
{
    public virtual string Codigo { get; protected set; } = string.Empty;
    public virtual string Nome { get; protected set; } = string.Empty;
    public virtual OrigemProcedimentoCatalogo Origem { get; protected set; }
    public virtual string? Capitulo { get; protected set; }
    public virtual bool Ativo { get; protected set; }

    protected ProcedimentoCatalogo() { }

    public static ProcedimentoCatalogo Criar(string codigo, string nome, OrigemProcedimentoCatalogo origem, string? capitulo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            throw new BusinessException("Código do procedimento é obrigatório.");
        if (codigo.Length > 20)
            throw new BusinessException("Código do procedimento não pode exceder 20 caracteres.");
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome do procedimento é obrigatório.");
        if (nome.Length > 300)
            throw new BusinessException("Nome do procedimento não pode exceder 300 caracteres.");
        if (capitulo?.Length > 80)
            throw new BusinessException("Capítulo não pode exceder 80 caracteres.");

        return new ProcedimentoCatalogo
        {
            Codigo = codigo.Trim(),
            Nome = nome.Trim(),
            Origem = origem,
            Capitulo = string.IsNullOrWhiteSpace(capitulo) ? null : capitulo.Trim(),
            Ativo = true
        };
    }

    public virtual void Atualizar(string nome, string? capitulo)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome do procedimento é obrigatório.");
        if (nome.Length > 300)
            throw new BusinessException("Nome do procedimento não pode exceder 300 caracteres.");
        if (capitulo?.Length > 80)
            throw new BusinessException("Capítulo não pode exceder 80 caracteres.");

        Nome = nome.Trim();
        Capitulo = string.IsNullOrWhiteSpace(capitulo) ? null : capitulo.Trim();
    }

    public virtual void Inativar()
    {
        if (!Ativo) throw new BusinessException("Procedimento já está inativo.");
        Ativo = false;
    }

    public virtual void Reativar()
    {
        if (Ativo) throw new BusinessException("Procedimento já está ativo.");
        Ativo = true;
    }
}
