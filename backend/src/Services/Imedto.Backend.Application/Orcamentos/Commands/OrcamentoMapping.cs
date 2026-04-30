using Imedto.Backend.Contracts.Orcamentos.Commands;
using Imedto.Backend.Domain.Orcamentos;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Orcamentos.Commands;

/// <summary>
/// Conversões entre os payloads do <c>Contracts</c> (sem dependência de Domain) e os
/// tipos do Domain (enums + payload records). Único ponto de validação dos strings
/// vindos da API (ex: <c>"Apartamento"</c> → <see cref="TipoInternacao"/>).
/// </summary>
internal static class OrcamentoMapping
{
    public static Orcamento.InternacaoPayload? MapInternacao(OrcamentoInternacaoPayload? p)
    {
        if (p is null) return null;
        if (!Enum.TryParse<TipoInternacao>(p.Tipo, ignoreCase: true, out var tipo))
            throw new BusinessException($"Tipo de internação '{p.Tipo}' inválido.");
        return new Orcamento.InternacaoPayload(tipo, p.Dias, p.ValorDiaria);
    }

    public static Orcamento.AnestesiaPayload? MapAnestesia(OrcamentoAnestesiaPayload? p)
    {
        if (p is null) return null;
        if (!Enum.TryParse<TipoAnestesia>(p.Tipo, ignoreCase: true, out var tipo))
            throw new BusinessException($"Tipo de anestesia '{p.Tipo}' inválido.");
        return new Orcamento.AnestesiaPayload(tipo, p.Valor, p.Observacao);
    }
}
