using Imedto.Backend.Domain.Notificacoes.Events;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Notificacoes;

/// <summary>
/// Notificação in-app destinada a um usuário específico, opcionalmente vinculada
/// a um estabelecimento (ex: lembrete da agenda do estabelecimento X). Notificações
/// sem estabelecimento são "globais do usuário" — convites pendentes, avisos do sistema.
///
/// Fluxo: Criar → (visível no sino) → MarcarComoLida (idempotente).
/// </summary>
public class Notificacao : Entity
{
    public virtual Guid UsuarioId { get; protected set; }
    public virtual long? EstabelecimentoId { get; protected set; }
    public virtual string Titulo { get; protected set; } = string.Empty;
    public virtual string Mensagem { get; protected set; } = string.Empty;
    public virtual CategoriaNotificacao Categoria { get; protected set; }
    public virtual string? LinkAcao { get; protected set; }
    public virtual bool Lida { get; protected set; }
    public virtual DateTime CriadaEm { get; protected set; }
    public virtual DateTime? LidaEm { get; protected set; }

    protected Notificacao() { }

    /// <summary>
    /// Cria a notificação. Não anexa evento aqui porque <see cref="Entity.Id"/> ainda é 0;
    /// o evento é anexado por <see cref="MarcarComoCriada"/> após o repo persistir o aggregate.
    /// </summary>
    public static Notificacao Criar(
        Guid usuarioId,
        long? estabelecimentoId,
        string titulo,
        string mensagem,
        CategoriaNotificacao categoria,
        string? linkAcao = null)
    {
        if (usuarioId == Guid.Empty)
            throw new BusinessException("Usuário destinatário é obrigatório.");
        if (string.IsNullOrWhiteSpace(titulo))
            throw new BusinessException("Título da notificação é obrigatório.");
        if (string.IsNullOrWhiteSpace(mensagem))
            throw new BusinessException("Mensagem da notificação é obrigatória.");
        if (titulo.Length > 200)
            throw new BusinessException("Título não pode exceder 200 caracteres.");
        if (mensagem.Length > 1000)
            throw new BusinessException("Mensagem não pode exceder 1000 caracteres.");
        if (estabelecimentoId.HasValue && estabelecimentoId.Value <= 0)
            throw new BusinessException("Estabelecimento inválido.");
        if (linkAcao is not null && linkAcao.Length > 500)
            throw new BusinessException("Link de ação não pode exceder 500 caracteres.");

        return new Notificacao
        {
            UsuarioId = usuarioId,
            EstabelecimentoId = estabelecimentoId,
            Titulo = titulo.Trim(),
            Mensagem = mensagem.Trim(),
            Categoria = categoria,
            LinkAcao = string.IsNullOrWhiteSpace(linkAcao) ? null : linkAcao.Trim(),
            Lida = false,
            CriadaEm = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Anexa <see cref="NotificacaoCriadaEvent"/> ao aggregate — chamar após o repositório
    /// persistir e popular o <see cref="Entity.Id"/>. Mesmo padrão de
    /// <c>VinculoProfissionalEstabelecimento.MarcarComoConvidado</c> e
    /// <c>Agendamento.MarcarComoCriado</c>.
    /// </summary>
    public virtual void MarcarComoCriada()
    {
        if (Id == 0)
            throw new InvalidOperationException("Notificação ainda não foi persistida — Id é 0.");

        AddDomainEvent(new NotificacaoCriadaEvent(
            Id, UsuarioId, EstabelecimentoId, Titulo, Mensagem, Categoria, LinkAcao));
    }

    /// <summary>
    /// Marca como lida. Idempotente: chamar duas vezes não erra nem reseta o
    /// <see cref="LidaEm"/> original — preserva o instante real do primeiro acesso.
    /// </summary>
    public virtual void MarcarComoLida()
    {
        if (Lida) return;

        Lida = true;
        LidaEm = DateTime.UtcNow;
    }
}
