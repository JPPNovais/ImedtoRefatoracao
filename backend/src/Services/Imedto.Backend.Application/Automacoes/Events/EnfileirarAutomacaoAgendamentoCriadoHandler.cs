using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Agendamentos.Events;
using Imedto.Backend.Domain.Automacoes;

namespace Imedto.Backend.Application.Automacoes.Events;

public class EnfileirarAutomacaoAgendamentoCriadoHandler : EnfileirarEventosAutomacaoBase<AgendamentoCriadoEvent>
{
    public EnfileirarAutomacaoAgendamentoCriadoHandler(
        IRegraAutomacaoRepository regraRepo,
        IEventoAutomacaoRepository eventoRepo,
        ILogger<EnfileirarAutomacaoAgendamentoCriadoHandler> logger)
        : base(regraRepo, eventoRepo, logger) { }

    protected override string NomeGatilho => "agendamento-criado";

    protected override long EstabelecimentoIdDoEvento(AgendamentoCriadoEvent evento)
        => evento.EstabelecimentoId;
}
