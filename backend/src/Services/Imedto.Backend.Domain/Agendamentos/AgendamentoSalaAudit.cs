using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Agendamentos;

/// <summary>
/// Trilha de auditoria de mudanças de sala num agendamento (quem trocou, quando, de qual para qual).
/// Uso interno/regulatório — sem endpoint de leitura por enquanto.
/// </summary>
public class AgendamentoSalaAudit : Entity
{
    public virtual long AgendamentoId { get; protected set; }
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual long? SalaIdAnterior { get; protected set; }
    public virtual long? SalaIdNova { get; protected set; }
    public virtual Guid UsuarioId { get; protected set; }
    public virtual DateTime Em { get; protected set; }

    protected AgendamentoSalaAudit() { }

    public static AgendamentoSalaAudit Registrar(
        long agendamentoId,
        long estabelecimentoId,
        long? salaIdAnterior,
        long? salaIdNova,
        Guid usuarioId)
    {
        return new AgendamentoSalaAudit
        {
            AgendamentoId = agendamentoId,
            EstabelecimentoId = estabelecimentoId,
            SalaIdAnterior = salaIdAnterior,
            SalaIdNova = salaIdNova,
            UsuarioId = usuarioId,
            Em = DateTime.UtcNow,
        };
    }
}
