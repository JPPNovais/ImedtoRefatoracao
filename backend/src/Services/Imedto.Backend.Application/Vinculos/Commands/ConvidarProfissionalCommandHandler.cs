using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Vinculos.Commands;

public class ConvidarProfissionalCommandHandler : ICommandHandler<ConvidarProfissionalCommand>
{
    private readonly IEstabelecimentoRepository _estabelecimentoRepo;
    private readonly IModeloPermissaoRepository _modeloRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IVinculoRepository _vinculoRepo;
    private readonly IEventBus _eventBus;

    public ConvidarProfissionalCommandHandler(
        IEstabelecimentoRepository estabelecimentoRepo,
        IModeloPermissaoRepository modeloRepo,
        IUsuarioRepository usuarioRepo,
        IVinculoRepository vinculoRepo,
        IEventBus eventBus)
    {
        _estabelecimentoRepo = estabelecimentoRepo;
        _modeloRepo = modeloRepo;
        _usuarioRepo = usuarioRepo;
        _vinculoRepo = vinculoRepo;
        _eventBus = eventBus;
    }

    public async Task Handle(ConvidarProfissionalCommand command)
    {
        var estab = await _estabelecimentoRepo.ObterPorId(command.EstabelecimentoId);

        if (estab.DonoUsuarioId != command.ConvidadoPorUsuarioId)
            throw new BusinessException("Apenas o dono do estabelecimento pode convidar profissionais.");

        if (command.ProfissionalUsuarioId == estab.DonoUsuarioId)
            throw new BusinessException("O dono do estabelecimento não precisa de convite.");

        // Resolve o modelo de permissão (explícito ou padrão do estabelecimento).
        long modeloId;
        if (command.ModeloPermissaoId is { } explicitId && explicitId > 0)
        {
            if (!await _modeloRepo.PertenceAoEstabelecimento(explicitId, command.EstabelecimentoId))
                throw new BusinessException("Modelo de permissão não pertence a este estabelecimento.");
            modeloId = explicitId;
        }
        else
        {
            var padrao = await _modeloRepo.ObterPadraoDoEstabelecimento(command.EstabelecimentoId);
            if (padrao is null)
                throw new BusinessException("Estabelecimento não possui modelo de permissão padrão.");
            modeloId = padrao.Id;
        }

        // Garante registro local do profissional (idempotente — pode já existir).
        var usuarioExistente = await _usuarioRepo.ObterPorIdOuNulo(command.ProfissionalUsuarioId);
        if (usuarioExistente is null)
        {
            var novo = Usuario.Criar(command.ProfissionalUsuarioId, command.ProfissionalEmail);
            await _usuarioRepo.Salvar(novo);
        }

        // Um mesmo profissional não pode ter dois vínculos não-inativos com o mesmo estabelecimento.
        var vinculoExistente = await _vinculoRepo.ObterVinculoAtivoOuPendente(
            command.ProfissionalUsuarioId, command.EstabelecimentoId);
        if (vinculoExistente is not null)
            throw new BusinessException("Este profissional já tem um vínculo ativo ou convite pendente para este estabelecimento.");

        var vinculo = VinculoProfissionalEstabelecimento.Convidar(
            command.ProfissionalUsuarioId,
            command.EstabelecimentoId,
            modeloId,
            command.ConvidadoPorUsuarioId);

        await _vinculoRepo.Salvar(vinculo);    // popula Id
        vinculo.MarcarComoConvidado();          // anexa event com Id correto

        foreach (var evt in vinculo.DomainEvents)
            await _eventBus.Publish(evt);

        vinculo.ClearDomainEvents();
    }
}
