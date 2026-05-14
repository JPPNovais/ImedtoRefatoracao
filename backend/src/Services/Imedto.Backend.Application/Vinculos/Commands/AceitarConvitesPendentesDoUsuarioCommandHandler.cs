using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Vinculos.Commands;

/// <summary>
/// Aceita TODOS os vínculos pendentes do profissional autenticado. Usado pelo
/// fluxo de aceite de convite por link de e-mail — completa o onboarding em
/// um único passo: definir senha → confirmar e-mail → ativar vínculo(s).
///
/// Idempotente: nenhuma pendência = no-op. Erros em vínculos individuais
/// (caso o status mude entre a leitura e o save por race) interrompem o lote
/// para preservar consistência transacional do UnitOfWork.
/// </summary>
public class AceitarConvitesPendentesDoUsuarioCommandHandler
    : ICommandHandler<AceitarConvitesPendentesDoUsuarioCommand>
{
    private readonly IVinculoRepository _repository;
    private readonly IEventBus _eventBus;

    public AceitarConvitesPendentesDoUsuarioCommandHandler(
        IVinculoRepository repository,
        IEventBus eventBus)
    {
        _repository = repository;
        _eventBus = eventBus;
    }

    public async Task Handle(AceitarConvitesPendentesDoUsuarioCommand command)
    {
        if (command.ProfissionalUsuarioId == Guid.Empty)
            throw new BusinessException("Usuário inválido.");

        var pendentes = await _repository.ListarPendentesPorUsuario(command.ProfissionalUsuarioId);
        if (pendentes.Count == 0) return;

        foreach (var vinculo in pendentes)
        {
            vinculo.Aceitar();
            await _repository.Salvar(vinculo);

            foreach (var evt in vinculo.DomainEvents)
                await _eventBus.Publish(evt);

            vinculo.ClearDomainEvents();
        }
    }
}
