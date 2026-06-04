using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Domain.Assinaturas;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.Infrastructure.Database.Repositories;
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
    private readonly CatalogoQueryRepository _catalogoRepo;

    public ConvidarProfissionalCommandHandler(
        IEstabelecimentoRepository estabelecimentoRepo,
        IModeloPermissaoRepository modeloRepo,
        IUsuarioRepository usuarioRepo,
        IVinculoRepository vinculoRepo,
        IEventBus eventBus,
        IAssinaturaService assinaturaService,
        CatalogoQueryRepository catalogoRepo)
    {
        _estabelecimentoRepo = estabelecimentoRepo;
        _modeloRepo = modeloRepo;
        _usuarioRepo = usuarioRepo;
        _vinculoRepo = vinculoRepo;
        _eventBus = eventBus;
        _assinaturaService = assinaturaService;
        _catalogoRepo = catalogoRepo;
    }

    public async Task Handle(ConvidarProfissionalCommand command)
    {
        // Valida e normaliza a mensagem personalizada antes de qualquer outra lógica.
        var mensagem = string.IsNullOrWhiteSpace(command.MensagemPersonalizada)
            ? null
            : command.MensagemPersonalizada.Trim();

        if (mensagem is not null && mensagem.Length > 1000)
            throw new BusinessException("Mensagem personalizada deve ter no máximo 1000 caracteres.");

        var estab = await _estabelecimentoRepo.ObterPorId(command.EstabelecimentoId);

        if (estab.DonoUsuarioId != command.ConvidadoPorUsuarioId)
            throw new BusinessException("Apenas o dono do estabelecimento pode convidar profissionais.");

        if (command.ProfissionalUsuarioId == estab.DonoUsuarioId)
            throw new BusinessException("O dono do estabelecimento não precisa de convite.");

        if (await _assinaturaService.LimiteAtingidoAsync(command.EstabelecimentoId, "profissionais"))
            throw new BusinessException("Plano não permite mais profissionais. Faça upgrade.");

        // Valida especialidade × profissão contra o catálogo (defense-in-depth do front).
        // Profissão pode vir sozinha (convidador sugere só a profissão); especialidade
        // sem profissão é inválida.
        if (command.ProfissaoId is { } profId && profId > 0)
        {
            if (!await _catalogoRepo.ExisteProfissaoAtiva(profId))
                throw new BusinessException("Profissão informada é inválida ou está inativa.");

            if (!string.IsNullOrWhiteSpace(command.Especialidade)
                && !await _catalogoRepo.ExisteEspecialidadeAtivaPorNome(profId, command.Especialidade))
                throw new BusinessException("Especialidade não pertence à profissão selecionada ou está inativa.");
        }
        else if (!string.IsNullOrWhiteSpace(command.Especialidade))
        {
            throw new BusinessException("Profissão é obrigatória quando especialidade for informada.");
        }

        // Modelo de permissão é OBRIGATÓRIO. Sem ele, o vínculo fica em limbo:
        // status Ativo no DB mas TenantAccessResolver retorna SemAcesso porque o JOIN
        // com modelo_permissao_estabelecimento não casa. Exigir aqui evita o "convite
        // aceito mas profissional preso sem acesso".
        if (command.ModeloPermissaoId is not { } explicitId || explicitId <= 0)
            throw new BusinessException("Selecione um modelo de permissão para o profissional.");

        if (!await _modeloRepo.PertenceAoEstabelecimento(explicitId, command.EstabelecimentoId))
            throw new BusinessException("Modelo de permissão não pertence a este estabelecimento.");

        long? modeloId = explicitId;

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
                command.Especialidade,
                command.ProfissaoId,
                mensagemPersonalizada: mensagem);
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
                command.Especialidade,
                command.ProfissaoId);

            await _vinculoRepo.Salvar(vinculo);              // popula Id
            vinculo.MarcarComoConvidado(mensagem);            // anexa event com Id correto
        }

        foreach (var evt in vinculo.DomainEvents)
            await _eventBus.Publish(evt);

        vinculo.ClearDomainEvents();
    }
}
