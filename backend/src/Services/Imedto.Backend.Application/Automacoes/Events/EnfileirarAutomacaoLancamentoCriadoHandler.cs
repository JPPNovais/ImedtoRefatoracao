using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Automacoes;
using Imedto.Backend.Domain.Financeiro.Events;

namespace Imedto.Backend.Application.Automacoes.Events;

public class EnfileirarAutomacaoLancamentoCriadoHandler : EnfileirarEventosAutomacaoBase<LancamentoCriadoEvent>
{
    public EnfileirarAutomacaoLancamentoCriadoHandler(
        IRegraAutomacaoRepository regraRepo,
        IEventoAutomacaoRepository eventoRepo,
        ILogger<EnfileirarAutomacaoLancamentoCriadoHandler> logger)
        : base(regraRepo, eventoRepo, logger) { }

    protected override string NomeGatilho => "lancamento-criado";

    protected override long EstabelecimentoIdDoEvento(LancamentoCriadoEvent evento)
        => evento.EstabelecimentoId;
}
