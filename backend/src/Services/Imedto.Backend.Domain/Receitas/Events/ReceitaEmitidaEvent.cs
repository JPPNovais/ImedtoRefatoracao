using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Domain.Receitas.Events;

/// <summary>
/// Disparado após emitir e persistir uma receita. Pode ser ouvido por:
/// engine de automações (regra "ao emitir receita controlada → notificar farmácia"),
/// notificações in-app, geração assíncrona de PDF.
/// </summary>
public record ReceitaEmitidaEvent(
    long ReceitaId,
    long ProntuarioId,
    long PacienteId,
    long EstabelecimentoId,
    Guid ProfissionalUsuarioId,
    TipoReceita Tipo) : IDomainEvent
{
    public DateTime OcorridoEm { get; } = DateTime.UtcNow;
}
