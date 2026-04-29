using Imedto.Backend.Contracts.Cirurgias.Commands;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.Cirurgias;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Cirurgias.Commands;

public class PlanejarProcedimentoCommandHandler : ICommandHandler<PlanejarProcedimentoCommand>
{
    private readonly IProcedimentoCirurgicoRepository _repo;
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IProntuarioRepository _prontuarioRepo;
    private readonly IAgendamentoRepository _agendamentoRepo;
    private readonly IProntuarioAcessoLogService _acessoLog;

    public PlanejarProcedimentoCommandHandler(
        IProcedimentoCirurgicoRepository repo,
        IPacienteRepository pacienteRepo,
        IProntuarioRepository prontuarioRepo,
        IAgendamentoRepository agendamentoRepo,
        IProntuarioAcessoLogService acessoLog)
    {
        _repo = repo;
        _pacienteRepo = pacienteRepo;
        _prontuarioRepo = prontuarioRepo;
        _agendamentoRepo = agendamentoRepo;
        _acessoLog = acessoLog;
    }

    public async Task Handle(PlanejarProcedimentoCommand cmd)
    {
        // Tenant guard — paciente, prontuário e agendamento (quando informado) precisam
        // pertencer ao mesmo estabelecimento. Defesa contra cross-tenant.
        var paciente = await _pacienteRepo.ObterPorId(cmd.PacienteId);
        if (paciente.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Paciente não pertence a este estabelecimento.");

        var prontuario = await _prontuarioRepo.ObterPorId(cmd.ProntuarioId);
        if (prontuario.EstabelecimentoId != cmd.EstabelecimentoId || prontuario.PacienteId != cmd.PacienteId)
            throw new BusinessException("Prontuário não pertence ao paciente neste estabelecimento.");

        if (cmd.AgendamentoId is { } ag)
        {
            var agendamento = await _agendamentoRepo.ObterPorId(ag);
            if (agendamento.EstabelecimentoId != cmd.EstabelecimentoId || agendamento.PacienteId != cmd.PacienteId)
                throw new BusinessException("Agendamento não pertence ao paciente neste estabelecimento.");
        }

        var equipe = cmd.EquipeInicial.Select(m =>
            new ProcedimentoCirurgico.EquipeInicialPayload(
                m.ProfissionalUsuarioId,
                ParsePapel(m.Papel)));

        var procedimento = ProcedimentoCirurgico.Planejar(
            cmd.PacienteId,
            cmd.ProntuarioId,
            cmd.EstabelecimentoId,
            cmd.AgendamentoId,
            cmd.CirurgiaPrincipal,
            cmd.CirurgiaCodigo,
            cmd.DataAgendada,
            equipe);

        await _repo.Salvar(procedimento);
        cmd.ProcedimentoIdCriado = procedimento.Id;

        // Audit LGPD — escrita no histórico clínico do paciente.
        if (cmd.SolicitanteUsuarioId != Guid.Empty)
        {
            await _acessoLog.RegistrarAsync(
                prontuario.Id, cmd.SolicitanteUsuarioId, cmd.EstabelecimentoId,
                TipoAcessoProntuario.Escrita);
        }
    }

    private static PapelCirurgia ParsePapel(string papel) =>
        Enum.TryParse<PapelCirurgia>(papel, ignoreCase: true, out var p)
            ? p
            : throw new BusinessException($"Papel '{papel}' inválido na equipe cirúrgica.");
}
