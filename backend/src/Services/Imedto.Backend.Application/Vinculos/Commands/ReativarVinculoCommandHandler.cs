using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Vinculos.Commands;

public class ReativarVinculoCommandHandler : ICommandHandler<ReativarVinculoCommand>
{
    private readonly IVinculoRepository _vinculoRepo;
    private readonly IEstabelecimentoRepository _estabelecimentoRepo;

    public ReativarVinculoCommandHandler(
        IVinculoRepository vinculoRepo,
        IEstabelecimentoRepository estabelecimentoRepo)
    {
        _vinculoRepo = vinculoRepo;
        _estabelecimentoRepo = estabelecimentoRepo;
    }

    public async Task Handle(ReativarVinculoCommand command)
    {
        var vinculo = await _vinculoRepo.ObterPorIdOuNulo(command.VinculoId)
            ?? throw new BusinessException("Vínculo não encontrado.");
        var estab = await _estabelecimentoRepo.ObterPorId(vinculo.EstabelecimentoId);

        // Apenas o Dono pode reativar — o próprio profissional inativo não consegue
        // logar/agir, então não faz sentido permitir self-service aqui.
        if (estab.DonoUsuarioId != command.UsuarioSolicitanteId)
            throw new BusinessException("Apenas o dono do estabelecimento pode reativar este vínculo.");

        vinculo.Reativar();
        await _vinculoRepo.Salvar(vinculo);
    }
}
