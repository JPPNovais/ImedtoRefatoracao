using Imedto.Backend.Contracts.Financeiro.Commands;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Financeiro.Commands;

public class CancelarLancamentoCommandHandler : ICommandHandler<CancelarLancamentoCommand>
{
    private readonly ILancamentoRepository _repo;

    public CancelarLancamentoCommandHandler(ILancamentoRepository repo) => _repo = repo;

    public async Task Handle(CancelarLancamentoCommand cmd)
    {
        var lancamento = await _repo.ObterPorIdOuNulo(cmd.LancamentoId)
            ?? throw new BusinessException("Lançamento não encontrado.");
        // Mensagem padronizada (defense-in-depth: nao vaza existencia cross-tenant).
        if (lancamento.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Lançamento não encontrado.");

        lancamento.Cancelar();
        await _repo.Salvar(lancamento);
    }
}
