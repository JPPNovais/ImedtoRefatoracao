using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Pacientes.Queries;

public class ObterDadosSensiveisPacienteQuery : IQuery<DadosSensiveisPacienteDto>
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    /// <summary>Audit LGPD: identifica quem revelou os dados sensíveis.</summary>
    public Guid SolicitanteUsuarioId { get; set; }
}

/// <summary>
/// Retorna CPF e telefone completos do paciente.
/// Exposto APENAS via <c>GET /api/paciente/{id}/dados-sensiveis</c> — auditado.
/// O DTO do detalhe (<see cref="Results.PacienteDto"/>) mantém esses campos por
/// dependência do web; este endpoint serve fluxos que exigem o valor completo
/// com trilha de auditoria explícita (ex: app mobile).
/// </summary>
public class DadosSensiveisPacienteDto
{
    public string? Cpf { get; set; }
    public string? Telefone { get; set; }
}
