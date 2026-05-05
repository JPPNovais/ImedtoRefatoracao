using Imedto.Backend.Contracts.Receitas.Commands;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Domain.Receitas;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Receitas.Commands;

/// <summary>
/// Duplica uma receita existente: cria uma nova com mesmos itens em status Emitida.
/// Não vincula a duas (não é "Substituir") — útil para repetir prescrição em outra
/// consulta. Atualiza ranking de favoritos com os mesmos itens.
/// </summary>
public class DuplicarReceitaCommandHandler : ICommandHandler<DuplicarReceitaCommand>
{
    private readonly IReceitaRepository _receitaRepo;
    private readonly IMedicamentoFavoritoRepository _favoritoRepo;
    private readonly IProntuarioAcessoLogService _acessoLog;
    private readonly IEventBus _eventBus;

    public DuplicarReceitaCommandHandler(
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

    public async Task Handle(DuplicarReceitaCommand cmd)
    {
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var origem = await _receitaRepo.ObterPorIdOuNulo(cmd.ReceitaIdOrigem, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Receita não encontrada.");

        if (origem.DeletadoEm is not null)
            throw new BusinessException("Receita não encontrada.");

        if (origem.Itens.Count == 0)
            throw new BusinessException("Receita de origem não possui itens.");

        // A duplicação herda o tipo. Em "Controlada" recalculamos a validade —
        // preservar a do original poderia gerar receita já vencida.
        DateTime? novaValidade = origem.Tipo == TipoReceita.Controlada
            ? DateTime.UtcNow.AddDays(30)
            : origem.ValidadeAte;

        var itensCopia = origem.Itens
            .OrderBy(i => i.Ordem)
            .Select(i => new Receita.ItemReceitaInput(
                i.Medicamento,
                i.Posologia,
                i.Quantidade,
                i.Via,
                i.Observacao,
                i.Concentracao,
                i.FormaFarmaceutica,
                i.Duracao));

        var nova = Receita.Emitir(
            origem.ProntuarioId,
            origem.PacienteId,
            cmd.ProfissionalUsuarioId,
            cmd.EstabelecimentoId,
            origem.Tipo,
            origem.TipoNotificacao,
            origem.Observacoes,
            novaValidade,
            itensCopia);

        await _receitaRepo.Salvar(nova);
        nova.MarcarComoEmitida();

        cmd.ReceitaIdCriada = nova.Id;

        foreach (var item in nova.Itens)
        {
            await _favoritoRepo.RegistrarUso(
                cmd.ProfissionalUsuarioId,
                cmd.EstabelecimentoId,
                item.Medicamento,
                item.Posologia,
                item.Via);
        }

        await _acessoLog.RegistrarAsync(
            nova.ProntuarioId,
            cmd.ProfissionalUsuarioId,
            cmd.EstabelecimentoId,
            TipoAcessoProntuario.Escrita);

        foreach (var ev in nova.DomainEvents)
            await _eventBus.Publish(ev);
        nova.ClearDomainEvents();
    }
}
