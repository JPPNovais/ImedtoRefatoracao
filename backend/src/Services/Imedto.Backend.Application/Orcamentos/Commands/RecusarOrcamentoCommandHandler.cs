using Imedto.Backend.Contracts.Orcamentos.Commands;
using Imedto.Backend.Domain.Orcamentos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Orcamentos.Commands;

public class RecusarOrcamentoCommandHandler : ICommandHandler<RecusarOrcamentoCommand>
{
    private readonly IOrcamentoRepository _repo;

    public RecusarOrcamentoCommandHandler(IOrcamentoRepository repo) => _repo = repo;

    public async Task Handle(RecusarOrcamentoCommand cmd)
    {
        var orcamento = await _repo.ObterPorId(cmd.OrcamentoId);

        if (orcamento.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Orçamento não encontrado neste estabelecimento.");

        orcamento.Recusar();
        await _repo.Salvar(orcamento);
    }
}
