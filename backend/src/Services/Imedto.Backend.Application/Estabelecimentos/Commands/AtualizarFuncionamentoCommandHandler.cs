using Imedto.Backend.Contracts.Estabelecimentos.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Estabelecimentos.Commands;

public class AtualizarFuncionamentoCommandHandler : ICommandHandler<AtualizarFuncionamentoCommand>
{
    private readonly IEstabelecimentoRepository _repository;
    private readonly IModeloPermissaoRepository _permissoes;

    public AtualizarFuncionamentoCommandHandler(
        IEstabelecimentoRepository repository,
        IModeloPermissaoRepository permissoes)
    {
        _repository = repository;
        _permissoes = permissoes;
    }

    public async Task Handle(AtualizarFuncionamentoCommand command)
    {
        var estab = await _repository.ObterPorIdOuNulo(command.EstabelecimentoId)
            ?? throw new BusinessException("Estabelecimento não encontrado.");

        // Dono OU Admin com permissão extra `config_estabelecimento` (dono é pass-through).
        var podeEditar = await _permissoes.UsuarioTemPermissaoExtra(
            command.UsuarioSolicitanteId,
            command.EstabelecimentoId,
            PermissoesExtras.ConfigEstabelecimento);
        if (!podeEditar)
            throw new BusinessException("Você não tem permissão para alterar este estabelecimento.");

        var horarios = command.HorariosBloqueados
            .Select(h => new HorarioBloqueado(h.Id ?? Guid.Empty, h.Inicio, h.Fim, h.Descricao ?? string.Empty))
            .ToList();

        var datas = command.DatasBloqueadas
            .Select(d => new DataBloqueada(d.Id ?? Guid.Empty, d.Data, d.Descricao ?? string.Empty))
            .ToList();

        estab.AtualizarFuncionamento(
            command.HorarioInicio,
            command.HorarioFim,
            command.DuracaoConsultaPadraoMinutos,
            command.IntervaloEntreConsultasMinutos,
            command.DiasSemana,
            horarios,
            datas);

        await _repository.Salvar(estab);
    }
}
