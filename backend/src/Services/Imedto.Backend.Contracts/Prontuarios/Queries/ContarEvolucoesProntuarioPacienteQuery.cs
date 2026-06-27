using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.Contracts.Prontuarios.Queries;

/// <summary>
/// Conta apenas o total de evoluções do prontuário de um paciente visíveis ao solicitante.
/// Gated por autor-ou-dono (R1/R6 briefing 2026-06-27_001): Profissional conta só as próprias;
/// Dono conta todas. Contagem coerente com a lista paginada para não vazar existência de colegas.
/// Retorna 0 se o paciente ainda não tem prontuário iniciado ou o solicitante não tem evoluções.
/// </summary>
public class ContarEvolucoesProntuarioPacienteQuery : IQuery<ContagemEvolucoesDto>
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    /// <summary>Id do usuário autenticado. Necessário para o predicado de autoria (R1).</summary>
    public Guid SolicitanteUsuarioId { get; set; }
    /// <summary>Papel do solicitante. Dono bypassa o predicado de autoria (R4).</summary>
    public TenantPapel SolicitantePapel { get; set; }
}

public record ContagemEvolucoesDto(int Total);
