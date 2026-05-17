using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Prontuarios.Commands;

/// <summary>
/// Handler do audit de exportação de uma evolução individual.
/// Valida tenant + paciente + prontuário + que a evolução pertence ao prontuário
/// (defense-in-depth IDOR/LGPD). Mensagens genéricas para não vazar cross-tenant.
/// </summary>
public class RegistrarExportacaoEvolucaoCommandHandler : ICommandHandler<RegistrarExportacaoEvolucaoCommand>
{
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IProntuarioRepository _prontuarioRepo;
    private readonly IProntuarioEvolucaoRepository _evolucaoRepo;
    private readonly IProntuarioAcessoLogService _acessoLog;

    public RegistrarExportacaoEvolucaoCommandHandler(
        IPacienteRepository pacienteRepo,
        IProntuarioRepository prontuarioRepo,
        IProntuarioEvolucaoRepository evolucaoRepo,
        IProntuarioAcessoLogService acessoLog)
    {
        _pacienteRepo = pacienteRepo;
        _prontuarioRepo = prontuarioRepo;
        _evolucaoRepo = evolucaoRepo;
        _acessoLog = acessoLog;
    }

    public async Task Handle(RegistrarExportacaoEvolucaoCommand command)
    {
        var paciente = await _pacienteRepo.ObterPorIdOuNulo(command.PacienteId, command.EstabelecimentoId)
            ?? throw new BusinessException("Paciente não encontrado.");
        if (paciente.EstaDeletado)
            throw new BusinessException("Paciente não encontrado.");

        var prontuario = await _prontuarioRepo.ObterPorPaciente(command.PacienteId, command.EstabelecimentoId)
            ?? throw new BusinessException("Prontuário ainda não foi iniciado para este paciente.");

        // Vínculo evolução↔prontuário: bloqueia IDOR (evolução de outro paciente / outro tenant).
        var evolucao = await _evolucaoRepo.ObterDoProntuarioOuNulo(command.EvolucaoId, prontuario.Id)
            ?? throw new BusinessException("Evolução não encontrada.");

        await _acessoLog.RegistrarAsync(
            prontuario.Id,
            command.SolicitanteUsuarioId,
            command.EstabelecimentoId,
            TipoAcessoProntuario.Exportacao);
    }
}
