using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Agendamentos;

/// <summary>
/// Entrada na lista de espera de agendamento. Paciente com interesse mas sem
/// horário fixo agendado — pode ser "encaixado" depois quando surgir uma vaga.
///
/// Após encaixar, a entrada fica registrada com <see cref="AtendidoEm"/> e
/// <see cref="AtendidoPorAgendamentoId"/> apontando para o agendamento criado.
/// O frontend só lista entradas ainda não atendidas.
/// </summary>
public class ListaEsperaAgendamento : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual long PacienteId { get; protected set; }
    public virtual string Motivo { get; protected set; } = string.Empty;
    public virtual Guid? ProfissionalPreferidoId { get; protected set; }
    public virtual ListaEsperaPrioridade Prioridade { get; protected set; }
    public virtual ListaEsperaPreferenciaPeriodo PreferenciaPeriodo { get; protected set; }
    public virtual Guid CriadoPorUsuarioId { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtendidoEm { get; protected set; }
    public virtual long? AtendidoPorAgendamentoId { get; protected set; }

    protected ListaEsperaAgendamento() { }

    public static ListaEsperaAgendamento Criar(
        long estabelecimentoId,
        long pacienteId,
        string motivo,
        Guid? profissionalPreferidoId,
        Guid criadoPorUsuarioId,
        ListaEsperaPrioridade prioridade = ListaEsperaPrioridade.Rotina,
        ListaEsperaPreferenciaPeriodo preferenciaPeriodo = ListaEsperaPreferenciaPeriodo.Qualquer)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (pacienteId <= 0)
            throw new BusinessException("Paciente é obrigatório.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new BusinessException("Motivo é obrigatório.");

        return new ListaEsperaAgendamento
        {
            EstabelecimentoId = estabelecimentoId,
            PacienteId = pacienteId,
            Motivo = motivo.Trim(),
            ProfissionalPreferidoId = profissionalPreferidoId,
            Prioridade = prioridade,
            PreferenciaPeriodo = preferenciaPeriodo,
            CriadoPorUsuarioId = criadoPorUsuarioId,
            CriadoEm = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Marca a entrada como atendida via encaixe em um agendamento. Idempotente — se
    /// já estiver atendida, lança <see cref="BusinessException"/>.
    /// </summary>
    public void Encaixar(long agendamentoId)
    {
        if (AtendidoEm is not null)
            throw new BusinessException("Esta entrada já foi atendida.");
        if (agendamentoId <= 0)
            throw new BusinessException("Agendamento inválido.");
        AtendidoEm = DateTime.UtcNow;
        AtendidoPorAgendamentoId = agendamentoId;
    }
}
