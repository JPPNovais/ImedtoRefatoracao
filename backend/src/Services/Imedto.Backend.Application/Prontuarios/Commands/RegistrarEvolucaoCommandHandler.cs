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
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        // Mensagem padronizada (nao vaza existencia cross-tenant).
        var paciente = await _pacienteRepo.ObterPorIdOuNulo(command.PacienteId, command.EstabelecimentoId)
            ?? throw new BusinessException("Paciente não encontrado.");
        if (paciente.EstaDeletado)
            throw new BusinessException("Paciente deletado — não é possível registrar evolução.");

        var prontuario = await _prontuarioRepo.ObterPorPaciente(command.PacienteId, command.EstabelecimentoId)
            ?? throw new BusinessException("Prontuário ainda não foi iniciado para este paciente.");

        // Snapshot do template — usa override se fornecido, senão usa o modelo do prontuário.
        var modeloId = command.ModeloDeProntuarioId ?? prontuario.ModeloDeProntuarioId;
        var modeloAtual = await _modeloRepo.ObterPorIdOuNulo(modeloId)
            ?? throw new BusinessException("Modelo não encontrado.");

        if (!modeloAtual.EhPadraoSistema && modeloAtual.EstabelecimentoId != command.EstabelecimentoId)
            throw new BusinessException("Modelo não encontrado.");

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
