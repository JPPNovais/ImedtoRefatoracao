using Imedto.Backend.Contracts.Estabelecimentos.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Estabelecimentos.Commands;

public class AtualizarFuncionamentoCommandHandler : ICommandHandler<AtualizarFuncionamentoCommand>
{
    private readonly IEstabelecimentoRepository _repository;

    public AtualizarFuncionamentoCommandHandler(IEstabelecimentoRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(AtualizarFuncionamentoCommand command)
    {
        var estab = await _repository.ObterPorIdOuNulo(command.EstabelecimentoId)
            ?? throw new BusinessException("Estabelecimento não encontrado.");

        if (estab.DonoUsuarioId != command.UsuarioSolicitanteId)
            throw new BusinessException("Apenas o dono do estabelecimento pode editar o funcionamento.");

        var horarios = command.HorariosBloqueados
            .Select(h => new HorarioBloqueado(h.Id ?? Guid.Empty, h.Inicio, h.Fim, h.Descricao ?? string.Empty))
            .ToList();

        var datas = command.DatasBloqueadas
            .Select(d => new DataBloqueada(d.Id ?? Guid.Empty, d.Data, d.Descricao ?? string.Empty))
            .ToList();

        estab.AtualizarFuncionamento(
            command.HorarioInicio,
            command.HorarioFim,
            command.DiasSemana,
            horarios,
            datas);

        await _repository.Salvar(estab);
    }
}
