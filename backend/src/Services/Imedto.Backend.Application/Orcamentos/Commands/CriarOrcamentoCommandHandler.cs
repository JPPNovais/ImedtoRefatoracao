using Imedto.Backend.Contracts.Orcamentos.Commands;
using Imedto.Backend.Domain.Orcamentos;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Orcamentos.Commands;

public class CriarOrcamentoCommandHandler : ICommandHandler<CriarOrcamentoCommand>
{
    private readonly IOrcamentoRepository _repo;
    private readonly IEventBus _events;

    public CriarOrcamentoCommandHandler(IOrcamentoRepository repo, IEventBus events)
    {
        _repo = repo;
        _events = events;
    }

    public async Task Handle(CriarOrcamentoCommand cmd)
    {
        var itens = cmd.Itens.Select(i =>
            new Orcamento.ItemPayload(i.Descricao, i.Quantidade, i.ValorUnitario, i.DescontoPercent));

        var orcamento = Orcamento.Criar(
            cmd.EstabelecimentoId,
            cmd.PacienteId,
            cmd.Validade,
            cmd.Observacoes,
            cmd.CriadoPorUsuarioId,
            itens);

        await _repo.Salvar(orcamento);

        orcamento.DefinirNumero();
        await _repo.Salvar(orcamento);

        cmd.OrcamentoIdCriado = orcamento.Id;

        foreach (var ev in orcamento.DomainEvents)
            await _events.Publish(ev);
        orcamento.ClearDomainEvents();
    }
}
