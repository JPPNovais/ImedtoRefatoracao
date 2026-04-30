using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Lgpd;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Infrastructure.Lgpd;

/// <summary>
/// Implementação de <see cref="IAnonimizacaoService"/> para pacientes.
///
/// LGPD: nenhum PII (nome, CPF, e-mail, telefone) é logado nesta classe.
/// O audit trail vai exclusivamente para <c>lgpd_anonimizacoes</c>.
/// </summary>
public class AnonimizacaoService : IAnonimizacaoService
{
    private readonly IPacienteRepository _pacientes;
    private readonly ILgpdAnonimizacaoRepository _anonimizacoes;
    private readonly ILogger<AnonimizacaoService> _logger;

    public AnonimizacaoService(
        IPacienteRepository pacientes,
        ILgpdAnonimizacaoRepository anonimizacoes,
        ILogger<AnonimizacaoService> logger)
    {
        _pacientes = pacientes;
        _anonimizacoes = anonimizacoes;
        _logger = logger;
    }

    public async Task AnonimizarPaciente(
        long pacienteId,
        MotivoAnonimizacao motivo,
        Guid? executadoPor,
        CancellationToken ct = default)
    {
        var paciente = await _pacientes.ObterPorId(pacienteId);

        if (paciente.EstaAnonimizado)
        {
            _logger.LogWarning(
                "[AnonimizacaoService] Paciente id={Id} já estava anonimizado — operação ignorada.",
                pacienteId);
            return;
        }

        paciente.Anonimizar(executadoPor);
        await _pacientes.Salvar(paciente);

        var registro = LgpdAnonimizacao.Registrar("pacientes", pacienteId, motivo, executadoPor);
        await _anonimizacoes.Salvar(registro);

        // Sem PII no log — apenas o id e o motivo.
        _logger.LogInformation(
            "[AnonimizacaoService] Paciente id={Id} anonimizado. Motivo={Motivo}, ExecutadoPor={Executor}.",
            pacienteId,
            motivo,
            executadoPor.HasValue ? "usuário" : "job-automático");
    }
}
