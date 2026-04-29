using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Automacoes.Queries;

public class ListarRegrasAutomacaoQuery : IQuery<IEnumerable<RegraAutomacaoDto>>
{
    public long EstabelecimentoId { get; set; }
}

public class ListarEventosAutomacaoQuery : IQuery<IEnumerable<EventoAutomacaoDto>>
{
    public long EstabelecimentoId { get; set; }
    public string? Status { get; set; }
    public int Pagina { get; set; } = 1;
    public int Tamanho { get; set; } = 50;
}
