namespace Imedto.Backend.Domain.Orcamentos;

/// <summary>
/// Tipo de local cirúrgico do orçamento — substitui o antigo <c>TipoInternacao</c>.
///
/// Os 3 primeiros tipos calculam o valor por tempo (base + blocos adicionais) e
/// os 2 últimos têm valor fixo. A escolha define qual configuração de
/// <see cref="Catalogos.ConfiguracaoLocalCirurgia"/> usar para o cálculo.
///
/// Mapeado como string no banco para legibilidade (Postgres).
/// </summary>
public enum TipoLocalCirurgia
{
    /// <summary>Com Internação — Anestesia Local + Sedação.</summary>
    IntLocal,
    /// <summary>Com Internação — Peridural/Raqui + Sedação.</summary>
    IntPeridural,
    /// <summary>Com Internação — Anestesia Geral + TOT.</summary>
    IntGeral,
    /// <summary>Sem Internação — Anestesia Local (valor fixo).</summary>
    SemInternacao,
    /// <summary>Ambulatório — Anestesia Local (valor fixo).</summary>
    Ambulatorio,
}
