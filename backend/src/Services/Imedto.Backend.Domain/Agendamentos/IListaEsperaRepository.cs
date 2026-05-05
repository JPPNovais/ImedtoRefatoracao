namespace Imedto.Backend.Domain.Agendamentos;

public interface IListaEsperaRepository
{
    /// <summary>
    /// Carrega a entrada da lista filtrando por <paramref name="estabelecimentoId"/>
    /// (defense-in-depth IDOR/LGPD). Retorna null se inexistente ou de outro tenant.
    /// </summary>
    Task<ListaEsperaAgendamento?> ObterPorIdOuNulo(long id, long estabelecimentoId);

    Task Salvar(ListaEsperaAgendamento entity);
    Task Remover(ListaEsperaAgendamento entity);
}
