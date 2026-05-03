using Imedto.Backend.Contracts.Financeiro.Commands;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Financeiro.Commands;

public class AtualizarLancamentoCommandHandler : ICommandHandler<AtualizarLancamentoCommand>
{
    private readonly ILancamentoRepository _repo;

    public AtualizarLancamentoCommandHandler(ILancamentoRepository repo) => _repo = repo;

    public async Task Handle(AtualizarLancamentoCommand cmd)
    {
        var lancamento = await _repo.ObterPorIdOuNulo(cmd.LancamentoId)
            ?? throw new BusinessException("Lançamento não encontrado.");
        // Mensagem padronizada (defense-in-depth: nao vaza existencia cross-tenant).
        if (lancamento.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Lançamento não encontrado.");

        lancamento.Atualizar(cmd.Descricao, cmd.Valor, cmd.DataVencimento, cmd.Categoria);
        await _repo.Salvar(lancamento);
    }
}
