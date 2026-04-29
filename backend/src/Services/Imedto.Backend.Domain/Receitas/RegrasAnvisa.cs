using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Receitas;

/// <summary>
/// Regras regulatórias da ANVISA aplicáveis à emissão de receitas. Centraliza
/// prazos máximos de validade (Portaria 344/98 e RDC 471/2021) e a marca
/// "requer retenção" usada pela farmácia.
///
/// <para>
/// Portaria 344/98 (controlados): prazo de validade da prescrição é de
/// <b>30 dias</b> a partir da emissão para todas as classes de notificação
/// (A, B, C). Para o tipo <see cref="TipoNotificacao.Especial"/> (receituário
/// de controle especial — anabolizantes/Lista C5) o prazo é livre e fica
/// a critério do prescritor, mas a data continua sendo obrigatória.
/// </para>
///
/// <para>
/// RDC 471/2021 (antibióticos): prazo de <b>10 dias</b> a partir da emissão
/// e a receita deve ser <b>retida</b> pela farmácia.
/// </para>
///
/// <para>
/// <b>TODO (Fase 5+):</b> validação de quantidade × dias de tratamento
/// (Portaria 344/98: A → 30 dias; B/C → 60 dias; alguns C de
/// anticonvulsivantes/antiparkinsonianos → 180 dias). Não é validável no
/// backend hoje sem catálogo de medicamentos com posologia/duração estruturada
/// — manter como aviso visual no frontend para evitar bloquear receitas
/// legítimas por falta de dado.
/// </para>
/// </summary>
public static class RegrasAnvisa
{
    // Portaria 344/98 — prazo de validade da prescrição controlada (em dias).
    public const int ValidadeControladaA_Dias = 30;
    public const int ValidadeControladaB_Dias = 30;
    public const int ValidadeControladaC_Dias = 30;

    // RDC 471/2021 — prazo de validade da prescrição de antibiótico (em dias).
    public const int ValidadeAntibiotico_Dias = 10;

    /// <summary>
    /// Retorna o prazo máximo (em dias) permitido entre a emissão e a validade
    /// da prescrição. <c>null</c> quando não há regra ANVISA aplicável (receita
    /// comum, especial sem notificação, ou notificação Especial — prazo livre).
    /// </summary>
    public static int? ObterPrazoMaximoEmDias(TipoReceita tipo, TipoNotificacao? tipoNotificacao)
    {
        if (tipo == TipoReceita.Antibiotico)
            return ValidadeAntibiotico_Dias;

        if (tipo == TipoReceita.Controlada)
        {
            return tipoNotificacao switch
            {
                TipoNotificacao.A => ValidadeControladaA_Dias,
                TipoNotificacao.B => ValidadeControladaB_Dias,
                TipoNotificacao.C => ValidadeControladaC_Dias,
                TipoNotificacao.Especial => null, // prazo livre — definido pelo prescritor
                _ => null
            };
        }

        return null;
    }

    /// <summary>
    /// Calcula a data de validade máxima permitida para a prescrição dada uma
    /// data de emissão. <c>null</c> quando não há regra aplicável (a validade
    /// é livre — pode ser definida pelo prescritor ou ficar nula).
    /// </summary>
    public static DateTime? CalcularValidadeMaxima(
        TipoReceita tipo,
        TipoNotificacao? tipoNotificacao,
        DateTime emitidaEm)
    {
        var prazoEmDias = ObterPrazoMaximoEmDias(tipo, tipoNotificacao);
        return prazoEmDias is null ? null : emitidaEm.AddDays(prazoEmDias.Value);
    }

    /// <summary>
    /// Indica se a receita deve ser retida pela farmácia (não devolvida ao
    /// paciente após dispensa). True para Controladas (Portaria 344/98) e
    /// Antibióticos (RDC 471/2021).
    /// </summary>
    public static bool RequerRetencao(TipoReceita tipo) =>
        tipo == TipoReceita.Controlada || tipo == TipoReceita.Antibiotico;

    /// <summary>
    /// Valida se a <paramref name="validadeAte"/> respeita o prazo máximo
    /// previsto pela ANVISA para o <paramref name="tipo"/> e
    /// <paramref name="tipoNotificacao"/>. Lança <see cref="BusinessException"/>
    /// com mensagem citando a norma quando excede.
    /// </summary>
    public static void ValidarPrazo(
        TipoReceita tipo,
        TipoNotificacao? tipoNotificacao,
        DateTime emitidaEm,
        DateTime validadeAte)
    {
        var prazoEmDias = ObterPrazoMaximoEmDias(tipo, tipoNotificacao);
        if (prazoEmDias is null)
            return;

        var maximo = emitidaEm.AddDays(prazoEmDias.Value);
        if (validadeAte > maximo)
            throw new BusinessException(MensagemPrazoExcedido(tipo, tipoNotificacao, prazoEmDias.Value));
    }

    private static string MensagemPrazoExcedido(
        TipoReceita tipo,
        TipoNotificacao? tipoNotificacao,
        int prazoEmDias)
    {
        if (tipo == TipoReceita.Antibiotico)
            return $"Receita de antibiótico tem prazo máximo de {prazoEmDias} dias da emissão (RDC 471/2021).";

        var classe = tipoNotificacao switch
        {
            TipoNotificacao.A => "classe A",
            TipoNotificacao.B => "classe B",
            TipoNotificacao.C => "classe C",
            _ => "controlada"
        };
        return $"Receita controlada {classe} tem prazo máximo de {prazoEmDias} dias da emissão (Portaria 344/98).";
    }
}
