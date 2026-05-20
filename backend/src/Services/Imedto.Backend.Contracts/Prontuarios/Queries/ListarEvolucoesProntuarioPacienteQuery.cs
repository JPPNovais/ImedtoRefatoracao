using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Prontuarios.Queries;

/// <summary>
/// Listagem paginada das evoluções do prontuário do paciente. Usada pela aba
/// "Consultas anteriores" — separada da carga inicial (<see cref="ObterProntuarioDoPacienteQuery"/>)
/// para que a página principal não precise carregar todas as evoluções de uma vez.
/// </summary>
public class ListarEvolucoesProntuarioPacienteQuery : IQuery<PaginaEvolucoesDto>
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 20;
}
