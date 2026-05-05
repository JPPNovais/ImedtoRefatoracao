using Imedto.Backend.Contracts.Salas.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Salas;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Salas.Commands;

public class DeletarSalaCommandHandler : ICommandHandler<DeletarSalaCommand>
{
    private readonly ISalaRepository _salas;
    private readonly IEstabelecimentoRepository _estabelecimentos;

    public DeletarSalaCommandHandler(ISalaRepository salas, IEstabelecimentoRepository estabelecimentos)
    {
        _salas = salas;
        _estabelecimentos = estabelecimentos;
    }

    public async Task Handle(DeletarSalaCommand command)
    {
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var sala = await _salas.ObterPorIdOuNulo(command.SalaId, command.EstabelecimentoId)
            ?? throw new BusinessException("Repartição não encontrada.");
        var estab = await _estabelecimentos.ObterPorIdOuNulo(command.EstabelecimentoId)
            ?? throw new BusinessException("Estabelecimento não encontrado.");

        if (estab.DonoUsuarioId != command.UsuarioSolicitanteId)
            throw new BusinessException("Apenas o dono pode excluir repartições.");

        await _salas.Excluir(sala);
    }
}
