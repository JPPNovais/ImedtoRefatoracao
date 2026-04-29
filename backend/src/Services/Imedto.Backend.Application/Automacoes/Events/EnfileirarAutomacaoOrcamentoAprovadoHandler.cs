using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Automacoes;
using Imedto.Backend.Domain.Orcamentos.Events;

namespace Imedto.Backend.Application.Automacoes.Events;

public class EnfileirarAutomacaoOrcamentoAprovadoHandler : EnfileirarEventosAutomacaoBase<OrcamentoAprovadoEvent>
{
    public EnfileirarAutomacaoOrcamentoAprovadoHandler(
        IRegraAutomacaoRepository regraRepo,
        IEventoAutomacaoRepository eventoRepo,
        ILogger<EnfileirarAutomacaoOrcamentoAprovadoHandler> logger)
        : base(regraRepo, eventoRepo, logger) { }

    protected override string NomeGatilho => "orcamento-aprovado";

    protected override long EstabelecimentoIdDoEvento(OrcamentoAprovadoEvent evento)
        => evento.EstabelecimentoId;
}
