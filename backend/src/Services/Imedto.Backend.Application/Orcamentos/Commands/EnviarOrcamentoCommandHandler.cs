using Imedto.Backend.Contracts.Orcamentos.Commands;
using Imedto.Backend.Domain.Orcamentos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Orcamentos.Commands;

public class EnviarOrcamentoCommandHandler : ICommandHandler<EnviarOrcamentoCommand>
{
    private readonly IOrcamentoRepository _repo;

    public EnviarOrcamentoCommandHandler(IOrcamentoRepository repo) => _repo = repo;

    public async Task Handle(EnviarOrcamentoCommand cmd)
    {
        var orcamento = await _repo.ObterPorIdCompleto(cmd.OrcamentoId);

        if (orcamento.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Orçamento não encontrado.");

        orcamento.Enviar();
        await _repo.Salvar(orcamento);
    }
}
