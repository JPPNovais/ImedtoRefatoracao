using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Prontuarios.Commands;

public class IniciarProntuarioCommandHandler : ICommandHandler<IniciarProntuarioCommand>
{
    private readonly IProntuarioRepository _prontuarioRepo;
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IModeloDeProntuarioRepository _modeloRepo;
    private readonly IProntuarioAcessoLogService _acessoLog;
    private readonly IEventBus _eventBus;

    public IniciarProntuarioCommandHandler(
        IProntuarioRepository prontuarioRepo,
        IPacienteRepository pacienteRepo,
        IModeloDeProntuarioRepository modeloRepo,
        IProntuarioAcessoLogService acessoLog,
        IEventBus eventBus)
    {
        _prontuarioRepo = prontuarioRepo;
        _pacienteRepo = pacienteRepo;
        _modeloRepo = modeloRepo;
        _acessoLog = acessoLog;
        _eventBus = eventBus;
    }

    public virtual async Task Handle(IniciarProntuarioCommand command)
    {
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        // Mensagem padronizada (nao vaza existencia cross-tenant).
        var paciente = await _pacienteRepo.ObterPorIdOuNulo(command.PacienteId, command.EstabelecimentoId)
            ?? throw new BusinessException("Paciente não encontrado.");
        if (paciente.EstaDeletado)
            throw new BusinessException("Paciente deletado — não é possível iniciar prontuário.");

        // Resolve modelo: se o caller informou um id, valida; senão busca o primeiro disponível
        // (padrão do estabelecimento ou padrão-sistema). Mobile envia body {} sem modelo.
        long modeloIdResolvido;
        if (command.ModeloDeProntuarioId is > 0)
        {
            // Valida que o modelo é visível para o tenant (padrão-sistema ou próprio).
            // Defense-in-depth multi-tenant: filtro padrao-sistema OR estabelecimento ativo no proprio repo.
            var modelo = await _modeloRepo.ObterVisivelOuNulo(command.ModeloDeProntuarioId.Value, command.EstabelecimentoId)
                ?? throw new BusinessException("Modelo não encontrado.");
            if (!modelo.Ativo)
                throw new BusinessException("Modelo inativo.");
            modeloIdResolvido = modelo.Id;
        }
        else
        {
            // Resolve server-side: primeiro modelo ativo visível ao estabelecimento.
            // Se não houver nenhum, inicia sem modelo (id = 0) — mobile não usa template.
            var modeloPadrao = await _modeloRepo.ObterPrimeiroVisivelOuNulo(command.EstabelecimentoId);
            modeloIdResolvido = modeloPadrao?.Id ?? 0;
        }

        // Já existe?
        var existente = await _prontuarioRepo.ObterPorPaciente(command.PacienteId, command.EstabelecimentoId);
        if (existente is not null)
            throw new BusinessException("Paciente já possui prontuário — use o existente.");

        var prontuario = Prontuario.Iniciar(command.PacienteId, command.EstabelecimentoId, modeloIdResolvido);

        await _prontuarioRepo.Salvar(prontuario);
        prontuario.MarcarComoIniciado();

        await _acessoLog.RegistrarAsync(
            prontuario.Id, command.SolicitanteUsuarioId, command.EstabelecimentoId, TipoAcessoProntuario.Escrita);

        foreach (var evt in prontuario.DomainEvents)
            await _eventBus.Publish(evt);

        prontuario.ClearDomainEvents();
    }
}
