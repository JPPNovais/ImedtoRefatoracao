namespace Imedto.Backend.Domain.Automacoes;

public interface IEventoAutomacaoRepository
{
    Task<EventoAutomacao?> ObterPorIdOuNulo(long id);

    /// <summary>
    /// Lista eventos pendentes prontos para execução (<c>Pendente</c> + <c>ExecutarEm &lt;= agora</c>).
    /// Worker chama isto a cada poll. Ordenado por <c>ExecutarEm</c> para favorecer atrasados.
    /// </summary>
    Task<List<EventoAutomacao>> ListarPendentesProntos(DateTime agora);

    /// <summary>
    /// Listagem para tela de debugging do dono (item 2.2 admin), com filtros opcionais.
    /// Retorna paginado para não puxar a fila inteira.
    /// </summary>
    Task<List<EventoAutomacao>> ListarParaDebug(long estabelecimentoId, string? status, int pagina, int tamanho);

    Task Salvar(EventoAutomacao evento);
}
