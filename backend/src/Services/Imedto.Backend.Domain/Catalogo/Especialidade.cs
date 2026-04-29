using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Catalogo;

public class Especialidade : Entity
{
    public virtual long ProfissaoId { get; protected set; }
    public virtual string Nome { get; protected set; } = string.Empty;
    public virtual bool Ativo { get; protected set; }

    protected Especialidade() { }

    public static Especialidade Criar(long profissaoId, string nome)
    {
        if (profissaoId <= 0)
            throw new BusinessException("Profissão é obrigatória.");
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome da especialidade é obrigatório.");
        if (nome.Length > 120)
            throw new BusinessException("Nome da especialidade não pode exceder 120 caracteres.");

        return new Especialidade
        {
            ProfissaoId = profissaoId,
            Nome = nome.Trim(),
            Ativo = true
        };
    }

    public virtual void Atualizar(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome da especialidade é obrigatório.");
        if (nome.Length > 120)
            throw new BusinessException("Nome da especialidade não pode exceder 120 caracteres.");

        Nome = nome.Trim();
    }

    public virtual void Inativar()
    {
        if (!Ativo) throw new BusinessException("Especialidade já está inativa.");
        Ativo = false;
    }

    public virtual void Reativar()
    {
        if (Ativo) throw new BusinessException("Especialidade já está ativa.");
        Ativo = true;
    }
}
