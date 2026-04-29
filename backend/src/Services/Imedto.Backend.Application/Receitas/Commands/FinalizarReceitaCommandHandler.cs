using Imedto.Backend.Contracts.Receitas.Commands;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Domain.Receitas;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Receitas.Commands;

/// <summary>
/// Finaliza um rascunho. Aqui sim registramos audit de Escrita e atualizamos
/// o ranking de medicamentos favoritos — passou a ser uma receita real.
/// </summary>
public class FinalizarReceitaCommandHandler : ICommandHandler<FinalizarReceitaCommand>
{
    private readonly IReceitaRepository _receitaRepo;
    private readonly IMedicamentoFavoritoRepository _favoritoRepo;
    private readonly IProntuarioAcessoLogService _acessoLog;
    private readonly IEventBus _eventBus;

    public FinalizarReceitaCommandHandler(
        IReceitaRepository receitaRepo,
        IMedicamentoFavoritoRepository favoritoRepo,
        IProntuarioAcessoLogService acessoLog,
        IEventBus eventBus)
    {
        _receitaRepo = receitaRepo;
        _favoritoRepo = favoritoRepo;
        _acessoLog = acessoLog;
        _eventBus = eventBus;
    }

    public async Task Handle(FinalizarReceitaCommand cmd)
    {
        var receita = await _receitaRepo.ObterPorIdOuNulo(cmd.ReceitaId)
            ?? throw new BusinessException("Receita não encontrada.");

        if (receita.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Receita não pertence a este estabelecimento.");
        if (receita.DeletadoEm is not null)
            throw new BusinessException("Receita não encontrada.");
        if (receita.ProfissionalUsuarioId != cmd.SolicitanteUsuarioId)
            throw new BusinessException("Apenas o profissional responsável pode finalizar este rascunho.");

        receita.Finalizar();

        await _receitaRepo.Salvar(receita);
        receita.MarcarComoEmitida();

        foreach (var item in receita.Itens)
        {
            await _favoritoRepo.RegistrarUso(
                cmd.SolicitanteUsuarioId,
                cmd.EstabelecimentoId,
                item.Medicamento,
                item.Posologia,
                item.Via);
        }

        await _acessoLog.RegistrarAsync(
            receita.ProntuarioId,
            cmd.SolicitanteUsuarioId,
            cmd.EstabelecimentoId,
            TipoAcessoProntuario.Escrita);

        foreach (var ev in receita.DomainEvents)
            await _eventBus.Publish(ev);
        receita.ClearDomainEvents();
    }
}
