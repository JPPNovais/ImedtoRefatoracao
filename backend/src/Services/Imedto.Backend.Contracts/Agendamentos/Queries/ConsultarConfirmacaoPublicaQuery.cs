using Imedto.Backend.Contracts.Agendamentos.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Agendamentos.Queries;

/// <summary>
/// Consulta anônima de agendamento via token público (Fase 2).
/// Não filtra por tenant — o token (256 bits) é o único segredo.
///
/// LGPD: retorna apenas nome fantasia do estabelecimento, profissional, tipo de serviço
/// e data/hora. Sem paciente_id, estabelecimento_id, nome, CPF ou e-mail do paciente.
/// </summary>
public class ConsultarConfirmacaoPublicaQuery : IQuery<ConfirmacaoPublicaDto>
{
    public string Token { get; set; } = string.Empty;
    public string? IpOrigem { get; set; }
    public string? UserAgent { get; set; }
}
