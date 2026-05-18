using Imedto.Backend.Domain.Agendamentos.Events;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Agendamentos;

public class Agendamento : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual long PacienteId { get; protected set; }
    public virtual Guid ProfissionalUsuarioId { get; protected set; }
    public virtual Guid CriadoPorUsuarioId { get; protected set; }
    public virtual DateTime InicioPrevisto { get; protected set; }
    public virtual DateTime FimPrevisto { get; protected set; }
    public virtual string TipoServico { get; protected set; } = string.Empty;
    public virtual string? Observacoes { get; protected set; }
    public virtual AgendamentoStatus Status { get; protected set; }
    public virtual string? MotivoCancelamento { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }
    public virtual bool LembretePorEmailEnviado { get; protected set; }
    public virtual DateTime? CheckInEm { get; protected set; }
    public virtual long? SalaId { get; protected set; }

    protected Agendamento() { }

    public virtual void MarcarLembretePorEmailEnviado()
        => LembretePorEmailEnviado = true;

    public static Agendamento Criar(
        long estabelecimentoId,
        long pacienteId,
        Guid profissionalUsuarioId,
        Guid criadoPorUsuarioId,
        DateTime inicioPrevisto,
        DateTime fimPrevisto,
        string tipoServico,
        string? observacoes)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (pacienteId <= 0)
            throw new BusinessException("Paciente é obrigatório.");
        if (profissionalUsuarioId == Guid.Empty)
            throw new BusinessException("Profissional é obrigatório.");
        if (criadoPorUsuarioId == Guid.Empty)
            throw new BusinessException("Usuário criador é obrigatório.");
        if (inicioPrevisto >= fimPrevisto)
            throw new BusinessException("O horário de início deve ser anterior ao de término.");
        if (inicioPrevisto < DateTime.UtcNow.AddMinutes(-5))
            throw new BusinessException("Não é possível agendar no passado.");
        if (string.IsNullOrWhiteSpace(tipoServico))
            throw new BusinessException("Tipo de serviço é obrigatório.");

        return new Agendamento
        {
            EstabelecimentoId = estabelecimentoId,
            PacienteId = pacienteId,
            ProfissionalUsuarioId = profissionalUsuarioId,
            CriadoPorUsuarioId = criadoPorUsuarioId,
            InicioPrevisto = inicioPrevisto,
            FimPrevisto = fimPrevisto,
            TipoServico = tipoServico.Trim(),
            Observacoes = string.IsNullOrWhiteSpace(observacoes) ? null : observacoes.Trim(),
            Status = AgendamentoStatus.Agendado,
            CriadoEm = DateTime.UtcNow
        };
    }

    /// <summary>Anexa AgendamentoCriadoEvent — chamar após persistir o aggregate.</summary>
    public virtual void MarcarComoCriado()
    {
        if (Id == 0)
            throw new InvalidOperationException("Agendamento ainda não foi persistido — Id é 0.");
        AddDomainEvent(new AgendamentoCriadoEvent(Id, EstabelecimentoId, PacienteId, ProfissionalUsuarioId, InicioPrevisto));
    }

    public virtual void Confirmar()
    {
        if (Status != AgendamentoStatus.Agendado)
            throw new BusinessException("Apenas agendamentos com status 'Agendado' podem ser confirmados.");
        Status = AgendamentoStatus.Confirmado;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Cancelar(string motivo)
    {
        if (Status == AgendamentoStatus.Cancelado)
            throw new BusinessException("Agendamento já está cancelado.");
        if (Status == AgendamentoStatus.Concluido)
            throw new BusinessException("Não é possível cancelar um agendamento já concluído.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new BusinessException("Motivo do cancelamento é obrigatório.");

        Status = AgendamentoStatus.Cancelado;
        MotivoCancelamento = motivo.Trim();
        AtualizadoEm = DateTime.UtcNow;

        AddDomainEvent(new AgendamentoCanceladoEvent(Id, EstabelecimentoId, MotivoCancelamento));
    }

    public virtual void RegistrarCheckIn()
    {
        if (Status == AgendamentoStatus.Cancelado)
            throw new BusinessException("Não é possível fazer check-in de agendamento cancelado.");
        if (Status == AgendamentoStatus.Concluido)
            throw new BusinessException("Não é possível fazer check-in de agendamento concluído.");
        if (CheckInEm != null)
            throw new BusinessException("Check-in já foi realizado para este agendamento.");

        CheckInEm = DateTime.UtcNow;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Concluir()
    {
        if (Status == AgendamentoStatus.Cancelado)
            throw new BusinessException("Não é possível concluir um agendamento cancelado.");
        if (Status == AgendamentoStatus.Concluido)
            throw new BusinessException("Agendamento já está concluído.");

        Status = AgendamentoStatus.Concluido;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void AlocarSala(long? salaId)
    {
        if (Status == AgendamentoStatus.Cancelado)
            throw new BusinessException("Não é possível alocar sala em agendamento cancelado.");
        if (Status == AgendamentoStatus.Concluido)
            throw new BusinessException("Não é possível alocar sala em agendamento concluído.");

        SalaId = salaId;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Atualizar(
        Guid profissionalUsuarioId,
        DateTime inicioPrevisto,
        DateTime fimPrevisto,
        string tipoServico,
        string? observacoes)
    {
        if (Status == AgendamentoStatus.Cancelado)
            throw new BusinessException("Não é possível alterar um agendamento cancelado.");
        if (Status == AgendamentoStatus.Concluido)
            throw new BusinessException("Não é possível alterar um agendamento concluído.");
        if (InicioPrevisto < DateTime.UtcNow.AddMinutes(-5))
            throw new BusinessException("Não é possível alterar um agendamento que já ocorreu.");
        if (profissionalUsuarioId == Guid.Empty)
            throw new BusinessException("Profissional é obrigatório.");
        if (inicioPrevisto >= fimPrevisto)
            throw new BusinessException("O horário de início deve ser anterior ao de término.");
        if (inicioPrevisto < DateTime.UtcNow.AddMinutes(-5))
            throw new BusinessException("Não é possível agendar no passado.");
        if (string.IsNullOrWhiteSpace(tipoServico))
            throw new BusinessException("Tipo de serviço é obrigatório.");

        ProfissionalUsuarioId = profissionalUsuarioId;
        InicioPrevisto = inicioPrevisto;
        FimPrevisto = fimPrevisto;
        TipoServico = tipoServico.Trim();
        Observacoes = string.IsNullOrWhiteSpace(observacoes) ? null : observacoes.Trim();
        AtualizadoEm = DateTime.UtcNow;
    }
}
