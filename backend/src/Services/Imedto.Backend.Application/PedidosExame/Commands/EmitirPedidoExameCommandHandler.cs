using Imedto.Backend.Contracts.PedidosExame.Commands;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.PedidosExame;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.PedidosExame.Commands;

public class EmitirPedidoExameCommandHandler : ICommandHandler<EmitirPedidoExameCommand>
{
    private readonly IPedidoExameRepository _pedidoRepo;
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IProntuarioRepository _prontuarioRepo;
    private readonly IProntuarioAcessoLogService _acessoLog;
    private readonly IEventBus _eventBus;

    public EmitirPedidoExameCommandHandler(
        IPedidoExameRepository pedidoRepo,
        IPacienteRepository pacienteRepo,
        IProntuarioRepository prontuarioRepo,
        IProntuarioAcessoLogService acessoLog,
        IEventBus eventBus)
    {
        _pedidoRepo = pedidoRepo;
        _pacienteRepo = pacienteRepo;
        _prontuarioRepo = prontuarioRepo;
        _acessoLog = acessoLog;
        _eventBus = eventBus;
    }

    public async Task Handle(EmitirPedidoExameCommand cmd)
    {
        var paciente = await _pacienteRepo.ObterPorIdOuNulo(cmd.PacienteId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Paciente não encontrado.");
        if (paciente.EstaDeletado)
            throw new BusinessException("Paciente deletado — não é possível emitir pedido de exame.");

        var tipo = PedidoExameParsers.ParseTipo(cmd.Tipo);
        var pedido = PedidoExame.Emitir(
            cmd.EstabelecimentoId,
            paciente.Id,
            cmd.ProfissionalUsuarioId,
            tipo,
            cmd.Exames,
            cmd.IndicacaoClinica,
            cmd.Cid10,
            cmd.Observacoes);

        await _pedidoRepo.Salvar(pedido);
        pedido.MarcarComoEmitido();
        cmd.PedidoExameIdCriado = pedido.Id;

        var prontuario = await _prontuarioRepo.ObterPorPaciente(paciente.Id, cmd.EstabelecimentoId);
        if (prontuario is not null)
        {
            await _acessoLog.RegistrarAsync(
                prontuario.Id, cmd.ProfissionalUsuarioId, cmd.EstabelecimentoId, TipoAcessoProntuario.Escrita);
        }

        foreach (var ev in pedido.DomainEvents)
            await _eventBus.Publish(ev);
        pedido.ClearDomainEvents();
    }
}
