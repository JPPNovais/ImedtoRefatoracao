using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.Application.Prontuarios.Commands;

/// <summary>
/// Atualiza os alertas clínicos do paciente, com gating por papel/vínculo (R3 LGPD).
/// Apenas Dono (sempre) ou Profissional que atendeu/está atendendo o paciente pode
/// executar. A operação é auditada como TipoAcessoProntuario.Escrita, best-effort.
/// </summary>
public class AtualizarAlertasProntuarioCommandHandler : ICommandHandler<AtualizarAlertasProntuarioCommand>
{
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IProntuarioRepository _prontuarioRepo;
    private readonly ProntuarioQueryRepository _prontuarioQuery;
    private readonly IProntuarioAcessoLogService _acessoLog;

    public AtualizarAlertasProntuarioCommandHandler(
        IPacienteRepository pacienteRepo,
        IProntuarioRepository prontuarioRepo,
        ProntuarioQueryRepository prontuarioQuery,
        IProntuarioAcessoLogService acessoLog)
    {
        _pacienteRepo = pacienteRepo;
        _prontuarioRepo = prontuarioRepo;
        _prontuarioQuery = prontuarioQuery;
        _acessoLog = acessoLog;
    }

    public virtual async Task Handle(AtualizarAlertasProntuarioCommand command)
    {
        // Defense-in-depth multi-tenant: paciente e prontuário filtrados por estabelecimentoId.
        var paciente = await _pacienteRepo.ObterPorIdOuNulo(command.PacienteId, command.EstabelecimentoId)
            ?? throw new BusinessException("Não encontrado.");

        // Gating R3: Dono sempre pode; Profissional precisa de vínculo de atendimento.
        // Recepcionista e papeis desconhecidos: sem permissão → 403 (CA12).
        if (command.SolicitantePapel != TenantPapel.Dono)
        {
            if (command.SolicitantePapel != TenantPapel.Profissional)
                throw new ForbiddenException("Sem permissão.");

            var temVinculo = await _prontuarioQuery.VerificarVinculoAtendimento(
                command.PacienteId, command.EstabelecimentoId, command.SolicitanteUsuarioId);

            if (!temVinculo)
                throw new ForbiddenException("Sem permissão.");
        }

        // Validação de negócio: array delegado ao aggregate (máx. 10, máx. 200 chars por item).
        paciente.AtualizarSomenteAlertas(command.Alertas);
        await _pacienteRepo.Salvar(paciente);

        // Audit LGPD: gestão de alertas é escrita sensível no prontuário (R8/CA15), best-effort.
        // Falha no audit não deve bloquear a operação clínica.
        try
        {
            var prontuario = await _prontuarioRepo.ObterPorPaciente(command.PacienteId, command.EstabelecimentoId);
            if (prontuario is not null)
            {
                await _acessoLog.RegistrarAsync(
                    prontuario.Id,
                    command.SolicitanteUsuarioId,
                    command.EstabelecimentoId,
                    TipoAcessoProntuario.Escrita);
            }
        }
        catch
        {
            // Best-effort: falha do audit não bloqueia a operação (R8).
        }
    }
}
