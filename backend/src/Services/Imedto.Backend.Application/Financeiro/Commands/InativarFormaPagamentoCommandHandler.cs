using Imedto.Backend.Contracts.Financeiro.Commands;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Financeiro.Commands;

public class InativarFormaPagamentoCommandHandler : ICommandHandler<InativarFormaPagamentoCommand>
{
    private readonly IFormaPagamentoRepository _repo;

    public InativarFormaPagamentoCommandHandler(IFormaPagamentoRepository repo) => _repo = repo;

    public async Task Handle(InativarFormaPagamentoCommand cmd)
    {
        var forma = await _repo.ObterPorId(cmd.FormaPagamentoId);

        if (forma.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Forma de pagamento não encontrada neste estabelecimento.");

        forma.Inativar();
        await _repo.Salvar(forma);
    }
}
