using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Vinculos.Commands;

public class AlterarProfissaoEspecialidadeDoVinculoCommandHandler
    : ICommandHandler<AlterarProfissaoEspecialidadeDoVinculoCommand>
{
    private readonly IVinculoRepository _vinculoRepo;
    private readonly IEstabelecimentoRepository _estabRepo;
    private readonly CatalogoQueryRepository _catalogoRepo;

    public AlterarProfissaoEspecialidadeDoVinculoCommandHandler(
        IVinculoRepository vinculoRepo,
        IEstabelecimentoRepository estabRepo,
        CatalogoQueryRepository catalogoRepo)
    {
        _vinculoRepo = vinculoRepo;
        _estabRepo = estabRepo;
        _catalogoRepo = catalogoRepo;
    }

    public async Task Handle(AlterarProfissaoEspecialidadeDoVinculoCommand command)
    {
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no próprio repo.
        var vinculo = await _vinculoRepo.ObterPorIdNoEstabelecimentoOuNulo(command.VinculoId, command.EstabelecimentoId)
            ?? throw new BusinessException("Vínculo não encontrado.");

        // Apenas o dono do estabelecimento pode editar profissão/especialidade do vínculo (R4).
        var estab = await _estabRepo.ObterPorId(command.EstabelecimentoId);
        if (estab.DonoUsuarioId != command.UsuarioSolicitanteId)
            throw new BusinessException("Apenas o dono do estabelecimento pode alterar a profissão ou especialidade do vínculo.");

        // Valida profissão e especialidade contra o catálogo (R1 — catálogo estrito, defense-in-depth).
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

        // Persistência atômica: profissão + especialidade num único comando (R2).
        vinculo.AtualizarProfissaoEspecialidade(command.ProfissaoId, command.Especialidade);
        await _vinculoRepo.Salvar(vinculo);
    }
}
