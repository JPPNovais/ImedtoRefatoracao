using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Domain.Financeiro.Events;

public record LancamentoCriadoEvent(
    long LancamentoId,
    long EstabelecimentoId,
    TipoLancamento Tipo,
    decimal Valor) : IDomainEvent
{
    public DateTime OcorridoEm { get; } = DateTime.UtcNow;
}
