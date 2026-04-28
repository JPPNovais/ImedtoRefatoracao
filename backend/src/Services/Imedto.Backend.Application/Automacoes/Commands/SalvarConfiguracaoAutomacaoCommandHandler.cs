using Imedto.Backend.Contracts.Automacoes.Commands;
using Imedto.Backend.Domain.Automacoes;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Automacoes.Commands;

public class SalvarConfiguracaoAutomacaoCommandHandler : ICommandHandler<SalvarConfiguracaoAutomacaoCommand>
{
    private readonly IConfiguracaoAutomacaoRepository _repo;

    public SalvarConfiguracaoAutomacaoCommandHandler(IConfiguracaoAutomacaoRepository repo)
        => _repo = repo;

    public async Task Handle(SalvarConfiguracaoAutomacaoCommand command)
    {
        var config = await _repo.ObterPorEstabelecimento(command.EstabelecimentoId)
            ?? ConfiguracaoAutomacao.PadraoParaEstabelecimento(command.EstabelecimentoId);

        config.Atualizar(
            command.LembretesHabilitados,
            command.HorasAntecedenciaLembrete,
            command.ExpiracaoOrcamentosHabilitada,
            command.EmailRemetente);

        await _repo.Salvar(config);
    }
}
