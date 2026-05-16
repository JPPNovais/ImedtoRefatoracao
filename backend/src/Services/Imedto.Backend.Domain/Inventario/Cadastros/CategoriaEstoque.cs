using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Inventario.Cadastros;

/// <summary>
/// Categoria de item de estoque (Anestésicos, Materiais cirúrgicos, EPIs, etc.).
/// Substitui a coluna texto livre <c>itens_inventario.categoria</c> por FK
/// (mantida durante deprecation).
/// </summary>
public class CategoriaEstoque : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual string Nome { get; protected set; } = string.Empty;
    /// <summary>Cor HSL no formato "hsl(218 70% 50%)" — usado no front para o pill.</summary>
    public virtual string Cor { get; protected set; } = string.Empty;
    /// <summary>Ícone Font Awesome (começa com "fa-"). Lista pré-aprovada no front.</summary>
    public virtual string Icone { get; protected set; } = string.Empty;
    public virtual bool Ativo { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    protected CategoriaEstoque() { }

    public static CategoriaEstoque Criar(long estabelecimentoId, string nome, string cor, string icone)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        ValidarNome(nome);
        ValidarCor(cor);
        ValidarIcone(icone);

        return new CategoriaEstoque
        {
            EstabelecimentoId = estabelecimentoId,
            Nome = nome.Trim(),
            Cor = cor.Trim(),
            Icone = icone.Trim(),
            Ativo = true,
            CriadoEm = DateTime.UtcNow
        };
    }

    public virtual void Atualizar(string nome, string cor, string icone)
    {
        if (!Ativo) throw new BusinessException("Categoria inativa não pode ser alterada.");
        ValidarNome(nome);
        ValidarCor(cor);
        ValidarIcone(icone);

        Nome = nome.Trim();
        Cor = cor.Trim();
        Icone = icone.Trim();
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Inativar()
    {
        if (!Ativo) throw new BusinessException("Categoria já está inativa.");
        Ativo = false;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Reativar()
    {
        if (Ativo) throw new BusinessException("Categoria já está ativa.");
        Ativo = true;
        AtualizadoEm = DateTime.UtcNow;
    }

    private static void ValidarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome da categoria é obrigatório.");
        if (nome.Trim().Length > 80)
            throw new BusinessException("Nome da categoria deve ter no máximo 80 caracteres.");
    }

    private static void ValidarCor(string cor)
    {
        if (string.IsNullOrWhiteSpace(cor))
            throw new BusinessException("Cor da categoria é obrigatória.");
        // hsl(218 70% 50%) — espaços e percentuais
        if (!System.Text.RegularExpressions.Regex.IsMatch(cor.Trim(),
                @"^hsl\(\s*\d{1,3}\s+\d{1,3}%\s+\d{1,3}%\s*\)$"))
            throw new BusinessException("Cor deve estar no formato 'hsl(<matiz> <sat>% <lum>%)'.");
    }

    private static void ValidarIcone(string icone)
    {
        if (string.IsNullOrWhiteSpace(icone))
            throw new BusinessException("Ícone da categoria é obrigatório.");
        if (!icone.Trim().StartsWith("fa-"))
            throw new BusinessException("Ícone deve começar com 'fa-' (Font Awesome).");
        if (icone.Trim().Length > 40)
            throw new BusinessException("Nome do ícone é muito longo.");
    }
}
