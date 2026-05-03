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
        var forma = await _repo.ObterPorIdOuNulo(cmd.FormaPagamentoId)
            ?? throw new BusinessException("Forma de pagamento não encontrada.");
        // Mensagem padronizada (defense-in-depth: nao vaza existencia cross-tenant).
        if (forma.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Forma de pagamento não encontrada.");

        forma.Atualizar(cmd.Nome);
        await _repo.Salvar(forma);
    }
}
