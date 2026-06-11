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
    private readonly PoolExtratorEvolucao _poolExtrator;
    private readonly PendenciaExtratorEvolucao _pendenciaExtrator;

    public RegistrarEvolucaoCommandHandler(
        IProntuarioRepository prontuarioRepo,
        IProntuarioEvolucaoRepository evolucaoRepo,
        IPacienteRepository pacienteRepo,
        IModeloDeProntuarioRepository modeloRepo,
        IProntuarioAcessoLogService acessoLog,
        IEventBus eventBus,
        PoolExtratorEvolucao poolExtrator,
        PendenciaExtratorEvolucao pendenciaExtrator)
    {
        _prontuarioRepo = prontuarioRepo;
        _evolucaoRepo = evolucaoRepo;
        _pacienteRepo = pacienteRepo;
        _modeloRepo = modeloRepo;
        _acessoLog = acessoLog;
        _eventBus = eventBus;
        _poolExtrator = poolExtrator;
        _pendenciaExtrator = pendenciaExtrator;
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
        // Defense-in-depth multi-tenant: filtro padrao-sistema OR estabelecimento ativo.
        var modeloId = command.ModeloDeProntuarioId ?? prontuario.ModeloDeProntuarioId;
        var modeloAtual = await _modeloRepo.ObterVisivelOuNulo(modeloId, command.EstabelecimentoId)
            ?? throw new BusinessException("Modelo não encontrado.");

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

        // Expoe o id criado para o controller retornar no body. O frontend precisa
        // dele para encadear, na mesma ação "Salvar consulta", o registro de
        // regioes anatomicas do exame fisico (POST /api/evolucoes/{id}/exame-fisico).
        command.EvolucaoIdCriada = evolucao.Id;

        await _acessoLog.RegistrarAsync(
            prontuario.Id, command.AutorUsuarioId, command.EstabelecimentoId, TipoAcessoProntuario.Escrita);

        // Extrai valores inéditos dos campos mapeados e cria itens no pool do estabelecimento.
        // Transacional com o salvamento (CA16): falha-suave — nunca interrompe a evolução.
        // CA6: não exige permissão ModelosProntuario (qualquer profissional com acesso ao prontuário).
        await _poolExtrator.ExtrairECriar(command.EstabelecimentoId, command.ConteudoJson);

        // Extrai ações de conduta marcadas e cria pendências de atendimento (F3B).
        // Falha-suave (CA75/R2): nunca interrompe a evolução.
        // Idempotente (CA62/R3): verifica existência antes de criar.
        await _pendenciaExtrator.ExtrairECriar(
            command.EstabelecimentoId,
            command.PacienteId,
            evolucao.Id,
            command.AgendamentoId,
            command.AutorUsuarioId,
            command.ConteudoJson);

        foreach (var evt in evolucao.DomainEvents)
            await _eventBus.Publish(evt);

        evolucao.ClearDomainEvents();
    }
}
