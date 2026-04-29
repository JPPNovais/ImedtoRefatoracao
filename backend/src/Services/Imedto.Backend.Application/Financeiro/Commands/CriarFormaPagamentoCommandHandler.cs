using Imedto.Backend.Contracts.Financeiro.Commands;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Financeiro.Commands;

public class CriarFormaPagamentoCommandHandler : ICommandHandler<CriarFormaPagamentoCommand>
{
    private readonly IFormaPagamentoRepository _repo;

    public CriarFormaPagamentoCommandHandler(IFormaPagamentoRepository repo) => _repo = repo;

    public async Task Handle(CriarFormaPagamentoCommand cmd)
    {
        var forma = FormaPagamento.Criar(cmd.EstabelecimentoId, cmd.Nome);
        await _repo.Salvar(forma);
    }
}
