using Imedto.Backend.Contracts.Automacoes.Commands;
using Imedto.Backend.Domain.Automacoes;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Automacoes.Commands;

public class AtivarRegraAutomacaoCommandHandler : ICommandHandler<AtivarRegraAutomacaoCommand>
{
    private readonly IRegraAutomacaoRepository _regraRepo;
    private readonly IEstabelecimentoRepository _estabelecimentoRepo;

    public AtivarRegraAutomacaoCommandHandler(
        IRegraAutomacaoRepository regraRepo,
        IEstabelecimentoRepository estabelecimentoRepo)
    {
        _regraRepo = regraRepo;
        _estabelecimentoRepo = estabelecimentoRepo;
    }

    public async Task Handle(AtivarRegraAutomacaoCommand command)
    {
        var regra = await ResolverRegra(_regraRepo, _estabelecimentoRepo, command.RegraId,
            command.EstabelecimentoId, command.SolicitanteUsuarioId);
        regra.Ativar();
        await _regraRepo.Salvar(regra);
    }

    internal static async Task<RegraAutomacao> ResolverRegra(
        IRegraAutomacaoRepository regraRepo,
        IEstabelecimentoRepository estabRepo,
        long regraId,
        long estabelecimentoId,
        Guid solicitanteId)
    {
        var regra = await regraRepo.ObterPorIdOuNulo(regraId)
            ?? throw new BusinessException("Regra não encontrada.");
        if (regra.EstabelecimentoId != estabelecimentoId)
            throw new BusinessException("Regra não encontrada.");

        var estab = await estabRepo.ObterPorIdOuNulo(estabelecimentoId)
            ?? throw new BusinessException("Estabelecimento não encontrado.");
        if (estab.DonoUsuarioId != solicitanteId)
            throw new BusinessException("Apenas o dono do estabelecimento pode alterar regras de automação.");

        return regra;
    }
}

public class DesativarRegraAutomacaoCommandHandler : ICommandHandler<DesativarRegraAutomacaoCommand>
{
    private readonly IRegraAutomacaoRepository _regraRepo;
    private readonly IEstabelecimentoRepository _estabelecimentoRepo;

    public DesativarRegraAutomacaoCommandHandler(
        IRegraAutomacaoRepository regraRepo,
        IEstabelecimentoRepository estabelecimentoRepo)
    {
        _regraRepo = regraRepo;
        _estabelecimentoRepo = estabelecimentoRepo;
    }

    public async Task Handle(DesativarRegraAutomacaoCommand command)
    {
        var regra = await AtivarRegraAutomacaoCommandHandler.ResolverRegra(
            _regraRepo, _estabelecimentoRepo, command.RegraId,
            command.EstabelecimentoId, command.SolicitanteUsuarioId);
        regra.Desativar();
        await _regraRepo.Salvar(regra);
    }
}
