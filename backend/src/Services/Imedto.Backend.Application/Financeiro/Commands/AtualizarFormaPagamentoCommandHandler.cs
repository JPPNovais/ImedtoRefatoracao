using Imedto.Backend.Contracts.Financeiro.Commands;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Financeiro.Commands;

public class AtualizarFormaPagamentoCommandHandler : ICommandHandler<AtualizarFormaPagamentoCommand>
{
    private readonly IFormaPagamentoRepository _repo;

    public AtualizarFormaPagamentoCommandHandler(IFormaPagamentoRepository repo) => _repo = repo;

    public async Task Handle(AtualizarFormaPagamentoCommand cmd)
    {
        var forma = await _repo.ObterPorId(cmd.FormaPagamentoId);

        if (forma.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Forma de pagamento não encontrada neste estabelecimento.");

        forma.Atualizar(cmd.Nome);
        await _repo.Salvar(forma);
    }
}
