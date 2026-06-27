using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.Contracts.Prontuarios.Queries;

/// <summary>
/// Listagem paginada das evoluções do prontuário do paciente. Usada pela aba
/// "Consultas anteriores" — separada da carga inicial (<see cref="ObterProntuarioDoPacienteQuery"/>)
/// para que a página principal não precise carregar todas as evoluções de uma vez.
/// Gated por autor-ou-dono (R1 briefing 2026-06-27_001): Profissional vê só as próprias; Dono vê todas.
/// </summary>
public class ListarEvolucoesProntuarioPacienteQuery : IQuery<PaginaEvolucoesDto>
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    /// <summary>Papel do solicitante. Dono bypassa o predicado de autoria (R4).</summary>
    public TenantPapel SolicitantePapel { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 10;
}
