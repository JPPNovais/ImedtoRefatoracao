using Imedto.Backend.Contracts.Cobrancas.Commands;
using Imedto.Backend.Domain.Cobrancas;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Cobrancas.Commands;

public class SalvarTabelaPrecoConsultaCommandHandler : ICommandHandler<SalvarTabelaPrecoConsultaCommand>
{
    private readonly ITabelaPrecoConsultaRepository _repo;

    public SalvarTabelaPrecoConsultaCommandHandler(ITabelaPrecoConsultaRepository repo)
        => _repo = repo;

    public async Task Handle(SalvarTabelaPrecoConsultaCommand cmd)
    {
        if (cmd.Id.HasValue && cmd.Id.Value > 0)
        {
            var existente = await _repo.ObterPorIdOuNulo(cmd.Id.Value, cmd.EstabelecimentoId)
                ?? throw new BusinessException("Não encontrado.");
            existente.Atualizar(cmd.ValorSugerido);
            await _repo.Salvar(existente);
        }
        else
        {
            var novo = TabelaPrecoConsulta.Criar(cmd.EstabelecimentoId, cmd.ProfissionalId, cmd.ValorSugerido);
            await _repo.Salvar(novo);
        }
    }
}
