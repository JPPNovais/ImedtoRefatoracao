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
        var regra = await _regraRepo.ObterPorIdOuNulo(command.RegraId)
            ?? throw new BusinessException("Regra não encontrada.");

        // Genérico para evitar enumeração: mensagem igual a "não encontrado".
        if (regra.EstabelecimentoId != command.EstabelecimentoId)
            throw new BusinessException("Regra não encontrada.");

        var estab = await _estabelecimentoRepo.ObterPorId(command.EstabelecimentoId);
        if (estab.DonoUsuarioId != command.SolicitanteUsuarioId)
            throw new BusinessException("Apenas o dono do estabelecimento pode editar regras de automação.");

        regra.AtualizarRegras(command.Nome, command.EventoGatilho, command.CondicoesJson, command.AcoesJson);
        await _regraRepo.Salvar(regra);
    }
}
