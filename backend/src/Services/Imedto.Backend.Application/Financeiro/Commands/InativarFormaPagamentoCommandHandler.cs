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
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var forma = await _repo.ObterPorIdOuNulo(cmd.FormaPagamentoId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Forma de pagamento não encontrada.");

        forma.Inativar();
        await _repo.Salvar(forma);
    }
}
