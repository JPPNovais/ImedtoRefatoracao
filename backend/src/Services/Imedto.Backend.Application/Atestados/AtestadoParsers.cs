using Imedto.Backend.Domain.Atestados;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Atestados;

/// <summary>
/// Conversores defensivos string → enum. Mensagens em PT-BR, BusinessException
/// (mapeado para 422 pelo <c>GlobalExceptionFilter</c>).
/// </summary>
internal static class AtestadoParsers
{
    public static TipoAtestado ParseTipo(string? tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo))
            throw new BusinessException("Tipo do atestado é obrigatório.");

        return tipo.Trim() switch
        {
            "Afastamento"     => TipoAtestado.Afastamento,
            "Comparecimento"  => TipoAtestado.Comparecimento,
            "Aptidao"         => TipoAtestado.Aptidao,
            "Aptidão"         => TipoAtestado.Aptidao,
            "Outro"           => TipoAtestado.Outro,
            _ => throw new BusinessException("Tipo de atestado inválido."),
        };
    }
}
