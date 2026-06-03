using System.Security.Cryptography;
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

    // Fase 2 — confirmação por link público
    public virtual string? TokenConfirmacao { get; protected set; }
    public virtual DateTime? TokenConfirmacaoExpiraEm { get; protected set; }
    public virtual DateTime? ConfirmadoPorLinkEm { get; protected set; }

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
        // R7: bloqueia Cancelado/Concluído e agendamento já ocorrido.
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

        // Comparar ANTES de sobrescrever — R1/R5/R6.
        var mudouHorarioProfissional =
            profissionalUsuarioId != ProfissionalUsuarioId
            || inicioPrevisto != InicioPrevisto
            || fimPrevisto != FimPrevisto;

        // R1: Confirmado com mudança de horário/profissional → volta a Agendado.
        // R3: zerar lembrete ao resetar.
        if (Status == AgendamentoStatus.Confirmado && mudouHorarioProfissional)
        {
            Status = AgendamentoStatus.Agendado;
            LembretePorEmailEnviado = false;
        }

        // R6: Agendado com mudança de horário/profissional → zerar lembrete
        // (lembrete antigo pode estar marcado para horário desatualizado).
        if (Status == AgendamentoStatus.Agendado && mudouHorarioProfissional)
            LembretePorEmailEnviado = false;

        ProfissionalUsuarioId = profissionalUsuarioId;
        InicioPrevisto = inicioPrevisto;
        FimPrevisto = fimPrevisto;
        TipoServico = tipoServico.Trim();
        Observacoes = string.IsNullOrWhiteSpace(observacoes) ? null : observacoes.Trim();
        AtualizadoEm = DateTime.UtcNow;

        // R4/R5: evento informativo quando muda horário ou profissional,
        // independente do status de origem (Agendado ou Confirmado).
        if (mudouHorarioProfissional)
            AddDomainEvent(new AgendamentoReagendadoEvent(
                Id, EstabelecimentoId, PacienteId, ProfissionalUsuarioId, InicioPrevisto));
    }

    // ─── Fase 2: link público de confirmação ──────────────────────────────────

    /// <summary>
    /// Gera token de confirmação url-safe (256 bits / 32 bytes, RFC 4648 §5 sem padding).
    /// Expira em <c>min(InicioPrevisto, agora + ttl)</c> — nunca além do início do agendamento.
    /// Default TTL: 7 dias. Sobrescreve token anterior se existir (reagendamento repete o link).
    /// </summary>
    public virtual void GerarTokenConfirmacao(TimeSpan? ttl = null)
    {
        var ttlEfetivo = ttl ?? TimeSpan.FromDays(7);
        var expiraCandidato = DateTime.UtcNow.Add(ttlEfetivo);
        TokenConfirmacao = GerarTokenUrlSafe(32);
        // R11: nunca além de InicioPrevisto.
        TokenConfirmacaoExpiraEm = expiraCandidato < InicioPrevisto ? expiraCandidato : InicioPrevisto;
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Confirma presença via link público anônimo (R13).
    /// Só transita Agendado → Confirmado com token válido e não expirado.
    /// Idempotência: se já Confirmado, lança <see cref="AgendamentoJaConfirmadoException"/> —
    /// o command handler trata separadamente e devolve 200 (não erro).
    /// </summary>
    public virtual void ConfirmarPorLinkPublico(string? ipOrigem, string? userAgent)
    {
        if (Status == AgendamentoStatus.Confirmado)
            throw new AgendamentoJaConfirmadoException();

        if (Status != AgendamentoStatus.Agendado)
            throw new BusinessException(MensagemLinkInvalido);

        if (string.IsNullOrWhiteSpace(TokenConfirmacao)
            || TokenConfirmacaoExpiraEm is null
            || TokenConfirmacaoExpiraEm < DateTime.UtcNow)
            throw new BusinessException(MensagemLinkInvalido);

        Status = AgendamentoStatus.Confirmado;
        ConfirmadoPorLinkEm = DateTime.UtcNow;
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>Mensagem genérica usada em todos os erros do fluxo público (anti-enumeração).</summary>
    public const string MensagemLinkInvalido =
        "Este link expirou ou não é mais válido. Entre em contato com o estabelecimento.";

    private static string GerarTokenUrlSafe(int bytes)
    {
        var buffer = RandomNumberGenerator.GetBytes(bytes);
        return Convert.ToBase64String(buffer)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
