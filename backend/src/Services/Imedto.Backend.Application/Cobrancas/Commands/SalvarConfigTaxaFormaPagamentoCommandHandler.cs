using Imedto.Backend.Contracts.Cobrancas.Commands;
using Imedto.Backend.Domain.Cobrancas;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Cobrancas.Commands;

public class SalvarConfigTaxaFormaPagamentoCommandHandler : ICommandHandler<SalvarConfigTaxaFormaPagamentoCommand>
{
    private readonly IConfigTaxaFormaPagamentoRepository _repo;

    public SalvarConfigTaxaFormaPagamentoCommandHandler(IConfigTaxaFormaPagamentoRepository repo)
        => _repo = repo;

    public async Task Handle(SalvarConfigTaxaFormaPagamentoCommand cmd)
    {
        if (cmd.Id.HasValue && cmd.Id.Value > 0)
        {
            var existente = await _repo.ObterPorIdOuNulo(cmd.Id.Value, cmd.EstabelecimentoId)
                ?? throw new BusinessException("Não encontrado.");
            existente.Atualizar(cmd.TaxaPercentual);
            if (cmd.Ativo && !existente.Ativo)
                existente.Reativar();
            else if (!cmd.Ativo && existente.Ativo)
                existente.Inativar();
            await _repo.Salvar(existente);
        }
        else
        {
            var novo = ConfigTaxaFormaPagamento.Criar(cmd.EstabelecimentoId, cmd.FormaPagamentoId, cmd.TaxaPercentual);
            await _repo.Salvar(novo);
        }
    }
}
