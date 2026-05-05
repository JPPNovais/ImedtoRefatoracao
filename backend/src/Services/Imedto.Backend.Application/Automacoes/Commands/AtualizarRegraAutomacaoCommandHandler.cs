using Imedto.Backend.Contracts.Automacoes.Commands;
using Imedto.Backend.Domain.Automacoes;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Automacoes.Commands;

public class AtualizarRegraAutomacaoCommandHandler : ICommandHandler<AtualizarRegraAutomacaoCommand>
{
    private readonly IRegraAutomacaoRepository _regraRepo;
    private readonly IEstabelecimentoRepository _estabelecimentoRepo;

    public AtualizarRegraAutomacaoCommandHandler(
        IRegraAutomacaoRepository regraRepo,
        IEstabelecimentoRepository estabelecimentoRepo)
    {
        _regraRepo = regraRepo;
        _estabelecimentoRepo = estabelecimentoRepo;
    }

    public async Task Handle(AtualizarRegraAutomacaoCommand command)
    {
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var regra = await _regraRepo.ObterPorIdOuNulo(command.RegraId, command.EstabelecimentoId)
            ?? throw new BusinessException("Regra não encontrada.");

        var estab = await _estabelecimentoRepo.ObterPorIdOuNulo(command.EstabelecimentoId)
            ?? throw new BusinessException("Estabelecimento não encontrado.");
        if (estab.DonoUsuarioId != command.SolicitanteUsuarioId)
            throw new BusinessException("Apenas o dono do estabelecimento pode editar regras de automação.");

        regra.AtualizarRegras(command.Nome, command.EventoGatilho, command.CondicoesJson, command.AcoesJson);
        await _regraRepo.Salvar(regra);
    }
}
