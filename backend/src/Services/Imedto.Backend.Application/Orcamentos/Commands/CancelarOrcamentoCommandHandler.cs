using Imedto.Backend.Contracts.Orcamentos.Commands;
using Imedto.Backend.Domain.Orcamentos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Orcamentos.Commands;

public class CancelarOrcamentoCommandHandler : ICommandHandler<CancelarOrcamentoCommand>
{
    private readonly IOrcamentoRepository _repo;

    public CancelarOrcamentoCommandHandler(IOrcamentoRepository repo) => _repo = repo;

    public async Task Handle(CancelarOrcamentoCommand cmd)
    {
        var orcamento = await _repo.ObterPorIdOuNulo(cmd.OrcamentoId)
            ?? throw new BusinessException("Orçamento não encontrado.");
        // Mensagem padronizada (defense-in-depth: nao vaza existencia cross-tenant).
        if (orcamento.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Orçamento não encontrado.");

        orcamento.Cancelar();
        await _repo.Salvar(orcamento);
    }
}
