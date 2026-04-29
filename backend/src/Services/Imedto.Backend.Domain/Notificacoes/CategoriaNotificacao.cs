namespace Imedto.Backend.Domain.Notificacoes;

/// <summary>
/// Categoria da notificação — usada para agrupar/filtrar no sino do frontend e
/// (futuramente) selecionar canal de entrega (push, email). Persistida como string
/// no Postgres para legibilidade direta na tabela.
/// </summary>
public enum CategoriaNotificacao
{
    /// <summary>Convite para vínculo profissional × estabelecimento.</summary>
    Convite,

    /// <summary>Eventos de agenda (lembrete, cancelamento, confirmação).</summary>
    Agenda,

    /// <summary>Eventos financeiros (lançamento criado, vencimento, pagamento).</summary>
    Financeiro,

    /// <summary>Mensagens do próprio sistema (manutenção, aviso técnico).</summary>
    Sistema,

    /// <summary>Notificações geradas pela engine de automações (regras configuradas pelo dono).</summary>
    Automacao
}
