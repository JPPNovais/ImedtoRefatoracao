using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Convenios;

/// <summary>
/// Plano de convênio — filho do aggregate Convenio. Acessado sempre via root.
/// Denormaliza EstabelecimentoId para filtro multi-tenant direto nas queries.
/// </summary>
public class ConvenioPlano : Entity
{
    public virtual long ConvenioId { get; protected set; }
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual string Nome { get; protected set; } = string.Empty;
    public virtual bool Ativo { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }

    protected ConvenioPlano() { }

    internal static ConvenioPlano Criar(long convenioId, long estabelecimentoId, string nome)
    {
        return new ConvenioPlano
        {
            ConvenioId = convenioId,
            EstabelecimentoId = estabelecimentoId,
            Nome = nome.Trim(),
            Ativo = true,
            CriadoEm = DateTime.UtcNow,
        };
    }

    internal void Inativar()
    {
        Ativo = false;
    }

    internal void Atualizar(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome do plano é obrigatório.");
        Nome = nome.Trim();
    }
}
