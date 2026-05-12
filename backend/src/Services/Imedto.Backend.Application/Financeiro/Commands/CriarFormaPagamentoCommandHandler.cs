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
        // Pré-valida unicidade do nome no estabelecimento — evita 500 da unique constraint.
        if (await _repo.ExisteComNomeNoEstabelecimento(cmd.Nome, cmd.EstabelecimentoId))
            throw new Imedto.Backend.SharedKernel.Domain.BusinessException(
                "Já existe uma forma de pagamento com este nome.");

        var forma = FormaPagamento.Criar(cmd.EstabelecimentoId, cmd.Nome);
        await _repo.Salvar(forma);
    }
}
