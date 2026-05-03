using Imedto.Backend.Contracts.Receitas.Commands;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Domain.Receitas;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Receitas.Commands;

public class EmitirReceitaCommandHandler : ICommandHandler<EmitirReceitaCommand>
{
    private readonly IReceitaRepository _receitaRepo;
    private readonly IProntuarioRepository _prontuarioRepo;
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IMedicamentoFavoritoRepository _favoritoRepo;
    private readonly IProntuarioAcessoLogService _acessoLog;
    private readonly IEventBus _eventBus;

    public EmitirReceitaCommandHandler(
        IReceitaRepository receitaRepo,
        IProntuarioRepository prontuarioRepo,
        IPacienteRepository pacienteRepo,
        IMedicamentoFavoritoRepository favoritoRepo,
        IProntuarioAcessoLogService acessoLog,
        IEventBus eventBus)
    {
        _receitaRepo = receitaRepo;
        _prontuarioRepo = prontuarioRepo;
        _pacienteRepo = pacienteRepo;
        _favoritoRepo = favoritoRepo;
        _acessoLog = acessoLog;
        _eventBus = eventBus;
    }

    public async Task Handle(EmitirReceitaCommand cmd)
    {
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var paciente = await _pacienteRepo.ObterPorIdOuNulo(cmd.PacienteId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Paciente não encontrado.");
        if (paciente.EstaDeletado)
            throw new BusinessException("Paciente deletado — não é possível emitir receita.");

        var prontuario = await _prontuarioRepo.ObterPorPaciente(cmd.PacienteId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Prontuário ainda não foi iniciado para este paciente.");

        var tipo = ReceitaParsers.ParseTipo(cmd.Tipo);
        var tipoNotificacao = ReceitaParsers.ParseTipoNotificacao(cmd.TipoNotificacao);

        var itensRicos = cmd.Itens.Select(ReceitaParsers.ToInput);

        var receita = Receita.Emitir(
            prontuario.Id,
            paciente.Id,
            cmd.ProfissionalUsuarioId,
            cmd.EstabelecimentoId,
            tipo,
            tipoNotificacao,
            cmd.Observacoes,
            cmd.ValidadeAte,
            itensRicos);

        await _receitaRepo.Salvar(receita);
        receita.MarcarComoEmitida();

        cmd.ReceitaIdCriada = receita.Id;

        // Atualiza ranking de favoritos do profissional. Falha silenciosa não cabe
        // (transação cobre tudo via UnitOfWorkFilter), mas o erro é "técnico" — se
        // o favorito não atualizar, a emissão não foi efetiva. Mantém consistente.
        foreach (var item in receita.Itens)
        {
            await _favoritoRepo.RegistrarUso(
                cmd.ProfissionalUsuarioId,
                cmd.EstabelecimentoId,
                item.Medicamento,
                item.Posologia,
                item.Via);
        }

        await _acessoLog.RegistrarAsync(
            prontuario.Id, cmd.ProfissionalUsuarioId, cmd.EstabelecimentoId, TipoAcessoProntuario.Escrita);

        foreach (var ev in receita.DomainEvents)
            await _eventBus.Publish(ev);
        receita.ClearDomainEvents();
    }
}
