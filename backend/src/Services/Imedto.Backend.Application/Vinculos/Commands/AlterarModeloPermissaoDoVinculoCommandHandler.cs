using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Vinculos.Commands;

public class AlterarModeloPermissaoDoVinculoCommandHandler : ICommandHandler<AlterarModeloPermissaoDoVinculoCommand>
{
    private readonly IVinculoRepository _vinculoRepo;
    private readonly IEstabelecimentoRepository _estabRepo;
    private readonly IModeloPermissaoRepository _modeloRepo;

    public AlterarModeloPermissaoDoVinculoCommandHandler(
        IVinculoRepository vinculoRepo,
        IEstabelecimentoRepository estabRepo,
        IModeloPermissaoRepository modeloRepo)
    {
        _vinculoRepo = vinculoRepo;
        _estabRepo = estabRepo;
        _modeloRepo = modeloRepo;
    }

    public async Task Handle(AlterarModeloPermissaoDoVinculoCommand command)
    {
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var vinculo = await _vinculoRepo.ObterPorIdNoEstabelecimentoOuNulo(command.VinculoId, command.EstabelecimentoId)
            ?? throw new BusinessException("Vínculo não encontrado.");

        // Apenas o dono do estabelecimento pode reatribuir o modelo de permissão.
        var estab = await _estabRepo.ObterPorId(command.EstabelecimentoId);
        if (estab.DonoUsuarioId != command.UsuarioSolicitanteId)
            throw new BusinessException("Apenas o dono do estabelecimento pode alterar permissões dos profissionais.");

        // Garante que o novo modelo pertence ao mesmo estabelecimento.
        if (!await _modeloRepo.PertenceAoEstabelecimento(command.NovoModeloPermissaoId, command.EstabelecimentoId))
            throw new BusinessException("Modelo de permissão não pertence a este estabelecimento.");

        vinculo.AtualizarModeloPermissao(command.NovoModeloPermissaoId);
        await _vinculoRepo.Salvar(vinculo);
    }
}
