using Imedto.Backend.Domain.Termos;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Termos;

/// <summary>
/// Parsers de strings vindas do contrato HTTP/JSON para os enums do domínio.
/// Aceita variações case-insensitive comuns ("lgpd", "LGPD", "Lgpd") — falha
/// com <see cref="BusinessException"/> em valor desconhecido.
/// </summary>
internal static class TermoParsers
{
    public static CategoriaTermo ParseCategoria(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            throw new BusinessException("Categoria é obrigatória.");

        return s.Trim().ToLowerInvariant() switch
        {
            "lgpd" => CategoriaTermo.Lgpd,
            "cirurgico" => CategoriaTermo.Cirurgico,
            "imagem" => CategoriaTermo.Imagem,
            "financeiro" => CategoriaTermo.Financeiro,
            "telemedicina" => CategoriaTermo.Telemedicina,
            "geral" => CategoriaTermo.Geral,
            _ => throw new BusinessException("Categoria inválida."),
        };
    }

    public static string SerializarCategoria(CategoriaTermo c) => c switch
    {
        CategoriaTermo.Lgpd => "lgpd",
        CategoriaTermo.Cirurgico => "cirurgico",
        CategoriaTermo.Imagem => "imagem",
        CategoriaTermo.Financeiro => "financeiro",
        CategoriaTermo.Telemedicina => "telemedicina",
        CategoriaTermo.Geral => "geral",
        _ => throw new ArgumentOutOfRangeException(nameof(c)),
    };

    public static AssinaturaTipo ParseAssinaturaTipo(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            throw new BusinessException("Tipo de assinatura é obrigatório.");

        return s.Trim().ToLowerInvariant() switch
        {
            "pdf_anexado" => AssinaturaTipo.PdfAnexado,
            "aceite_link" => AssinaturaTipo.AceiteLink,
            _ => throw new BusinessException("Tipo de assinatura inválido (use 'pdf_anexado' ou 'aceite_link')."),
        };
    }

    public static string SerializarAssinaturaTipo(AssinaturaTipo t) => t switch
    {
        AssinaturaTipo.PdfAnexado => "pdf_anexado",
        AssinaturaTipo.AceiteLink => "aceite_link",
        _ => throw new ArgumentOutOfRangeException(nameof(t)),
    };

    public static string SerializarStatus(StatusTermoEmitido s) => s switch
    {
        StatusTermoEmitido.Pendente => "pendente",
        StatusTermoEmitido.Assinado => "assinado",
        StatusTermoEmitido.Recusado => "recusado",
        StatusTermoEmitido.Revogado => "revogado",
        StatusTermoEmitido.Expirado => "expirado",
        _ => throw new ArgumentOutOfRangeException(nameof(s)),
    };
}
