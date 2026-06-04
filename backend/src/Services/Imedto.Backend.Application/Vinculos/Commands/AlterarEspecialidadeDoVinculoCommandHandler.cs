using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Vinculos.Commands;

public class AlterarEspecialidadeDoVinculoCommandHandler : ICommandHandler<AlterarEspecialidadeDoVinculoCommand>
{
    private readonly IVinculoRepository _vinculoRepo;
    private readonly IEstabelecimentoRepository _estabRepo;

    public AlterarEspecialidadeDoVinculoCommandHandler(
        IVinculoRepository vinculoRepo,
        IEstabelecimentoRepository estabRepo)
    {
        _vinculoRepo = vinculoRepo;
        _estabRepo = estabRepo;
    }

    public async Task Handle(AlterarEspecialidadeDoVinculoCommand command)
    {
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no próprio repo.
        var vinculo = await _vinculoRepo.ObterPorIdNoEstabelecimentoOuNulo(command.VinculoId, command.EstabelecimentoId)
            ?? throw new BusinessException("Vínculo não encontrado.");

        // Apenas o dono do estabelecimento pode editar a especialidade do vínculo.
        var estab = await _estabRepo.ObterPorId(command.EstabelecimentoId);
        if (estab.DonoUsuarioId != command.UsuarioSolicitanteId)
            throw new BusinessException("Apenas o dono do estabelecimento pode alterar a especialidade do vínculo.");

        vinculo.AtualizarEspecialidade(command.Especialidade);
        await _vinculoRepo.Salvar(vinculo);
    }
}
