using Imedto.Backend.Contracts.Automacoes.Commands;
using Imedto.Backend.Domain.Automacoes;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Automacoes.Commands;

/// <summary>
/// Cria regra de automação. Apenas o dono do estabelecimento pode definir regras —
/// elas afetam fluxos automáticos do tenant inteiro, então a permissão é restrita.
/// </summary>
public class CriarRegraAutomacaoCommandHandler : ICommandHandler<CriarRegraAutomacaoCommand>
{
    private readonly IRegraAutomacaoRepository _regraRepo;
    private readonly IEstabelecimentoRepository _estabelecimentoRepo;

    public CriarRegraAutomacaoCommandHandler(
        IRegraAutomacaoRepository regraRepo,
        IEstabelecimentoRepository estabelecimentoRepo)
    {
        _regraRepo = regraRepo;
        _estabelecimentoRepo = estabelecimentoRepo;
    }

    public async Task Handle(CriarRegraAutomacaoCommand command)
    {
        var estab = await _estabelecimentoRepo.ObterPorId(command.EstabelecimentoId);
        if (estab.DonoUsuarioId != command.SolicitanteUsuarioId)
            throw new BusinessException("Apenas o dono do estabelecimento pode criar regras de automação.");

        var regra = RegraAutomacao.Criar(
            command.EstabelecimentoId,
            command.Nome,
            command.EventoGatilho,
            command.CondicoesJson,
            command.AcoesJson);

        await _regraRepo.Salvar(regra);
    }
}
