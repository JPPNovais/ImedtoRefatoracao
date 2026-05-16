using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Inventario.Cadastros;

/// <summary>
/// Fabricante / marca do item (Ex: Becton-Dickinson, Pfizer). Substitui o
/// campo "marca" string livre que existia no design legado.
/// </summary>
public class FabricanteEstoque : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual string Nome { get; protected set; } = string.Empty;
    public virtual string? Pais { get; protected set; }
    public virtual bool Ativo { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    protected FabricanteEstoque() { }

    public static FabricanteEstoque Criar(long estabelecimentoId, string nome, string? pais)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        ValidarNome(nome);
        ValidarPais(pais);

        return new FabricanteEstoque
        {
            EstabelecimentoId = estabelecimentoId,
            Nome = nome.Trim(),
            Pais = string.IsNullOrWhiteSpace(pais) ? null : pais.Trim(),
            Ativo = true,
            CriadoEm = DateTime.UtcNow
        };
    }

    public virtual void Atualizar(string nome, string? pais)
    {
        if (!Ativo) throw new BusinessException("Fabricante inativo não pode ser alterado.");
        ValidarNome(nome);
        ValidarPais(pais);

        Nome = nome.Trim();
        Pais = string.IsNullOrWhiteSpace(pais) ? null : pais.Trim();
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Inativar()
    {
        if (!Ativo) throw new BusinessException("Fabricante já está inativo.");
        Ativo = false;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Reativar()
    {
        if (Ativo) throw new BusinessException("Fabricante já está ativo.");
        Ativo = true;
        AtualizadoEm = DateTime.UtcNow;
    }

    private static void ValidarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome do fabricante é obrigatório.");
        if (nome.Trim().Length > 150)
            throw new BusinessException("Nome do fabricante deve ter no máximo 150 caracteres.");
    }

    private static void ValidarPais(string? pais)
    {
        if (!string.IsNullOrWhiteSpace(pais) && pais.Trim().Length > 60)
            throw new BusinessException("País deve ter no máximo 60 caracteres.");
    }
}
