using Imedto.Backend.Domain.Cirurgias.Events;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Cirurgias;

/// <summary>
/// Aggregate root de um procedimento cirúrgico. Carrega o ciclo Planejado → Confirmado →
/// Realizado/Cancelado e a equipe operacional (sem valores — comissões ficam em
/// <c>OrcamentoEquipe</c>). Pode estar associado a um agendamento (mesma unidade temporal)
/// e sempre pertence a um prontuário (uma cirurgia é parte do histórico clínico).
///
/// Soft-deletable porque histórico clínico não pode ser apagado fisicamente (LGPD/CFM).
/// </summary>
public class ProcedimentoCirurgico : Entity, ISoftDeletable
{
    public virtual long PacienteId { get; protected set; }
    public virtual long ProntuarioId { get; protected set; }
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual long? AgendamentoId { get; protected set; }
    public virtual DateTime? DataAgendada { get; protected set; }
    public virtual DateTime? DataRealizada { get; protected set; }
    public virtual StatusProcedimento Status { get; protected set; }
    public virtual string CirurgiaPrincipal { get; protected set; } = string.Empty;
    public virtual string? CirurgiaCodigo { get; protected set; }
    public virtual string? DescricaoCirurgica { get; protected set; }
    public virtual FichaAnestesica? FichaAnestesica { get; protected set; }
    public virtual string? EvolucaoPosOp { get; protected set; }
    public virtual string? Observacoes { get; protected set; }
    public virtual DateTime? CanceladoEm { get; protected set; }
    public virtual string? MotivoCancelamento { get; protected set; }

    public virtual DateTime? DeletadoEm { get; protected set; }
    public virtual Guid? DeletadoPorUsuarioId { get; protected set; }

    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    public virtual List<MembroEquipeCirurgica> Equipe { get; protected set; } = new();

    protected ProcedimentoCirurgico() { }

    /// <summary>
    /// Cria um procedimento no estado <c>Planejado</c>. A equipe inicial é opcional na
    /// criação (pode ser ajustada antes da confirmação), mas é obrigatória ter ao menos
    /// um <see cref="PapelCirurgia.Cirurgiao"/> antes de marcar como <c>Realizado</c>.
    /// </summary>
    public static ProcedimentoCirurgico Planejar(
        long pacienteId,
        long prontuarioId,
        long estabelecimentoId,
        long? agendamentoId,
        string cirurgiaPrincipal,
        string? cirurgiaCodigo,
        DateTime? dataAgendada,
        IEnumerable<EquipeInicialPayload> equipeInicial)
    {
        if (pacienteId <= 0)
            throw new BusinessException("Paciente é obrigatório.");
        if (prontuarioId <= 0)
            throw new BusinessException("Prontuário é obrigatório.");
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (string.IsNullOrWhiteSpace(cirurgiaPrincipal))
            throw new BusinessException("Cirurgia principal é obrigatória.");
        if (cirurgiaPrincipal.Length > 200)
            throw new BusinessException("Cirurgia principal não pode ter mais de 200 caracteres.");
        if (cirurgiaCodigo is { Length: > 40 })
            throw new BusinessException("Código da cirurgia não pode ter mais de 40 caracteres.");

        var proc = new ProcedimentoCirurgico
        {
            PacienteId = pacienteId,
            ProntuarioId = prontuarioId,
            EstabelecimentoId = estabelecimentoId,
            AgendamentoId = agendamentoId,
            DataAgendada = dataAgendada,
            Status = StatusProcedimento.Planejado,
            CirurgiaPrincipal = cirurgiaPrincipal.Trim(),
            CirurgiaCodigo = cirurgiaCodigo?.Trim(),
            CriadoEm = DateTime.UtcNow
        };

        var ordem = 0;
        foreach (var membro in equipeInicial ?? Enumerable.Empty<EquipeInicialPayload>())
        {
            proc.Equipe.Add(MembroEquipeCirurgica.Criar(0, membro.ProfissionalUsuarioId, membro.Papel, ordem++));
        }

        ValidarEquipeSemDuplicatas(proc.Equipe);
        return proc;
    }

    /// <summary>Planejado → Confirmado. Dispara evento para notificar a equipe.</summary>
    public virtual void Confirmar()
    {
        if (Status != StatusProcedimento.Planejado)
            throw new BusinessException("Apenas procedimentos planejados podem ser confirmados.");
        if (Equipe.Count == 0)
            throw new BusinessException("Equipe é obrigatória para confirmar o procedimento.");

        Status = StatusProcedimento.Confirmado;
        AtualizadoEm = DateTime.UtcNow;

        AddDomainEvent(new ProcedimentoConfirmadoEvent(
            Id,
            EstabelecimentoId,
            PacienteId,
            CirurgiaPrincipal,
            DataAgendada,
            Equipe.Select(m => m.ProfissionalUsuarioId).Distinct().ToList()));
    }

    private static readonly IReadOnlySet<string> _viasAnestesicasValidas =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "EV", "IM", "SC", "Topica", "Inalatoria", "Oral"
        };

    /// <summary>Confirmado/Planejado → Realizado.</summary>
    public virtual void RegistrarRealizacao(
        DateTime dataRealizada,
        string? descricaoCirurgica,
        FichaAnestesica? fichaAnestesica,
        string? evolucaoPosOp)
    {
        if (Status is not (StatusProcedimento.Planejado or StatusProcedimento.Confirmado))
            throw new BusinessException("Procedimento já finalizado ou cancelado não pode ser registrado.");
        if (dataRealizada > DateTime.UtcNow.AddMinutes(5))
            throw new BusinessException("Data de realização não pode estar no futuro.");
        if (!Equipe.Any(m => m.Papel == PapelCirurgia.Cirurgiao))
            throw new BusinessException("É obrigatório ao menos um cirurgião na equipe para registrar a realização.");

        if (fichaAnestesica is not null)
            ValidarFichaAnestesica(fichaAnestesica);

        Status = StatusProcedimento.Realizado;
        DataRealizada = dataRealizada;
        DescricaoCirurgica = descricaoCirurgica?.Trim();
        FichaAnestesica = fichaAnestesica;
        EvolucaoPosOp = evolucaoPosOp?.Trim();
        AtualizadoEm = DateTime.UtcNow;
    }

    private static void ValidarFichaAnestesica(FichaAnestesica ficha)
    {
        if (ficha.InicioAnestesia.HasValue && ficha.FimAnestesia.HasValue
            && ficha.InicioAnestesia >= ficha.FimAnestesia)
            throw new BusinessException("Início da anestesia deve ser anterior ao fim da anestesia.");

        foreach (var droga in ficha.Drogas)
        {
            if (string.IsNullOrWhiteSpace(droga.Nome))
                throw new BusinessException("Nome da droga anestésica é obrigatório.");
            if (string.IsNullOrWhiteSpace(droga.Dose))
                throw new BusinessException($"Dose da droga '{droga.Nome}' é obrigatória.");
            if (!string.IsNullOrWhiteSpace(droga.Via) && !_viasAnestesicasValidas.Contains(droga.Via))
                throw new BusinessException($"Via '{droga.Via}' inválida. Permitidas: {string.Join(", ", _viasAnestesicasValidas)}.");
        }
    }

    public virtual void Cancelar(string motivo)
    {
        if (Status is StatusProcedimento.Realizado or StatusProcedimento.Cancelado)
            throw new BusinessException("Procedimento já finalizado ou cancelado não pode ser cancelado novamente.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new BusinessException("Motivo do cancelamento é obrigatório.");
        if (motivo.Length > 500)
            throw new BusinessException("Motivo do cancelamento não pode ter mais de 500 caracteres.");

        Status = StatusProcedimento.Cancelado;
        MotivoCancelamento = motivo.Trim();
        CanceladoEm = DateTime.UtcNow;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void AtualizarDescricao(
        string cirurgiaPrincipal,
        string? cirurgiaCodigo,
        string? observacoes,
        DateTime? dataAgendada,
        long? agendamentoId)
    {
        if (Status is StatusProcedimento.Realizado or StatusProcedimento.Cancelado)
            throw new BusinessException("Procedimento já finalizado ou cancelado não pode ser editado.");
        if (string.IsNullOrWhiteSpace(cirurgiaPrincipal))
            throw new BusinessException("Cirurgia principal é obrigatória.");
        if (cirurgiaPrincipal.Length > 200)
            throw new BusinessException("Cirurgia principal não pode ter mais de 200 caracteres.");
        if (cirurgiaCodigo is { Length: > 40 })
            throw new BusinessException("Código da cirurgia não pode ter mais de 40 caracteres.");
        if (observacoes is { Length: > 2000 })
            throw new BusinessException("Observações não podem ter mais de 2000 caracteres.");

        CirurgiaPrincipal = cirurgiaPrincipal.Trim();
        CirurgiaCodigo = cirurgiaCodigo?.Trim();
        Observacoes = observacoes?.Trim();
        DataAgendada = dataAgendada;
        AgendamentoId = agendamentoId;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void AdicionarMembroEquipe(Guid profissionalUsuarioId, PapelCirurgia papel)
    {
        if (Status is StatusProcedimento.Realizado or StatusProcedimento.Cancelado)
            throw new BusinessException("Equipe não pode ser alterada após realização ou cancelamento.");
        if (Equipe.Any(m => m.ProfissionalUsuarioId == profissionalUsuarioId && m.Papel == papel))
            throw new BusinessException("Profissional já está nessa função na equipe.");

        var ordem = Equipe.Count == 0 ? 0 : Equipe.Max(m => m.Ordem) + 1;
        Equipe.Add(MembroEquipeCirurgica.Criar(Id, profissionalUsuarioId, papel, ordem));
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void RemoverMembroEquipe(long membroId)
    {
        if (Status is StatusProcedimento.Realizado or StatusProcedimento.Cancelado)
            throw new BusinessException("Equipe não pode ser alterada após realização ou cancelamento.");

        var membro = Equipe.FirstOrDefault(m => m.Id == membroId)
            ?? throw new BusinessException("Membro da equipe não encontrado.");
        Equipe.Remove(membro);
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>Substitui a equipe inteira (usado em PUT /equipe).</summary>
    public virtual void SubstituirEquipe(IEnumerable<EquipeInicialPayload> nova)
    {
        if (Status is StatusProcedimento.Realizado or StatusProcedimento.Cancelado)
            throw new BusinessException("Equipe não pode ser alterada após realização ou cancelamento.");

        Equipe.Clear();
        var ordem = 0;
        foreach (var membro in nova ?? Enumerable.Empty<EquipeInicialPayload>())
            Equipe.Add(MembroEquipeCirurgica.Criar(Id, membro.ProfissionalUsuarioId, membro.Papel, ordem++));

        ValidarEquipeSemDuplicatas(Equipe);
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void MarcarComoDeletado(Guid usuarioId)
    {
        if (usuarioId == Guid.Empty)
            throw new BusinessException("Usuário responsável pela exclusão é obrigatório.");
        if (DeletadoEm is not null)
            throw new BusinessException("Procedimento já está deletado.");
        DeletadoEm = DateTime.UtcNow;
        DeletadoPorUsuarioId = usuarioId;
    }

    private static void ValidarEquipeSemDuplicatas(IEnumerable<MembroEquipeCirurgica> equipe)
    {
        var dup = equipe
            .GroupBy(m => new { m.ProfissionalUsuarioId, m.Papel })
            .FirstOrDefault(g => g.Count() > 1);
        if (dup is not null)
            throw new BusinessException("Profissional duplicado na mesma função da equipe.");
    }

    public record EquipeInicialPayload(Guid ProfissionalUsuarioId, PapelCirurgia Papel);
}
