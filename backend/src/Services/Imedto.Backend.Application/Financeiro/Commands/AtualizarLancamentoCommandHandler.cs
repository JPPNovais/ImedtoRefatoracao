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
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var lancamento = await _repo.ObterPorIdOuNulo(cmd.LancamentoId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Lançamento não encontrado.");

        lancamento.Atualizar(cmd.Descricao, cmd.Valor, cmd.DataVencimento, cmd.Categoria);
        await _repo.Salvar(lancamento);
    }
}
