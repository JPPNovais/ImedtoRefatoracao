using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Vinculos.Commands;

/// <summary>
/// Apenas o próprio profissional que criou a solicitação pode cancelar.
/// Validado por <c>SolicitanteUsuarioId == solicitacao.ProfissionalUsuarioId</c>.
/// </summary>
public class CancelarSolicitacaoVinculoCommandHandler : ICommandHandler<CancelarSolicitacaoVinculoCommand>
{
    private readonly ISolicitacaoVinculoRepository _solicitacaoRepo;

    public CancelarSolicitacaoVinculoCommandHandler(ISolicitacaoVinculoRepository solicitacaoRepo)
    {
        _solicitacaoRepo = solicitacaoRepo;
    }

    public async Task Handle(CancelarSolicitacaoVinculoCommand command)
    {
        var solicitacao = await _solicitacaoRepo.ObterPorId(command.SolicitacaoId);

        if (solicitacao.ProfissionalUsuarioId != command.SolicitanteUsuarioId)
            throw new BusinessException("Apenas o profissional que criou a solicitação pode cancelá-la.");

        solicitacao.Cancelar();
        await _solicitacaoRepo.Salvar(solicitacao);
    }
}
