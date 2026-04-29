using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Vinculos.Commands;

/// <summary>
/// Profissional pede acesso a um estabelecimento.
///
/// Validações:
/// - Estabelecimento existe e está ativo (validado via <c>ObterPorId</c>).
/// - Profissional não é o próprio dono (não precisa de vínculo).
/// - Não há vínculo ativo/convidado já — não faz sentido pedir acesso a algo que já tem.
/// - Não há solicitação pendente para o mesmo par (idempotência app-level + unique parcial DB).
/// </summary>
public class SolicitarVinculoCommandHandler : ICommandHandler<SolicitarVinculoCommand>
{
    private readonly IEstabelecimentoRepository _estabelecimentoRepo;
    private readonly IVinculoRepository _vinculoRepo;
    private readonly ISolicitacaoVinculoRepository _solicitacaoRepo;
    private readonly IEventBus _eventBus;

    public SolicitarVinculoCommandHandler(
        IEstabelecimentoRepository estabelecimentoRepo,
        IVinculoRepository vinculoRepo,
        ISolicitacaoVinculoRepository solicitacaoRepo,
        IEventBus eventBus)
    {
        _estabelecimentoRepo = estabelecimentoRepo;
        _vinculoRepo = vinculoRepo;
        _solicitacaoRepo = solicitacaoRepo;
        _eventBus = eventBus;
    }

    public async Task Handle(SolicitarVinculoCommand command)
    {
        var estab = await _estabelecimentoRepo.ObterPorId(command.EstabelecimentoId);

        if (estab.DonoUsuarioId == command.ProfissionalUsuarioId)
            throw new BusinessException("O dono do estabelecimento não precisa solicitar vínculo.");

        var vinculoExistente = await _vinculoRepo.ObterPorProfissionalEEstabelecimentoOuNulo(
            command.ProfissionalUsuarioId, command.EstabelecimentoId);

        if (vinculoExistente is { Status: VinculoStatus.Ativo or VinculoStatus.Convidado })
            throw new BusinessException("Você já possui vínculo ativo ou convite pendente para este estabelecimento.");

        var pendente = await _solicitacaoRepo.ObterPendentePorProfissionalEEstab(
            command.ProfissionalUsuarioId, command.EstabelecimentoId);

        if (pendente is not null)
            throw new BusinessException("Você já tem uma solicitação pendente para este estabelecimento.");

        var solicitacao = SolicitacaoVinculo.Solicitar(
            command.ProfissionalUsuarioId,
            command.EstabelecimentoId,
            command.Mensagem);

        await _solicitacaoRepo.Salvar(solicitacao);   // popula Id
        solicitacao.MarcarComoCriada();                // anexa event com Id correto

        foreach (var evt in solicitacao.DomainEvents)
            await _eventBus.Publish(evt);

        solicitacao.ClearDomainEvents();
    }
}
