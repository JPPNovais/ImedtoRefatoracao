using Imedto.Backend.Contracts.Orcamentos;
using Imedto.Backend.Contracts.Orcamentos.Commands;
using Imedto.Backend.Domain.Orcamentos;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Orcamentos.Commands;

/// <summary>
/// Conversões entre os payloads do <c>Contracts</c> (sem dependência de Domain) e os
/// tipos do Domain (enums + POCO de configuração). Centralizar aqui evita repetição
/// nos dois handlers (Criar/Atualizar) e fornece um único ponto de validação dos
/// strings vindos da API.
/// </summary>
internal static class OrcamentoCompletoMapping
{
    public static TipoOrcamento ParseTipoOrcamento(string tipo) =>
        Enum.TryParse<TipoOrcamento>(tipo, ignoreCase: true, out var t)
            ? t
            : throw new BusinessException($"Tipo de orçamento '{tipo}' inválido.");

    public static ConfigPagamentoOrcamento? MapConfiguracao(ConfigPagamentoOrcamentoDto? dto)
    {
        if (dto is null) return null;
        return new ConfigPagamentoOrcamento
        {
            DescontoPercentual = dto.DescontoPercentual,
            DescontoValor = dto.DescontoValor,
            JurosPercentual = dto.JurosPercentual,
            ParcelasMaximas = dto.ParcelasMaximas,
            TaxaParcela = dto.TaxaParcela,
            Observacoes = dto.Observacoes
        };
    }

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
