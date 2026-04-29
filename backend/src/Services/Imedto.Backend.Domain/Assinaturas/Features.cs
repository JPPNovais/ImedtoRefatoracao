namespace Imedto.Backend.Domain.Assinaturas;

/// <summary>
/// Constantes das features gated por plano. Os valores são chaves estáveis salvas no
/// <c>features_json</c> de cada <see cref="Plano"/> e usadas pelo
/// <see cref="IAssinaturaService.TenantTemFeature"/> e pelo <c>[FeatureGate]</c> da API.
///
/// Não renomear sem migration de dados — se uma feature precisar ser renomeada, adicionar
/// alias no <see cref="Plano.TemFeature"/> ou rodar UPDATE no <c>features_json</c>.
/// </summary>
public static class Features
{
    public const string Receitas = "receitas";
    public const string ExameFisico = "exame_fisico";
    public const string ProcedimentosCirurgicos = "procedimentos_cirurgicos";
    public const string OrcamentoCompleto = "orcamento_completo";
    public const string Ia = "ia";
    public const string RelatoriosAvancados = "relatorios_avancados";
    public const string AutomacoesIlimitadas = "automacoes_ilimitadas";
    public const string AnexosIlimitados = "anexos_ilimitados";
}
