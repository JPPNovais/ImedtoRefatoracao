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

    Task Salvar(Cobranca cobranca);
}
