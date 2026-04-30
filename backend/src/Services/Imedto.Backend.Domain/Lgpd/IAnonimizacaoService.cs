namespace Imedto.Backend.Domain.Lgpd;

public interface IAnonimizacaoService
{
    /// <summary>
    /// Anonimiza o paciente indicado: substitui PII por valores neutros, persiste o aggregate
    /// e registra em <c>lgpd_anonimizacoes</c>.
    /// </summary>
    /// <param name="pacienteId">Id do paciente a anonimizar.</param>
    /// <param name="motivo">Motivo legal da anonimização.</param>
    /// <param name="executadoPor">Id do usuário solicitante; null para job automático.</param>
    Task AnonimizarPaciente(long pacienteId, MotivoAnonimizacao motivo, Guid? executadoPor, CancellationToken ct = default);
}
