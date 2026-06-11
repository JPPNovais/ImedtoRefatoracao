namespace Imedto.Backend.Domain.Prontuarios.Pendencias;

/// <summary>
/// Tipo de ação de conduta que originou a pendência do atendimento.
/// Os 6 itens fixos do checklist de Conduta (briefing 2026-06-10_012 §5).
/// </summary>
public enum AcaoPendencia
{
    CriarReceita,
    CriarAtestado,
    PedirExame,
    CriarOrcamento,
    MarcarProcedimentoRealizado,
    AgendarRetorno,
}
