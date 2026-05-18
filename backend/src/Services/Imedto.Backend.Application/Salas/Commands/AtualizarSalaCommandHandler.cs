using Imedto.Backend.Contracts.Salas.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Salas;
using Imedto.Backend.Domain.Unidades;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Salas.Commands;

public class AtualizarSalaCommandHandler : ICommandHandler<AtualizarSalaCommand>
{
    private readonly ISalaRepository _salas;
    private readonly IEstabelecimentoRepository _estabelecimentos;
    private readonly IUnidadeRepository _unidades;

    public AtualizarSalaCommandHandler(
        ISalaRepository salas,
        IEstabelecimentoRepository estabelecimentos,
        IUnidadeRepository unidades)
    {
        _salas = salas;
        _estabelecimentos = estabelecimentos;
        _unidades = unidades;
    }

    public async Task Handle(AtualizarSalaCommand command)
    {
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var sala = await _salas.ObterPorIdOuNulo(command.SalaId, command.EstabelecimentoId)
            ?? throw new BusinessException("Repartição não encontrada.");
        var estab = await _estabelecimentos.ObterPorIdOuNulo(command.EstabelecimentoId)
            ?? throw new BusinessException("Estabelecimento não encontrado.");

        if (estab.DonoUsuarioId != command.UsuarioSolicitanteId)
            throw new BusinessException("Apenas o dono pode editar repartições.");

        var unidade = await _unidades.ObterPorIdOuNulo(command.UnidadeId, command.EstabelecimentoId)
            ?? throw new BusinessException("Unidade não encontrada.");

        if (await _salas.ExisteOutraComMesmoNomeNaUnidade(estab.Id, command.UnidadeId, command.Nome ?? string.Empty, sala.Id))
            throw new BusinessException("Já existe uma repartição com esse nome nesta unidade.");

        sala.AtualizarDados(command.UnidadeId, command.TipoSalaId, command.Nome, command.Descricao);
        await _salas.Salvar(sala);
    }
}
