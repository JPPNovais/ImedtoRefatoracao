namespace Imedto.Backend.Domain.Receitas;

/// <summary>
/// Estado da assinatura digital da receita.
///
/// <list type="bullet">
///   <item><see cref="NaoAssinada"/>: default. Receita gerada apenas com a
///   identificação do profissional emissor (nome, conselho, especialidade). Não
///   tem validade jurídica como receita digital — deve ser impressa e assinada
///   manualmente quando exigido (CFM 2.299/2021 + Lei 14.063/2020).</item>
///   <item><see cref="AssinadaIcp"/>: assinada com certificado ICP-Brasil (A1/A3
///   ou nuvem) — validade jurídica plena, dispensa impressão.</item>
///   <item><see cref="AssinadaMemed"/>: assinada via integração Memed (ou similar)
///   — equivale a ICP-Brasil para uso em farmácias parceiras.</item>
/// </list>
///
/// Hoje o sistema só emite <see cref="NaoAssinada"/>; os demais valores são
/// reservados para a integração futura com provedores de assinatura. A UI deve
/// exibir aviso visível em <see cref="NaoAssinada"/> orientando o profissional
/// a assinar manualmente ao imprimir.
/// </summary>
public enum StatusAssinaturaDigital
{
    NaoAssinada = 0,
    AssinadaIcp = 1,
    AssinadaMemed = 2
}
