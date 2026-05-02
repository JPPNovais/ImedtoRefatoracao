using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Domain.Assinaturas;
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
    private readonly IAssinaturaService _assinaturaService;

    public ConvidarProfissionalCommandHandler(
        IEstabelecimentoRepository estabelecimentoRepo,
        IModeloPermissaoRepository modeloRepo,
        IUsuarioRepository usuarioRepo,
        IVinculoRepository vinculoRepo,
        IEventBus eventBus,
        IAssinaturaService assinaturaService)
    {
        _estabelecimentoRepo = estabelecimentoRepo;
        _modeloRepo = modeloRepo;
        _usuarioRepo = usuarioRepo;
        _vinculoRepo = vinculoRepo;
        _eventBus = eventBus;
        _assinaturaService = assinaturaService;
    }

    public async Task Handle(ConvidarProfissionalCommand command)
    {
        var estab = await _estabelecimentoRepo.ObterPorId(command.EstabelecimentoId);

        if (estab.DonoUsuarioId != command.ConvidadoPorUsuarioId)
            throw new BusinessException("Apenas o dono do estabelecimento pode convidar profissionais.");

        if (command.ProfissionalUsuarioId == estab.DonoUsuarioId)
            throw new BusinessException("O dono do estabelecimento não precisa de convite.");

        if (await _assinaturaService.LimiteAtingidoAsync(command.EstabelecimentoId, "profissionais"))
            throw new BusinessException("Plano não permite mais profissionais. Faça upgrade.");

        // Modelo de permissão é opcional. Se vier explícito, valida que pertence
        // ao estabelecimento; se não vier, o vínculo é criado sem permissão (o
        // dono atribui depois, ou ao convidado fica sem acesso até atribuição).
        long? modeloId = null;
        if (command.ModeloPermissaoId is { } explicitId && explicitId > 0)
        {
            if (!await _modeloRepo.PertenceAoEstabelecimento(explicitId, command.EstabelecimentoId))
                throw new BusinessException("Modelo de permissão não pertence a este estabelecimento.");
            modeloId = explicitId;
        }

        // Garante registro local do profissional (idempotente — pode já existir).
        var usuarioExistente = await _usuarioRepo.ObterPorIdOuNulo(command.ProfissionalUsuarioId);
        if (usuarioExistente is null)
        {
            var novo = Usuario.Criar(command.ProfissionalUsuarioId, command.ProfissionalEmail);
            await _usuarioRepo.Salvar(novo);
        }

        // Re-convite: se já existir vínculo (qualquer status), trata conforme o estado.
        // - Ativo/Convidado → bloqueia (não duplica).
        // - Inativo → reativa o registro como novo convite (preserva linha + histórico).
        var existente = await _vinculoRepo.ObterPorProfissionalEEstabelecimentoOuNulo(
            command.ProfissionalUsuarioId, command.EstabelecimentoId);

        if (existente is { Status: VinculoStatus.Ativo or VinculoStatus.Convidado })
            throw new BusinessException("Este profissional já tem um vínculo ativo ou convite pendente para este estabelecimento.");

        VinculoProfissionalEstabelecimento vinculo;
        if (existente is { Status: VinculoStatus.Inativo })
        {
            existente.ReativarComoConvite(
                modeloId,
                command.ConvidadoPorUsuarioId,
                command.Nome,
                command.Telefone,
                command.Especialidade);
            vinculo = existente;
            await _vinculoRepo.Salvar(vinculo);
        }
        else
        {
            vinculo = VinculoProfissionalEstabelecimento.Convidar(
                command.ProfissionalUsuarioId,
                command.EstabelecimentoId,
                modeloId,
                command.ConvidadoPorUsuarioId,
                command.Nome,
                command.Telefone,
                command.Especialidade);

            await _vinculoRepo.Salvar(vinculo);    // popula Id
            vinculo.MarcarComoConvidado();          // anexa event com Id correto
        }

        foreach (var evt in vinculo.DomainEvents)
            await _eventBus.Publish(evt);

        vinculo.ClearDomainEvents();
    }
}
