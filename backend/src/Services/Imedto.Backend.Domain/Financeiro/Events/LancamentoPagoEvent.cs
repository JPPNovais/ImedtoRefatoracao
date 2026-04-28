using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Domain.Financeiro.Events;

public record LancamentoPagoEvent(
    long LancamentoId,
    long EstabelecimentoId,
    TipoLancamento Tipo,
    decimal Valor,
    DateOnly DataPagamento) : IDomainEvent
{
    public DateTime OcorridoEm { get; } = DateTime.UtcNow;
}
