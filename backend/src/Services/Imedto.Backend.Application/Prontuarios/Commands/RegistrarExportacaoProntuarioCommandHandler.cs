using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Prontuarios.Commands;

/// <summary>
/// Handler do audit de exportação do histórico completo do prontuário.
/// Espelha as validações de leitura: tenant + paciente + prontuário existente.
/// Mensagens genéricas para não vazar cross-tenant.
/// </summary>
public class RegistrarExportacaoProntuarioCommandHandler : ICommandHandler<RegistrarExportacaoProntuarioCommand>
{
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IProntuarioRepository _prontuarioRepo;
    private readonly IProntuarioAcessoLogService _acessoLog;

    public RegistrarExportacaoProntuarioCommandHandler(
        IPacienteRepository pacienteRepo,
        IProntuarioRepository prontuarioRepo,
        IProntuarioAcessoLogService acessoLog)
    {
        _pacienteRepo = pacienteRepo;
        _prontuarioRepo = prontuarioRepo;
        _acessoLog = acessoLog;
    }

    public async Task Handle(RegistrarExportacaoProntuarioCommand command)
    {
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no repo.
        var paciente = await _pacienteRepo.ObterPorIdOuNulo(command.PacienteId, command.EstabelecimentoId)
            ?? throw new BusinessException("Paciente não encontrado.");
        if (paciente.EstaDeletado)
            throw new BusinessException("Paciente não encontrado.");

        var prontuario = await _prontuarioRepo.ObterPorPaciente(command.PacienteId, command.EstabelecimentoId)
            ?? throw new BusinessException("Prontuário ainda não foi iniciado para este paciente.");

        await _acessoLog.RegistrarAsync(
            prontuario.Id,
            command.SolicitanteUsuarioId,
            command.EstabelecimentoId,
            TipoAcessoProntuario.Exportacao);
    }
}
