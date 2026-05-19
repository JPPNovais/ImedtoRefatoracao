using Imedto.Backend.Contracts.Orcamentos.Commands;
using Imedto.Backend.Domain.Orcamentos;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Orcamentos.Commands;

/// <summary>
/// Conversões entre os payloads do <c>Contracts</c> (sem dependência de Domain) e os
/// tipos do Domain (enums + payload records). Único ponto de validação dos strings
/// vindos da API (ex: <c>"IntGeral"</c> → <see cref="TipoLocalCirurgia"/>).
/// </summary>
internal static class OrcamentoMapping
{
    public static TipoLocalCirurgia ParseTipoLocal(string raw)
    {
        if (!Enum.TryParse<TipoLocalCirurgia>(raw, ignoreCase: true, out var tipo))
            throw new BusinessException($"Tipo de local cirúrgico '{raw}' inválido.");
        return tipo;
    }

    public static Orcamento.AnestesiaPayload? MapAnestesia(OrcamentoAnestesiaPayload? p)
    {
        if (p is null) return null;
        if (!Enum.TryParse<TipoAnestesia>(p.Tipo, ignoreCase: true, out var tipo))
            throw new BusinessException($"Tipo de anestesia '{p.Tipo}' inválido.");
        return new Orcamento.AnestesiaPayload(tipo, p.Valor, p.Observacao);
    }
}
