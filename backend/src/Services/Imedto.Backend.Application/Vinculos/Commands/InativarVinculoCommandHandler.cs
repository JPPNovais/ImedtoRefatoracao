using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Vinculos.Commands;

public class InativarVinculoCommandHandler : ICommandHandler<InativarVinculoCommand>
{
    private readonly IVinculoRepository _vinculoRepo;
    private readonly IEstabelecimentoRepository _estabelecimentoRepo;

    public InativarVinculoCommandHandler(
        IVinculoRepository vinculoRepo,
        IEstabelecimentoRepository estabelecimentoRepo)
    {
        _vinculoRepo = vinculoRepo;
        _estabelecimentoRepo = estabelecimentoRepo;
    }

    public async Task Handle(InativarVinculoCommand command)
    {
        var vinculo = await _vinculoRepo.ObterPorId(command.VinculoId);
        var estab = await _estabelecimentoRepo.ObterPorId(vinculo.EstabelecimentoId);

        // O dono do estabelecimento OU o próprio profissional podem encerrar o vínculo.
        var ehDono = estab.DonoUsuarioId == command.UsuarioSolicitanteId;
        var ehProprioProfissional = vinculo.ProfissionalUsuarioId == command.UsuarioSolicitanteId;

        if (!ehDono && !ehProprioProfissional)
            throw new BusinessException("Apenas o dono do estabelecimento ou o próprio profissional podem inativar este vínculo.");

        vinculo.Inativar();
        await _vinculoRepo.Salvar(vinculo);
    }
}
