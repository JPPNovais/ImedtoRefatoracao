using Imedto.Backend.Contracts.Salas.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Salas;
using Imedto.Backend.Domain.Unidades;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Salas.Commands;

public class CriarSalaCommandHandler : ICommandHandler<CriarSalaCommand>
{
    private readonly ISalaRepository _salas;
    private readonly IEstabelecimentoRepository _estabelecimentos;
    private readonly IUnidadeRepository _unidades;

    public CriarSalaCommandHandler(
        ISalaRepository salas,
        IEstabelecimentoRepository estabelecimentos,
        IUnidadeRepository unidades)
    {
        _salas = salas;
        _estabelecimentos = estabelecimentos;
        _unidades = unidades;
    }

    public async Task Handle(CriarSalaCommand command)
    {
        var estab = await _estabelecimentos.ObterPorIdOuNulo(command.EstabelecimentoId)
            ?? throw new BusinessException("Estabelecimento não encontrado.");

        if (estab.DonoUsuarioId != command.UsuarioSolicitanteId)
            throw new BusinessException("Apenas o dono pode cadastrar repartições.");

        var unidade = await _unidades.ObterPorIdOuNulo(command.UnidadeId, command.EstabelecimentoId)
            ?? throw new BusinessException("Unidade não encontrada.");

        if (await _salas.ExisteOutraComMesmoNome(estab.Id, command.Nome ?? string.Empty, 0))
            throw new BusinessException("Já existe uma repartição com esse nome neste estabelecimento.");

        var sala = Sala.Criar(estab.Id, command.UnidadeId, command.TipoSalaId, command.Nome, command.Descricao);
        await _salas.Salvar(sala);
    }
}
