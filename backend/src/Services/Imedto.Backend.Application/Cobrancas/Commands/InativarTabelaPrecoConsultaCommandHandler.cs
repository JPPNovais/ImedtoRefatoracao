using Imedto.Backend.Contracts.Cobrancas.Commands;
using Imedto.Backend.Domain.Cobrancas;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Cobrancas.Commands;

public class InativarTabelaPrecoConsultaCommandHandler : ICommandHandler<InativarTabelaPrecoConsultaCommand>
{
    private readonly ITabelaPrecoConsultaRepository _repo;

    public InativarTabelaPrecoConsultaCommandHandler(ITabelaPrecoConsultaRepository repo)
        => _repo = repo;

    public async Task Handle(InativarTabelaPrecoConsultaCommand cmd)
    {
        var tabela = await _repo.ObterPorIdOuNulo(cmd.Id, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Não encontrado.");
        tabela.Inativar();
        await _repo.Salvar(tabela);
    }
}
