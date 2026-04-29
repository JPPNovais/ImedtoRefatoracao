namespace Imedto.Backend.Domain.Orcamentos;

/// <summary>
/// Tipo de internação de um orçamento cirúrgico (item 6 — paridade com legado
/// <c>orcamento_internacao</c>). Mapeado como string no banco para preservar
/// legibilidade no Postgres.
/// </summary>
public enum TipoInternacao
{
    Apartamento,
    Enfermaria,
    UTI,
    Ambulatorial
}
