using Imedto.Backend.Contracts.Atestados.Commands;
using Imedto.Backend.Domain.Atestados;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Atestados.Commands;

public class EmitirAtestadoCommandHandler : ICommandHandler<EmitirAtestadoCommand>
{
    private readonly IAtestadoRepository _atestadoRepo;
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IProntuarioRepository _prontuarioRepo;
    private readonly IProntuarioAcessoLogService _acessoLog;
    private readonly IEventBus _eventBus;

    public EmitirAtestadoCommandHandler(
        IAtestadoRepository atestadoRepo,
        IPacienteRepository pacienteRepo,
        IProntuarioRepository prontuarioRepo,
        IProntuarioAcessoLogService acessoLog,
        IEventBus eventBus)
    {
        _atestadoRepo = atestadoRepo;
        _pacienteRepo = pacienteRepo;
        _prontuarioRepo = prontuarioRepo;
        _acessoLog = acessoLog;
        _eventBus = eventBus;
    }

    public async Task Handle(EmitirAtestadoCommand cmd)
    {
        // Defense-in-depth multi-tenant — repos sempre filtram por estabelecimento.
        var paciente = await _pacienteRepo.ObterPorIdOuNulo(cmd.PacienteId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Paciente não encontrado.");
        if (paciente.EstaDeletado)
            throw new BusinessException("Paciente deletado — não é possível emitir atestado.");

        var tipo = AtestadoParsers.ParseTipo(cmd.Tipo);
        var atestado = Atestado.Emitir(
            cmd.EstabelecimentoId,
            paciente.Id,
            cmd.ProfissionalUsuarioId,
            tipo,
            cmd.DiasAfastamento,
            cmd.Cid10,
            cmd.Conteudo);

        await _atestadoRepo.Salvar(atestado);
        atestado.MarcarComoEmitido();
        cmd.AtestadoIdCriado = atestado.Id;

        // Audit LGPD — atestado é dado clínico ligado ao paciente. Marca como Escrita
        // no prontuário se ele existir; se ainda não foi iniciado, segue sem audit
        // (paciente pode receber atestado sem ter prontuário aberto).
        var prontuario = await _prontuarioRepo.ObterPorPaciente(paciente.Id, cmd.EstabelecimentoId);
        if (prontuario is not null)
        {
            await _acessoLog.RegistrarAsync(
                prontuario.Id, cmd.ProfissionalUsuarioId, cmd.EstabelecimentoId, TipoAcessoProntuario.Escrita);
        }

        foreach (var ev in atestado.DomainEvents)
            await _eventBus.Publish(ev);
        atestado.ClearDomainEvents();
    }
}
