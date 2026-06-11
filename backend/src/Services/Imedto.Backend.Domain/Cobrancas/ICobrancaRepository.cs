namespace Imedto.Backend.Domain.Cobrancas;

public interface ICobrancaRepository
{
    /// <summary>
    /// Carrega cobrança com todos os pagamentos e estornos, filtrando por tenant (falha-fechada R14).
    /// Retorna null se não encontrada ou de outro tenant.
    /// </summary>
    Task<Cobranca?> ObterPorIdOuNulo(long id, long estabelecimentoId);

    /// <summary>Carrega cobrança pelo agendamento (com pagamentos e estornos). Retorna null se inexistente.</summary>
    Task<Cobranca?> ObterPorAgendamentoOuNulo(long agendamentoId, long estabelecimentoId);

    /// <summary>
    /// Carrega cobrança de cirurgia pelo orçamento (com pagamentos, estornos e histórico de valor).
    /// Usada pelo OrcamentoAprovadoEventHandler para idempotência (R6/F5).
    /// Retorna null se inexistente ou de outro tenant.
    /// </summary>
    Task<Cobranca?> ObterPorOrcamentoOuNulo(long orcamentoId, long estabelecimentoId);

    Task Salvar(Cobranca cobranca);
}
