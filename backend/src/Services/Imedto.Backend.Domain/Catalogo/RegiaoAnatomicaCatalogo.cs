using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Catalogo;

/// <summary>
/// Aggregate de catálogo global de regiões anatômicas. Ref data sem multi-tenant.
/// </summary>
public class RegiaoAnatomicaCatalogo : Entity
{
    public virtual string Codigo { get; protected set; } = string.Empty;
    public virtual string Nome { get; protected set; } = string.Empty;
    public virtual string? PaiCodigo { get; protected set; }
    public virtual short Nivel { get; protected set; }
    public virtual string? Vista { get; protected set; }
    public virtual string? TemplateTexto { get; protected set; }
    public virtual string? SvgCoordsJson { get; protected set; }
    public virtual short Ordem { get; protected set; }
    public virtual bool Lateralidade { get; protected set; }
    public virtual bool Ativo { get; protected set; }

    protected RegiaoAnatomicaCatalogo() { }

    public static RegiaoAnatomicaCatalogo Criar(
        string codigo,
        string nome,
        string? paiCodigo,
        short nivel,
        string? vista,
        string? templateTexto,
        string? svgCoordsJson,
        short ordem,
        bool lateralidade)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            throw new BusinessException("Código da região anatômica é obrigatório.");
        if (codigo.Length > 60)
            throw new BusinessException("Código da região anatômica não pode exceder 60 caracteres.");
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome da região anatômica é obrigatório.");
        if (nome.Length > 120)
            throw new BusinessException("Nome da região anatômica não pode exceder 120 caracteres.");

        return new RegiaoAnatomicaCatalogo
        {
            Codigo = codigo.Trim(),
            Nome = nome.Trim(),
            PaiCodigo = paiCodigo?.Trim(),
            Nivel = nivel,
            Vista = vista?.Trim(),
            TemplateTexto = templateTexto?.Trim(),
            SvgCoordsJson = svgCoordsJson,
            Ordem = ordem,
            Lateralidade = lateralidade,
            Ativo = true
        };
    }

    public virtual void Atualizar(string nome, string? templateTexto, string? svgCoordsJson)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome da região anatômica é obrigatório.");
        if (nome.Length > 120)
            throw new BusinessException("Nome da região anatômica não pode exceder 120 caracteres.");

        Nome = nome.Trim();
        TemplateTexto = templateTexto?.Trim();
        SvgCoordsJson = svgCoordsJson;
    }

    public virtual void Inativar()
    {
        if (!Ativo) throw new BusinessException("Região anatômica já está inativa.");
        Ativo = false;
    }

    public virtual void Reativar()
    {
        if (Ativo) throw new BusinessException("Região anatômica já está ativa.");
        Ativo = true;
    }
}
