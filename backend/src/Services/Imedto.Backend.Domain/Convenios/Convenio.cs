using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Convenios;

/// <summary>
/// Aggregate root de convênio do estabelecimento.
/// ConvenioPlano é filho, acessado via root (DDD).
/// Soft-delete via Ativo=false (R3).
/// </summary>
public class Convenio : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual string Nome { get; protected set; } = string.Empty;
    public virtual string? RegistroAns { get; protected set; }
    public virtual bool Ativo { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    private readonly List<ConvenioPlano> _planos = new();
    public virtual IReadOnlyCollection<ConvenioPlano> Planos => _planos.AsReadOnly();

    protected Convenio() { }

    public static Convenio Criar(
        long estabelecimentoId,
        string nome,
        string? registroAns)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome do convênio é obrigatório.");

        return new Convenio
        {
            EstabelecimentoId = estabelecimentoId,
            Nome = nome.Trim(),
            RegistroAns = string.IsNullOrWhiteSpace(registroAns) ? null : registroAns.Trim(),
            Ativo = true,
            CriadoEm = DateTime.UtcNow,
        };
    }

    public virtual void Atualizar(string nome, string? registroAns)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome do convênio é obrigatório.");
        Nome = nome.Trim();
        RegistroAns = string.IsNullOrWhiteSpace(registroAns) ? null : registroAns.Trim();
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Inativar()
    {
        Ativo = false;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Reativar()
    {
        Ativo = true;
        AtualizadoEm = DateTime.UtcNow;
    }

    // ── Planos (filho via root) ───────────────────────────────────────────────

    public virtual ConvenioPlano AdicionarPlano(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome do plano é obrigatório.");

        var plano = ConvenioPlano.Criar(Id, EstabelecimentoId, nome);
        _planos.Add(plano);
        AtualizadoEm = DateTime.UtcNow;
        return plano;
    }

    public virtual void InativarPlano(long planoId)
    {
        var plano = _planos.FirstOrDefault(p => p.Id == planoId)
            ?? throw new BusinessException("Plano não encontrado.");
        plano.Inativar();
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void AtualizarPlano(long planoId, string nome)
    {
        var plano = _planos.FirstOrDefault(p => p.Id == planoId)
            ?? throw new BusinessException("Plano não encontrado.");
        plano.Atualizar(nome);
        AtualizadoEm = DateTime.UtcNow;
    }
}
