using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Domain.Cobrancas.Events;

public class CobrancaCriadaEvent : IDomainEvent
{
    public long CobrancaId { get; }
    public long EstabelecimentoId { get; }
    public long PacienteId { get; }
    public TipoAtendimento TipoAtendimento { get; }
    public decimal ValorCobrado { get; }
    public DateTime OcorridoEm { get; }

    public CobrancaCriadaEvent(
        long cobrancaId,
        long estabelecimentoId,
        long pacienteId,
        TipoAtendimento tipoAtendimento,
        decimal valorCobrado)
    {
        CobrancaId = cobrancaId;
        EstabelecimentoId = estabelecimentoId;
        PacienteId = pacienteId;
        TipoAtendimento = tipoAtendimento;
        ValorCobrado = valorCobrado;
        OcorridoEm = DateTime.UtcNow;
    }
}
