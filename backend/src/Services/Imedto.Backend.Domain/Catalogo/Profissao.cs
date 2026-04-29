using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Catalogo;

public class Profissao : Entity
{
    public virtual string Nome { get; protected set; } = string.Empty;
    public virtual string? ConselhoSigla { get; protected set; }
    public virtual bool Ativo { get; protected set; }

    protected Profissao() { }

    public static Profissao Criar(string nome, string? conselhoSigla)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome da profissão é obrigatório.");
        if (nome.Length > 80)
            throw new BusinessException("Nome da profissão não pode exceder 80 caracteres.");
        if (conselhoSigla?.Length > 10)
            throw new BusinessException("Sigla do conselho não pode exceder 10 caracteres.");

        return new Profissao
        {
            Nome = nome.Trim(),
            ConselhoSigla = string.IsNullOrWhiteSpace(conselhoSigla) ? null : conselhoSigla.Trim().ToUpperInvariant(),
            Ativo = true
        };
    }

    public virtual void Atualizar(string nome, string? conselhoSigla)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome da profissão é obrigatório.");
        if (nome.Length > 80)
            throw new BusinessException("Nome da profissão não pode exceder 80 caracteres.");
        if (conselhoSigla?.Length > 10)
            throw new BusinessException("Sigla do conselho não pode exceder 10 caracteres.");

        Nome = nome.Trim();
        ConselhoSigla = string.IsNullOrWhiteSpace(conselhoSigla) ? null : conselhoSigla.Trim().ToUpperInvariant();
    }

    public virtual void Inativar()
    {
        if (!Ativo) throw new BusinessException("Profissão já está inativa.");
        Ativo = false;
    }

    public virtual void Reativar()
    {
        if (Ativo) throw new BusinessException("Profissão já está ativa.");
        Ativo = true;
    }
}
