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
        var vinculo = await _vinculoRepo.ObterPorIdOuNulo(command.VinculoId)
            ?? throw new BusinessException("Vínculo não encontrado.");

        // Mensagem padronizada (defense-in-depth: nao vaza existencia cross-tenant).
        if (vinculo.EstabelecimentoId != command.EstabelecimentoId)
            throw new BusinessException("Vínculo não encontrado.");

        // Apenas o dono do estabelecimento pode reatribuir o modelo de permissão.
        var estab = await _estabRepo.ObterPorId(vinculo.EstabelecimentoId);
        if (estab.DonoUsuarioId != command.UsuarioSolicitanteId)
            throw new BusinessException("Apenas o dono do estabelecimento pode alterar permissões dos profissionais.");

        // Garante que o novo modelo pertence ao mesmo estabelecimento.
        if (!await _modeloRepo.PertenceAoEstabelecimento(command.NovoModeloPermissaoId, command.EstabelecimentoId))
            throw new BusinessException("Modelo de permissão não pertence a este estabelecimento.");

        vinculo.AtualizarModeloPermissao(command.NovoModeloPermissaoId);
        await _vinculoRepo.Salvar(vinculo);
    }
}
