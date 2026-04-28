using Imedto.Backend.Domain.Prontuarios.Events;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Prontuarios;

/// <summary>
/// Aggregate root — container do prontuário de um paciente em um estabelecimento.
/// Unique por (paciente_id, estabelecimento_id). Guarda o template ativo (<see cref="ModeloDeProntuarioId"/>)
/// usado na próxima evolução. Evoluções antigas já guardam snapshot do modelo — se o dono trocar
/// o template, as evoluções antigas continuam rendering com o schema original.
/// </summary>
public class Prontuario : Entity
{
    public virtual long PacienteId { get; protected set; }
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual long ModeloDeProntuarioId { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    protected Prontuario() { }

    public static Prontuario Iniciar(long pacienteId, long estabelecimentoId, long modeloDeProntuarioId)
    {
        if (pacienteId <= 0)
            throw new BusinessException("Paciente é obrigatório.");
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (modeloDeProntuarioId <= 0)
            throw new BusinessException("Modelo de prontuário é obrigatório.");

        return new Prontuario
        {
            PacienteId = pacienteId,
            EstabelecimentoId = estabelecimentoId,
            ModeloDeProntuarioId = modeloDeProntuarioId,
            CriadoEm = DateTime.UtcNow
        };
    }

    public virtual void MarcarComoIniciado()
    {
        if (Id == 0)
            throw new InvalidOperationException("Prontuário ainda não foi persistido — Id é 0.");
        AddDomainEvent(new ProntuarioIniciadoEvent(Id, PacienteId, EstabelecimentoId));
    }

    /// <summary>Troca o template ativo — só afeta evoluções futuras.</summary>
    public virtual void TrocarModelo(long novoModeloId)
    {
        if (novoModeloId <= 0)
            throw new BusinessException("Modelo de prontuário é obrigatório.");
        ModeloDeProntuarioId = novoModeloId;
        AtualizadoEm = DateTime.UtcNow;
    }
}
