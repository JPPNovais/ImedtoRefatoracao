using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Prontuarios.Commands;

public class RegistrarEvolucaoCommandHandler : ICommandHandler<RegistrarEvolucaoCommand>
{
    private readonly IProntuarioRepository _prontuarioRepo;
    private readonly IProntuarioEvolucaoRepository _evolucaoRepo;
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IModeloDeProntuarioRepository _modeloRepo;
    private readonly IProntuarioAcessoLogService _acessoLog;
    private readonly IEventBus _eventBus;

    public RegistrarEvolucaoCommandHandler(
        IProntuarioRepository prontuarioRepo,
        IProntuarioEvolucaoRepository evolucaoRepo,
        IPacienteRepository pacienteRepo,
        IModeloDeProntuarioRepository modeloRepo,
        IProntuarioAcessoLogService acessoLog,
        IEventBus eventBus)
    {
        _prontuarioRepo = prontuarioRepo;
        _evolucaoRepo = evolucaoRepo;
        _pacienteRepo = pacienteRepo;
        _modeloRepo = modeloRepo;
        _acessoLog = acessoLog;
        _eventBus = eventBus;
    }

    public async Task Handle(RegistrarEvolucaoCommand command)
    {
        // Valida paciente e tenant (defense-in-depth mesmo com o filter).
        var paciente = await _pacienteRepo.ObterPorId(command.PacienteId);
        if (paciente.EstabelecimentoId != command.EstabelecimentoId)
            throw new BusinessException("Paciente não pertence a este estabelecimento.");
        if (paciente.EstaDeletado)
            throw new BusinessException("Paciente deletado — não é possível registrar evolução.");

        var prontuario = await _prontuarioRepo.ObterPorPaciente(command.PacienteId, command.EstabelecimentoId)
            ?? throw new BusinessException("Prontuário ainda não foi iniciado para este paciente.");

        // Snapshot do template — usa override se fornecido, senão usa o modelo do prontuário.
        var modeloId = command.ModeloDeProntuarioId ?? prontuario.ModeloDeProntuarioId;
        var modeloAtual = await _modeloRepo.ObterPorId(modeloId);

        if (!modeloAtual.EhPadraoSistema && modeloAtual.EstabelecimentoId != command.EstabelecimentoId)
            throw new BusinessException("Modelo não pertence a este estabelecimento.");

        if (!modeloAtual.Ativo)
            throw new BusinessException("O modelo selecionado está inativo.");

        var snapshot = modeloAtual.EstruturaJson;

        var evolucao = ProntuarioEvolucao.Registrar(
            prontuario.Id,
            command.AutorUsuarioId,
            modeloAtual.Id,
            snapshot,
            command.ConteudoJson);

        await _evolucaoRepo.Salvar(evolucao);
        evolucao.MarcarComoRegistrada();

        await _acessoLog.RegistrarAsync(
            prontuario.Id, command.AutorUsuarioId, command.EstabelecimentoId, TipoAcessoProntuario.Escrita);

        foreach (var evt in evolucao.DomainEvents)
            await _eventBus.Publish(evt);

        evolucao.ClearDomainEvents();
    }
}
