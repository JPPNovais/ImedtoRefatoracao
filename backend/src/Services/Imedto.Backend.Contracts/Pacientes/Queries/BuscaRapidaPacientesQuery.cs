using Imedto.Backend.Contracts.Pacientes.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Pacientes.Queries;

/// <summary>
/// Autocomplete de paciente (seletor de agendamento, orçamento, etc.).
/// Retorna apenas <c>id</c> + <c>nomeCompleto</c> — sem CPF/telefone/data
/// (LGPD: minimização — tela do seletor não exibe esses campos).
/// </summary>
public class BuscaRapidaPacientesQuery : IQuery<IReadOnlyList<PacienteBuscaRapidaDto>>
{
    public long EstabelecimentoId { get; set; }
    public string Q { get; set; }
    public int Limite { get; set; } = 10;
}
