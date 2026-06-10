using Imedto.Backend.Contracts.Pacientes.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Pacientes.Queries;

/// <summary>
/// Query do relatório de acessos LGPD (Art. 9º/18) — consolida
/// paciente_acesso_log + prontuario_acesso_log em lista paginada leiga.
/// Gate: apenas papel Dono (aplicado no controller via RequiresPapel).
/// </summary>
public class ListarAcessosDoPacienteQuery : IQuery<PaginaAcessosDto>
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 20;
}
