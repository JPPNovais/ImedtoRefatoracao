namespace Imedto.Backend.Domain.Termos;

/// <summary>
/// Categoria do termo de consentimento. Persistida como string (snake_case) via
/// HasConversion&lt;string&gt; no Configuration. Não usar valores numéricos no banco —
/// a estabilidade do enum é dada pelo nome.
/// </summary>
public enum CategoriaTermo
{
    Lgpd,
    Cirurgico,
    Imagem,
    Financeiro,
    Telemedicina,
    Geral,
}
