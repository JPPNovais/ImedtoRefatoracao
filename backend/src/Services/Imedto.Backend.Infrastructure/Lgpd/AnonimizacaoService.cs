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
        // Aqui o lookup eh sem tenant — intencional: tanto o job de retencao
        // (varre todos pacientes inativos) quanto o handler "AnonimizarMinhaConta"
        // (titular cancelando suas contas em multiplos estabelecimentos) precisam
        // operar cross-tenant. Os callers ja garantem autorizacao apropriada
        // (job = system; handler = titular agindo sobre proprio email).
#pragma warning disable CS0618 // intencional — ver comentario acima
        var paciente = await _pacientes.ObterPorIdOuNulo(pacienteId);
#pragma warning restore CS0618
        if (paciente is null)
        {
            _logger.LogWarning(
                "[AnonimizacaoService] Paciente id={Id} nao encontrado — operacao ignorada.",
                pacienteId);
            return;
        }

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
