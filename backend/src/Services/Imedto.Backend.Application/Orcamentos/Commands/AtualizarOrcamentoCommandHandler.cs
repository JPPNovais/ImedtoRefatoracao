using Imedto.Backend.Contracts.Orcamentos.Commands;
using Imedto.Backend.Domain.Orcamentos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Orcamentos.Commands;

public class AtualizarOrcamentoCommandHandler : ICommandHandler<AtualizarOrcamentoCommand>
{
    private readonly IOrcamentoRepository _repo;

    public AtualizarOrcamentoCommandHandler(IOrcamentoRepository repo) => _repo = repo;

    public async Task Handle(AtualizarOrcamentoCommand cmd)
    {
        var orcamento = await _repo.ObterPorIdComItens(cmd.OrcamentoId);

        if (orcamento.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Orçamento não encontrado neste estabelecimento.");

        var itens = cmd.Itens.Select(i =>
            new Orcamento.ItemPayload(i.Descricao, i.Quantidade, i.ValorUnitario, i.DescontoPercent));

        orcamento.Atualizar(cmd.Validade, cmd.Observacoes, itens);
        await _repo.Salvar(orcamento);
    }
}
