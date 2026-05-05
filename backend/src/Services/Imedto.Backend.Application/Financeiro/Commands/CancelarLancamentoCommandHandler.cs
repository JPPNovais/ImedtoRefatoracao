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
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var lancamento = await _repo.ObterPorIdOuNulo(cmd.LancamentoId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Lançamento não encontrado.");

        lancamento.Cancelar();
        await _repo.Salvar(lancamento);
    }
}
