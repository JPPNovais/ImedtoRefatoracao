using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Prontuarios.Pendencias;

/// <summary>
/// Projeção operacional das ações de conduta marcadas pelo profissional ao salvar a evolução.
/// Não carrega conteúdo clínico — apenas tipo da ação e vínculos por id (R4/CA71 LGPD).
/// Imutável no status exceto via Concluir (R14/R15).
/// UNIQUE (evolucao_id, acao) garante idempotência na criação (R3/CA62).
/// </summary>
public class PendenciaAtendimento : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual long PacienteId { get; protected set; }
    public virtual long EvolucaoId { get; protected set; }
    public virtual long? AgendamentoId { get; protected set; }
    public virtual AcaoPendencia Acao { get; protected set; }
    public virtual StatusPendencia Status { get; protected set; }
    /// <summary>Id do documento (receita/atestado/pedido/orçamento/agendamento) que concluiu a pendência.
    /// Null em conclusão manual (R14).</summary>
    public virtual long? ReferenciaId { get; protected set; }
    public virtual DateTime? ConcluidaEm { get; protected set; }
    public virtual Guid CriadoPorUsuarioId { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    protected PendenciaAtendimento() { }

    public static PendenciaAtendimento Criar(
        long estabelecimentoId,
        long pacienteId,
        long evolucaoId,
        long? agendamentoId,
        AcaoPendencia acao,
        Guid criadoPorUsuarioId)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (pacienteId <= 0)
            throw new BusinessException("Paciente é obrigatório.");
        if (evolucaoId <= 0)
            throw new BusinessException("Evolução é obrigatória.");
        if (criadoPorUsuarioId == Guid.Empty)
            throw new BusinessException("Usuário criador é obrigatório.");

        return new PendenciaAtendimento
        {
            EstabelecimentoId = estabelecimentoId,
            PacienteId = pacienteId,
            EvolucaoId = evolucaoId,
            AgendamentoId = agendamentoId,
            Acao = acao,
            Status = StatusPendencia.Pendente,
            CriadoPorUsuarioId = criadoPorUsuarioId,
            CriadoEm = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Conclui a pendência automaticamente por gatilho de evento de domínio (R7-R11).
    /// referenciaId = id do documento que concluiu (receita/atestado/pedido/orçamento/agendamento).
    /// Idempotente: pendência já concluída permanece com a referenciaId original (R12/CA65).
    /// </summary>
    public virtual void ConcluirPorGatilho(long referenciaId)
    {
        if (Status == StatusPendencia.Concluida)
            return; // idempotente — não sobrescreve

        Status = StatusPendencia.Concluida;
        ReferenciaId = referenciaId;
        ConcluidaEm = DateTime.UtcNow;
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Conclui manualmente pelo painel (R14/CA66/CA67). referenciaId = null (sem documento associado).
    /// </summary>
    public virtual void ConcluirManualmente()
    {
        if (Status == StatusPendencia.Concluida)
            return; // idempotente

        Status = StatusPendencia.Concluida;
        ReferenciaId = null;
        ConcluidaEm = DateTime.UtcNow;
        AtualizadoEm = DateTime.UtcNow;
    }
}
