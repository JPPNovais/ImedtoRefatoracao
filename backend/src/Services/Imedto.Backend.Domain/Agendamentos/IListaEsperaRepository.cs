namespace Imedto.Backend.Domain.Agendamentos;

public interface IListaEsperaRepository
{
    Task<ListaEsperaAgendamento?> ObterPorIdOuNulo(long id);
    Task Salvar(ListaEsperaAgendamento entity);
    Task Remover(ListaEsperaAgendamento entity);
}
