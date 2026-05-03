using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Vinculos.Commands;

public class AceitarConviteCommandHandler : ICommandHandler<AceitarConviteCommand>
{
    private readonly IVinculoRepository _repository;
    private readonly IEventBus _eventBus;

    public AceitarConviteCommandHandler(IVinculoRepository repository, IEventBus eventBus)
    {
        _repository = repository;
        _eventBus = eventBus;
    }

    public async Task Handle(AceitarConviteCommand command)
    {
        var vinculo = await _repository.ObterPorIdOuNulo(command.VinculoId)
            ?? throw new BusinessException("Convite não encontrado.");

        // Só o próprio profissional convidado pode aceitar — mensagem generica
        // (nao vaza para outros usuarios qual convite eh de quem).
        if (vinculo.ProfissionalUsuarioId != command.UsuarioSolicitanteId)
            throw new BusinessException("Convite não encontrado.");

        vinculo.Aceitar();
        await _repository.Salvar(vinculo);

        foreach (var evt in vinculo.DomainEvents)
            await _eventBus.Publish(evt);

        vinculo.ClearDomainEvents();
    }
}
