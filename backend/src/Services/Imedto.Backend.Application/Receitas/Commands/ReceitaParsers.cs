using Imedto.Backend.Contracts.Receitas.Commands;
using Imedto.Backend.Domain.Receitas;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Receitas.Commands;

/// <summary>
/// Conversores compartilhados entre os handlers do domínio Receitas. Isolados
/// aqui para evitar duplicação entre <c>EmitirReceitaCommandHandler</c>,
/// <c>IniciarRascunhoReceitaCommandHandler</c> e <c>AtualizarRascunhoReceitaCommandHandler</c>
/// — todos consomem os mesmos enums string-based dos contratos.
/// </summary>
internal static class ReceitaParsers
{
    public static TipoReceita ParseTipo(string? tipo)
    {
        if (!Enum.TryParse<TipoReceita>(tipo, ignoreCase: true, out var t))
            throw new BusinessException($"Tipo de receita inválido: {tipo}.");
        return t;
    }

    public static TipoNotificacao? ParseTipoNotificacao(string? tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo)) return null;
        if (!Enum.TryParse<TipoNotificacao>(tipo, ignoreCase: true, out var t))
            throw new BusinessException($"Tipo de notificação inválido: {tipo}.");
        return t;
    }

    public static ViaAdministracao? ParseVia(string? via)
    {
        if (string.IsNullOrWhiteSpace(via)) return null;
        if (!Enum.TryParse<ViaAdministracao>(via, ignoreCase: true, out var v))
            throw new BusinessException($"Via de administração inválida: {via}.");
        return v;
    }

    public static Receita.ItemReceitaInput ToInput(ItemReceitaPayload p) =>
        new(p.Medicamento,
            p.Posologia,
            p.Quantidade,
            ParseVia(p.Via),
            p.Observacao,
            p.Concentracao,
            p.FormaFarmaceutica,
            p.Duracao);
}
