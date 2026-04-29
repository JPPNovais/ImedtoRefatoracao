using Imedto.Backend.Domain.Vinculos.Events;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Vinculos;

/// <summary>
/// Solicitação inversa de vínculo: o <b>profissional</b> pede acesso a um estabelecimento
/// (a clínica, depois, aprova ou recusa). Complementa o fluxo de
/// <see cref="VinculoProfissionalEstabelecimento.Convidar"/>, em que o dono é quem inicia.
///
/// Estados (transições válidas):
///   Pendente → Aprovada (pelo dono)
///   Pendente → Recusada (pelo dono, com motivo)
///   Pendente → Cancelada (pelo próprio profissional)
///
/// Após Aprovada, um <see cref="VinculoProfissionalEstabelecimento"/> é criado por um
/// <c>IEventHandler</c>. A solicitação não é apagada — preserva histórico/auditoria.
///
/// Concorrência: a unique parcial em (profissional, estabelecimento, status='Pendente')
/// no banco evita duas solicitações pendentes simultâneas (defense-in-depth do app-level
/// check em <see cref="ISolicitacaoVinculoRepository.ObterPendentePorProfissionalEEstab"/>).
/// </summary>
public class SolicitacaoVinculo : Entity
{
    public virtual Guid ProfissionalUsuarioId { get; protected set; }
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual StatusSolicitacaoVinculo Status { get; protected set; }
    public virtual string Mensagem { get; protected set; }
    public virtual DateTime CriadaEm { get; protected set; }
    public virtual DateTime? RespondidaEm { get; protected set; }
    public virtual Guid? RespondidaPorUsuarioId { get; protected set; }
    public virtual string MotivoRecusa { get; protected set; }

    protected SolicitacaoVinculo() { }

    public static SolicitacaoVinculo Solicitar(
        Guid profissionalUsuarioId,
        long estabelecimentoId,
        string mensagem)
    {
        if (profissionalUsuarioId == Guid.Empty)
            throw new BusinessException("Profissional é obrigatório.");
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");

        var msg = string.IsNullOrWhiteSpace(mensagem) ? null : mensagem.Trim();
        if (msg != null && msg.Length > 1000)
            throw new BusinessException("Mensagem deve ter no máximo 1000 caracteres.");

        return new SolicitacaoVinculo
        {
            ProfissionalUsuarioId = profissionalUsuarioId,
            EstabelecimentoId = estabelecimentoId,
            Status = StatusSolicitacaoVinculo.Pendente,
            Mensagem = msg,
            CriadaEm = DateTime.UtcNow
        };
    }

    /// <summary>Anexa <see cref="SolicitacaoVinculoCriadaEvent"/> — chamar após persistir.</summary>
    public virtual void MarcarComoCriada()
    {
        if (Id == 0)
            throw new InvalidOperationException("Solicitação ainda não foi persistida — Id é 0.");

        AddDomainEvent(new SolicitacaoVinculoCriadaEvent(Id, ProfissionalUsuarioId, EstabelecimentoId));
    }

    public virtual void Aprovar(Guid respondidoPorUsuarioId)
    {
        if (Status != StatusSolicitacaoVinculo.Pendente)
            throw new BusinessException("Apenas solicitações pendentes podem ser aprovadas.");
        if (respondidoPorUsuarioId == Guid.Empty)
            throw new BusinessException("Usuário responsável pela aprovação é obrigatório.");

        Status = StatusSolicitacaoVinculo.Aprovada;
        RespondidaEm = DateTime.UtcNow;
        RespondidaPorUsuarioId = respondidoPorUsuarioId;

        AddDomainEvent(new SolicitacaoVinculoAprovadaEvent(
            Id, ProfissionalUsuarioId, EstabelecimentoId, respondidoPorUsuarioId));
    }

    public virtual void Recusar(Guid respondidoPorUsuarioId, string motivo)
    {
        if (Status != StatusSolicitacaoVinculo.Pendente)
            throw new BusinessException("Apenas solicitações pendentes podem ser recusadas.");
        if (respondidoPorUsuarioId == Guid.Empty)
            throw new BusinessException("Usuário responsável pela recusa é obrigatório.");

        var motivoTrim = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();
        if (motivoTrim != null && motivoTrim.Length > 500)
            throw new BusinessException("Motivo deve ter no máximo 500 caracteres.");

        Status = StatusSolicitacaoVinculo.Recusada;
        RespondidaEm = DateTime.UtcNow;
        RespondidaPorUsuarioId = respondidoPorUsuarioId;
        MotivoRecusa = motivoTrim;

        AddDomainEvent(new SolicitacaoVinculoRecusadaEvent(Id, ProfissionalUsuarioId, EstabelecimentoId));
    }

    /// <summary>Apenas o próprio profissional pode cancelar — checagem feita no handler.</summary>
    public virtual void Cancelar()
    {
        if (Status != StatusSolicitacaoVinculo.Pendente)
            throw new BusinessException("Apenas solicitações pendentes podem ser canceladas.");

        Status = StatusSolicitacaoVinculo.Cancelada;
        RespondidaEm = DateTime.UtcNow;
        // RespondidaPorUsuarioId fica nulo — cancelamento é do próprio profissional (já em ProfissionalUsuarioId).
    }
}
